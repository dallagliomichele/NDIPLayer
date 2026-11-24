using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;
namespace ImageNdi {
public sealed class VideoPlaybackStatus {
 public string Path;public double Duration,Position,Speed=1;public bool Loop,Ended,Ready;
 public double Remaining {get{return !Loop&&Ended?0:Math.Max(0,Duration-Position)/Speed;}}
 public VideoPlaybackStatus Copy(){return (VideoPlaybackStatus)MemberwiseClone();}
 public static VideoPlaybackStatus FromFrames(string path,double duration,long frames,int fps,bool loop,bool ended,double speed=1){if(Double.IsNaN(duration)||Double.IsInfinity(duration)||duration<0||duration>TimeSpan.MaxValue.TotalSeconds-1)duration=0;if(Double.IsNaN(speed)||Double.IsInfinity(speed)||speed<.5||speed>5)throw new ArgumentException("Velocità video non valida.");double position=frames>0?(frames-1)/(double)fps*speed:0;if(duration>0)position=loop?position%duration:Math.Min(position,duration);return new VideoPlaybackStatus{Path=path,Duration=duration,Position=position,Speed=speed,Loop=loop,Ended=ended&&!loop,Ready=frames>0};}
 public string Display(){if(!Ready)return "Avvio video…";if(!Loop&&Ended)return "Video terminato · 00:00.0";if(Duration<=0)return "Video · durata non disponibile";double seconds=Math.Ceiling(Remaining*10)/10;var time=TimeSpan.FromSeconds(seconds);string clock=(time.TotalHours>=1?((int)time.TotalHours).ToString("00")+":":"")+time.Minutes.ToString("00")+":"+time.Seconds.ToString("00")+"."+time.Milliseconds/100;return (Loop?"Al prossimo loop: ":"Alla fine: ")+clock+(Loop?" · Loop attivo":"");}
}
public interface IPlaybackSource {byte[] ReadPlaybackFrame(out VideoPlaybackStatus status);}
public static class VideoMetadata {
 static readonly ConcurrentDictionary<string,double> durations=new ConcurrentDictionary<string,double>(StringComparer.OrdinalIgnoreCase);
 public static Size Dimensions(string path){string executable=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"video","ffprobe.exe");using(var probe=new Process{StartInfo=new ProcessStartInfo(executable,"-v error -select_streams v:0 -show_entries stream=width,height -of csv=p=0:s=x "+VideoTools.Quote(path)){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}}){probe.Start();string output=probe.StandardOutput.ReadToEnd();if(!probe.WaitForExit(5000)){try{probe.Kill();}catch(InvalidOperationException){}throw new IOException("Tempo scaduto durante la lettura del formato video.");}if(probe.ExitCode!=0)throw new IOException("Formato video non leggibile.");string[] parts=output.Trim().Split('x');int w,h;if(parts.Length!=2||!Int32.TryParse(parts[0],out w)||!Int32.TryParse(parts[1],out h)||w<1||h<1)throw new IOException("Dimensioni video non disponibili.");return new Size(w,h);}}
 public static double Duration(string path){var info=new FileInfo(path);string key=info.FullName+"|"+info.Length+"|"+info.LastWriteTimeUtc.Ticks;double found;if(durations.TryGetValue(key,out found))return found;
  string executable=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"video","ffprobe.exe");
  using(var probe=new Process{StartInfo=new ProcessStartInfo(executable,"-v error -select_streams v:0 -show_entries stream=duration:format=duration -of json "+VideoTools.Quote(path)){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}}){probe.Start();var output=probe.StandardOutput.ReadToEndAsync();var error=probe.StandardError.ReadToEndAsync();if(!probe.WaitForExit(5000)){try{probe.Kill();}catch(InvalidOperationException){}return 0;}if(probe.ExitCode!=0)return 0;var data=new JavaScriptSerializer().Deserialize<Metadata>(output.Result);if(data!=null){if(data.streams!=null)foreach(var stream in data.streams)if(TryDuration(stream.duration,out found))break;if(found<=0&&data.format!=null)TryDuration(data.format.duration,out found);}}
  if(durations.Count>=128)durations.Clear();durations[key]=found;return found;
 }
 static bool TryDuration(string value,out double result){if(Double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out result)&&result>0&&result<TimeSpan.MaxValue.TotalSeconds-1&&!Double.IsInfinity(result)&&!Double.IsNaN(result))return true;result=0;return false;}
 public sealed class Entry {public string duration {get;set;}}
 public sealed class Metadata {public Entry[] streams {get;set;}public Entry format {get;set;}}
}
}
