using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using ImageNdi;

static class ThumbnailLifetimeTests {
 public static void Run(Action<bool,string> check){
  string root=Path.Combine(Path.GetTempPath(),"NDIPlayer-cache-test-"+Guid.NewGuid());
  Directory.CreateDirectory(root);
  try{
   string source=Path.Combine(root,"asset.png");
   string first=ThumbnailCache.ProjectDirectory(root,Path.Combine(root,"A","Show.ndiproj"),null);
   string second=ThumbnailCache.ProjectDirectory(root,Path.Combine(root,"B","Show.ndiproj"),null);
   check(first!=second,"progetti omonimi usano cache separate");
   check(ThumbnailCache.ProjectDirectory(root,null,"")==ThumbnailCache.ProjectDirectory(root,null,null),"progetto locale senza sorgente supporta le vecchie preferenze");
   WriteSource(source,Color.Red);
   string key=ThumbnailCache.Key(source,128);
   string png=Path.Combine(first,key+".png");
   using(var thumb=ThumbnailCache.Load(first,source,128,key,false))
    check(thumb!=null&&thumb.GetPixel(64,64).R==255&&thumb.GetPixel(64,1).A==0&&File.Exists(png),"cache fredda salva il PNG con bande trasparenti");
   var lastWrite=File.GetLastWriteTimeUtc(png).AddHours(-1);
   File.SetLastWriteTimeUtc(png,lastWrite);
   lastWrite=File.GetLastWriteTimeUtc(png);
   using(var thumb=ThumbnailCache.Load(first,source,128,key,false))
    check(thumb.GetPixel(64,64).R==255&&File.GetLastWriteTimeUtc(png)==lastWrite,"cache calda riusa il PNG persistente");
   File.WriteAllText(png,"PNG danneggiato");
   using(var thumb=ThumbnailCache.Load(first,source,128,key,false))
    check(thumb!=null&&new FileInfo(png).Length>20,"un PNG di cache danneggiato viene ricreato");
   check(ThumbnailCache.Key(source,192)!=key,"dimensioni diverse producono chiavi distinte");
   WriteSource(source,Color.Blue);
   File.SetLastWriteTimeUtc(source,DateTime.UtcNow.AddMinutes(-10));
   string changedKey=ThumbnailCache.Key(source,128);
   using(var thumb=ThumbnailCache.Load(second,source,128,changedKey,false))
    check(changedKey!=key&&thumb.GetPixel(64,64).B==255,"una sorgente modificata genera una nuova miniatura");

   using(var worker=new ThumbnailWorker(delegate{})){
    worker.Configure(first,128,new[]{source});
    worker.Request(source,null);
    check(worker.Clear().Wait(5000),"svuotamento serializzato con il worker");
    check(Directory.GetFiles(first,"*.png").Length==0&&Directory.GetFiles(second,"*.png").Length==1&&File.Exists(source),"svuotamento limitato ai PNG del progetto");
   }

   string background=Path.Combine(root,"prefetch"),other=Path.Combine(root,"other.png");
   WriteSource(other,Color.Green);
   string backgroundPng=Path.Combine(background,ThumbnailCache.Key(other,128)+".png");
   ThumbnailWorker prefetchWorker=null;
   using(var visibleReady=new ManualResetEventSlim()){
    prefetchWorker=new ThumbnailWorker(delegate{
     using(var ready=prefetchWorker.TakeResult())if(ready!=null&&ready.Image!=null)visibleReady.Set();
    });
    using(prefetchWorker){
     prefetchWorker.Configure(background,128,new[]{other});
     prefetchWorker.Request(source,null);
     check(visibleReady.Wait(5000)&&!File.Exists(backgroundPng),"lavoro su richiesta disponibile anche durante la sospensione del precache");
     prefetchWorker.SetPrefetchAllowed(true);
     check(SpinWait.SpinUntil(()=>File.Exists(backgroundPng),5000),"a player inattivo il worker prepara il resto della libreria");
     check(prefetchWorker.Clear().Wait(5000),"svuotamento completa dopo il precache");
    }
   }

   using(var browser=new LibraryBrowser()){
    var handle=browser.Handle;
    browser.SetThumbnailPlayerIdle(false);
    browser.SetThumbnailLibrary(first,new[]{source});
    browser.SetPreferences(true,128);
    browser.SetFiles(new List<string>{source});
    check(WaitUntil(()=>Retrieve(browser).ImageIndex>0),"il browser carica la miniatura con il worker");
    int index=Retrieve(browser).ImageIndex;
    browser.SetFiles(new List<string>{source});
    check(Retrieve(browser).ImageIndex==index,"il refresh della vista conserva la miniatura in RAM");
    browser.SetPreferences(true,192);
    check(Retrieve(browser).ImageIndex==0&&WaitUntil(()=>Retrieve(browser).ImageIndex>0),"il ridimensionamento rigenera la miniatura con il worker persistente");
    browser.Dispose();
    check(Retrieve(browser)!=null,"richiesta virtuale durante chiusura resta sicura");
   }
  }finally{Directory.Delete(root,true);}
 }

 static ListViewItem Retrieve(LibraryBrowser browser){
  var request=new RetrieveVirtualItemEventArgs(0);
  typeof(LibraryBrowser).GetMethod("Retrieve",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(browser,new object[]{null,request});
  return request.Item;
 }

 static bool WaitUntil(Func<bool> condition){
  return SpinWait.SpinUntil(delegate{Application.DoEvents();return condition();},5000);
 }

 static void WriteSource(string path,Color color){
  using(var image=new Bitmap(16,8)){
   using(var graphics=Graphics.FromImage(image))graphics.Clear(color);
   image.Save(path,ImageFormat.Png);
  }
 }
}
