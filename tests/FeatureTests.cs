using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using ImageNdi;

static class FeatureTests {
 public static string BrowserProofPath;
 public static void Run(Action<bool,string> check){
  var sender=new ControlledSender();var output=new Playout(sender,16,16,50);long version=0;
  try{
   check(sender.Entered.WaitOne(2000),"sender pronto al primo frame");
   var red=new byte[1024];red[2]=255;red[3]=255;output.Cue(red,0);
   check(output.ReadSentFrame(ref version)==null,"programma non mostra un frame prima dell'invio");
   sender.Allow.Set();check(sender.Entered.WaitOne(2000),"sender pronto al cut");
   var initial=output.ReadSentFrame(ref version);check(initial!=null&&initial[3]==0,"programma iniziale trasparente");
   sender.Allow.Set();check(sender.Entered.WaitOne(2000),"cut completato");
   var live=output.ReadSentFrame(ref version);check(live!=null&&live[2]==255&&live[3]==255,"programma mostra esattamente il cut inviato");
   live[3]=7;check(output.ReadSentFrame(ref version)==null,"monitor non ripubblica lo stesso frame");
   sender.Allow.Set();check(sender.Entered.WaitOne(2000),"frame successivo disponibile");
   live=output.ReadSentFrame(ref version);check(live!=null&&live[3]==255,"snapshot monitor isolato dal buffer trasmesso");
   output.Cue(new byte[1024],0);sender.Allow.Set();check(sender.Entered.WaitOne(2000),"clear pronto");sender.Allow.Set();check(sender.Entered.WaitOne(2000),"clear inviato");
   live=output.ReadSentFrame(ref version);check(live!=null&&live[3]==0,"programma segue il clear trasparente");
   output.Cue(red,2);Thread.Sleep(40);sender.Allow.Set();check(sender.Entered.WaitOne(2000),"dissolvenza in corso");sender.Allow.Set();check(sender.Entered.WaitOne(2000),"frame intermedio inviato");
   live=output.ReadSentFrame(ref version);check(live!=null&&live[3]>0&&live[3]<255&&live[2]==255,"programma mostra alpha intermedio della dissolvenza inviata");
  }finally{sender.Unblock();output.Dispose();sender.Dispose();}
  var failing=new FailingSender();using(var failed=new Playout(failing,16,16,50)){
   check(SpinWait.SpinUntil(()=>failed.Error!=null,2000),"errore sender segnalato");long sequence=0;check(failed.ReadSentFrame(ref sequence)==null,"invio fallito non appare nel programma");
  }
  string dir=Path.Combine(Path.GetTempPath(),"NDIPlayer-browser-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try{
   string file=Path.Combine(dir,"sample.png");using(var sample=new Bitmap(40,20)){using(var g=Graphics.FromImage(sample))g.Clear(Color.Coral);sample.Save(file,System.Drawing.Imaging.ImageFormat.Png);}
   using(var browser=new LibraryBrowser()){
    browser.Size=new Size(500,500);var handle=browser.Handle;browser.SetPreferences(false,128);browser.SetFiles(new List<string>{file});
    check(browser.Count==1&&browser.SelectedPath==file,"libreria seleziona il percorso originale");
    browser.SetPreferences(true,192);check(browser.ThumbnailsEnabled&&browser.ThumbnailSize==192&&browser.SelectedPath==file,"passaggio a miniature conserva selezione e dimensione");
    ListView list=null;foreach(Control control in browser.Controls)if(control is ListView)list=(ListView)control;
    var item=list.Items[0];
    var deadline=DateTime.UtcNow.AddSeconds(3);while(DateTime.UtcNow<deadline&&list.Items[0].ImageIndex==0){Application.DoEvents();Thread.Sleep(10);}
    check(list.Items[0].ImageIndex>0,"miniatura decodificata in sottofondo");
    int activated=0;browser.ImageDoubleClicked+=delegate{activated++;};
    var doubleClick=typeof(Control).GetMethod("OnMouseDoubleClick",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
    var rect=list.GetItemRect(0,ItemBoundsPortion.Label);
    doubleClick.Invoke(list,new object[]{new MouseEventArgs(MouseButtons.Left,2,rect.Left+2,rect.Top+2,0)});
    check(activated==1&&browser.SelectedPath==file,"doppio clic miniatura attiva immagine corretta");
    doubleClick.Invoke(list,new object[]{new MouseEventArgs(MouseButtons.Left,2,480,410,0)});check(activated==1,"doppio clic su area vuota non invia");
    if(BrowserProofPath!=null)using(var shot=new Bitmap(browser.Width,browser.Height)){browser.DrawToBitmap(shot,new Rectangle(0,0,shot.Width,shot.Height));shot.Save(BrowserProofPath,System.Drawing.Imaging.ImageFormat.Png);}
    browser.SetPreferences(false,256);check(!browser.ThumbnailsEnabled&&browser.SelectedPath==file,"ritorno elenco conserva selezione");
    rect=list.GetItemRect(0,ItemBoundsPortion.Label);doubleClick.Invoke(list,new object[]{new MouseEventArgs(MouseButtons.Left,2,rect.Left+2,rect.Top+2,0)});check(activated==2,"doppio clic elenco attiva immagine");
    browser.SetFiles(new List<string>());check(browser.SelectedPath==null&&browser.Count==0,"cartella vuota rimuove selezione precedente");
   }
  }finally{Directory.Delete(dir,true);}
  string catalog=Path.Combine(Path.GetTempPath(),"NDIPlayer-filter-"+Guid.NewGuid());Directory.CreateDirectory(catalog);
  try{
   var keep=new List<string>{Path.Combine(catalog,"foto.JPG"),Path.Combine(catalog,"_grafica.png"),Path.Combine(catalog,"-grafica.png")};
   foreach(string path in keep)File.WriteAllText(path,"test");
   File.WriteAllText(Path.Combine(catalog,"._foto.JPG"),"AppleDouble");File.WriteAllText(Path.Combine(catalog,".-foto.png"),"metadata");
   string hidden=Path.Combine(catalog,"nascosta.png");File.WriteAllText(hidden,"hidden");File.SetAttributes(hidden,File.GetAttributes(hidden)|FileAttributes.Hidden);
   foreach(string folder in new[]{"__MACOSX",".AppleDouble","nascosta"}){string sub=Path.Combine(catalog,folder);Directory.CreateDirectory(sub);File.WriteAllText(Path.Combine(sub,"icona.png"),"metadata");if(folder=="nascosta")File.SetAttributes(sub,File.GetAttributes(sub)|FileAttributes.Hidden);}
   string visible=Path.Combine(catalog,"visibile");Directory.CreateDirectory(visible);string nested=Path.Combine(visible,"contenuto.jpeg");File.WriteAllText(nested,"image");keep.Add(nested);
   var warnings=new List<string>();var scanned=Library.Scan(catalog,warnings);
   check(!scanned.Contains(hidden),"catalogo esclude attributo nascosto Windows");
   check(!scanned.Exists(p=>Path.GetFileName(p).StartsWith("._")||Path.GetFileName(p).StartsWith(".-")),"catalogo esclude file AppleDouble e prefisso .-");
   check(scanned.Count==keep.Count&&keep.TrueForAll(p=>scanned.Contains(p))&&warnings.Count==0,"catalogo salta cartelle nascoste e metadata Mac mantenendo immagini normali e sottocartelle");
  }finally{Directory.Delete(catalog,true);}
  check(NdiChannel.Validate("  Regia È 1  ")=="Regia È 1","nome canale personalizzato con accenti e spazi");
  bool empty=false;try{NdiChannel.Validate("   ");}catch(ArgumentException){empty=true;}check(empty,"nome canale vuoto rifiutato");
  bool invalid=true;foreach(char c in "\\/:*?\"<>|\n\0"){try{NdiChannel.Validate("Regia"+c+"1");invalid=false;}catch(ArgumentException){}}check(invalid,"caratteri NDI non validi rifiutati");
  bool tooLong=false;try{NdiChannel.Validate(new string('x',129));}catch(ArgumentException){tooLong=true;}check(tooLong,"nome canale troppo lungo rifiutato");
  var serializer=new System.Web.Script.Serialization.JavaScriptSerializer();
  check(serializer.Deserialize<Settings>("{\"Images\":{}}").ChannelName==NdiChannel.DefaultName,"vecchie preferenze mantengono nome NDI predefinito");
  var saved=serializer.Deserialize<Settings>(serializer.Serialize(new Settings{ChannelName="Regia È 1"}));check(saved.ChannelName=="Regia È 1","nome canale salvato nelle preferenze");
  string preferences=Path.Combine(Path.GetTempPath(),"NDIPlayer-name-"+Guid.NewGuid()+".json");
  try{File.WriteAllText(preferences,serializer.Serialize(saved));using(var window=new MainWindow(preferences)){var fields=window.Controls.Find("NdiChannelName",true);check(fields.Length==1&&fields[0].Text=="Regia È 1","campo nome canale caricato nell'interfaccia");}}
  finally{File.Delete(preferences);}
 }
 sealed class ControlledSender:IVideoSender {
  public readonly AutoResetEvent Entered=new AutoResetEvent(false),Allow=new AutoResetEvent(false);volatile bool released;
  public void Send(byte[] pixels,int w,int h,int fps){Entered.Set();if(!released)Allow.WaitOne();}
  public void Unblock(){released=true;Allow.Set();}
  public void Dispose(){}
 }
 sealed class FailingSender:IVideoSender {public void Send(byte[] pixels,int w,int h,int fps){throw new IOException("Test sender failure");}public void Dispose(){}}
}
