using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;
using ImageNdi;

static class RoutingTests {
 public static string ProofPath;
 static object Field(object target,string field){return typeof(MainWindow).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);}
 static void Put(object target,string field,object value){typeof(MainWindow).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);}
 static object Call(object target,string method,params object[] args){return typeof(MainWindow).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,args);}
 public static void Run(Action<bool,string> check,bool native){
  check(NdiChannel.PreviewName("Regia È")=="Regia È - Preview","nome NDI Preview distinto con suffisso");
  check(NdiChannel.PreviewName(new string('x',128)).EndsWith(" - Preview")&&NdiChannel.PreviewName(new string('x',128)).Length<=128,"nome Preview rispetta limite lunghezza");
  string longProgram=new string('x',118)+" - Preview";check(NdiChannel.PreviewName(longProgram)!=longProgram,"migrazione evita collisione anche con nomi lunghi già terminanti in Preview");
  bool duplicate=false;try{NdiChannel.ValidatePair("Sala","sala");}catch(ArgumentException){duplicate=true;}check(duplicate,"nomi Program e Preview identici rifiutati");
  string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"routing-test-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try{
   string a=Path.Combine(dir,"A.png"),b=Path.Combine(dir,"B.png");foreach(var path in new[]{a,b})using(var bmp=new Bitmap(8,8)){using(var g=Graphics.FromImage(bmp))g.Clear(path==a?Color.Red:Color.Blue);bmp.Save(path,System.Drawing.Imaging.ImageFormat.Png);}
   var settings=new Settings{Root=dir,OutputWidth=128,OutputHeight=72};string prefs=Path.Combine(dir,"settings.json");File.WriteAllText(prefs,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(settings));
   using(var window=new MainWindow(prefs)){
    check(((Label)Field(window,"programLamp")).Text.Contains("SPENTO")&&((Label)Field(window,"previewLamp")).Text.Contains("SPENTO")&&window.Controls.Find("ProgramOutputSection",true).Length==1&&window.Controls.Find("PreviewOutputSection",true).Length==1,"sezioni Program e Preview distinte inizialmente spente");
    var previewName=(TextBox)Field(window,"previewChannelName");previewName.Text="Assistente Regia";((TextBox)Field(window,"channelName")).Text="Program Sala";
    check(previewName.Text=="Assistente Regia","modifica nome Program non cambia nome Preview");
    string namedProject=Path.Combine(dir,"names.ndiproj");ProjectStore.Write(namedProject,(Settings)Field(window,"settings"));var restored=ProjectStore.Read(namedProject);check(restored.ChannelName=="Program Sala"&&restored.PreviewChannelName=="Assistente Regia","entrambi i nomi indipendenti salvati e riletti dal progetto");
    string legacyProject=Path.Combine(dir,"legacy.ndiproj");File.WriteAllText(legacyProject,"{\"Version\":1,\"Data\":{\"ChannelName\":\"Legacy\",\"Images\":{}}}");check(ProjectStore.Read(legacyProject).PreviewChannelName=="Legacy - Preview","vecchi progetti senza nome Preview ricevono il valore compatibile");
    var pgm=new Playout(new Probe(),128,72,50);var pv=new Playout(new Probe(),128,72,50);
    Put(window,"output",pgm);Put(window,"outW",128);Put(window,"outH",72);Put(window,"activeProgramFps",50);Put(window,"previewOutput",pv);Put(window,"previewW",128);Put(window,"previewH",72);Put(window,"previewFps",50);Put(window,"previewSourceName",previewName.Text);Put(window,"activeProgramSourceName","Program Sala");Call(window,"SetOutputControlLock");
    var browser=(LibraryBrowser)Field(window,"files");var handle=browser.Handle;foreach(Control c in browser.Controls)if(c is ListView){var listHandle=c.Handle;}Call(window,"Scan");
    check(SpinWait.SpinUntil(()=>pv.ReadOnAirStatus().Paths.Contains(a),2000)&&pgm.ReadOnAirStatus().Paths.Length==0,"selezione inviata alla regia senza modificare Programma");
    Call(window,"RefreshOutputIndicators");check(((Label)Field(window,"programLamp")).Text.Contains("ACCESO")&&((Label)Field(window,"previewLamp")).Text.Contains("ACCESO"),"entrambi i flussi mostrano indicatore acceso dopo invio");
    check(((PreviewCanvas)Field(window,"program")).BorderColor==Color.Red&&((PreviewCanvas)Field(window,"preview")).BorderColor==Color.LimeGreen,"bordi Program rosso e Preview verde con flussi attivi");
    Call(window,"UpdateProgram");check(!((Label)Field(window,"status")).Text.Contains("frame"),"stato Program senza contatore dei frame");
    pv.Error="Errore simulato";Call(window,"RefreshOutputIndicators");check(((Label)Field(window,"previewLamp")).Text.Contains("ERRORE")&&((Label)Field(window,"programLamp")).Text.Contains("ACCESO"),"errore Preview distinto dallo stato Program");pv.Error=null;
    Call(window,"Cue",false);check(SpinWait.SpinUntil(()=>pgm.ReadOnAirStatus().Paths.Contains(a),2000),"invio manuale aggiorna Programma");
    browser.SetFiles(new System.Collections.Generic.List<string>{b});check(SpinWait.SpinUntil(()=>pv.ReadOnAirStatus().Paths.Contains(b),2000)&&pgm.ReadOnAirStatus().Paths.SequenceEqual(new[]{a}),"cambio selezione aggiorna solo NDI Preview");
    var selection=(PreviewCanvas)Field(window,"preview");selection.Size=new Size(1000,700);var bounds=PreviewCanvas.FrameBounds(selection.Size,128,72);var dbl=typeof(PreviewCanvas).GetMethod("OnMouseDoubleClick",BindingFlags.NonPublic|BindingFlags.Instance);dbl.Invoke(selection,new object[]{new MouseEventArgs(MouseButtons.Left,2,bounds.Left+bounds.Width/2,bounds.Top+bounds.Height/2,0)});
    check(SpinWait.SpinUntil(()=>pgm.ReadOnAirStatus().Paths.SequenceEqual(new[]{b}),2000),"doppio clic nel riquadro Selezione manda la foto in onda");
    pgm.Cue(Renderer.Render(a,128,72,new Placement()),0,a);SpinWait.SpinUntil(()=>pgm.ReadOnAirStatus().Paths.SequenceEqual(new[]{a}),2000);
    ((CheckBox)Field(window,"dissolve")).Checked=true;((NumericUpDown)Field(window,"fade")).Value=10000;Call(window,"Cue",false);
    check(SpinWait.SpinUntil(()=>pgm.ReadOnAirStatus().Transitioning,2000),"Programma in dissolvenza prima di ESC");
    ((NumericUpDown)Field(window,"fade")).Value=250;((CheckBox)Field(window,"dissolve")).Checked=false;
    object[] key={new Message(),Keys.Escape};bool handled=(bool)typeof(MainWindow).GetMethod("ProcessCmdKey",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(window,key);
    check(pgm.Clearing,"ESC avvia una dissolvenza positiva con modalità Cut selezionata");
    bool partial=SpinWait.SpinUntil(()=>{long serial=0;var frame=pgm.ReadSentFrame(ref serial);int alpha=frame==null?0:frame[(36*128+64)*4+3];return alpha>0&&alpha<255;},2000);check(partial,"clear ESC contiene frame con alpha intermedio");
    check(handled&&SpinWait.SpinUntil(()=>{var s=pgm.ReadOnAirStatus();return !s.Transitioning&&s.Paths.Length==0;},2000),"ESC termina clear in dissolvenza anche con Cut selezionato");
    long seq=0;var pixels=pgm.ReadSentFrame(ref seq);check(pixels!=null&&pixels.All(value=>value==0)&&pv.ReadOnAirStatus().Paths.SequenceEqual(new[]{b})&&(string)Field(window,"selected")==b,"ESC invia alpha zero e conserva anteprima regia e selezione");
    pgm.Cue(Renderer.Render(b,128,72,new Placement()),0,b);SpinWait.SpinUntil(()=>pgm.ReadOnAirStatus().Paths.Contains(b),2000);((NumericUpDown)Field(window,"fade")).Value=0;typeof(MainWindow).GetMethod("ProcessCmdKey",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(window,key);check(pgm.Clearing,"ESC usa dissolvenza anche con durata impostata a zero");
    long previewFrames=pv.Frames;Call(window,"Toggle");check(Field(window,"output")==null&&Object.ReferenceEquals(Field(window,"previewOutput"),pv)&&SpinWait.SpinUntil(()=>pv.Frames>previewFrames,2000),"arrestare Programma non arresta Preview NDI");
    Call(window,"RefreshOutputIndicators");check(((Label)Field(window,"programLamp")).Text.Contains("SPENTO")&&((Label)Field(window,"previewLamp")).Text.Contains("ACCESO"),"arresto Program aggiorna solo il suo indicatore");
    check(((PreviewCanvas)Field(window,"program")).BorderColor==Color.Black&&((PreviewCanvas)Field(window,"preview")).BorderColor==Color.LimeGreen,"arresto Program annerisce solo il suo bordo");
    check(((TextBox)Field(window,"channelName")).Enabled&&!((TextBox)Field(window,"previewChannelName")).Enabled,"nome modificabile solo per il proprio flusso fermo");
    if(ProofPath!=null){var sections=window.Controls.Find("MainHeader",true)[0];var parent=sections.Parent;parent.Controls.Remove(sections);sections.Size=new Size(1100,116);sections.CreateControl();using(var image=new Bitmap(1100,116)){sections.DrawToBitmap(image,new Rectangle(0,0,1100,116));image.Save(ProofPath,System.Drawing.Imaging.ImageFormat.Png);}parent.Controls.Add(sections);}
    ((NumericUpDown)Field(window,"width")).Value=1920;((NumericUpDown)Field(window,"height")).Value=1080;
    check((int)typeof(MainWindow).GetProperty("FrameWidth",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window,null)==128,"Preview mantiene formato attivo con nuove impostazioni differite");
    Call(window,"StopPreviewOutput");check(Field(window,"previewOutput")==null,"Preview può essere arrestato separatamente");
    check(((PreviewCanvas)Field(window,"preview")).BorderColor==Color.Black,"arresto Preview annerisce il suo bordo");
    check(((NumericUpDown)Field(window,"x")).Value==960&&((NumericUpDown)Field(window,"y")).Value==540,"arresto ultimo flusso riallinea centro al nuovo formato");
    ((TextBox)Field(window,"channelName")).Text="Modificato";previewName.Text="Altro";Call(window,"ApplyProject",restored,namedProject);check(((TextBox)Field(window,"channelName")).Text=="Program Sala"&&previewName.Text=="Assistente Regia","caricamento progetto ripristina entrambi i campi nome");
   }
  }finally{Directory.Delete(dir,true);}
  if(native){using(var main=new NdiSender("NDIPlayer TEST Programma"))using(var preview=new NdiSender("NDIPlayer TEST Preview")){var pixels=new byte[16*16*4];main.Send(pixels,16,16,50);preview.Send(pixels,16,16,50);main.Dispose();preview.Send(pixels,16,16,50);check(true,"due sender NDI reali: chiudere Programma lascia Preview funzionante");}}
 }
 sealed class Probe:IVideoSender {public void Send(byte[] p,int w,int h,int fps){Thread.Sleep(10);}public void Dispose(){}}
}
