using System;
using System.Diagnostics;
namespace ImageNdi {
public class AudioRoute {
 public bool Enabled {get;set;} public bool Muted {get;set;} public string Backend {get;set;} public string DeviceId {get;set;} public int FirstChannel {get;set;} public int VideoOffsetMs {get;set;}
 public AudioRoute(){Backend="WASAPI";DeviceId="";FirstChannel=1;}
 public AudioRoute Copy(){return (AudioRoute)MemberwiseClone();}
 public string Key {get{return Backend+":"+DeviceId;}}
 public void Validate(){if(Backend!="WASAPI"&&Backend!="ASIO")throw new ArgumentException("Tipo di uscita audio non valido.");if(FirstChannel<1||FirstChannel>127||FirstChannel%2==0)throw new ArgumentException("Scegli una coppia stereo 1–2, 3–4, ecc.");if(VideoOffsetMs < -500||VideoOffsetMs>500)throw new ArgumentException("Correzione sincronismo fuori intervallo.");if(Enabled&&String.IsNullOrWhiteSpace(DeviceId))throw new ArgumentException("Seleziona una scheda audio per ogni uscita abilitata.");}
}
public class AudioConfiguration {
 public AudioRoute Program {get;set;} public AudioRoute Preview {get;set;}
 public AudioConfiguration(){Program=new AudioRoute();Preview=new AudioRoute();}
 public void Normalize(){if(Program==null)Program=new AudioRoute();if(Preview==null)Preview=new AudioRoute();}
 public void Validate(){Normalize();Program.Validate();Preview.Validate();if(Program.Enabled&&Preview.Enabled&&String.Equals(Program.Key,Preview.Key,StringComparison.OrdinalIgnoreCase)&&Program.FirstChannel==Preview.FirstChannel)throw new ArgumentException("Program e Preview devono usare coppie diverse sulla stessa scheda.");}
}
public sealed class AudioLevels {
 public double LeftPeak,RightPeak,LeftRms,RightRms;public bool Clipped;public long Samples;
 public static double Db(double amplitude){return amplitude<=.000001?-120:20*Math.Log10(amplitude);}
}
public sealed class AudioMeter {
 readonly object gate=new object();double lp,rp,ls,rs;long count;bool clip;
 public void Add(float left,float right,bool clipped){lock(gate){lp=Math.Max(lp,Math.Abs(left));rp=Math.Max(rp,Math.Abs(right));ls+=left*left;rs+=right*right;count++;clip|=clipped;}}
 public AudioLevels Take(){lock(gate){var result=new AudioLevels{LeftPeak=lp,RightPeak=rp,LeftRms=count==0?0:Math.Sqrt(ls/count),RightRms=count==0?0:Math.Sqrt(rs/count),Clipped=clip,Samples=count};lp=rp=ls=rs=0;count=0;clip=false;return result;}}
}
// Audio-enabled clips use the device presentation clock, not a second independent playback timer.
public sealed class MediaTimeline {
 readonly object gate=new object();readonly Stopwatch wall=new Stopwatch();Func<double> external;double last,lastWall,origin;
 public void Start(Func<double> clock){lock(gate){external=clock;origin=0;last=0;lastWall=0;wall.Restart();}}
 public double Seconds {get{lock(gate){if(!wall.IsRunning)return 0;double now=wall.Elapsed.TotalSeconds;double value;if(external!=null){try{value=external();if(Double.IsNaN(value)||Double.IsInfinity(value))throw new InvalidOperationException();last=Math.Max(last,value);lastWall=now;return last;}catch{external=null;origin=last-lastWall;}}value=now+origin;last=Math.Max(last,value);return last;}}}
}
public sealed class AudioEnvelope {
 public readonly long Start,End;public readonly float From,To;
 public AudioEnvelope(long start,long end,float from,float to){Start=start;End=end;From=from;To=to;}
 public float At(long frame){if(frame<=Start)return From;if(frame>=End||End<=Start)return To;return From+(To-From)*(float)(frame-Start)/(End-Start);}
}
}
