using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ImageNdi;
using NAudio.Wave;
static class AudioTests {
 public sealed class FakeSink:IAudioSink {
  public int Channels {get;set;}public string Name{get{return "Scheda audio simulata";}}public string Error{get;set;}public bool Closed;public double Position;IWaveProvider provider;
  public double PositionSeconds{get{return Position;}}
  public FakeSink(int channels){Channels=channels;}
  public void Start(IWaveProvider input){provider=input;}
  public float[] Pump(int frames){var bytes=new byte[frames*Channels*4];provider.Read(bytes,0,bytes.Length);Position+=frames/48000.0;var result=new float[frames*Channels];Buffer.BlockCopy(bytes,0,result,0,bytes.Length);return result;}
  public void Stop(){}public void Dispose(){Closed=true;}
 }
 sealed class Sender:IVideoSender {public void Send(byte[] b,int w,int h,int fps){Thread.Sleep(10);}public void Dispose(){}}
 static double Peak(float[] data,int channels,int channel,int start=0){double peak=0;for(int i=start;i<data.Length/channels;i++)peak=Math.Max(peak,Math.Abs(data[i*channels+channel]));return peak;}
 static AudioConfiguration Config(){return new AudioConfiguration{Program=new AudioRoute{Enabled=true,Backend="ASIO",DeviceId="Dante simulata",FirstChannel=1},Preview=new AudioRoute{Enabled=true,Backend="ASIO",DeviceId="Dante simulata",FirstChannel=3}};}
 static void Wait(Func<bool> test){if(!SpinWait.SpinUntil(test,5000))throw new Exception("Timeout prova audio");}
 public static void Run(Action<bool,string> check){
  var envelope=new AudioEnvelope(0,48000,1,0);check(envelope.At(0)==1&&Math.Abs(envelope.At(24000)-.5)<.0001&&envelope.At(48000)==0,"inviluppo audio della dissolvenza lineare raggiunge zero");
  bool conflict=false;var bad=Config();bad.Preview.FirstChannel=1;try{bad.Validate();}catch(ArgumentException){conflict=true;}check(conflict,"Program e Preview non possono sovrapporsi sulla stessa coppia");
  check(Math.Abs(AudioLevels.Db(.5)+6.0206)<.001&&AudioLevels.Db(0)==-120,"meter dBFS corretto per mezzo livello e silenzio");
  string dir=Path.Combine(Path.GetTempPath(),"ndi-audio-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try{
   string movie=Path.Combine(dir,"audio.mov"),previewMovie=Path.Combine(dir,"preview.mov"),silent=Path.Combine(dir,"muto.mov");
   using(var p=VideoTools.Start("-f lavfi -i \"color=c=red:s=32x16:r=25:d=2\" -f lavfi -i \"sine=frequency=440:sample_rate=48000:duration=2\" -map 0:v:0 -map 1:a:0 -af \"volume=4,pan=stereo|c0=c0|c1=c0\" -c:v qtrle -c:a pcm_s16le -shortest -y "+VideoTools.Quote(movie))){string error=p.StandardError.ReadToEnd();p.WaitForExit();check(p.ExitCode==0,"genera MOV reale con video e audio stereo di prova");}File.Copy(movie,previewMovie);
   using(var p=VideoTools.Start("-i "+VideoTools.Quote(movie)+" -an -c:v copy -y "+VideoTools.Quote(silent))){var err=p.StandardError.ReadToEnd();p.WaitForExit();check(p.ExitCode==0,"genera MOV senza audio");}
   check(AudioDecoder.ContainsAudio(movie)&&!AudioDecoder.ContainsAudio(silent),"rilevamento traccia audio distingue MOV muti");
   foreach(double speed in new[]{.5,5.0})using(var decoder=new AudioDecoder(movie,false,speed)){int samples=0;var pcm=new float[4096];var timeout=System.Diagnostics.Stopwatch.StartNew();while(!decoder.Finished&&timeout.ElapsedMilliseconds<10000){samples+=decoder.ReadStereo(pcm,pcm.Length);Thread.Sleep(1);}samples+=decoder.ReadStereo(pcm,pcm.Length);double seconds=samples/(2.0*AudioDecoder.Rate);check(decoder.Error==null&&decoder.Finished&&Math.Abs(seconds-2/speed)<.18,"durata audio segue velocità "+speed+" ("+seconds+" s)");}
   var sink=new FakeSink(8);int opens=0;using(var manager=new AudioManager(Config(),route=>{opens++;return sink;})){
    check(opens==1&&manager.Program.Engine==manager.Preview.Engine,"Program e Preview condividono un unico motore ASIO");
    var pgm=manager.Program.Prepare(movie,true,0);var pv=manager.Preview.Prepare(previewMovie,true,-6.020599913);check(pgm.Decoder.Error==null&&pv.Decoder.Error==null,"decoder audio preparano buffer PCM senza errori");pgm.Activate(0);pv.Activate(0);
    var buffer=sink.Pump(4800);double pp=Peak(buffer,8,0),qp=Peak(buffer,8,2);check(Math.Abs(pp-.5)<.015&&Math.Abs(qp-.25)<.015,"audio instradato alle coppie 1–2 e 3–4 con gain distinto");check(Peak(buffer,8,4)==0&&Peak(buffer,8,7)==0,"canali non assegnati restano a zero");
    var meter=manager.Program.Meter.Take();check(Math.Abs(meter.LeftPeak-pp)<.001&&Math.Abs(meter.LeftRms-.3535)<.015,"meter misura picco e RMS consegnati al buffer di uscita");
    manager.Program.Muted=true;buffer=sink.Pump(4800);check(Peak(buffer,8,0)==0&&Peak(buffer,8,2)>.2,"Mute Program lascia ascolto Preview indipendente");manager.Program.Muted=false;
    manager.SetGain(movie,12);buffer=sink.Pump(4800);meter=manager.Program.Meter.Take();check(meter.Clipped&&Peak(buffer,8,0)==1,"boost oltre 0 dBFS viene segnalato CLIP e contenuto nel range del driver");check(Peak(buffer,8,2)<.27,"gain del Program non modifica un altro file in Preview");
    manager.SetGain(movie,0);sink.Pump(4800);pgm.FadeOut(1);buffer=sink.Pump(48000);check(Peak(buffer,8,0,47000)<.02&&Peak(buffer,8,2)>.2,"clear in dissolvenza sfuma Program mantenendo Preview");
    pv.Release();sink.Pump(480);buffer=sink.Pump(480);check(Peak(buffer,8,0)==0&&Peak(buffer,8,2)==0,"rilascio delle voci termina in silenzio");
   }check(sink.Closed,"chiusura motore rilascia il driver una sola volta");
   var missing=Config();missing.Preview.Enabled=false;int attempted=0;using(var manager=new AudioManager(missing,route=>{attempted++;throw new IOException("Scheda assente");})){check(manager.Program==null&&manager.ProgramError.Contains("assente")&&attempted==1,"scheda mancante produce errore senza fallback su un'altra uscita");}
   var limited=Config();limited.Preview.Enabled=false;limited.Program.FirstChannel=9;using(var manager=new AudioManager(limited,route=>new FakeSink(8))){check(manager.Program==null&&manager.ProgramError.Contains("non esiste"),"coppia fuori dai canali disponibili viene rifiutata");}
   var clock=new MediaTimeline();double position=1;bool lost=false;clock.Start(()=>{if(lost)throw new IOException();return position;});check(clock.Seconds==1,"timeline video segue il clock di presentazione audio");position=2;check(clock.Seconds==2,"avanzamento del driver aggiorna il clock comune");lost=true;Thread.Sleep(20);double after=clock.Seconds;Thread.Sleep(20);check(clock.Seconds>after&&after>=2,"perdita driver non blocca il video: clock di continuità");
   var syncSink=new FakeSink(2);var syncConfig=Config();syncConfig.Preview.Enabled=false;using(var manager=new AudioManager(syncConfig,r=>syncSink)){
    byte[] initial=Renderer.Render(movie,16,16,new Placement());using(var video=new VideoSession(movie,16,16,50,new Placement(),true,initial,manager.Program,0))using(var playout=new Playout(new Sender(),16,16,50)){
     playout.CueSource(video,initial,0,movie);syncSink.Pump(24000);Wait(()=>video.DecodedFrames>=24);VideoPlaybackStatus status;video.ReadPlaybackFrame(out status);check(Math.Abs(status.Position-.5)<.03,"video e countdown seguono i campioni presentati dalla scheda");
     video.SetPaused(true);long paused=video.DecodedFrames;var silence=syncSink.Pump(24000);Thread.Sleep(100);check(Peak(silence,2,0)==0&&video.DecodedFrames==paused,"pausa ferma audio e video senza avanzare il clock");
     video.SetPaused(false);syncSink.Pump(4800);Wait(()=>video.DecodedFrames>=paused+4);video.ReadPlaybackFrame(out status);check(status.Position>=.55&&status.Position<.7,"ripresa mantiene sincronismo dopo la pausa");
     for(int i=0;i<120;i++){syncSink.Pump(960);Thread.Sleep(2);}Wait(()=>video.DecodedFrames>=140);video.ReadPlaybackFrame(out status);check(status.Loop&&status.Position>=.9&&status.Position<1.2&&!video.Ended,"Loop audio e video proseguono sullo stesso clock");
     syncSink.Error="driver perso";long frames=video.DecodedFrames;Thread.Sleep(100);Wait(()=>video.DecodedFrames>frames);check(playout.Error==null,"errore audio non interrompe il mittente video");
    }
   }
   var eofSink=new FakeSink(2);using(var manager=new AudioManager(syncConfig,r=>eofSink)){
    var voice=manager.Program.Prepare(movie,false,0);voice.Activate(0);for(int i=0;i<110;i++){eofSink.Pump(960);Thread.Sleep(2);}Wait(()=>voice.Decoder.Finished);var tail=eofSink.Pump(960);check(Peak(tail,2,0)==0,"video senza Loop termina l'audio senza ripetere l'ultimo buffer");voice.Release();eofSink.Pump(960);
   }
   var speedSink=new FakeSink(2);using(var manager=new AudioManager(syncConfig,r=>speedSink)){
    byte[] initial=Renderer.Render(movie,16,16,new Placement());using(var video=new VideoSession(movie,16,16,50,new Placement(),true,initial,manager.Program,0,2))using(var playout=new Playout(new Sender(),16,16,50)){
     playout.CueSource(video,initial,0,movie);speedSink.Pump(24000);Wait(()=>video.DecodedFrames>=49);VideoPlaybackStatus playback;video.ReadPlaybackFrame(out playback);check(Math.Abs(playback.Position-1)<.04&&Math.Abs(playback.Remaining-.5)<.03,"video a 2× segue il clock audio e countdown indica il tempo reale restante");
    }
   }
   var data=new Settings{Root=dir,Audio=Config()};data.AudioGains[movie]=-7.5;data.Images[movie]=new Placement{Loop=true,X=10};string path=Path.Combine(dir,"audio.ndiproj");ProjectStore.Write(path,data);var loaded=ProjectStore.Read(path);check(loaded.Audio.Program.Enabled&&loaded.Audio.Preview.FirstChannel==3&&loaded.AudioGains[movie]==-7.5,"progetto salva routing e gain per singolo video");ProjectStore.Relocate(loaded,Path.Combine(dir,"new"));check(loaded.AudioGains[Path.Combine(dir,"new","audio.mov")]==-7.5,"ricollegamento cartella mantiene gain audio dei file");
   var prefs=Path.Combine(dir,"prefs.json");var uiData=new Settings{Root=dir,OutputWidth=16,OutputHeight=16,OutputFps=50};uiData.AudioGains[movie]=-6;var only=ProjectStore.AddBlock(uiData,"Audio");ProjectStore.AddFiles(only,new[]{movie});uiData.ActiveBlockId=only.Id;File.WriteAllText(prefs,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(uiData));
   using(var window=new MainWindow(prefs)){
    var type=typeof(MainWindow);var flags=BindingFlags.NonPublic|BindingFlags.Instance;var box=(System.Windows.Forms.NumericUpDown)type.GetField("audioGain",flags).GetValue(window);check(box.Minimum==-60&&box.Maximum==12,"controllo gain video espone intervallo in dB");var browser=(LibraryBrowser)type.GetField("files",flags).GetValue(window);var h=browser.Handle;foreach(System.Windows.Forms.Control control in browser.Controls)if(control is System.Windows.Forms.ListView){var handle=control.Handle;}type.GetMethod("Scan",flags).Invoke(window,null);check(box.Value==-6,"selezione video carica gain salvato");
    var uiSink=new FakeSink(8);var manager=new AudioManager(Config(),r=>uiSink);((AudioManager)type.GetField("audio",flags).GetValue(window)).Dispose();type.GetField("audio",flags).SetValue(window,manager);var program=new Playout(new Sender(),16,16,50);type.GetField("output",flags).SetValue(window,program);type.GetField("outW",flags).SetValue(window,16);type.GetField("outH",flags).SetValue(window,16);type.GetField("activeProgramFps",flags).SetValue(window,50);type.GetMethod("Cue",flags).Invoke(window,new object[]{false});box.Value=-3.5m;var samples=uiSink.Pump(4800);check(Math.Abs(Peak(samples,8,0)-.334)<.02,"gain della UI aggiorna l'audio già in Program senza riavviare il video");
    ((System.Windows.Forms.NumericUpDown)type.GetField("scale",flags).GetValue(window)).Value=125;var current=(Settings)type.GetField("settings",flags).GetValue(window);check(current.AudioGains[movie]==-3.5,"resize non perde il gain del video");
    ((System.Windows.Forms.CheckBox)type.GetField("muteProgram",flags).GetValue(window)).Checked=true;check(Peak(uiSink.Pump(960),8,0)==0,"Mute della UI silenzia il Program durante riproduzione");
   }
  }finally{Directory.Delete(dir,true);}
 }
}
