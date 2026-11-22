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
 public bool InteractionEnabled;public bool ShowFooter=true;
 public event EventHandler DragStarted;
 public event EventHandler<PreviewDragEventArgs> DragMoved;
 public event EventHandler DragCompleted;
 public event EventHandler<PreviewZoomEventArgs> ZoomRequested;
 public event EventHandler ImageDoubleClicked;
 public event EventHandler PlayPauseRequested,RestartRequested;
 public event EventHandler VideoSpeedChanged;
 readonly Panel transport=new Panel{Name="VideoTransport",Size=new Size(164,30),BackColor=Color.FromArgb(40,40,40),Visible=false};
 readonly Button transportPlay=new Button{Name="VideoPlayPause",Size=new Size(32,26),Location=new Point(2,2),AccessibleName="Riproduci o metti in pausa"};
 readonly Button transportRestart=new Button{Name="VideoRestart",Text="⟲",Size=new Size(32,26),Location=new Point(36,2),AccessibleName="Riavvia video"};
 readonly NumericUpDown transportSpeed=new NumericUpDown{Name="VideoSpeed",Minimum=.5m,Maximum=5m,Increment=.1m,DecimalPlaces=1,Size=new Size(68,26),Location=new Point(72,3),AccessibleName="Velocità video"};
 readonly Label transportUnit=new Label{Text="×",ForeColor=Color.White,Size=new Size(18,20)};
 bool updatingTransport,hasVideoTransport;
 bool dragging;Point dragOrigin;Rectangle dragFrame;
 public PreviewCanvas(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.FromArgb(30,32,36);SetStyle(ControlStyles.Selectable|ControlStyles.StandardClick|ControlStyles.StandardDoubleClick,true);TabStop=true;transport.Controls.Add(transportPlay);transport.Controls.Add(transportRestart);transport.Controls.Add(transportSpeed);transport.Controls.Add(transportUnit);Controls.Add(transport);transportPlay.Click+=delegate{if(PlayPauseRequested!=null)PlayPauseRequested(this,EventArgs.Empty);};transportRestart.Click+=delegate{if(RestartRequested!=null)RestartRequested(this,EventArgs.Empty);};transportSpeed.ValueChanged+=delegate{if(!updatingTransport&&VideoSpeedChanged!=null)VideoSpeedChanged(this,EventArgs.Empty);};}
 public double VideoSpeed {get{return (double)transportSpeed.Value;}}
 public void SetVideoTransport(VideoSession video){updatingTransport=true;try{hasVideoTransport=video!=null;if(video!=null){transportPlay.Text=video.Paused||video.Ended?"▶":"❚❚";decimal value=Math.Max(transportSpeed.Minimum,Math.Min(transportSpeed.Maximum,(decimal)video.Speed));if(transportSpeed.Value!=value)transportSpeed.Value=value;}PositionTransport();}finally{updatingTransport=false;}}
 void PositionTransport(){var frame=FrameBounds(ClientSize,frameWidth,frameHeight,ShowFooter);transport.Visible=hasVideoTransport&&frame.Width>=120&&frame.Height>=38;if(!transport.Visible&&!hasVideoTransport)return;if(frame.Width<120||frame.Height<38)return;int w=Math.Min(164,frame.Width-8),button=Math.Min(32,(w-64)/2);transport.Size=new Size(w,30);transportPlay.SetBounds(2,2,button,26);transportRestart.SetBounds(4+button,2,button,26);int speedX=6+button*2;transportSpeed.SetBounds(speedX,3,w-speedX-20,26);transportUnit.Location=new Point(w-18,6);transport.Location=new Point(frame.Right-w-4,frame.Bottom-34);}
 protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);if(!InteractionEnabled||image==null||e.Button!=MouseButtons.Left||!FrameBounds(ClientSize,frameWidth,frameHeight).Contains(e.Location))return;CancelInteraction();if(ImageDoubleClicked!=null)ImageDoubleClicked(this,EventArgs.Empty);}
 public void CancelInteraction(){if(!dragging)return;dragging=false;Capture=false;Cursor=Cursors.Default;if(DragCompleted!=null)DragCompleted(this,EventArgs.Empty);}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);var frame=FrameBounds(ClientSize,frameWidth,frameHeight);if(!InteractionEnabled||image==null||e.Button!=MouseButtons.Left||!frame.Contains(e.Location))return;Focus();dragOrigin=e.Location;dragFrame=frame;dragging=true;Capture=true;Cursor=Cursors.SizeAll;if(DragStarted!=null)DragStarted(this,EventArgs.Empty);}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!dragging)return;if(!InteractionEnabled||image==null){CancelInteraction();return;}var delta=PlacementCoordinates.DragDelta(dragOrigin,e.Location,dragFrame,frameWidth,frameHeight);if(DragMoved!=null)DragMoved(this,new PreviewDragEventArgs(delta));}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left)CancelInteraction();}
 protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)CancelInteraction();}
 protected override void OnResize(EventArgs e){CancelInteraction();base.OnResize(e);if(transport!=null)PositionTransport();}
 protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);if(InteractionEnabled&&image!=null)Focus();}
 protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);if(!InteractionEnabled||image==null||dragging||!FrameBounds(ClientSize,frameWidth,frameHeight).Contains(e.Location))return;if(ZoomRequested!=null)ZoomRequested(this,new PreviewZoomEventArgs(e.Delta,(ModifierKeys&Keys.Shift)!=0));}
 public static Rectangle FrameBounds(Size viewport,int width,int height){return FrameBounds(viewport,width,height,true);}
 public static Rectangle FrameBounds(Size viewport,int width,int height,bool showFooter){
  int availableWidth=Math.Max(0,viewport.Width-40), availableHeight=Math.Max(0,viewport.Height-(showFooter?72:48));
  if(availableWidth==0||availableHeight==0||width<=0||height<=0)return Rectangle.Empty;
  double factor=Math.Min((double)availableWidth/width,(double)availableHeight/height);
  int w=Math.Max(1,(int)Math.Floor(width*factor)),h=Math.Max(1,(int)Math.Floor(height*factor));
  return new Rectangle((viewport.Width-w)/2,36+(availableHeight-h)/2,w,h);
 }
 public void SetFrame(Bitmap next,int width,int height){if(next==null||width!=frameWidth||height!=frameHeight)CancelInteraction();var old=image;image=next;frameWidth=width;frameHeight=height;if(old!=null)old.Dispose();PositionTransport();Invalidate();}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);DrawPreview(e.Graphics,ClientSize);}
 public void DrawPreview(Graphics graphics,Size viewport){
  graphics.Clear(BackColor);
  var frame=FrameBounds(viewport,frameWidth,frameHeight,ShowFooter);
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
  if(ShowFooter)TextRenderer.DrawText(graphics,Footer,Font,new Rectangle(12,viewport.Height-24,Math.Max(1,viewport.Width-24),22),Color.Gainsboro,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);
 }
 protected override void Dispose(bool disposing){if(disposing&&image!=null){image.Dispose();image=null;}base.Dispose(disposing);}
}
public sealed class PreviewDragEventArgs:EventArgs {public readonly PointF Delta;public PreviewDragEventArgs(PointF delta){Delta=delta;}}
public sealed class PreviewZoomEventArgs:EventArgs {public readonly int Delta;public readonly bool Fine;public PreviewZoomEventArgs(int delta,bool fine){Delta=delta;Fine=fine;}}
}
