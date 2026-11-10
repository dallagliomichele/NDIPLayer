using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ImageNdi;
static class CountdownPaintTests {
 public static void Run(Action<bool,string> check){using(var label=new CountdownLabel{Size=new Size(588,56),BackColor=Color.FromArgb(30,32,35),ForeColor=Color.White,Text="Al prossimo loop: 00:17.0 · Loop attivo"}){
  var style=typeof(Control).GetMethod("GetStyle",BindingFlags.Instance|BindingFlags.NonPublic);var flags=ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.Opaque;
  check((bool)style.Invoke(label,new object[]{flags})&&!label.AutoSize,"countdown usa disegno opaco e buffer doppio con dimensioni fisse");
  using(var bitmap=new Bitmap(588,56))using(var graphics=Graphics.FromImage(bitmap)){graphics.Clear(Color.Magenta);typeof(CountdownLabel).GetMethod("OnPaintBackground",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(label,new object[]{new PaintEventArgs(graphics,label.ClientRectangle)});check(bitmap.GetPixel(0,0).ToArgb()==Color.Magenta.ToArgb(),"countdown non cancella separatamente lo sfondo prima del testo");}
  bool stable=true,textVisible=true;for(int i=0;i<30;i++){label.ShowCountdown("Al prossimo loop: 00:"+(17-i/10)+"."+(9-i%10)+" · Loop attivo",i<15?Color.White:Color.DarkOrange);using(var bitmap=new Bitmap(588,56)){label.DrawToBitmap(bitmap,label.ClientRectangle);stable&=bitmap.GetPixel(0,0).ToArgb()==label.BackColor.ToArgb()&&bitmap.GetPixel(587,55).ToArgb()==label.BackColor.ToArgb();int ink=0;for(int y=0;y<56;y++)for(int x=0;x<588;x++)if(bitmap.GetPixel(x,y).R>100)ink++;textVisible&=ink>20;}}
  check(stable&&textVisible,"aggiornamenti rapidi mantengono sfondo scuro e testo in ogni fotogramma");
  check(typeof(CountdownLabel).BaseType==typeof(Control),"countdown disegnato senza controllo STATIC nativo");using(var parent=new Panel()){parent.Controls.Add(label);int layouts=0;parent.Layout+=delegate{layouts++;};for(int n=0;n<30;n++)label.ShowCountdown("Alla fine: 00:20."+(n%10),Color.White);check(layouts==0,"cambio countdown non forza il layout del contenitore");parent.Controls.Remove(label);}
  int invalidated=0;var handle=label.Handle;label.Invalidated+=delegate{invalidated++;};string current=label.Text;Color color=label.ForeColor;label.ShowCountdown(current,color);check(invalidated==0,"stesso valore del countdown non richiede ridisegno");
 }}
}
