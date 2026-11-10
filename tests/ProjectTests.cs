using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using ImageNdi;

static class ProjectTests {
 public static string ProofPath;
 static object Get(object target,string field){return typeof(MainWindow).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);}
 static void Set(object target,string field,object value){typeof(MainWindow).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);}
 static object Call(object target,string method,params object[] args){return typeof(MainWindow).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);}
 public static void Run(Action<bool,string> check){
  var placement=new WindowPreferences{MonitorLayout="same",Bounds=new System.Drawing.Rectangle(-1800,100,1400,800),Maximized=true};check(WindowPlacement.CanRestore(placement,"same")&&!WindowPlacement.CanRestore(placement,"changed"),"posizione finestra ripristinata solo con stessa configurazione monitor");check(!WindowPlacement.CanRestore(new WindowPreferences(),"same"),"vecchie preferenze finestra senza geometria mantengono apertura predefinita");check(WindowPlacement.VisibleBounds(placement.Bounds,new System.Drawing.Rectangle(-1920,0,1920,1080))==placement.Bounds,"coordinate negative su monitor secondario conservate");check(WindowPlacement.VisibleBounds(new System.Drawing.Rectangle(4000,3000,1600,900),new System.Drawing.Rectangle(0,0,1280,720))==new System.Drawing.Rectangle(0,0,1280,720),"posizione non visibile e dimensioni eccessive ricondotte allo schermo");
  string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"project-test-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try{
   string assets=Path.Combine(dir,"assets");Directory.CreateDirectory(assets);var paths=new List<string>();for(int i=0;i<30;i++){string p=Path.Combine(assets,"Foto "+i.ToString("D2")+".png");using(var bmp=new Bitmap(8,8)){using(var g=Graphics.FromImage(bmp))g.Clear(Color.FromArgb(255,i+10,20,30));bmp.Save(p,System.Drawing.Imaging.ImageFormat.Png);}paths.Add(p);}
   var settings=new Settings{Root=assets,ChannelName="Regia Progetto",OutputWidth=128,OutputHeight=72,OutputFps=50,Dissolve=true,FadeMs=300};
   for(int b=0;b<5;b++){var block=ProjectStore.AddBlock(settings,"Blocco "+(b+1));ProjectStore.AddFiles(block,paths.Skip(b*6).Take(6));}
   check(settings.Blocks.Count==5&&settings.Blocks.All(b=>b.Files.Count==6),"30 foto organizzate in cinque blocchi da sei");
   var first=settings.Blocks[0];ProjectStore.AddFiles(first,new[]{paths[0],paths[6]});check(first.Files.Count==7&&settings.Blocks[1].Files.Contains(paths[6]),"foto condivisa fra blocchi senza duplicati nello stesso blocco");
   ProjectStore.MoveFiles(first,new[]{paths[0],paths[1]},paths[4]);check(first.Files[0]==paths[2]&&first.Files[2]==paths[0]&&first.Files[3]==paths[1],"riordino multiplo mantiene ordine relativo");
   check(ProjectStore.Visible(settings,paths,first.Id,"foto 06").SequenceEqual(new[]{paths[6]}),"ricerca case insensitive limitata al blocco");
   check(ProjectStore.Visible(settings,paths,null,"Foto 2").Count==10,"ricerca nell'intera libreria");
   settings.Images[paths[0]]=new Placement{X=100,Y=-20,Scale=.75};settings.ActiveBlockId=first.Id;string project=Path.Combine(dir,"show.ndiproj");ProjectStore.Write(project,settings);
   check(!File.ReadAllText(project).Contains(dir.Replace("\\","\\\\")),"progetto memorizza percorsi relativi e non duplica immagini");
   var restored=ProjectStore.Read(project);check(restored.Blocks.Count==5&&restored.Blocks[0].Files.SequenceEqual(first.Files)&&restored.Images[paths[0]].X==100&&restored.ChannelName=="Regia Progetto"&&restored.OutputWidth==128,"salva/carica conserva blocchi ordine trasformazioni e formato");
   string moved=Path.Combine(dir,"postazione2");Directory.CreateDirectory(moved);string movedProject=Path.Combine(moved,"show.ndiproj");File.Copy(project,movedProject);var portable=ProjectStore.Read(movedProject);check(portable.Root==Path.Combine(moved,"assets")&&portable.Blocks[0].Files.All(p=>p.StartsWith(moved,StringComparison.OrdinalIgnoreCase)),"progetto copiato risolve immagini rispetto alla nuova cartella");
   ProjectStore.Write(project,restored);check(File.Exists(project+".bak"),"salvataggio sostitutivo conserva backup precedente");
   string relocated=Path.Combine(dir,"altro");ProjectStore.Relocate(restored,relocated);check(restored.Root==relocated&&restored.Images.ContainsKey(Path.Combine(relocated,"Foto 00.png"))&&restored.Blocks[1].Files[0]==Path.Combine(relocated,"Foto 06.png"),"ricollegamento cartella aggiorna playlist e posizioni insieme");
   string bad=Path.Combine(dir,"bad.ndiproj");File.WriteAllText(bad,"{\"Version\":99,\"Data\":{}}");bool rejected=false;try{ProjectStore.Read(bad);}catch(InvalidDataException){rejected=true;}check(rejected,"versione progetto sconosciuta rifiutata");
   using(var browser=new LibraryBrowser()){
    browser.Size=new Size(520,500);var handle=browser.Handle;browser.SetPreferences(false,128);browser.SetFiles(paths.Take(3).ToList());browser.SelectPaths(new[]{paths[1],paths[2]});browser.SetOnAir(new OnAirStatus{Paths=new[]{paths[0]}});
    check(browser.SelectedPaths.Count==2&&browser.IndicatorsFor(paths[0])=="IN ONDA"&&browser.IndicatorsFor(paths[1])=="SELEZIONATA","indicatori distinguono selezione multipla e immagine in onda");
    browser.SelectPaths(new[]{paths[0]});check(browser.IndicatorsFor(paths[0]).Contains("SELEZIONATA")&&browser.IndicatorsFor(paths[0]).Contains("IN ONDA"),"stessa immagine può essere selezionata e in onda");
    if(ProofPath!=null){using(var proof=new Bitmap(1040,500)){using(var g=Graphics.FromImage(proof)){
     using(var detail=new Bitmap(520,500)){browser.DrawToBitmap(detail,new Rectangle(0,0,520,500));g.DrawImageUnscaled(detail,0,0);}
     browser.SetPreferences(true,128);ListView native=null;foreach(Control child in browser.Controls)if(child is ListView)native=(ListView)child;var firstItem=native.Items[0];var deadline=DateTime.UtcNow.AddSeconds(3);while(native.Items[0].ImageIndex==0&&DateTime.UtcNow<deadline){Application.DoEvents();Thread.Sleep(10);}
     using(var thumb=new Bitmap(520,500)){browser.DrawToBitmap(thumb,new Rectangle(0,0,520,500));g.DrawImageUnscaled(thumb,520,0);}
    }proof.Save(ProofPath,System.Drawing.Imaging.ImageFormat.Png);}}
    browser.SetFiles(new List<string>{paths[1]});check(browser.IndicatorsFor(paths[0])=="IN ONDA","filtrare la libreria non cancella stato in onda");
    browser.SetOnAir(new OnAirStatus{Paths=new[]{paths[0],paths[1]},Transitioning=true});check(browser.IndicatorsFor(paths[0])=="IN DISSOLVENZA"&&browser.IndicatorsFor(paths[1]).Contains("IN DISSOLVENZA"),"due indicatori durante dissolvenza");
    browser.SetOnAir(new OnAirStatus());check(!browser.IndicatorsFor(paths[0]).Contains("ONDA"),"clear rimuove indicatori di programma");
   }
   var probe=new Probe();using(var output=new Playout(probe,128,72,50)){
    byte[] red=new byte[128*72*4];red[3]=255;output.Cue(red,0,paths[0]);check(SpinWait.SpinUntil(()=>output.ReadOnAirStatus().Paths.Contains(paths[0]),2000),"indicatore segue primo frame inviato");
    output.Cue(red,.5,paths[1]);check(SpinWait.SpinUntil(()=>output.ReadOnAirStatus().Paths.Length==2,2000),"fade pubblica entrambi i contributi");
    output.Cue(red,.5,paths[2]);check(SpinWait.SpinUntil(()=>output.ReadOnAirStatus().Paths.Length==3,2000),"fade interrotta mantiene tutti i contributi visibili");
    check(SpinWait.SpinUntil(()=>{var s=output.ReadOnAirStatus();return !s.Transitioning&&s.Paths.SequenceEqual(new[]{paths[2]});},2000),"fine fade lascia solo immagine destinazione");
    output.Cue(new byte[red.Length],.1);check(SpinWait.SpinUntil(()=>!output.ReadOnAirStatus().Transitioning&&output.ReadOnAirStatus().Paths.Length==0,2000),"clear fade elimina indicatori dopo il frame trasparente");
   }
   string prefs=Path.Combine(dir,"prefs.json");File.WriteAllText(prefs,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(settings));
   using(var window=new MainWindow(prefs)){
    var browser=(LibraryBrowser)Get(window,"files");var handle=browser.Handle;foreach(Control child in browser.Controls)if(child is ListView){var native=child.Handle;}Call(window,"Scan");
    var probe2=new Probe();var output=new Playout(probe2,128,72,50);try{
     Set(window,"outW",128);Set(window,"outH",72);Set(window,"output",output);output.Cue(new byte[128*72*4],0,paths[0]);check(SpinWait.SpinUntil(()=>output.ReadOnAirStatus().Paths.Contains(paths[0]),2000),"prova integrazione ha una foto già in onda");
     var list=(ListBox)Get(window,"blockList");list.SelectedIndex=2;((TextBox)Get(window,"search")).Text="Foto 07";
     check(output.ReadOnAirStatus().Paths.SequenceEqual(new[]{paths[0]})&&browser.Count==1&&browser.SelectedPath==paths[7],"cambio blocco e ricerca aggiornano preview senza cambiare programma");
     var loaded=ProjectStore.Read(project);loaded.OutputWidth=1920;loaded.OutputHeight=1080;loaded.ChannelName="Altro canale";Call(window,"ApplyProject",loaded,project);
     check(Object.ReferenceEquals(Get(window,"output"),output)&&output.ReadOnAirStatus().Paths.SequenceEqual(new[]{paths[0]})&&loaded.OutputWidth==1920,"caricare progetto con altro formato conserva sender e immagine in onda");
     check((int)typeof(MainWindow).GetProperty("FrameWidth",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window,null)==128,"preview usa dimensione attiva mentre formato nuovo è differito");
    }finally{Set(window,"output",null);output.Dispose();}
   }
   string splitPrefs=Path.Combine(dir,"split-test.json");var savedWindow=new WindowPreferences{MonitorLayout=WindowPlacement.MonitorLayout(),Bounds=new System.Drawing.Rectangle(0,0,1400,850),LibrarySplitterDistance=410};File.WriteAllText(splitPrefs+".window.json",new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(savedWindow));
   using(var window=new MainWindow(splitPrefs)){Call(window,"RestoreWindowPreferences");var split=(SplitContainer)Get(window,"librarySplit");check(split.SplitterDistance==410,"divisorio libreria ripristinato dalle preferenze locali");var pv=(Control)Get(window,"preview");var pgm=(Control)Get(window,"program");var monitors=(TableLayoutPanel)pv.Parent.Parent;check(monitors.ColumnCount==1&&monitors.GetRow(pv.Parent)==0&&monitors.GetRow(pgm.Parent)==1,"monitor verticali con Preview sopra e Program sotto");}
   savedWindow.LibrarySplitterDistance=100000;File.WriteAllText(splitPrefs+".window.json",new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(savedWindow));using(var window=new MainWindow(splitPrefs)){Call(window,"RestoreWindowPreferences");var split=(SplitContainer)Get(window,"librarySplit");check(split.SplitterDistance<=split.ClientSize.Width-split.SplitterWidth-split.Panel2MinSize,"divisorio salvato fuori intervallo limitato allo spazio disponibile");}
   string windowPrefs=Path.Combine(dir,"window-test.json");
   using(var window=new MainWindow(windowPrefs)){var toggle=(CheckBox)window.Controls.Find("AlwaysOnTop",true)[0];toggle.Checked=true;check(window.TopMost,"checkbox mantiene finestra in primo piano");Call(window,"ApplyProject",new Settings(),null);check(window.TopMost&&toggle.Checked,"aprire o creare progetto mantiene preferenza finestra");}
   using(var window=new MainWindow(windowPrefs)){check(window.TopMost,"preferenza primo piano ripristinata al riavvio");((CheckBox)window.Controls.Find("AlwaysOnTop",true)[0]).Checked=false;check(!window.TopMost,"checkbox disattivata libera finestra");}
  }finally{Directory.Delete(dir,true);}
 }
 sealed class Probe:IVideoSender {public void Send(byte[] frame,int w,int h,int fps){Thread.Sleep(10);}public void Dispose(){}}
}
