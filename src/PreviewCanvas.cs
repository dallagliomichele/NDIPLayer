using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImageNdi {
// This control owns its display bitmap. Guides are painted here, never into a video frame.
public class PreviewCanvas:Control {
 Bitmap image;
 int frameWidth=1920,frameHeight=1080;
 public string Heading="SELEZIONE";
 public string Footer="Scacchiera = trasparenza · Premi INVIA per andare in onda";
 public Color BorderColor=Color.FromArgb(55,205,245);
 public bool InteractionEnabled;
 public event EventHandler DragStarted;
 public event EventHandler<PreviewDragEventArgs> DragMoved;
 public event EventHandler DragCompleted;
 public event EventHandler<PreviewZoomEventArgs> ZoomRequested;
 public event EventHandler ImageDoubleClicked;
 bool dragging;Point dragOrigin;Rectangle dragFrame;
 public PreviewCanvas(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.FromArgb(30,32,36);SetStyle(ControlStyles.Selectable|ControlStyles.StandardClick|ControlStyles.StandardDoubleClick,true);TabStop=true;}
 protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);if(!InteractionEnabled||image==null||e.Button!=MouseButtons.Left||!FrameBounds(ClientSize,frameWidth,frameHeight).Contains(e.Location))return;CancelInteraction();if(ImageDoubleClicked!=null)ImageDoubleClicked(this,EventArgs.Empty);}
 public void CancelInteraction(){if(!dragging)return;dragging=false;Capture=false;Cursor=Cursors.Default;if(DragCompleted!=null)DragCompleted(this,EventArgs.Empty);}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);var frame=FrameBounds(ClientSize,frameWidth,frameHeight);if(!InteractionEnabled||image==null||e.Button!=MouseButtons.Left||!frame.Contains(e.Location))return;Focus();dragOrigin=e.Location;dragFrame=frame;dragging=true;Capture=true;Cursor=Cursors.SizeAll;if(DragStarted!=null)DragStarted(this,EventArgs.Empty);}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!dragging)return;if(!InteractionEnabled||image==null){CancelInteraction();return;}var delta=PlacementCoordinates.DragDelta(dragOrigin,e.Location,dragFrame,frameWidth,frameHeight);if(DragMoved!=null)DragMoved(this,new PreviewDragEventArgs(delta));}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left)CancelInteraction();}
 protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)CancelInteraction();}
 protected override void OnResize(EventArgs e){CancelInteraction();base.OnResize(e);}
 protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);if(InteractionEnabled&&image!=null)Focus();}
 protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);if(!InteractionEnabled||image==null||dragging||!FrameBounds(ClientSize,frameWidth,frameHeight).Contains(e.Location))return;if(ZoomRequested!=null)ZoomRequested(this,new PreviewZoomEventArgs(e.Delta,(ModifierKeys&Keys.Shift)!=0));}
 public static Rectangle FrameBounds(Size viewport,int width,int height){
  int availableWidth=Math.Max(0,viewport.Width-40), availableHeight=Math.Max(0,viewport.Height-72);
  if(availableWidth==0||availableHeight==0||width<=0||height<=0)return Rectangle.Empty;
  double factor=Math.Min((double)availableWidth/width,(double)availableHeight/height);
  int w=Math.Max(1,(int)Math.Floor(width*factor)),h=Math.Max(1,(int)Math.Floor(height*factor));
  return new Rectangle((viewport.Width-w)/2,36+(availableHeight-h)/2,w,h);
 }
 public void SetFrame(Bitmap next,int width,int height){if(next==null||width!=frameWidth||height!=frameHeight)CancelInteraction();var old=image;image=next;frameWidth=width;frameHeight=height;if(old!=null)old.Dispose();Invalidate();}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);DrawPreview(e.Graphics,ClientSize);}
 public void DrawPreview(Graphics graphics,Size viewport){
  graphics.Clear(BackColor);
  var frame=FrameBounds(viewport,frameWidth,frameHeight);
  if(frame.IsEmpty)return;
  TextRenderer.DrawText(graphics,Heading+" · "+frameWidth+" × "+frameHeight,Font,new Rectangle(12,8,Math.Max(1,viewport.Width-24),22),Color.Gainsboro,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);
  var saved=graphics.Save();
  try {
   graphics.SetClip(frame);
   using(var light=new SolidBrush(Color.FromArgb(120,120,120)))using(var dark=new SolidBrush(Color.FromArgb(88,88,88))){
    graphics.FillRectangle(dark,frame);
    for(int y=frame.Top;y<frame.Bottom;y+=12)for(int x=frame.Left;x<frame.Right;x+=12)
     if(((x-frame.Left)/12+(y-frame.Top)/12)%2==0)graphics.FillRectangle(light,x,y,12,12);
   }
   if(image!=null){graphics.CompositingMode=CompositingMode.SourceOver;graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.DrawImage(image,frame);}
  } finally {graphics.Restore(saved);}
  using(var border=new Pen(BorderColor,2)) graphics.DrawRectangle(border,frame.Left-1,frame.Top-1,frame.Width+1,frame.Height+1);
  TextRenderer.DrawText(graphics,Footer,Font,new Rectangle(12,viewport.Height-24,Math.Max(1,viewport.Width-24),22),Color.Gainsboro,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);
 }
 protected override void Dispose(bool disposing){if(disposing&&image!=null){image.Dispose();image=null;}base.Dispose(disposing);}
}
public sealed class PreviewDragEventArgs:EventArgs {public readonly PointF Delta;public PreviewDragEventArgs(PointF delta){Delta=delta;}}
public sealed class PreviewZoomEventArgs:EventArgs {public readonly int Delta;public readonly bool Fine;public PreviewZoomEventArgs(int delta,bool fine){Delta=delta;Fine=fine;}}
}
