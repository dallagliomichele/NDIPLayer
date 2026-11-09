using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using ImageNdi;
class Tests {
 static int count; static void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;Console.WriteLine("PASS "+label);}
 [STAThread] static void Main(string[] args){try{Run(args);}catch(Exception e){Console.Error.WriteLine(e.ToString());Environment.ExitCode=1;}}
 static void Run(string[] args){
  var r=Renderer.Fit(100,100,1920,1080,new Placement());Check(r.Width==1080&&r.X==420&&r.Y==0,"fit quadrato senza distorsione");
  r=Renderer.Fit(200,100,100,100,new Placement{Scale=.5,X=10,Y=-5});Check(r.Width==50&&r.Height==25&&r.X==35&&r.Y==32.5,"scala e offset");
  var transparent=Renderer.Render(null,16,16,new Placement());Check(Array.TrueForAll(transparent,value=>value==0),"clear alpha zero");
  byte[] a={0,0,255,255},b={0,0,0,0},c=new byte[4];Renderer.Blend(a,b,.5,c);Check(c[2]==255&&c[3]==128,"dissolvenza senza alone nero");
  Renderer.Blend(a,b,0,c);Check(c[2]==255&&c[3]==255,"inizio transizione");Renderer.Blend(a,b,1,c);Check(c[3]==0&&c[2]==0,"fine transizione");
  byte[] blue={255,0,0,255};Renderer.Blend(a,blue,.5,c);Check(c[0]==128&&c[2]==128&&c[3]==255,"crossfade opaco");
  string dir=Path.Combine(Path.GetTempPath(),"ImageNdi-test-"+Guid.NewGuid());Directory.CreateDirectory(Path.Combine(dir,"nested"));
  try{string path=Path.Combine(dir,"nested","sample.png");using(var img=new Bitmap(8,8)){using(var g=Graphics.FromImage(img)){g.Clear(Color.FromArgb(128,255,0,0));}img.Save(path,ImageFormat.Png);}
   File.WriteAllText(Path.Combine(dir,"ignored.txt"),"test");Check(Library.Scan(dir,new System.Collections.Generic.List<string>()).Count==1,"scansione ricorsiva filtra formati");
   var pixels=Renderer.Render(path,16,8,new Placement());Check(pixels[3]==0&&pixels[(4*4)+3]==128,"PNG conserva alpha e bande trasparenti");
  }finally{Directory.Delete(dir,true);}
  Check(Marshal.SizeOf(typeof(NdiSender.Frame))==72,"ABI frame NDI x64 72 byte");
  var fake=new Fake();using(var p=new Playout(fake,16,16,50)){var frame=new byte[1024];frame[3]=255;p.Cue(frame,0);Thread.Sleep(100);Check(fake.Seen,"playout invia cut");p.Cue(new byte[1024],.02);Thread.Sleep(100);Check(fake.Alpha==0,"playout dissolve clear");}Check(fake.Disposed,"rilascio sender");
  if(args.Length>0&&args[0]=="--ndi"){using(var sender=new NdiSender("NDIPlayer TEST — regia è")){for(int i=0;i<25;i++)sender.Send(transparent,16,16,50);}Check(true,"runtime NDI reale crea/invia/distrugge con nome UTF-8");}
  var bounds=PreviewCanvas.FrameBounds(new Size(880,600),1920,1080);
  Check(Math.Abs((double)bounds.Width/bounds.Height-16.0/9)<.005&&bounds.Top>36,"preview 16:9 centrato in finestra più alta");
  var portrait=PreviewCanvas.FrameBounds(new Size(880,600),1080,1920);
  Check(Math.Abs((double)portrait.Width/portrait.Height-9.0/16)<.005&&portrait.Left>20,"preview verticale mantiene aspect ratio");
  Check(PreviewCanvas.FrameBounds(new Size(10,10),1920,1080).IsEmpty,"preview minimizzato sicuro");
  using(var canvas=new PreviewCanvas())using(var shot=new Bitmap(880,600))using(var g=Graphics.FromImage(shot)){
   var display=new Bitmap(1920,1080,PixelFormat.Format32bppArgb);
   using(var paint=Graphics.FromImage(display)){paint.Clear(Color.Transparent);paint.FillRectangle(Brushes.OrangeRed,690,270,540,540);}
   canvas.SetFrame(display,1920,1080);canvas.DrawPreview(g,shot.Size);
   Check(shot.GetPixel(4,300).R==30,"area esterna distinta dalla trasparenza");
   Check(shot.GetPixel(bounds.Left+20,bounds.Top+20).R==120,"scacchiera confinata al frame");
   Check(shot.GetPixel(bounds.Left-1,bounds.Top-1).B>200,"bordo di uscita visibile");
   Check(shot.GetPixel(bounds.Left+bounds.Width/2,bounds.Top+bounds.Height/2).R==255,"immagine nel frame visualizzata in scala");
   Check(display.GetPixel(0,0).A==0,"guide non alterano bitmap trasmessa");
   if(args.Length>0&&args[0]=="--ui")shot.Save("preview-frame.png",ImageFormat.Png);
  }
  if(args.Length>0&&args[0]=="--ui")FeatureTests.BrowserProofPath="browser-proof.png";
  FeatureTests.Run(Check);
  TransformTests.Run(Check);
  if(args.Length>0&&args[0]=="--ui")ProjectTests.ProofPath="playlist-indicators-proof.png";
  ProjectTests.Run(Check);
  using(var window=new MainWindow(Path.Combine(Path.GetTempPath(),"NDIPlayer-no-preferences-"+Guid.NewGuid()+".json"))){window.CreateControl();window.PerformLayout();Check(window.Controls.Count==3,"interfaccia costruzione");}
  Console.WriteLine(count+" test superati");
 }
 class Fake:IVideoSender{public volatile bool Seen,Disposed;public volatile int Alpha;public void Send(byte[] p,int w,int h,int fps){Alpha=p[3];if(Alpha==255)Seen=true;Thread.Sleep(10);}public void Dispose(){Disposed=true;}}
}

