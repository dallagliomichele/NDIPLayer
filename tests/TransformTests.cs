using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using ImageNdi;

static class TransformTests {
 static void Mouse(PreviewCanvas canvas,string method,MouseButtons buttons,int x,int y,int delta){typeof(PreviewCanvas).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(canvas,new object[]{new MouseEventArgs(buttons,1,x,y,delta)});}
 static T Find<T>(Control root,string name) where T:Control{return (T)root.Controls.Find(name,true)[0];}
 public static void Run(Action<bool,string> check){
  var center=PlacementCoordinates.Center(new Placement(),1920,1080);check(center.X==960&&center.Y==540,"centro iniziale 1920x1080 uguale a 960/540");
  var p=PlacementCoordinates.FromCenter(1060,490,1920,1080,.5);var r=Renderer.Fit(100,100,1920,1080,p);check(r.X+r.Width/2==1060&&r.Y+r.Height/2==490,"coordinate assolute coincidono col centro renderizzato");
  var odd=PlacementCoordinates.Center(new Placement(),1920,1081);check(odd.Y==540.5f&&PlacementCoordinates.FromCenter(960,540.5m,1920,1081,1).Y==0,"centro mezzo pixel su altezza dispari senza spostamento");
  check(PlacementCoordinates.ZoomPercent(100,120,false)==110&&PlacementCoordinates.ZoomPercent(100,120,true)==102,"rotellina 10 percento e Shift 2 percento");
  check(PlacementCoordinates.ZoomPercent(400,120,false)==400&&PlacementCoordinates.ZoomPercent(1,-120,false)==1,"rotellina rispetta limiti scala");
  using(var canvas=new PreviewCanvas()){
   canvas.Size=new Size(1000,700);canvas.SetFrame(new Bitmap(1920,1080),1920,1080);var frame=PreviewCanvas.FrameBounds(canvas.Size,1920,1080);Point at=new Point(frame.Left+frame.Width/2,frame.Top+frame.Height/2);int moves=0,zooms=0;PointF delta=PointF.Empty;
   canvas.DragMoved+=delegate(object sender,PreviewDragEventArgs e){moves++;delta=e.Delta;};canvas.ZoomRequested+=delegate{zooms++;};
   Mouse(canvas,"OnMouseDown",MouseButtons.Left,at.X,at.Y,0);Mouse(canvas,"OnMouseMove",MouseButtons.Left,at.X+24,at.Y+12,0);Mouse(canvas,"OnMouseWheel",MouseButtons.None,at.X,at.Y,120);
   check(moves==0&&zooms==0,"programma non interattivo ignora trascinamento e rotellina");
   canvas.InteractionEnabled=true;Mouse(canvas,"OnMouseDown",MouseButtons.Left,at.X,at.Y,0);Mouse(canvas,"OnMouseMove",MouseButtons.Left,at.X+24,at.Y+12,0);
   check(moves==1&&Math.Abs(delta.X-24*1920f/frame.Width)<.01&&Math.Abs(delta.Y-12*1080f/frame.Height)<.01,"drag convertito da pixel anteprima a pixel uscita");
   Mouse(canvas,"OnMouseUp",MouseButtons.Left,at.X+24,at.Y+12,0);Mouse(canvas,"OnMouseMove",MouseButtons.None,at.X+48,at.Y+24,0);check(moves==1,"rilascio mouse termina trascinamento");
   Mouse(canvas,"OnMouseWheel",MouseButtons.None,at.X,at.Y,120);Mouse(canvas,"OnMouseWheel",MouseButtons.None,0,0,120);check(zooms==1,"rotellina attiva solo dentro il fotogramma");
   Mouse(canvas,"OnMouseDown",MouseButtons.Left,0,0,0);Mouse(canvas,"OnMouseMove",MouseButtons.Left,at.X,at.Y,0);check(moves==1,"area esterna non avvia drag");
  }
  string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"NDIPlayer-transform-test-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try{
   string file=Path.Combine(dir,"sample.png"),state=Path.Combine(dir,"settings.json");using(var image=new Bitmap(20,20)){using(var g=Graphics.FromImage(image))g.Clear(Color.Coral);image.Save(file,System.Drawing.Imaging.ImageFormat.Png);}
   var old=new Settings();old.Images[file]=new Placement{Scale=.5,X=100,Y=-50};var json=new JavaScriptSerializer();File.WriteAllText(state,json.Serialize(old));
   using(var window=new MainWindow(state)){
    var x=Find<NumericUpDown>(window,"ImageCenterX");var y=Find<NumericUpDown>(window,"ImageCenterY");var scale=Find<NumericUpDown>(window,"ImageScale");
    var browser=Find<LibraryBrowser>(window,"ImageLibrary");var handle=browser.Handle;foreach(Control child in browser.Controls)if(child is ListView){var listHandle=child.Handle;}browser.SetFiles(new System.Collections.Generic.List<string>{file});
    check(x.Value==1060&&y.Value==490&&scale.Value==50,"vecchie posizioni mostrate come centro senza cambiare inquadratura ("+x.Value+", "+y.Value+", "+scale.Value+")");
    var width=Find<NumericUpDown>(window,"OutputWidth");var height=Find<NumericUpDown>(window,"OutputHeight");width.Value=1280;height.Value=720;
    check(x.Value==740&&y.Value==310,"cambio risoluzione aggiorna centro conservando offset salvato");
    var typed=(CommittedNumericUpDown)width;int changes=0;width.ValueChanged+=delegate{changes++;};width.Controls.OfType<TextBox>().First().Text="1";var shown=typeof(MainWindow).GetMethod("UpdateShellStatus",BindingFlags.Instance|BindingFlags.NonPublic);shown.Invoke(window,null);
    check(changes==0&&width.Text=="1"&&typed.Committed==1280,"campo risoluzione non si autocorregge finché si digita ("+changes+", "+width.Text+", "+typed.Committed+")");
    var click=typeof(Button).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic);check(width.Value==16,"uscita dal campo applica il limite minimo");var preset=Find<Button>(window,"ResolutionPreset3840x2160");click.Invoke(preset,new object[]{EventArgs.Empty});check(width.Value==3840&&height.Value==2160,"preset 4K imposta risoluzione");click.Invoke(Find<Button>(window,"ResolutionPreset1920x1080"),new object[]{EventArgs.Empty});check(width.Value==1920&&height.Value==1080,"preset Full HD imposta risoluzione");width.Value=1280;height.Value=720;
    width.Value=1920;height.Value=1080;x.Value=960;y.Value=540;var saved=json.Deserialize<Settings>(File.ReadAllText(state));
    check(saved.Images[file].X==0&&saved.Images[file].Y==0,"campi centro salvano offset compatibili con versioni precedenti (salvati "+saved.Images[file].X+", "+saved.Images[file].Y+"; campi "+x.Value+", "+y.Value+")");
    var preview=Find<PreviewCanvas>(window,"SelectionPreview");preview.Size=new Size(1000,700);var frame=PreviewCanvas.FrameBounds(preview.Size,1920,1080);var at=new Point(frame.Left+frame.Width/2,frame.Top+frame.Height/2);
    Mouse(preview,"OnMouseDown",MouseButtons.Left,at.X,at.Y,0);Mouse(preview,"OnMouseMove",MouseButtons.Left,at.X+24,at.Y+12,0);Mouse(preview,"OnMouseUp",MouseButtons.Left,at.X+24,at.Y+12,0);
    int dx=(int)Math.Round(24*1920m/frame.Width,MidpointRounding.AwayFromZero),dy=(int)Math.Round(12*1080m/frame.Height,MidpointRounding.AwayFromZero);
    check(x.Value==960+dx&&y.Value==540+dy,"drag aggiorna campi centro X/Y");
    Mouse(preview,"OnMouseWheel",MouseButtons.None,at.X,at.Y,120);check(scale.Value==55&&x.Value==960+dx&&y.Value==540+dy,"rotellina aggiorna scala lasciando fermo il centro");
    saved=json.Deserialize<Settings>(File.ReadAllText(state));check(saved.Images[file].X==dx&&saved.Images[file].Y==dy&&Math.Abs(saved.Images[file].Scale-.55)<.0001,"trasformazioni mouse salvate per singola immagine");
    Button reset=Find<Button>(window,"ResetPlacement");
    typeof(Button).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(reset,new object[]{EventArgs.Empty});
    check(x.Value==960&&y.Value==540&&scale.Value==100,"reset ripristina centro del flusso e fit 100 percento");
    using(var cached=Renderer.LoadBitmap(file)){File.Delete(file);var pixels=Renderer.RenderImage(cached,32,32,new Placement());check(pixels[(16*32+16)*4+3]==255,"sorgente decodificata riutilizzabile senza bloccare il file");}
   }
  }finally{Directory.Delete(dir,true);}
 }
}
