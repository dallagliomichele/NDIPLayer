using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
namespace ImageNdi {
public sealed class AudioDeviceInfo {
 public string Backend,Id,Name;public int Channels;
 public override string ToString(){return Backend=="ASIO"?"ASIO · "+Name:"WDM/WASAPI · "+Name;}
}
public static class AudioDevices {
 public static List<AudioDeviceInfo> Enumerate(){var list=new List<AudioDeviceInfo>();try{using(var enumerator=new MMDeviceEnumerator()){foreach(var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render,DeviceState.Active)){try{list.Add(new AudioDeviceInfo{Backend="WASAPI",Id=device.ID,Name=device.FriendlyName,Channels=device.AudioClient.MixFormat.Channels});}finally{device.Dispose();}}}}catch{}try{foreach(string name in AsioOut.GetDriverNames())list.Add(new AudioDeviceInfo{Backend="ASIO",Id=name,Name=name,Channels=0});}catch{}return list;}
 public static IAudioSink Open(AudioRoute route){return new WindowsAudioSink(route);}
}
public sealed class WindowsAudioSink:IAudioSink {
 readonly IWavePlayer player;readonly WasapiOut wasapi;readonly AsioOut asio;readonly MMDevice device;readonly string name;int channels;volatile bool stopping;volatile string error;long delivered;long stamp;long playStarted;double lastPosition;volatile bool started;readonly Stopwatch clock=Stopwatch.StartNew();
 public int Channels {get{return channels;}}public string Name {get{return name;}}public string Error {get{if(started&&!stopping&&(clock.ElapsedTicks-Math.Max(Interlocked.Read(ref stamp),Interlocked.Read(ref playStarted)))/(double)Stopwatch.Frequency>2)error="Driver audio fermo: riapplica le uscite audio.";return error;}}
 public WindowsAudioSink(AudioRoute route){
  if(route.Backend=="ASIO"){asio=new AsioOut(route.DeviceId);try{channels=asio.DriverOutputChannelCount;if(channels<2||channels>128)throw new InvalidOperationException("Numero di canali ASIO non supportato (2–128).");asio.ChannelOffset=0;name=asio.DriverName;player=asio;}catch{asio.Dispose();throw;}}
  else{using(var enumerator=new MMDeviceEnumerator())device=enumerator.GetDevice(route.DeviceId);try{if(device.State!=DeviceState.Active)throw new InvalidOperationException("Scheda WDM non disponibile.");channels=device.AudioClient.MixFormat.Channels;if(channels<2||channels>128)throw new InvalidOperationException("La scheda deve offrire almeno due canali di uscita.");name=device.FriendlyName;wasapi=new WasapiOut(device,AudioClientShareMode.Shared,true,40);player=wasapi;}catch{device.Dispose();throw;}}
  player.PlaybackStopped+=delegate(object sender,StoppedEventArgs e){if(!stopping)error=e.Exception==null?"Uscita audio arrestata dal driver.":e.Exception.Message;};
 }
 sealed class ClockedProvider:IWaveProvider {readonly WindowsAudioSink owner;readonly IWaveProvider source;public ClockedProvider(WindowsAudioSink value,IWaveProvider input){owner=value;source=input;}public WaveFormat WaveFormat{get{return source.WaveFormat;}}public int Read(byte[] b,int o,int c){int read=source.Read(b,o,c);Interlocked.Add(ref owner.delivered,read/(4*source.WaveFormat.Channels));Interlocked.Exchange(ref owner.stamp,owner.clock.ElapsedTicks);return read;}}
 public void Start(IWaveProvider provider){player.Init(new ClockedProvider(this,provider));Interlocked.Exchange(ref playStarted,clock.ElapsedTicks);started=true;player.Play();}
 public double PositionSeconds {get{if(error!=null)throw new InvalidOperationException(error);if(wasapi!=null){double position=wasapi.GetPosition()/(double)wasapi.OutputWaveFormat.AverageBytesPerSecond;if(position+.1<lastPosition){error="Clock della scheda audio reimpostato: riapplica le uscite.";throw new InvalidOperationException(error);}lastPosition=Math.Max(lastPosition,position);return position;}double queued=Interlocked.Read(ref delivered)/48000.0;double since=(clock.ElapsedTicks-Interlocked.Read(ref stamp))/(double)Stopwatch.Frequency;return Math.Min(queued,Math.Max(0,queued-asio.PlaybackLatency/1000.0+since));}}
 public void Stop(){started=false;stopping=true;try{player.Stop();}catch{} }
 public void Dispose(){stopping=true;player.Dispose();if(device!=null)device.Dispose();}
}
}
