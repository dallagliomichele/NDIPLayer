using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using ImageNdi;

static class StopAndNumberTests {
 static object Get(object target,string name){return typeof(MainWindow).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);}
 static void Put(object target,string name,object value){typeof(MainWindow).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);}
 static object Call(object target,string name,params object[] args){return typeof(MainWindow).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,args);}
 public static void Run(Action<bool,string> check,bool native){
  var record=new RecordingSender();using(var stream=new Playout(record,16,16,50)){var photo=new byte[1024];photo[3]=255;stream.Cue(photo,0,"A.png");check(SpinWait.SpinUntil(()=>record.HasPhoto,2000),"prova arresto parte da una foto visibile");stream.Cue(photo,10,"B.png");bool clean=stream.StopTransparent();check(clean&&record.Disposed&&record.LastThreeClear,"mittente riceve solo trasparenza nei frame finali prima della distruzione");check(!stream.ReadOnAirStatus().Transitioning&&stream.ReadOnAirStatus().Paths.Length==0,"arresto durante fade completa clear e indicatori vuoti");int count=record.Count;stream.Cue(photo,0,"C.png");check(record.Count==count&&stream.StopTransparent(),"arresto idempotente e nessuna foto successiva ai frame trasparenti");}
  using(var failed=new Playout(new FailedSender(),16,16,50)){SpinWait.SpinUntil(()=>failed.Error!=null,2000);check(!failed.StopTransparent(),"errore mittente non viene dichiarato clear riuscito");}
  check(ImageNumbers.Normalize(" 001 ")=="001","numero fittizio conserva zeri iniziali");bool invalid=false;try{ImageNumbers.Normalize("12A");}catch(ArgumentException){invalid=true;}check(invalid,"numero non numerico rifiutato");
  string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"numbers-test-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try{string a=Path.Combine(dir,"A.png"),b=Path.Combine(dir,"B.png");foreach(var path in new[]{a,b})using(var bitmap=new Bitmap(8,8)){using(var g=Graphics.FromImage(bitmap))g.Clear(Color.Coral);bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}
   var data=new Settings{Root=dir,OutputWidth=16,OutputHeight=16};var block=ProjectStore.AddBlock(data,"Relatore");ImageNumbers.Set(data,b,"023");string project=Path.Combine(dir,"show.ndiproj");ProjectStore.Write(project,data);var loaded=ProjectStore.Read(project);check(loaded.ImageNumbers[b]=="023"&&File.Exists(b),"numero salvato nel progetto senza rinominare il file");check(ProjectStore.Visible(loaded,new[]{a,b},null,"023").SequenceEqual(new[]{b}),"ricerca trova anche il numero assegnato");ProjectStore.Relocate(loaded,Path.Combine(dir,"nuovo"));check(loaded.ImageNumbers[Path.Combine(dir,"nuovo","B.png")]=="023","ricollegamento cartella mantiene numeri associati");
   string prefs=Path.Combine(dir,"prefs.json");File.WriteAllText(prefs,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(data));var programSender=new RecordingSender();var previewSender=new RecordingSender();
   using(var window=new MainWindow(prefs)){
    var pgm=new Playout(programSender,16,16,50);var pv=new Playout(previewSender,16,16,50);Put(window,"output",pgm);Put(window,"outW",16);Put(window,"outH",16);Put(window,"previewOutput",pv);Put(window,"previewW",16);Put(window,"previewH",16);Put(window,"previewFps",50);Call(window,"BindCompanion",pgm);
    var browser=(LibraryBrowser)Get(window,"files");var handle=browser.Handle;ListView view=null;foreach(Control c in browser.Controls)if(c is ListView){view=(ListView)c;var listHandle=c.Handle;}Call(window,"Scan");var photo=Renderer.Render(a,16,16,new Placement());pgm.Cue(photo,0,a);SpinWait.SpinUntil(()=>pgm.ReadOnAirStatus().Paths.Contains(a),2000);
    var rect=view.GetItemRect(1,ItemBoundsPortion.Label);typeof(Control).GetMethod("OnMouseDown",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{new MouseEventArgs(MouseButtons.Right,1,rect.Left+2,rect.Top+2,0)});
    var cancel=new CancelEventArgs();typeof(ToolStripDropDown).GetMethod("OnOpening",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view.ContextMenuStrip,new object[]{cancel});
    check(!cancel.Cancel&&browser.SelectedPath==b&&pgm.ReadOnAirStatus().Paths.SequenceEqual(new[]{a}),"tasto destro seleziona foto corretta senza mandarla in onda");
    var add=(ToolStripMenuItem)view.ContextMenuStrip.Items["AddToBlock"];var target=add.DropDownItems.Cast<ToolStripItem>().First(i=>(string)i.Tag==block.Id);target.PerformClick();var current=(Settings)Get(window,"settings");check(current.Blocks[0].Files.SequenceEqual(new[]{b})&&pgm.ReadOnAirStatus().Paths.SequenceEqual(new[]{a}),"menu contestuale aggiunge foto al blocco senza modificare Program");
    Call(window,"SetImageNumber",b,"007");check(browser.NumberFor(b)=="007"&&browser.DisplayLabel(b).StartsWith("#007")&&File.Exists(b),"numero indipendente visibile senza modificare il percorso foto");
    browser.SetPreferences(true,128);check(browser.DisplayLabel(b).Contains("007"),"numero mantenuto passando alle miniature");
    rect=view.GetItemRect(1,ItemBoundsPortion.Label);typeof(Control).GetMethod("OnMouseDown",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{new MouseEventArgs(MouseButtons.Right,1,rect.Left+2,rect.Top+2,0)});cancel=new CancelEventArgs();typeof(ToolStripDropDown).GetMethod("OnOpening",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view.ContextMenuStrip,new object[]{cancel});check(!cancel.Cancel&&view.ContextMenuStrip.Items["EditImageNumber"]!=null,"menu numero disponibile anche con tasto destro sulle miniature");
    view.ContextMenuStrip.Items["RemoveImageNumber"].PerformClick();check(!current.ImageNumbers.ContainsKey(b),"menu contestuale rimuove numero senza rimuovere foto");
    view.ContextMenuStrip.Items["AutoNumberImages"].PerformClick();check(view.ContextMenuStrip.Items["NumberImagesFrom"]!=null&&current.ImageNumbers[b]=="1"&&pgm.ReadOnAirStatus().Paths.SequenceEqual(new[]{a}),"menu numerazione automatica assegna numero senza cambiare Program");
    Call(window,"NumberImagesFrom",new string[]{b,a},40);check(current.ImageNumbers[b]=="40"&&current.ImageNumbers[a]=="41"&&pgm.ReadOnAirStatus().Paths.SequenceEqual(new[]{a}),"numero iniziale dalla UI mantiene ordine dei file e Program invariato");
    check(SpinWait.SpinUntil(()=>previewSender.HasPhoto,2000),"anteprima di prova resta popolata prima dell'arresto Program");Call(window,"Toggle");check(programSender.LastThreeClear&&programSender.Disposed&&!previewSender.Disposed,"arresto Program pulisce il suo mittente e lascia Preview acceso");
    Call(window,"StopPreviewOutput");check(previewSender.LastThreeClear&&previewSender.Disposed,"arresto Preview pulisce anche il suo mittente prima della chiusura");
   }
   var closeProgram=new RecordingSender();var closePreview=new RecordingSender();using(var window=new MainWindow(prefs)){var pgm=new Playout(closeProgram,16,16,50);var pv=new Playout(closePreview,16,16,50);Put(window,"output",pgm);Put(window,"outW",16);Put(window,"outH",16);Put(window,"previewOutput",pv);Put(window,"previewW",16);Put(window,"previewH",16);var photo=new byte[1024];photo[3]=255;pgm.Cue(photo,0,a);pv.Cue(photo,0,b);SpinWait.SpinUntil(()=>closeProgram.HasPhoto&&closePreview.HasPhoto,2000);}check(closeProgram.LastThreeClear&&closePreview.LastThreeClear,"chiusura app invia clear finale su entrambi i canali");
  }finally{Directory.Delete(dir,true);}
  if(native){using(var stream=new Playout(new NdiSender("NDIPlayer TEST Stop Clear"),16,16,50)){var photo=new byte[1024];photo[3]=255;stream.Cue(photo,0,"A.png");SpinWait.SpinUntil(()=>stream.Frames>1,2000);check(stream.StopTransparent(),"runtime NDI reale invia trasparenza prima di arrestarsi");}}
 }
 sealed class RecordingSender:IVideoSender {readonly object gate=new object();readonly List<bool> history=new List<bool>();public volatile bool Disposed,HasPhoto;public int Count{get{lock(gate)return history.Count;}}public bool LastThreeClear{get{lock(gate)return history.Count>=3&&history.Skip(history.Count-3).All(clear=>clear);}}public void Send(byte[] p,int w,int h,int fps){bool clear=p.All(v=>v==0);lock(gate)history.Add(clear);if(!clear)HasPhoto=true;Thread.Sleep(5);}public void Dispose(){Disposed=true;}}
 sealed class FailedSender:IVideoSender {public void Send(byte[] p,int w,int h,int fps){throw new IOException("Errore di prova");}public void Dispose(){}}
}
