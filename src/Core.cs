using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;

namespace ImageNdi {
public class Placement { public double Scale {get;set;} public int X {get;set;} public int Y {get;set;} public Placement(){Scale=1;} }
public static class PlacementCoordinates {
 // Keep stored offsets compatible with older versions; expose absolute centers in the UI.
 public static PointF Center(Placement placement,int width,int height){return new PointF(width/2f+placement.X,height/2f+placement.Y);}
 public static Placement FromCenter(decimal centerX,decimal centerY,int width,int height,double scale){return new Placement{Scale=scale,X=(int)Math.Round(centerX-width/2m,MidpointRounding.AwayFromZero),Y=(int)Math.Round(centerY-height/2m,MidpointRounding.AwayFromZero)};}
 public static PointF DragDelta(Point start,Point end,Rectangle frame,int width,int height){if(frame.Width<=0||frame.Height<=0)return PointF.Empty;return new PointF((end.X-start.X)*(float)width/frame.Width,(end.Y-start.Y)*(float)height/frame.Height);}
 public static decimal ZoomPercent(decimal current,int wheelDelta,bool fine){double next=(double)current*Math.Pow(fine?1.02:1.10,wheelDelta/120.0);return Math.Max(1m,Math.Min(400m,(decimal)Math.Round(next,1)));}
}
public static class Renderer {
 public static Bitmap ToBitmap(byte[] pixels,int width,int height){
  if(pixels.Length!=width*height*4)throw new ArgumentException("Frame size");
  var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb);
  try {var data=bitmap.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{for(int y=0;y<height;y++)Marshal.Copy(pixels,y*width*4,IntPtr.Add(data.Scan0,y*data.Stride),width*4);}finally{bitmap.UnlockBits(data);}return bitmap;}catch{bitmap.Dispose();throw;}
 }
 public static RectangleF Fit(int iw,int ih,int w,int h,Placement p) {
  double s=Math.Min((double)w/iw,(double)h/ih)*p.Scale;
  return new RectangleF((float)((w-iw*s)/2+p.X),(float)((h-ih*s)/2+p.Y),(float)(iw*s),(float)(ih*s));
 }
 public static Bitmap LoadBitmap(string path){
  using(var src=Image.FromFile(path)){
   try { if(Array.IndexOf(src.PropertyIdList,0x112)>=0) { int o=src.GetPropertyItem(0x112).Value[0]; RotateFlipType[] ops={RotateFlipType.RotateNoneFlipNone,RotateFlipType.RotateNoneFlipNone,RotateFlipType.RotateNoneFlipX,RotateFlipType.Rotate180FlipNone,RotateFlipType.Rotate180FlipX,RotateFlipType.Rotate90FlipX,RotateFlipType.Rotate90FlipNone,RotateFlipType.Rotate270FlipX,RotateFlipType.Rotate270FlipNone}; if(o<ops.Length) src.RotateFlip(ops[o]); }} catch(ArgumentException){}
   return new Bitmap(src);
  }
 }
 public static byte[] Render(string path,int w,int h,Placement p) {if(path==null)return RenderImage(null,w,h,p);using(var src=LoadBitmap(path))return RenderImage(src,w,h,p);}
 public static byte[] RenderImage(Image src,int w,int h,Placement p) {
  using(var b=new Bitmap(w,h,PixelFormat.Format32bppArgb)) {
   using(var g=Graphics.FromImage(b)) { g.Clear(Color.Transparent); g.CompositingMode=CompositingMode.SourceCopy; g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    if(src!=null) {
     var r=Fit(src.Width,src.Height,w,h,p); using(var a=new ImageAttributes()){ a.SetWrapMode(WrapMode.TileFlipXY); g.DrawImage(src,Rectangle.Round(r),0,0,src.Width,src.Height,GraphicsUnit.Pixel,a); }
    }
   }
   var data=b.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   try { var bytes=new byte[w*h*4]; for(int y=0;y<h;y++) Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),bytes,y*w*4,w*4); return bytes; } finally {b.UnlockBits(data);}
  }
 }
 public static void Blend(byte[] a,byte[] b,double t,byte[] result) {
  if(a.Length!=b.Length || result.Length!=a.Length) throw new ArgumentException("Frame sizes differ");
  t=Math.Max(0,Math.Min(1,t));
  for(int i=0;i<a.Length;i+=4) { double aa=a[i+3]*(1-t),ba=b[i+3]*t,alpha=aa+ba;
   for(int c=0;c<3;c++) result[i+c]=alpha==0?(byte)0:(byte)Math.Round((a[i+c]*aa+b[i+c]*ba)/alpha);
   result[i+3]=(byte)Math.Round(alpha);
  }
 }
}
public static class Library {
 static readonly HashSet<string> Formats=new HashSet<string>(StringComparer.OrdinalIgnoreCase){".png",".jpg",".jpeg",".bmp",".gif",".tif",".tiff",".ico"};
 static bool Excluded(string path,FileAttributes attributes){string name=Path.GetFileName(path);
  return (attributes&FileAttributes.Hidden)!=0 || name.StartsWith("._",StringComparison.Ordinal) || name.StartsWith(".-",StringComparison.Ordinal) ||
   ((attributes&FileAttributes.Directory)!=0&&(name.StartsWith(".",StringComparison.Ordinal)||String.Equals(name,"__MACOSX",StringComparison.OrdinalIgnoreCase)));
 }
 public static List<string> Scan(string root,List<string> warnings) {
  var found=new List<string>(); var dirs=new Stack<string>(); dirs.Push(root);
  while(dirs.Count>0) {string d=dirs.Pop(); try {foreach(var f in Directory.GetFiles(d)) {
    if(!Formats.Contains(Path.GetExtension(f)))continue;
    try{if(!Excluded(f,File.GetAttributes(f)))found.Add(f);}catch(UnauthorizedAccessException e){warnings.Add(e.Message);}catch(IOException e){warnings.Add(e.Message);}
   }
   foreach(var sub in Directory.GetDirectories(d)) {
    try{var attributes=File.GetAttributes(sub);if((attributes&FileAttributes.ReparsePoint)==0&&!Excluded(sub,attributes))dirs.Push(sub);}catch(UnauthorizedAccessException e){warnings.Add(e.Message);}catch(IOException e){warnings.Add(e.Message);}
   }
  } catch(UnauthorizedAccessException e){warnings.Add(e.Message);} catch(IOException e){warnings.Add(e.Message);} }
  found.Sort(StringComparer.OrdinalIgnoreCase); return found;
 }
}
}
