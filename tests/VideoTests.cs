using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using ImageNdi;
static class VideoTests {
 static object Get(object o,string n){return typeof(MainWindow).GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static void Put(object o,string n,object v){typeof(MainWindow).GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);}
 static object Call(object o,string n,params object[] a){return typeof(MainWindow).GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,a);}
 sealed class Probe:IVideoSender {public void Send(byte[] p,int w,int h,int f){Thread.Sleep(20);}public void Dispose(){}}
 public static void Run(Action<bool,string> check,bool native){
  string dir=Path.Combine(Path.GetTempPath(),"ndi-mov-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try{
   string path=Path.Combine(dir,"clip con spazi è.mov");
   using(var make=VideoTools.Start("-f lavfi -i \"color=c=red@0.5:s=32x16:r=10:d=0.4,format=argb\" -an -c:v qtrle -y "+VideoTools.Quote(path))){var error=make.StandardError.ReadToEnd();make.WaitForExit();check(make.ExitCode==0,"genera MOV reale con canale alpha per prove decoder");}
   check(Library.Scan(dir,new List<string>()).Contains(path),"browser include MOV e conserva filtri catalogo");
   using(var poster=Renderer.LoadBitmap(path))check(poster.Width==640&&poster.Height==320&&poster.GetPixel(320,160).A>=120&&poster.GetPixel(320,160).A<=130,"poster MOV mantiene proporzioni e alpha");
   foreach(string codec in new[]{"libopenh264","prores_ks"}){
    string clip=Path.Combine(dir,codec+".mov");string options=codec=="libopenh264"?"-pix_fmt yuv420p -b:v 500k":"-profile:v 4 -alpha_bits 16 -pix_fmt yuva444p10le";
    using(var make=VideoTools.Start("-f lavfi -i \"color=c=red@0.5:s=32x16:r=10:d=0.3,format=argb\" -an -c:v "+codec+" "+options+" -y "+VideoTools.Quote(clip))){string error=make.StandardError.ReadToEnd();make.WaitForExit();check(make.ExitCode==0,"genera MOV codec "+codec);}
    using(var poster=VideoTools.Poster(clip))check(poster.Width==640&&(codec=="libopenh264"?poster.GetPixel(320,160).A==255:Math.Abs(poster.GetPixel(320,160).A-128)<=3),"decodifica MOV "+codec+" con alpha previsto");
   }
   var initial=Renderer.Render(path,16,16,new Placement());
   using(var video=new VideoSession(path,16,16,10,new Placement(),false,initial)){
    check(SpinWait.SpinUntil(()=>video.Ended,5000)&&video.Error==null&&video.DecodedFrames>=3,"MOV senza loop raggiunge fine senza errori");var last=video.ReadFrame();check(last[3]==0&&last[(8*16+8)*4+3]>=120&&last[(8*16+8)*4+3]<=130,"video mantiene bande trasparenti e alpha in BGRA");long count=video.DecodedFrames;Thread.Sleep(150);check(video.DecodedFrames==count&&Object.ReferenceEquals(last,video.ReadFrame()),"fine video mantiene ultimo frame senza auto clear");
   }
   using(var video=new VideoSession(path,16,16,10,new Placement{Scale=.5,X=3,Y=-2},true,initial)){
    check(SpinWait.SpinUntil(()=>video.DecodedFrames>=9,5000)&&!video.Ended&&video.Error==null,"Loop ripete MOV oltre durata originale");var frame=video.ReadFrame();check(frame[(6*16+11)*4+3]>=120&&frame[(10*16+3)*4+3]==0,"scala e offset applicati al video con trasparenza");
   }
   var settings=new Settings{Root=dir,OutputWidth=16,OutputHeight=16,OutputFps=10};settings.Images[path]=new Placement{Loop=true,Scale=.5,X=3,Y=-2};string project=Path.Combine(dir,"show.ndiproj");ProjectStore.Write(project,settings);check(ProjectStore.Read(project).Images[path].Loop,"progetto conserva Loop del singolo video");
   string prefs=Path.Combine(dir,"prefs.json");File.WriteAllText(prefs,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(settings));
   using(var window=new MainWindow(prefs)){
    var browser=(LibraryBrowser)Get(window,"files");var h=browser.Handle;foreach(Control c in browser.Controls)if(c is ListView){var handle=c.Handle;}Call(window,"Scan");Thread.Sleep(160);Call(window,"UpdateSelectionVideo");
    check(Get(window,"selectionVideo")!=null&&((CheckBox)Get(window,"videoLoop")).Checked,"selezione MOV avvia anteprima con Loop salvato");
    Call(window,"ApplyCenter",8m,8m,75m,true);check(((CheckBox)Get(window,"videoLoop")).Checked&&((Settings)Get(window,"settings")).Images[path].Loop,"ridimensionamento e posizione non azzerano Loop");
    var program=new Playout(new Probe(),16,16,10);Put(window,"output",program);Put(window,"outW",16);Put(window,"outH",16);Put(window,"activeProgramFps",10);Call(window,"Cue",false);
    check(SpinWait.SpinUntil(()=>program.ReadOnAirStatus().Paths.Contains(path),2000),"invio manuale MOV entra in Program");var source=typeof(Playout).GetField("frameSource",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(program);check(source!=null&&!Object.ReferenceEquals(source,Get(window,"selectionVideo")),"Program e anteprima usano riproduzioni indipendenti");
    Call(window,"Cue",true);check(SpinWait.SpinUntil(()=>program.ReadOnAirStatus().Paths.Length==0,2000)&&((VideoSession)source).Ended,"clear Program interrompe decoder e invia trasparenza");
   }
   if(native){var frame=Renderer.Render(path,1920,1080,new Placement());using(var stream=new Playout(new NdiSender("NDIPlayer TEST MOV 1080p50"),1920,1080,50)){var movie=new VideoSession(path,1920,1080,50,new Placement(),true,frame);stream.CueSource(movie,frame,0,path);check(SpinWait.SpinUntil(()=>stream.Frames>=40&&movie.DecodedFrames>=35,8000)&&stream.Error==null&&movie.Error==null,"MOV in loop su sender NDI reale 1920x1080p50");check(stream.StopTransparent()&&movie.Ended,"arresto MOV reale chiude decoder dopo clear NDI");}}
   string broken=Path.Combine(dir,"broken.mov");File.WriteAllText(broken,"invalid");bool failed=false;try{using(var image=VideoTools.Poster(broken)){} }catch(IOException){failed=true;}check(failed,"MOV danneggiato produce errore leggibile senza bloccare app");
  }finally{Directory.Delete(dir,true);}
 }
}
