using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace ImageNdi {
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
 public NdiSender(string name) {
  string dir=Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V6") ?? Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V6",EnvironmentVariableTarget.Machine) ?? Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V5") ?? Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V5",EnvironmentVariableTarget.Machine);
  string pf=Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
  string[] paths={Path.Combine(AppDomain.CurrentDomain.BaseDirectory,Dll), dir==null?"":Path.Combine(dir,Dll),Path.Combine(pf,"NDI","NDI 6 Runtime","v6",Dll),Path.Combine(pf,"NDI","NDI 6 Runtime",Dll),Path.Combine(pf,"NDI","NDI 6 Tools","Runtime",Dll)};
  bool loaded=false; foreach(string p in paths) if(File.Exists(p) && LoadLibrary(p)!=IntPtr.Zero){loaded=true;break;}
  if(!loaded) throw new InvalidOperationException("Runtime NDI x64 assente. Installa NDI Runtime/Tools o imposta NDI_RUNTIME_DIR_V6.");
  if(!NDIlib_initialize()) throw new InvalidOperationException("Inizializzazione NDI fallita."); initialized=true;
  IntPtr text=Marshal.StringToCoTaskMemAnsi(name);
  try {var c=new Create{name=text,video=true,audio=false};handle=NDIlib_send_create(ref c);if(handle==IntPtr.Zero) throw new InvalidOperationException("Creazione sender NDI fallita.");} catch {Dispose();throw;} finally {Marshal.FreeCoTaskMem(text);}
 }
 public void Send(byte[] pixels,int w,int h,int fps){var pin=GCHandle.Alloc(pixels,GCHandleType.Pinned);try {var f=new Frame{width=w,height=h,fourcc=0x41524742,n=fps,d=1,aspect=(float)w/h,format=1,timecode=long.MaxValue,data=pin.AddrOfPinnedObject(),stride=w*4};NDIlib_send_send_video_v2(handle,ref f);}finally{pin.Free();}}
 public void Dispose(){if(handle!=IntPtr.Zero){NDIlib_send_destroy(handle);handle=IntPtr.Zero;}if(initialized){NDIlib_destroy();initialized=false;}}
}
public sealed class Playout:IDisposable {
 readonly object gate=new object(); readonly int w,h,fps; readonly IVideoSender sender; readonly Thread thread;
 readonly object monitorGate=new object(); byte[] lastSent; long sequence;
 byte[] current,from,target; double started,duration; volatile bool stop; public volatile string Error; public long Frames;
 readonly Stopwatch clock=Stopwatch.StartNew();
 public Playout(IVideoSender output,int width,int height,int rate){sender=output;w=width;h=height;fps=rate;current=new byte[w*h*4];from=current;target=current;thread=new Thread(Run){IsBackground=true};thread.Start();}
 public void Cue(byte[] next,double seconds){if(next.Length!=w*h*4)throw new ArgumentException("Frame size");lock(gate){Evaluate();from=(byte[])current.Clone();target=(byte[])next.Clone();started=clock.Elapsed.TotalSeconds;duration=seconds;current=seconds<=0?target:new byte[target.Length];}}
 void Evaluate(){if(duration>0){double t=(clock.Elapsed.TotalSeconds-started)/duration;if(t>=1){current=target;duration=0;}else{if(object.ReferenceEquals(current,from)||object.ReferenceEquals(current,target))current=new byte[target.Length];Renderer.Blend(from,target,t,current);}}}
 // Only publish a frame after the synchronous NDI call returns successfully.
 public byte[] ReadSentFrame(ref long lastSequence){lock(monitorGate){if(lastSent==null||lastSequence==sequence)return null;lastSequence=sequence;return (byte[])lastSent.Clone();}}
 void Run(){try {while(!stop){byte[] frame;lock(gate){Evaluate();frame=duration>0?(byte[])current.Clone():current;}sender.Send(frame,w,h,fps);lock(monitorGate){lastSent=frame;sequence++;}Interlocked.Increment(ref Frames);}}catch(Exception e){Error=e.Message;}finally{sender.Dispose();}}
 public void Dispose(){stop=true;thread.Join();}
}
}
