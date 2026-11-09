using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace ImageNdi {
public static class NdiChannel {
 public const string DefaultName="Watchout Image NDI";
 public static string Validate(string value){
  string name=(value??"").Trim();
  if(name.Length==0)throw new ArgumentException("Inserisci il nome del canale NDI.");
  if(name.Length>128||Encoding.UTF8.GetByteCount(Environment.MachineName+" ("+name+")")>253)throw new ArgumentException("Il nome del canale NDI è troppo lungo: accorcialo.");
  foreach(char c in name)if(Char.IsControl(c)||"\\/:*?\"<>|".IndexOf(c)>=0)throw new ArgumentException("Il nome NDI non può contenere caratteri di controllo oppure \\ / : * ? \" < > |.");
  return name;
 }
 public static string PreviewName(string value){string original=Validate(value),name=original,suffix=" - Preview";while(true){try{string candidate=Validate(name+suffix);if(String.Equals(candidate,original,StringComparison.OrdinalIgnoreCase)){suffix=" - Preview 2";continue;}return candidate;}catch(ArgumentException){if(name.Length==0)throw;name=name.Substring(0,name.Length-1);if(name.Length>0&&Char.IsHighSurrogate(name[name.Length-1]))name=name.Substring(0,name.Length-1);}}}
 public static void ValidatePair(string program,string preview){if(String.Equals(Validate(program),Validate(preview),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Program e Preview devono avere nomi NDI diversi.");}
}
public interface IVideoSender:IDisposable {void Send(byte[] pixels,int width,int height,int fps);}
public sealed class NdiSender:IVideoSender {
 const string Dll="Processing.NDI.Lib.x64.dll";
 [StructLayout(LayoutKind.Sequential)] struct Create {public IntPtr name,groups; [MarshalAs(UnmanagedType.I1)] public bool video; [MarshalAs(UnmanagedType.I1)] public bool audio;}
 [StructLayout(LayoutKind.Sequential)] public struct Frame {public int width,height,fourcc,n,d;public float aspect;public int format;public long timecode;public IntPtr data;public int stride;public IntPtr metadata;public long timestamp;}
 [DllImport("kernel32",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibrary(string path);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool NDIlib_initialize();
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr NDIlib_send_create(ref Create c);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern void NDIlib_send_send_video_v2(IntPtr sender,ref Frame f);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern void NDIlib_send_destroy(IntPtr sender);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern void NDIlib_destroy();
 IntPtr handle; bool initialized;
 static readonly object RuntimeGate=new object();static int runtimeUsers;static IntPtr runtimeModule;
 public NdiSender(string name) {
  name=NdiChannel.Validate(name);
  string dir=Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V6") ?? Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V6",EnvironmentVariableTarget.Machine) ?? Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V5") ?? Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V5",EnvironmentVariableTarget.Machine);
  string pf=Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
  string[] paths={Path.Combine(AppDomain.CurrentDomain.BaseDirectory,Dll), dir==null?"":Path.Combine(dir,Dll),Path.Combine(pf,"NDI","NDI 6 Runtime","v6",Dll),Path.Combine(pf,"NDI","NDI 6 Runtime",Dll),Path.Combine(pf,"NDI","NDI 6 Tools","Runtime",Dll)};
  lock(RuntimeGate){if(runtimeUsers==0){if(runtimeModule==IntPtr.Zero)foreach(string p in paths)if(File.Exists(p)){runtimeModule=LoadLibrary(p);if(runtimeModule!=IntPtr.Zero)break;}
   if(runtimeModule==IntPtr.Zero)throw new InvalidOperationException("Runtime NDI x64 assente. Installa NDI Runtime/Tools o imposta NDI_RUNTIME_DIR_V6.");
   if(!NDIlib_initialize())throw new InvalidOperationException("Inizializzazione NDI fallita.");}runtimeUsers++;initialized=true;}
  byte[] utf8=Encoding.UTF8.GetBytes(name+"\0");IntPtr text=Marshal.AllocHGlobal(utf8.Length);
  try {Marshal.Copy(utf8,0,text,utf8.Length);var c=new Create{name=text,video=true,audio=false};handle=NDIlib_send_create(ref c);if(handle==IntPtr.Zero) throw new InvalidOperationException("Creazione sender NDI fallita.");} catch {Dispose();throw;} finally {Marshal.FreeHGlobal(text);}
 }
 public void Send(byte[] pixels,int w,int h,int fps){var pin=GCHandle.Alloc(pixels,GCHandleType.Pinned);try {var f=new Frame{width=w,height=h,fourcc=0x41524742,n=fps,d=1,aspect=(float)w/h,format=1,timecode=long.MaxValue,data=pin.AddrOfPinnedObject(),stride=w*4};NDIlib_send_send_video_v2(handle,ref f);}finally{pin.Free();}}
 public void Dispose(){if(handle!=IntPtr.Zero){NDIlib_send_destroy(handle);handle=IntPtr.Zero;}lock(RuntimeGate){if(initialized){initialized=false;runtimeUsers--;if(runtimeUsers==0)NDIlib_destroy();}}}
}
public sealed class OnAirStatus {public string[] Paths=new string[0];public bool Transitioning;public long Sequence;}
public sealed class Playout:IDisposable {
 readonly object gate=new object(); readonly int w,h,fps; readonly IVideoSender sender; readonly Thread thread;
 readonly object monitorGate=new object(); byte[] lastSent; long sequence;
 Dictionary<string,double> contributors=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase),fromContributors=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
 IFrameSource frameSource,fadingSource;public string MediaError {get{lock(gate)return frameSource==null?null:frameSource.Error;}}
 string targetPath;OnAirStatus sentStatus=new OnAirStatus();
 byte[] current,from,target; double started,duration; volatile bool stop; public volatile string Error; public long Frames;
 bool transparentStop;int transparentStopFrames;double firstTransparentStop=-1;
 readonly Stopwatch clock=Stopwatch.StartNew();
 bool lastPresence;
 public event EventHandler<ProgramPresenceEventArgs> PresenceChanged;
 public volatile string NotificationError;
 public Playout(IVideoSender output,int width,int height,int rate){sender=output;w=width;h=height;fps=rate;current=new byte[w*h*4];from=current;target=current;thread=new Thread(Run){IsBackground=true};thread.Start();}
 public void CueSource(IFrameSource source,byte[] initial,double seconds,string path){try{Cue(initial,seconds,path);lock(gate){if(transparentStop){source.Dispose();return;}frameSource=source;}}catch{source.Dispose();throw;}}
 public void Cue(byte[] next,double seconds,string imagePath=null){if(next.Length!=w*h*4)throw new ArgumentException("Frame size");lock(gate){if(transparentStop)return;Evaluate();if(fadingSource!=null){fadingSource.Dispose();fadingSource=null;}if(frameSource!=null){if(seconds>0&&duration<=0)fadingSource=frameSource;else frameSource.Dispose();frameSource=null;}from=(byte[])current.Clone();fromContributors=new Dictionary<string,double>(contributors,StringComparer.OrdinalIgnoreCase);targetPath=imagePath;target=(byte[])next.Clone();started=clock.Elapsed.TotalSeconds;duration=seconds;current=seconds<=0?target:new byte[target.Length];if(seconds<=0)SetContributors(1);}}
 void SetContributors(double t){var next=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);foreach(var entry in fromContributors)if(entry.Value*(1-t)>.000001)next[entry.Key]=entry.Value*(1-t);if(targetPath!=null&&t>0){double old;next.TryGetValue(targetPath,out old);next[targetPath]=old+t;}contributors=next;}
 void Evaluate(){if(fadingSource!=null){var outgoing=fadingSource.ReadFrame();if(outgoing!=null&&outgoing.Length==w*h*4)from=outgoing;}if(frameSource!=null){var live=frameSource.ReadFrame();if(live!=null&&live.Length==w*h*4){target=live;if(duration<=0)current=target;}}if(duration>0){double t=(clock.Elapsed.TotalSeconds-started)/duration;if(t>=1){current=target;duration=0;SetContributors(1);if(fadingSource!=null){fadingSource.Dispose();fadingSource=null;}}else{if(object.ReferenceEquals(current,from)||object.ReferenceEquals(current,target))current=new byte[target.Length];Renderer.Blend(from,target,t,current);SetContributors(t);}}}
 // Only publish a frame after the synchronous NDI call returns successfully.
 public byte[] ReadSentFrame(ref long lastSequence){lock(monitorGate){if(lastSent==null||lastSequence==sequence)return null;lastSequence=sequence;return (byte[])lastSent.Clone();}}
 public OnAirStatus ReadOnAirStatus(){lock(monitorGate)return new OnAirStatus{Paths=(string[])sentStatus.Paths.Clone(),Transitioning=sentStatus.Transitioning,Sequence=sentStatus.Sequence};}
 public bool Clearing {get{lock(gate)return duration>0&&targetPath==null;}}
 void Run(){try {while(!stop){byte[] frame;string[] paths;bool transitioning,stopFrame;lock(gate){Evaluate();frame=duration>0?(byte[])current.Clone():current;paths=new string[contributors.Count];contributors.Keys.CopyTo(paths,0);transitioning=duration>0;stopFrame=transparentStop;}sender.Send(frame,w,h,fps);lock(monitorGate){lastSent=frame;sequence++;sentStatus=new OnAirStatus{Paths=paths,Transitioning=transitioning,Sequence=sequence};}Interlocked.Increment(ref Frames);bool present=paths.Length>0;if(present!=lastPresence){lastPresence=present;var handler=PresenceChanged;if(handler!=null)try{handler(this,new ProgramPresenceEventArgs(present));}catch(Exception e){NotificationError=e.Message;}}if(stopFrame){if(firstTransparentStop<0)firstTransparentStop=clock.Elapsed.TotalSeconds;transparentStopFrames++;if(transparentStopFrames>=3&&clock.Elapsed.TotalSeconds-firstTransparentStop>=.2)stop=true;}}}catch(Exception e){Error=e.Message;}finally{sender.Dispose();}}
 public bool StopTransparent(){lock(gate){if(!transparentStop){transparentStop=true;if(fadingSource!=null){fadingSource.Dispose();fadingSource=null;}if(frameSource!=null){frameSource.Dispose();frameSource=null;}current=new byte[w*h*4];from=current;target=current;duration=0;targetPath=null;contributors.Clear();fromContributors.Clear();}}thread.Join();return Error==null&&transparentStopFrames>=3;}
 public void Dispose(){stop=true;thread.Join();lock(gate){if(fadingSource!=null){fadingSource.Dispose();fadingSource=null;}if(frameSource!=null){frameSource.Dispose();frameSource=null;}}}
}
}
