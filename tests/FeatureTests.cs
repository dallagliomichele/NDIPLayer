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
    if(BrowserProofPath!=null)using(var shot=new Bitmap(browser.Width,browser.Height)){browser.DrawToBitmap(shot,new Rectangle(0,0,shot.Width,shot.Height));shot.Save(BrowserProofPath,System.Drawing.Imaging.ImageFormat.Png);}
    browser.SetPreferences(false,256);check(!browser.ThumbnailsEnabled&&browser.SelectedPath==file,"ritorno elenco conserva selezione");
    browser.SetFiles(new List<string>());check(browser.SelectedPath==null&&browser.Count==0,"cartella vuota rimuove selezione precedente");
   }
  }finally{Directory.Delete(dir,true);}
 }
 sealed class ControlledSender:IVideoSender {
  public readonly AutoResetEvent Entered=new AutoResetEvent(false),Allow=new AutoResetEvent(false);volatile bool released;
  public void Send(byte[] pixels,int w,int h,int fps){Entered.Set();if(!released)Allow.WaitOne();}
  public void Unblock(){released=true;Allow.Set();}
  public void Dispose(){}
 }
 sealed class FailingSender:IVideoSender {public void Send(byte[] pixels,int w,int h,int fps){throw new IOException("Test sender failure");}public void Dispose(){}}
}
