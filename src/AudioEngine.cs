using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NAudio.Wave;
namespace ImageNdi {
public interface IAudioSink:IDisposable {int Channels {get;}string Name {get;}string Error {get;}double PositionSeconds {get;}void Start(IWaveProvider provider);void Stop();}
public sealed class AudioVoice {
 public readonly string Path;public readonly AudioBus Bus;public readonly AudioDecoder Decoder;public long StartFrame;public volatile float TargetGain;float currentGain,gainEnd;int ramp;volatile AudioEnvelope envelope;public bool Attached;int retired;float[] scratch=new float[0];
 public AudioVoice(AudioBus bus,string path,bool loop,double gain){Bus=bus;Path=path;TargetGain=currentGain=gainEnd=(float)Math.Pow(10,gain/20);Decoder=new AudioDecoder(path,loop);}
 public double Activate(double fade){Bus.Engine.Attach(this,fade);return Math.Max(0,StartFrame/(double)AudioDecoder.Rate-Bus.Engine.PresentationSeconds);}
 internal void Begin(long start,double fade){StartFrame=start;Attached=true;envelope=new AudioEnvelope(start,start+(long)(Math.Max(0,fade)*AudioDecoder.Rate),fade>0?0:1,1);}
 public double FadeOut(double seconds){long at=Bus.Engine.NextFrame;var old=envelope;float value=old==null?0:old.At(at);envelope=new AudioEnvelope(at,at+(long)(Math.Max(.005,seconds)*AudioDecoder.Rate),value,0);return Math.Max(0,at/(double)AudioDecoder.Rate-Bus.Engine.PresentationSeconds);}
 public double Position {get{if(Bus.Engine.Error!=null)throw new InvalidOperationException(Bus.Engine.Error);return Math.Max(0,Bus.Engine.PresentationSeconds-StartFrame/(double)AudioDecoder.Rate-Bus.Route.VideoOffsetMs/1000.0);}}
 public void SetGain(double db){if(Double.IsNaN(db)||Double.IsInfinity(db)||db < -60||db>12)throw new ArgumentException("Gain audio non valido.");TargetGain=(float)Math.Pow(10,db/20);}
 internal bool Mix(float[] output,int frames,int channels,long firstFrame){if(scratch.Length<frames*2)scratch=new float[frames*2];Decoder.ReadStereo(scratch,frames*2);var fade=envelope;bool silent=Bus.Muted||Decoder.Error!=null||Bus.Engine.Error!=null;for(int i=0;i<frames;i++){if(TargetGain!=gainEnd){gainEnd=TargetGain;ramp=960;}if(ramp>0){currentGain+=(gainEnd-currentGain)/ramp;ramp--;}long frame=firstFrame+i;float level=(fade==null||frame<StartFrame||silent)?0:fade.At(frame)*currentGain;int at=i*channels+Bus.Route.FirstChannel-1;output[at]+=scratch[i*2]*level;output[at+1]+=scratch[i*2+1]*level;}return fade!=null&&fade.To==0&&firstFrame+frames>=fade.End;}
 public void Release(){if(!Attached){Retire();return;}try{FadeOut(0);}catch{Retire();}}
 internal void Retire(){if(Interlocked.Exchange(ref retired,1)==0)ThreadPool.QueueUserWorkItem(delegate{Decoder.Dispose();});}
 internal void Close(){if(Interlocked.Exchange(ref retired,1)==0)Decoder.Dispose();}
}
public sealed class AudioBus {
 public readonly AudioRoute Route;public readonly AudioEngine Engine;public readonly AudioMeter Meter=new AudioMeter();public volatile bool Muted;
 internal AudioBus(AudioEngine engine,AudioRoute route){Engine=engine;Route=route.Copy();Muted=route.Muted;}
 public AudioVoice Prepare(string path,bool loop,double gain){return new AudioVoice(this,path,loop,gain);}
 public string Error {get{return Engine.BusError(this);}}
}
public sealed class AudioEngine:IWaveProvider,IDisposable {
 readonly object gate=new object();readonly IAudioSink sink;readonly List<AudioVoice> voices=new List<AudioVoice>();readonly List<AudioBus> buses=new List<AudioBus>();float[] buffer=new float[0];long nextFrame;bool stopping;public volatile string Fault;
 public string Error {get{return Fault??sink.Error;}}
 public string Name {get{return sink.Name;}}
 public WaveFormat WaveFormat {get;private set;}
 public AudioEngine(IAudioSink output){sink=output;WaveFormat=WaveFormat.CreateIeeeFloatWaveFormat(AudioDecoder.Rate,sink.Channels);}
 public void Start(){sink.Start(this);}
 public long NextFrame {get{return Interlocked.Read(ref nextFrame);}}
 public double PresentationSeconds {get{double value=sink.PositionSeconds;return Double.IsNaN(value)||Double.IsInfinity(value)?NextFrame/(double)AudioDecoder.Rate:Math.Max(0,value);}}
 public AudioBus AddBus(AudioRoute route){if(route.FirstChannel+1>sink.Channels)throw new ArgumentException("La coppia selezionata non esiste su "+sink.Name+" ("+sink.Channels+" canali).");var bus=new AudioBus(this,route);lock(gate)buses.Add(bus);return bus;}
 internal void Attach(AudioVoice voice,double fade){lock(gate){if(stopping)throw new ObjectDisposedException("AudioEngine");voice.Begin(nextFrame,fade);voices.Add(voice);}}
 public void SetGain(string path,double db){lock(gate)foreach(var voice in voices)if(String.Equals(voice.Path,path,StringComparison.OrdinalIgnoreCase))voice.SetGain(db);}
 internal string BusError(AudioBus bus){if(Error!=null)return Error;lock(gate)foreach(var voice in voices)if(voice.Bus==bus){if(voice.Decoder.Error!=null)return voice.Decoder.Error;if(Interlocked.Read(ref voice.Decoder.Underruns)>0)return "Buffer audio insufficiente: verificare carico e latenza.";}return null;}
 public int Read(byte[] destination,int offset,int count){int samples=count/4,channels=WaveFormat.Channels,frames=samples/channels;lock(gate){if(buffer.Length<samples)buffer=new float[samples];Array.Clear(buffer,0,samples);if(!stopping){for(int i=voices.Count-1;i>=0;i--)if(voices[i].Mix(buffer,frames,channels,nextFrame)){var voice=voices[i];voices.RemoveAt(i);voice.Retire();}foreach(var bus in buses)for(int i=0;i<frames;i++){int at=i*channels+bus.Route.FirstChannel-1;float l=buffer[at],r=buffer[at+1];bool badL=Single.IsNaN(l)||Single.IsInfinity(l),badR=Single.IsNaN(r)||Single.IsInfinity(r);bool clipped=badL||badR||Math.Abs(l)>1||Math.Abs(r)>1;l=badL?0:Math.Max(-1,Math.Min(1,l));r=badR?0:Math.Max(-1,Math.Min(1,r));buffer[at]=l;buffer[at+1]=r;bus.Meter.Add(l,r,clipped);}nextFrame+=frames;}Buffer.BlockCopy(buffer,0,destination,offset,samples*4);if(count>samples*4)Array.Clear(destination,offset+samples*4,count-samples*4);return count;}}
 public void Dispose(){List<AudioVoice> old;lock(gate){if(stopping)return;stopping=true;old=new List<AudioVoice>(voices);voices.Clear();}sink.Stop();sink.Dispose();foreach(var voice in old)voice.Close();}
}
public sealed class AudioManager:IDisposable {
 readonly Dictionary<string,AudioEngine> engines=new Dictionary<string,AudioEngine>(StringComparer.OrdinalIgnoreCase);public AudioBus Program,Preview;public string ProgramError,PreviewError;
 public AudioManager(AudioConfiguration config,Func<AudioRoute,IAudioSink> factory){config.Validate();Program=Open(config.Program,factory,out ProgramError);Preview=Open(config.Preview,factory,out PreviewError);}
 AudioBus Open(AudioRoute route,Func<AudioRoute,IAudioSink> factory,out string error){error=null;if(!route.Enabled)return null;try{AudioEngine engine;if(!engines.TryGetValue(route.Key,out engine)){engine=new AudioEngine(factory(route));try{var bus=engine.AddBus(route);engine.Start();engines.Add(route.Key,engine);return bus;}catch{engine.Dispose();throw;}}return engine.AddBus(route);}catch(Exception e){error=e.Message;return null;}}
 public void SetGain(string path,double gain){foreach(var engine in engines.Values)engine.SetGain(path,gain);}
 public void Dispose(){foreach(var engine in engines.Values)engine.Dispose();engines.Clear();}
}
}
