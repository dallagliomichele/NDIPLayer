using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;
using ImageNdi;

static class RoutingTests {
 static object Field(object target,string field){return typeof(MainWindow).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);}
 static void Put(object target,string field,object value){typeof(MainWindow).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);}
 static object Call(object target,string method,params object[] args){return typeof(MainWindow).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,args);}
 public static void Run(Action<bool,string> check,bool native){
  check(NdiChannel.PreviewName("Regia È")=="Regia È - Preview","nome NDI Preview distinto con suffisso");
  check(NdiChannel.PreviewName(new string('x',128)).EndsWith(" - Preview")&&NdiChannel.PreviewName(new string('x',128)).Length<=128,"nome Preview rispetta limite lunghezza");
  string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"routing-test-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try{
   string a=Path.Combine(dir,"A.png"),b=Path.Combine(dir,"B.png");foreach(var path in new[]{a,b})using(var bmp=new Bitmap(8,8)){using(var g=Graphics.FromImage(bmp))g.Clear(path==a?Color.Red:Color.Blue);bmp.Save(path,System.Drawing.Imaging.ImageFormat.Png);}
   var settings=new Settings{Root=dir,OutputWidth=128,OutputHeight=72};string prefs=Path.Combine(dir,"settings.json");File.WriteAllText(prefs,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(settings));
   using(var window=new MainWindow(prefs)){
    var pgm=new Playout(new Probe(),128,72,50);var pv=new Playout(new Probe(),128,72,50);
    Put(window,"output",pgm);Put(window,"outW",128);Put(window,"outH",72);Put(window,"activeProgramFps",50);Put(window,"previewOutput",pv);Put(window,"previewW",128);Put(window,"previewH",72);Put(window,"previewFps",50);Call(window,"SetOutputControlLock");
    var browser=(LibraryBrowser)Field(window,"files");var handle=browser.Handle;foreach(Control c in browser.Controls)if(c is ListView){var listHandle=c.Handle;}Call(window,"Scan");
    check(SpinWait.SpinUntil(()=>pv.ReadOnAirStatus().Paths.Contains(a),2000)&&pgm.ReadOnAirStatus().Paths.Length==0,"selezione inviata alla regia senza modificare Programma");
    Call(window,"Cue",false);check(SpinWait.SpinUntil(()=>pgm.ReadOnAirStatus().Paths.Contains(a),2000),"invio manuale aggiorna Programma");
    browser.SetFiles(new System.Collections.Generic.List<string>{b});check(SpinWait.SpinUntil(()=>pv.ReadOnAirStatus().Paths.Contains(b),2000)&&pgm.ReadOnAirStatus().Paths.SequenceEqual(new[]{a}),"cambio selezione aggiorna solo NDI Preview");
    ((CheckBox)Field(window,"dissolve")).Checked=true;((NumericUpDown)Field(window,"fade")).Value=10000;Call(window,"Cue",false);
    check(SpinWait.SpinUntil(()=>pgm.ReadOnAirStatus().Transitioning,2000),"Programma in dissolvenza prima di ESC");
    object[] key={new Message(),Keys.Escape};bool handled=(bool)typeof(MainWindow).GetMethod("ProcessCmdKey",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(window,key);
    check(handled&&SpinWait.SpinUntil(()=>{var s=pgm.ReadOnAirStatus();return !s.Transitioning&&s.Paths.Length==0;},2000),"ESC interrompe dissolvenza e pulisce con cut");
    long seq=0;var pixels=pgm.ReadSentFrame(ref seq);check(pixels!=null&&pixels.All(value=>value==0)&&pv.ReadOnAirStatus().Paths.SequenceEqual(new[]{b})&&(string)Field(window,"selected")==b,"ESC invia alpha zero e conserva anteprima regia e selezione");
    long previewFrames=pv.Frames;Call(window,"Toggle");check(Field(window,"output")==null&&Object.ReferenceEquals(Field(window,"previewOutput"),pv)&&SpinWait.SpinUntil(()=>pv.Frames>previewFrames,2000),"arrestare Programma non arresta Preview NDI");
    ((NumericUpDown)Field(window,"width")).Value=1920;((NumericUpDown)Field(window,"height")).Value=1080;
    check((int)typeof(MainWindow).GetProperty("FrameWidth",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window,null)==128,"Preview mantiene formato attivo con nuove impostazioni differite");
    Call(window,"StopPreviewOutput");check(Field(window,"previewOutput")==null,"Preview può essere arrestato separatamente");
    check(((NumericUpDown)Field(window,"x")).Value==960&&((NumericUpDown)Field(window,"y")).Value==540,"arresto ultimo flusso riallinea centro al nuovo formato");
   }
  }finally{Directory.Delete(dir,true);}
  if(native){using(var main=new NdiSender("NDIPlayer TEST Programma"))using(var preview=new NdiSender("NDIPlayer TEST Preview")){var pixels=new byte[16*16*4];main.Send(pixels,16,16,50);preview.Send(pixels,16,16,50);main.Dispose();preview.Send(pixels,16,16,50);check(true,"due sender NDI reali: chiudere Programma lascia Preview funzionante");}}
 }
 sealed class Probe:IVideoSender {public void Send(byte[] p,int w,int h,int fps){Thread.Sleep(10);}public void Dispose(){}}
}
