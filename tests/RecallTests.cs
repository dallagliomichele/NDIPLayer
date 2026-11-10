using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ImageNdi;
static class RecallTests {
 static object Get(object o,string n){return typeof(MainWindow).GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static object Call(object o,string n,params object[] a){return typeof(MainWindow).GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,a);}
 sealed class Probe:IVideoSender {public void Send(byte[] p,int w,int h,int f){System.Threading.Thread.Sleep(5);}public void Dispose(){}}
 public static void Run(Action<bool,string> check){
  var automatic=new Settings();ImageNumbers.AutoNumber(automatic,new[]{"a.png"});check(automatic.ImageNumbers["a.png"]=="1","numerazione automatica primo file parte da uno");ImageNumbers.AutoNumber(automatic,new[]{"c.mov","b.png"});check(automatic.ImageNumbers["c.mov"]=="2"&&automatic.ImageNumbers["b.png"]=="3","selezione multipla numerata nell'ordine fornito dal browser");ImageNumbers.AutoNumber(automatic,new[]{"b.png","c.mov"});check(automatic.ImageNumbers["b.png"]=="2"&&automatic.ImageNumbers["c.mov"]=="3","file selezionati gia numerati rinumerati ignorando i loro vecchi numeri");var autoBlock=ProjectStore.AddBlock(automatic,"Indipendente");ProjectStore.AddFiles(autoBlock,new[]{"b.png","c.mov"});automatic.ActiveBlockId=autoBlock.Id;ImageNumbers.AutoNumber(automatic,new[]{"c.mov","b.png"});check(autoBlock.Numbers["c.mov"]=="1"&&automatic.ImageNumbers["c.mov"]=="3","numerazione automatica isolata al blocco corrente");autoBlock.Numbers["c.mov"]="999999999";bool overflow=false;try{ImageNumbers.AutoNumber(automatic,new[]{"b.png"});}catch(ArgumentException){overflow=true;}check(overflow&&autoBlock.Numbers["b.png"]=="2","esaurimento numeri non modifica parzialmente la selezione");
  var explicitStart=new Settings();ImageNumbers.Set(explicitStart,"untouched.png","5");ImageNumbers.AutoNumber(explicitStart,new[]{"z.mov","a.png"},20);check(explicitStart.ImageNumbers["z.mov"]=="20"&&explicitStart.ImageNumbers["a.png"]=="21"&&explicitStart.ImageNumbers["untouched.png"]=="5","numero iniziale scelto segue ordine selezione e mantiene gli altri numeri");bool collision=false;try{ImageNumbers.AutoNumber(explicitStart,new[]{"z.mov","a.png"},4);}catch(ArgumentException){collision=true;}check(collision&&explicitStart.ImageNumbers["z.mov"]=="20"&&explicitStart.ImageNumbers["a.png"]=="21","conflitto nella sequenza scelta non applica modifiche parziali");var explicitBlock=ProjectStore.AddBlock(explicitStart,"Altro");ProjectStore.AddFiles(explicitBlock,new[]{"z.mov","a.png"});explicitStart.ActiveBlockId=explicitBlock.Id;ImageNumbers.AutoNumber(explicitStart,new[]{"a.png","z.mov"},20);check(explicitBlock.Numbers["a.png"]=="20"&&explicitStart.ImageNumbers["a.png"]=="21","numero iniziale scelto resta separato per blocco");
  string dir=Path.Combine(Path.GetTempPath(),"recall-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try{
   string a=Path.Combine(dir,"a.png"),b=Path.Combine(dir,"b.png");using(var image=new Bitmap(8,8)){using(var g=Graphics.FromImage(image))g.Clear(Color.Coral);image.Save(a);image.Save(b);}
   var data=new Settings{Root=dir,OutputWidth=16,OutputHeight=16};var first=ProjectStore.AddBlock(data,"Uno");var second=ProjectStore.AddBlock(data,"Due");ProjectStore.AddFiles(first,new[]{a,b});ProjectStore.AddFiles(second,new[]{a,b});ProjectStore.Normalize(data);
   data.ActiveBlockId=first.Id;ImageNumbers.Set(data,a,"01");ImageNumbers.Set(data,b,"12");data.ActiveBlockId=second.Id;ImageNumbers.Set(data,a,"12");ImageNumbers.Set(data,b,"01");
   check(ImageNumbers.Find(data,second.Files,"0012").SequenceEqual(new[]{a}),"richiamo numerico ignora zeri iniziali e usa blocco attivo");
   bool duplicate=false;try{ImageNumbers.Set(data,b,"0012");}catch(ArgumentException){duplicate=true;}check(duplicate&&second.Numbers[b]=="01","duplicati equivalenti rifiutati nello stesso blocco");
   string project=Path.Combine(dir,"show.ndiproj");ProjectStore.Write(project,data);var loaded=ProjectStore.Read(project);check(loaded.Blocks[0].Numbers[a]=="01"&&loaded.Blocks[1].Numbers[a]=="12","stessa foto conserva numeri diversi dopo salvataggio");
   ProjectStore.Relocate(loaded,Path.Combine(dir,"new"));check(loaded.Blocks[1].Numbers[Path.Combine(dir,"new","a.png")]=="12","ricollegamento conserva numeri per blocco");
   var legacy=new Settings();var old=ProjectStore.AddBlock(legacy,"Vecchio");ProjectStore.AddFiles(old,new[]{a,b});legacy.ImageNumbers[a]="07";legacy.ImageNumbers[b]="007";ProjectStore.Normalize(legacy);check(old.Numbers.Count==2&&legacy.ImageNumbers[a]=="07","migrazione conserva vecchi numeri anche duplicati");legacy.ActiveBlockId=old.Id;check(ImageNumbers.Find(legacy,old.Files,"7").Count==2,"duplicati storici segnalati come ambigui");old.Numbers.Remove(a);ProjectStore.Normalize(legacy);check(!old.Numbers.ContainsKey(a),"numero rimosso non ricompare dopo migrazione");
   string prefs=Path.Combine(dir,"prefs.json");File.WriteAllText(prefs,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(data));
   using(var window=new MainWindow(prefs)){
    var browser=(LibraryBrowser)Get(window,"files");var handle=browser.Handle;foreach(Control c in browser.Controls)if(c is ListView){var h=c.Handle;}
    Call(window,"Scan");var input=(TextBox)Get(window,"recallNumber");input.Text="12";check((string)Get(window,"selected")==a&&Get(window,"output")==null,"numero seleziona anteprima senza avviare Program");
    var stream=new Playout(new Probe(),16,16,50);typeof(MainWindow).GetField("output",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,stream);typeof(MainWindow).GetField("outW",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,16);typeof(MainWindow).GetField("outH",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,16);
    stream.Cue(Renderer.Render(b,16,16,new Placement()),0,b);System.Threading.SpinWait.SpinUntil(()=>stream.ReadOnAirStatus().Paths.Contains(b),2000);
    input.Text="12";check(stream.ReadOnAirStatus().Paths.SequenceEqual(new[]{b}),"digitare numero lascia invariato Program già in onda");
    Call(window,"TryProcessRecallKey",Keys.Enter,input,false);check(System.Threading.SpinWait.SpinUntil(()=>stream.ReadOnAirStatus().Paths.SequenceEqual(new[]{a}),2000)&&input.Text=="","Invio manda la foto richiamata in onda e prepara nuovo richiamo");
    input.Text="99";Call(window,"TryProcessRecallKey",Keys.Enter,input,false);check(stream.ReadOnAirStatus().Paths.SequenceEqual(new[]{a}),"Invio con numero inesistente non cambia Program");
    input.Text="99";check(!(bool)Call(window,"ResolveNumberRecall"),"numero assente non abilita invio della foto precedente");
    check(!(bool)Call(window,"TryProcessRecallKey",Keys.D1,new TextBox(),false),"cifre nei campi di testo restano normali modifiche");
    check(!(bool)Call(window,"TryProcessRecallKey",Keys.Enter,new NumericUpDown(),false),"Invio sui valori numerici non manda foto in onda");
    input.Clear();check((bool)Call(window,"TryProcessRecallKey",Keys.NumPad1,browser,false)&&input.Text=="1","tastierino numerico avvia richiamo");
    data=(Settings)Get(window,"settings");data.ActiveBlockId=first.Id;Call(window,"RefreshVisible");check(input.Text=="","cambio vista o blocco azzera numero in corso");
    ((TextBox)Get(window,"search")).Text="nessunrisultato";input.Text="12";check((string)Get(window,"selected")==b&&((TextBox)Get(window,"search")).Text=="","richiamo trova foto nel blocco anche con ricerca attiva");
   }
  }finally{Directory.Delete(dir,true);}
 }
}

