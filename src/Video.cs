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
public static class VideoTools {
 public static bool IsVideo(string path){return String.Equals(Path.GetExtension(path),".mov",StringComparison.OrdinalIgnoreCase);}
 public static string Executable {get{string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"video","ffmpeg.exe");if(!File.Exists(path))throw new FileNotFoundException("Componente video assente: reinstalla NDIPlayer con il nuovo installer.",path);return path;}}
 public static string Quote(string value){return "\""+value.Replace("\"","\\\"")+"\"";}
 public static Process Start(string arguments){var process=new Process{StartInfo=new ProcessStartInfo(Executable,"-hide_banner -loglevel error -nostdin -threads 2 "+arguments){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}};process.Start();return process;}
 public static Bitmap Poster(string path){
  using(var process=Start("-i "+Quote(path)+" -map 0:v:0 -an -sn -dn -vf \"scale=w='max(1,round(iw*sar))':h=ih,setsar=1,scale=640:640:force_original_aspect_ratio=decrease\" -frames:v 1 -c:v png -threads 1 -f image2pipe pipe:1")){
   var errors=process.StandardError.ReadToEndAsync();var bytes=new MemoryStream();var copy=process.StandardOutput.BaseStream.CopyToAsync(bytes);
   if(!copy.Wait(10000)){try{process.Kill();}catch(InvalidOperationException){}throw new IOException("Tempo scaduto durante la lettura del MOV.");}process.WaitForExit();
   if(process.ExitCode!=0||bytes.Length==0)throw new IOException("MOV non leggibile: "+errors.Result);bytes.Position=0;using(bytes)using(var image=Image.FromStream(bytes))return new Bitmap(image);
  }
 }
 public static string Filter(int w,int h,int fps,Placement placement){string scale=placement.Scale.ToString("0.########",CultureInfo.InvariantCulture);return "[0:v:0]scale=w='max(1,round(iw*sar))':h=ih,setsar=1,fps="+fps+",scale=w='max(1,round(iw*min("+w+"/iw,"+h+"/ih)*"+scale+"))':h='max(1,round(ih*min("+w+"/iw,"+h+"/ih)*"+scale+"))',setsar=1,format=bgra[clip];color=c=black@0:s="+w+"x"+h+":r="+fps+",format=bgra[bg];[bg][clip]overlay=x='(W-w)/2+"+placement.X+"':y='(H-h)/2+"+placement.Y+"':shortest=1:format=auto,format=bgra[out]";}
}
// Latest-frame delivery never blocks the NDI sender or accumulates a full clip in memory.
public sealed class VideoSession:IFrameSource,IPlaybackSource,IAudioVideoSource {
 readonly Process process;readonly Thread reader;readonly MediaTimeline mediaTimeline;readonly AudioVoice voice;int audioActivated;readonly int frameSize,rate;readonly string path;readonly double duration;readonly bool loop;long frameNumber;readonly object gate=new object();byte[] latest;volatile bool stopping;volatile string error;public volatile bool Ended;public long DecodedFrames;
 public string Error {get{return error;}}
 public VideoSession(string path,int w,int h,int fps,Placement placement,bool loop,byte[] initial,AudioBus audioBus=null,double gainDb=0){
  if(w<1||h<1||fps<1||initial==null||initial.Length!=w*h*4)throw new ArgumentException("Formato video non valido.");this.path=path;this.rate=fps;this.loop=loop;try{duration=VideoMetadata.Duration(path);}catch{duration=0;}frameSize=w*h*4;latest=(byte[])initial.Clone();
  if(audioBus!=null){mediaTimeline=new MediaTimeline();voice=audioBus.Prepare(path,loop,gainDb);}
  try{process=VideoTools.Start((audioBus==null?"-readrate 1 ":"")+(loop?"-stream_loop -1 ":"")+"-i "+VideoTools.Quote(path)+" -an -sn -dn -filter_complex_threads 1 -filter_complex "+VideoTools.Quote(VideoTools.Filter(w,h,fps,placement))+" -map [out] -pix_fmt bgra -c:v rawvideo -threads 1 -f rawvideo pipe:1");}catch{if(voice!=null)voice.Release();throw;}
  reader=new Thread(Read){IsBackground=true,Name="NDIPlayer MOV decoder"};reader.Start();
 }
 void Read(){var errors=process.StandardError.ReadToEndAsync();try{
   var input=process.StandardOutput.BaseStream;while(!stopping){var frame=new byte[frameSize];int offset=0;while(offset<frame.Length&&!stopping){int n=input.Read(frame,offset,frame.Length-offset);if(n==0)break;offset+=n;}if(offset!=frame.Length)break;if(mediaTimeline!=null){double due=frameNumber/(double)rate;while(!stopping&&mediaTimeline.Seconds<due)Thread.Sleep(1);}if(stopping)break;lock(gate){latest=frame;frameNumber++;}Interlocked.Increment(ref DecodedFrames);}
   if(!stopping){process.WaitForExit();if(process.ExitCode!=0)error="Errore MOV: "+errors.Result;Ended=true;}
  }catch(Exception e){if(!stopping)error="Errore MOV: "+e.Message;}finally{Ended=true;}}
 public double ActivateAudio(double fade){if(voice==null||Interlocked.Exchange(ref audioActivated,1)!=0)return 0;double delay=voice.Activate(fade);mediaTimeline.Start(()=>voice.Position);return delay;}
 public double FadeAudio(double seconds){return voice==null?0:voice.FadeOut(seconds);}
 public byte[] ReadPlaybackFrame(out VideoPlaybackStatus status){lock(gate){status=VideoPlaybackStatus.FromFrames(path,duration,frameNumber,rate,loop,Ended&&error==null);return latest;}}
 public byte[] ReadFrame(){lock(gate)return latest;}
 public void Dispose(){if(stopping)return;stopping=true;if(voice!=null)voice.Release();try{if(!process.HasExited)process.Kill();}catch(InvalidOperationException){}reader.Join();process.Dispose();}
}
public sealed class BorrowedFrames:IFrameSource,IPlaybackSource {readonly IFrameSource source;public BorrowedFrames(IFrameSource value){source=value;}public byte[] ReadFrame(){return source.ReadFrame();}public byte[] ReadPlaybackFrame(out VideoPlaybackStatus status){var clock=source as IPlaybackSource;if(clock!=null)return clock.ReadPlaybackFrame(out status);status=null;return source.ReadFrame();}public string Error{get{return source.Error;}}public void Dispose(){}}
}
