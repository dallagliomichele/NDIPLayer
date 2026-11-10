using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;
using System.Reflection;
using System.Windows.Forms;
using ImageNdi;

static class CompanionTests {
 static object Get(object target,string field){return typeof(MainWindow).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);}
 static void Put(object target,string field,object value){typeof(MainWindow).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);}
 static object Call(object target,string name,params object[] args){return typeof(MainWindow).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);}
 public static void Run(Action<bool,string> check){
  var gesture=new EscapeGesture();check(gesture.Press(0,false)==EscapeAction.Fade&&gesture.Press(100,true)==EscapeAction.None&&gesture.Press(200,false)==EscapeAction.Cut,"doppio ESC fisico rapido va in cut; autorepeat ignorato");
  check(gesture.Press(1000,false)==EscapeAction.Fade&&gesture.Press(1500,false)==EscapeAction.Fade,"ESC distanziati non producono cut");
  check(CompanionSettings.Line("LOCATION 1/1/1 PRESS\r\n")=="LOCATION 1/1/1 PRESS","terminatore TCP normalizzato senza doppie righe");
  bool bad=false;try{CompanionSettings.Line("A\nB");}catch(ArgumentException){bad=true;}check(bad,"stringa Companion multilinea rifiutata");
  string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"companion-test-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try{using(var server=new LoopbackServer())using(var tcp=new CompanionTcp()){
   var config=new CompanionSettings{Enabled=true,Address="127.0.0.1",Port=server.Port,OnAirCommand="FIRST è",EmptyCommand="EMPTY"};tcp.Configure(config);tcp.Presence(true);tcp.Presence(false);
   check(SpinWait.SpinUntil(()=>server.Messages.Count>=2,3000)&&server.Messages.ToArray().SequenceEqual(new[]{"FIRST è","EMPTY"}),"TCP loopback consegna stringhe UTF-8 terminate da newline in ordine ("+tcp.Status+"; "+String.Join(" / ",server.Messages.ToArray())+")");
   config.Enabled=false;tcp.Configure(config);tcp.Presence(true);Thread.Sleep(50);check(server.Messages.Count==2,"Companion disabilitato non trasmette");
   tcp.Test(config);check(SpinWait.SpinUntil(()=>tcp.Status.Contains("porta raggiungibile"),3000)&&server.Messages.Count==2,"test connessione TCP senza eseguire comandi");
   var draft=config.Copy();draft.OnAirCommand="TEST FIRST è";draft.EmptyCommand="TEST EMPTY";tcp.TestCommand(draft,true);tcp.TestCommand(draft,false);
   check(tcp.Flush(3000)&&SpinWait.SpinUntil(()=>server.Messages.Count==4,3000)&&server.Messages.ToArray().Skip(2).SequenceEqual(new[]{"TEST FIRST è","TEST EMPTY"}),"test manuali inviano entrambe le stringhe non salvate anche con notifiche disabilitate");
   tcp.Presence(true);Thread.Sleep(50);check(server.Messages.Count==4&&config.OnAirCommand=="FIRST è","test manuale non abilita notifiche né modifica la configurazione applicata");
   draft.EmptyCommand="";bool blank=false;try{tcp.TestCommand(draft,false);}catch(ArgumentException){blank=true;}check(blank&&server.Messages.Count==4,"test stringa vuota rifiutato senza inviare comandi");
   config.Enabled=true;tcp.Configure(config);int baseline=server.Messages.Count;
   using(var playout=new Playout(new Probe(),16,16,50)){
    playout.PresenceChanged+=delegate(object sender,ProgramPresenceEventArgs e){tcp.Presence(e.HasImage);};var frame=new byte[1024];frame[3]=255;
    Thread.Sleep(40);check(server.Messages.Count==baseline,"avvio Program trasparente non emette false notifiche");
    playout.Cue(frame,0,"A.png");check(SpinWait.SpinUntil(()=>server.Messages.Count==baseline+1,3000),"prima foto emette una sola notifica");
    playout.Cue(frame,.08,"B.png");check(SpinWait.SpinUntil(()=>!playout.ReadOnAirStatus().Transitioning&&playout.ReadOnAirStatus().Paths.Contains("B.png"),2000)&&server.Messages.Count==baseline+1,"cambio foto e crossfade non ripetono notifica prima foto");
    playout.Cue(new byte[1024],.25);Thread.Sleep(40);check(server.Messages.Count==baseline+1,"stringa vuoto non parte all'inizio della dissolvenza");
    check(SpinWait.SpinUntil(()=>server.Messages.Count==baseline+2,3000)&&server.Messages.ToArray().Last()=="EMPTY","stringa vuoto parte dopo clear completato");
    playout.Cue(frame,0,"C.png");check(SpinWait.SpinUntil(()=>server.Messages.Count==baseline+3,3000),"nuova prima foto dopo clear genera nuovo evento");
    playout.Cue(new byte[1024],0);check(SpinWait.SpinUntil(()=>server.Messages.Count==baseline+4,3000),"clear in cut genera subito evento vuoto");
    playout.Cue(frame,0,"D.png");SpinWait.SpinUntil(()=>server.Messages.Count==baseline+5,3000);bool stopped=playout.StopTransparent();check(stopped&&tcp.Flush(1500)&&SpinWait.SpinUntil(()=>server.Messages.Count==baseline+6,1500)&&server.Messages.ToArray().Last()=="EMPTY","arresto trasparente invia notifica Companion vuoto prima della chiusura");
   }
   var settings=new Settings{EscClearMs=1400,FadeMs=300,Companion=config};string project=Path.Combine(dir,"show.ndiproj");ProjectStore.Write(project,settings);var loaded=ProjectStore.Read(project);check(loaded.EscClearMs==1400&&loaded.FadeMs==300&&loaded.Companion.Port==server.Port&&loaded.Companion.OnAirCommand=="FIRST è"&&loaded.Companion.Enabled,"progetto conserva durata Esc separata e configurazione Companion");
   string guiPrefs=Path.Combine(dir,"gui.json");File.WriteAllText(guiPrefs,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new Settings{OutputWidth=16,OutputHeight=16,Companion=config}));int guiBaseline=server.Messages.Count;
   using(var window=new MainWindow(guiPrefs)){
    var pgm=new Playout(new Probe(),16,16,50);var preview=new Playout(new Probe(),16,16,50);Put(window,"output",pgm);Put(window,"outW",16);Put(window,"outH",16);Put(window,"previewOutput",preview);Put(window,"previewW",16);Put(window,"previewH",16);Call(window,"BindCompanion",pgm);var frame=new byte[1024];frame[3]=255;preview.Cue(frame,0,"Preview-only.png");Thread.Sleep(40);check(server.Messages.Count==guiBaseline,"Preview NDI non genera messaggi Companion");
    pgm.Cue(frame,0,"Program.png");check(SpinWait.SpinUntil(()=>server.Messages.Count==guiBaseline+1,3000),"collegamento UI invia messaggio solo per Program");
    Call(window,"ApplyProject",new Settings{OutputWidth=16,OutputHeight=16,Companion=config.Copy()},project);Thread.Sleep(40);check(server.Messages.Count==guiBaseline+1&&pgm.ReadOnAirStatus().Paths.Contains("Program.png"),"caricare progetto non ripete eventi o pulisce Program");
    pgm.Cue(new byte[1024],0);check(SpinWait.SpinUntil(()=>server.Messages.Count==guiBaseline+2,3000),"clear Program collegato invia seconda stringa dalla UI");
   }
  }
  string prefs=Path.Combine(dir,"prefs.json");File.WriteAllText(prefs,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new Settings{OutputWidth=16,OutputHeight=16,EscClearMs=2000,FadeMs=100}));
  using(var window=new MainWindow(prefs)){
   var pgm=new Playout(new Probe(),16,16,50);Put(window,"output",pgm);Put(window,"outW",16);Put(window,"outH",16);var frame=new byte[1024];frame[3]=255;pgm.Cue(frame,0,"A.png");SpinWait.SpinUntil(()=>pgm.ReadOnAirStatus().Paths.Length>0,2000);
   object[] key={new Message(),Keys.Escape};typeof(MainWindow).GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,key);check(pgm.Clearing&&((NumericUpDown)Get(window,"escClearFade")).Value==2000&&((NumericUpDown)Get(window,"fade")).Value==100,"primo ESC usa durata dedicata senza cambiare durata foto");
   var repeated=Message.Create(IntPtr.Zero,0x100,(IntPtr)27,(IntPtr)(1L<<30));typeof(MainWindow).GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{repeated,Keys.Escape});check(pgm.Clearing,"tenere premuto ESC non simula doppio tocco");
   typeof(MainWindow).GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,key);check(SpinWait.SpinUntil(()=>!pgm.Clearing&&pgm.ReadOnAirStatus().Paths.Length==0,2000),"secondo ESC rapido interrompe clear lungo con cut");
  }
  using(var closed=new TcpListenerWrapper()){using(var tcp=new CompanionTcp()){tcp.Configure(new CompanionSettings{Enabled=true,Address="127.0.0.1",Port=closed.Port,OnAirCommand="TEST"});tcp.Presence(true);check(SpinWait.SpinUntil(()=>tcp.Status.Contains("ERRORE"),3000),"errore TCP visibile senza interrompere video");}}
  }finally{Directory.Delete(dir,true);}
 }
 sealed class Probe:IVideoSender {public void Send(byte[] p,int w,int h,int fps){Thread.Sleep(10);}public void Dispose(){}}
 sealed class TcpListenerWrapper:IDisposable {public readonly int Port;public TcpListenerWrapper(){var l=new TcpListener(IPAddress.Loopback,0);l.Start();Port=((IPEndPoint)l.LocalEndpoint).Port;l.Stop();}public void Dispose(){}}
 sealed class LoopbackServer:IDisposable {
  readonly TcpListener listener=new TcpListener(IPAddress.Loopback,0);readonly Thread worker;volatile bool stop;public readonly ConcurrentQueue<string> Messages=new ConcurrentQueue<string>();public readonly int Port;
  public LoopbackServer(){listener.Start();Port=((IPEndPoint)listener.LocalEndpoint).Port;worker=new Thread(Run){IsBackground=true};worker.Start();}
  void Run(){while(!stop){try{if(!listener.Pending()){Thread.Sleep(5);continue;}using(var client=listener.AcceptTcpClient()){client.ReceiveTimeout=1500;using(var reader=new StreamReader(client.GetStream(),Encoding.UTF8)){string line=reader.ReadLine();if(line!=null)Messages.Enqueue(line);}}}catch(SocketException){if(stop)return;}catch(IOException){if(stop)return;}catch(ObjectDisposedException){return;}}}
  public void Dispose(){stop=true;listener.Stop();worker.Join();}
 }
}
