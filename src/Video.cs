using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace ImageNdi {
public interface IAudioVideoSource {double ActivateAudio(double fade);double FadeAudio(double seconds);}
public interface IFrameSource:IDisposable {byte[] ReadFrame();string Error {get;}}
public sealed class VideoCue {public double InSeconds {get;set;} public double OutSeconds {get;set;} public bool AutoStart {get;set;} public VideoCue(){AutoStart=true;}public void Validate(){if(Double.IsNaN(InSeconds)||Double.IsInfinity(InSeconds)||InSeconds<0||Double.IsNaN(OutSeconds)||Double.IsInfinity(OutSeconds)||OutSeconds<0||OutSeconds>0&&OutSeconds<=InSeconds)throw new ArgumentException("Punti di entrata e uscita video non validi.");}}
public static class VideoTools {
 public static bool IsVideo(string path){return String.Equals(Path.GetExtension(path),".mov",StringComparison.OrdinalIgnoreCase);}
 public static string Executable {get{string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"video","ffmpeg.exe");if(!File.Exists(path))throw new FileNotFoundException("Componente video assente: reinstalla NDIPlayer con il nuovo installer.",path);return path;}}
 public static string Quote(string value){return "\""+value.Replace("\"","\\\"")+"\"";}
 public static Process Start(string arguments){var process=new Process{StartInfo=new ProcessStartInfo(Executable,"-hide_banner -loglevel error -nostdin -threads 2 "+arguments){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}};process.Start();return process;}
 public static Bitmap Poster(string path){return Poster(path,false);}
 internal static Bitmap Poster(string path,bool lowPriority){
  using(var process=Start("-i "+Quote(path)+" -map 0:v:0 -an -sn -dn -vf \"scale=w='max(1,round(iw*sar))':h=ih,setsar=1,scale=640:640:force_original_aspect_ratio=decrease\" -frames:v 1 -c:v png -threads 1 -f image2pipe pipe:1")){
   if(lowPriority)try{process.PriorityClass=ProcessPriorityClass.BelowNormal;}catch(System.ComponentModel.Win32Exception){}catch(InvalidOperationException){}
   var errors=process.StandardError.ReadToEndAsync();var bytes=new MemoryStream();var copy=process.StandardOutput.BaseStream.CopyToAsync(bytes);
   if(!copy.Wait(10000)){try{process.Kill();}catch(InvalidOperationException){}throw new IOException("Tempo scaduto durante la lettura del MOV.");}process.WaitForExit();
   if(process.ExitCode!=0||bytes.Length==0)throw new IOException("MOV non leggibile: "+errors.Result);bytes.Position=0;using(bytes)using(var image=Image.FromStream(bytes))return new Bitmap(image);
  }
 }
 public static string Filter(int w,int h,int fps,Placement placement,double speed=1){string scale=placement.Scale.ToString("0.########",CultureInfo.InvariantCulture),tempo=speed.ToString("0.########",CultureInfo.InvariantCulture);return "[0:v:0]scale=w='max(1,round(iw*sar))':h=ih,setsar=1,setpts=PTS/"+tempo+",fps="+fps+",scale=w='max(1,round(iw*min("+w+"/iw,"+h+"/ih)*"+scale+"))':h='max(1,round(ih*min("+w+"/iw,"+h+"/ih)*"+scale+"))',setsar=1,format=bgra[clip];color=c=black@0:s="+w+"x"+h+":r="+fps+",format=bgra[bg];[bg][clip]overlay=x='(W-w)/2+"+placement.X+"':y='(H-h)/2+"+placement.Y+"':shortest=1:format=auto,format=bgra[out]";}
}
// Latest-frame delivery never blocks the NDI sender or accumulates a full clip in memory.
public sealed class VideoSession:IFrameSource,IPlaybackSource,IAudioVideoSource {
 Process process;readonly Thread reader;readonly MediaTimeline mediaTimeline;readonly AudioVoice voice;readonly AudioBus audioBus;int audioActivated;readonly int frameSize,rate,width,height;readonly string path;readonly double duration,speed,startSeconds,outSeconds;readonly bool loop;readonly Placement placement;readonly byte[] firstFrame;long frameNumber;readonly object gate=new object();byte[] latest;volatile bool stopping,paused;volatile string error;public volatile bool Ended;public long DecodedFrames;
 public string Error {get{return error;}}
 public string Path {get{return path;}} public string AssetKey {get;private set;}public double Speed {get{return speed;}}public bool Paused {get{return paused;}}public double StartSeconds {get{return startSeconds;}}public double OutSeconds {get{return outSeconds;}}
 readonly bool pausedOnAir;
 bool preparing;
 public VideoSession(string path,int w,int h,int fps,Placement placement,bool loop,byte[] initial,AudioBus audioBus=null,double gainDb=0,double speed=1,bool startPaused=false,double startSeconds=0,double outSeconds=0,string assetKey=null,bool prepareForCue=false){
  AssetKey=assetKey??path;
  if(w<1||h<1||fps<1||initial==null||initial.Length!=w*h*4||Double.IsNaN(speed)||Double.IsInfinity(speed)||speed<.5||speed>5||startSeconds<0||outSeconds>0&&outSeconds<=startSeconds)throw new ArgumentException("Formato o intervallo video non validi.");this.path=path;this.rate=fps;this.width=w;this.height=h;this.placement=new Placement{Scale=placement.Scale,X=placement.X,Y=placement.Y,Loop=placement.Loop};this.loop=loop;this.speed=speed;this.audioBus=audioBus;this.startSeconds=startSeconds;this.outSeconds=outSeconds;paused=startPaused;try{double full=VideoMetadata.Duration(path);duration=outSeconds>0?outSeconds-startSeconds:Math.Max(0,full-startSeconds);}catch{duration=outSeconds>0?outSeconds-startSeconds:0;}frameSize=w*h*4;firstFrame=(byte[])initial.Clone();latest=firstFrame;
  preparing=prepareForCue;pausedOnAir=startPaused;paused=startPaused||prepareForCue;
  mediaTimeline=new MediaTimeline();if(audioBus!=null){voice=audioBus.Prepare(path,loop,gainDb,speed,startSeconds,outSeconds,AssetKey);voice.SetPaused(paused);}else{mediaTimeline.Start(null);if(paused)mediaTimeline.Pause();}
  try{process=StartDecoder();}catch{if(voice!=null)voice.Release();throw;}
  reader=new Thread(Read){IsBackground=true,Name="NDIPlayer MOV decoder"};reader.Start();
 }
 Process StartDecoder(){string start=startSeconds>0?"-ss "+startSeconds.ToString("0.###",CultureInfo.InvariantCulture)+" ":"";string limit=outSeconds>0?"-t "+(outSeconds-startSeconds).ToString("0.###",CultureInfo.InvariantCulture)+" ":"";return VideoTools.Start((loop&&startSeconds==0&&outSeconds<=0?"-stream_loop -1 ":"")+start+"-i "+VideoTools.Quote(path)+" "+limit+"-an -sn -dn -filter_complex_threads 1 -filter_complex "+VideoTools.Quote(VideoTools.Filter(width,height,rate,placement,speed))+" -map [out] -pix_fmt bgra -c:v rawvideo -threads 1 -f rawvideo pipe:1");}
 public VideoSession Restart(double newSpeed,bool startPaused,double gainDb){return new VideoSession(path,width,height,rate,placement,loop,firstFrame,audioBus,gainDb,newSpeed,startPaused,startSeconds,outSeconds,AssetKey);}
 public VideoSession RestartAt(double seconds,double newSpeed,bool startPaused,double gainDb){return new VideoSession(path,width,height,rate,placement,loop,firstFrame,audioBus,gainDb,newSpeed,startPaused,seconds,outSeconds,AssetKey);}
 public VideoSession Seek(double seconds,bool startPaused,double gainDb){return new VideoSession(path,width,height,rate,placement,loop,firstFrame,audioBus,gainDb,speed,startPaused,seconds,outSeconds,AssetKey);}
 public void SetPaused(bool value){lock(gate){if(paused==value)return;paused=value;if(value)mediaTimeline.Pause();else mediaTimeline.Resume();if(voice!=null)voice.SetPaused(value);}}
 void Read(){try{while(!stopping){var current=process;var errors=current.StandardError.ReadToEndAsync();var input=current.StandardOutput.BaseStream;while(!stopping){var frame=new byte[frameSize];int offset=0;while(offset<frame.Length&&!stopping){int n=input.Read(frame,offset,frame.Length-offset);if(n==0)break;offset+=n;}if(offset!=frame.Length)break;double due=frameNumber/(double)rate;while(!stopping){lock(gate){if((!paused||frameNumber==0)&&mediaTimeline.Seconds>=due){latest=frame;frameNumber++;Interlocked.Increment(ref DecodedFrames);break;}}Thread.Sleep(1);}if(stopping)break;}if(stopping)break;current.WaitForExit();if(current.ExitCode!=0){error="Errore MOV: "+errors.Result;break;}if(stopping||!loop||startSeconds==0&&outSeconds<=0)break;lock(gate){if(stopping)break;process=StartDecoder();current.Dispose();}}}catch(Exception e){if(!stopping)error="Errore MOV: "+e.Message;}finally{Ended=true;}}
 public void SetAudioGain(double gain){if(voice!=null)voice.SetGain(gain);}
 public double ActivateAudio(double fade){
  if(Interlocked.Exchange(ref audioActivated,1)!=0)return 0;
  lock(gate){
   if(preparing){preparing=false;paused=pausedOnAir;if(voice!=null)voice.SetPaused(paused);}
   if(voice==null){if(!paused)mediaTimeline.Resume();return 0;}
   double delay=voice.Activate(fade);
   mediaTimeline.Start(()=>voice.Position);if(paused)mediaTimeline.Pause();
   return delay;
  }
 }
 public double FadeAudio(double seconds){return voice==null?0:voice.FadeOut(seconds);}
 public byte[] ReadPlaybackFrame(out VideoPlaybackStatus status){lock(gate){status=VideoPlaybackStatus.FromFrames(path,duration,frameNumber,rate,loop,Ended&&error==null,speed);return latest;}}
 public byte[] ReadFrame(){lock(gate)return latest;}
 public void Dispose(){Process current;lock(gate){if(stopping)return;stopping=true;current=process;}if(voice!=null)voice.Release();try{if(!current.HasExited)current.Kill();}catch(InvalidOperationException){}reader.Join();try{current.WaitForExit(2000);}catch(InvalidOperationException){}current.Dispose();}
}
public sealed class BorrowedFrames:IFrameSource,IPlaybackSource {readonly IFrameSource source;public BorrowedFrames(IFrameSource value){source=value;}public byte[] ReadFrame(){return source.ReadFrame();}public byte[] ReadPlaybackFrame(out VideoPlaybackStatus status){var clock=source as IPlaybackSource;if(clock!=null)return clock.ReadPlaybackFrame(out status);status=null;return source.ReadFrame();}public string Error{get{return source.Error;}}public void Dispose(){}}
}
