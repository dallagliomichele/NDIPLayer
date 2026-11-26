using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ImageNdi {
internal sealed class LibraryScrollBar : Control {
 const int VerticalBar = 1, VerticalBarObject = -5, UnavailableOrHidden = 0x18001;
 const int ScrollRangePagePosition = 7, ListScroll = 0x1014, MouseWheelMessage = 0x020A;
 readonly BrowserListView view;
 ScrollInfo scroll;
 bool hovered, dragging;
 int dragOffset;

 internal LibraryScrollBar(BrowserListView view) {
  this.view = view;
  Name = "LibraryVerticalScrollBar";
  AccessibleName = "Scorrimento verticale della libreria";
  AccessibleRole = AccessibleRole.ScrollBar;
  TabStop = false;
  Visible = false;
  SetStyle(ControlStyles.Selectable, false);
  SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
   ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
  view.BackColorChanged += delegate { Invalidate(); };
  view.ScrollLayoutChanged += Synchronize;
 }

 // Cover only the native gutter. Windows still lays out and scrolls the list.
 void Synchronize() {
  if (IsDisposed || Disposing || Parent == null || !view.IsHandleCreated) return;
  var bar = new ScrollBarInfo { Size = Marshal.SizeOf(typeof(ScrollBarInfo)) };
  var current = new ScrollInfo { Size = Marshal.SizeOf(typeof(ScrollInfo)), Mask = ScrollRangePagePosition };
  if (SystemInformation.HighContrast || !GetScrollBarInfo(view.Handle, VerticalBarObject, ref bar) ||
      (bar.State & UnavailableOrHidden) != 0 || !GetScrollInfo(view.Handle, VerticalBar, ref current) ||
      current.LastPosition <= current.Minimum) {
   if (Capture) Capture = false;
   hovered = false;
   Visible = false;
   return;
  }
  bool changed = current.Minimum != scroll.Minimum || current.Maximum != scroll.Maximum ||
     current.Page != scroll.Page || current.Position != scroll.Position;
  scroll = current;
  var origin = Parent.PointToClient(new Point(bar.Left, bar.Top));
  Bounds = new Rectangle(origin.X, origin.Y, bar.Right - bar.Left, bar.Bottom - bar.Top);
  Visible = true;
  if (changed) Invalidate();
 }

 Rectangle Thumb {
  get {
   int inset = Math.Max(1, Width / 6);
   int trackHeight = Math.Max(1, Height - 2 * inset);
   double extent = (double)scroll.Maximum - scroll.Minimum + 1;
   int height = Math.Min(trackHeight, Math.Max(Width, (int)Math.Round(trackHeight * scroll.Page / extent)));
   double position = (scroll.Position - (double)scroll.Minimum) / (scroll.LastPosition - (double)scroll.Minimum);
   int top = inset + (int)Math.Round((trackHeight - height) * position);
   int width = Math.Max(1, Width / (hovered || dragging ? 2 : 4));
   return new Rectangle(Width - inset - width, top, width, height);
  }
 }

 protected override void OnPaint(PaintEventArgs e) {
  bool dark = view.BackColor.GetBrightness() < 0.5f;
  e.Graphics.Clear(view.BackColor);
  if (scroll.LastPosition <= scroll.Minimum) return;
  bool active = hovered || dragging;
  var thumb = Thumb;
  if (active) {
   int inset = Math.Max(1, Width / 6);
   using (var track = new SolidBrush(dark ? Color.FromArgb(47, 49, 54) : Color.FromArgb(235, 237, 240)))
    e.Graphics.FillRectangle(track, thumb.X, inset, thumb.Width, Math.Max(1, Height - 2 * inset));
  }
  using (var fill = new SolidBrush(dark ? (active ? Color.Silver : Color.Gray) :
   (active ? Color.FromArgb(86, 94, 104) : Color.FromArgb(135, 142, 152))))
   e.Graphics.FillRectangle(fill, thumb);
 }

 protected override void OnMouseEnter(EventArgs e) {
  base.OnMouseEnter(e);
  hovered = true;
  Invalidate();
 }

 protected override void OnMouseLeave(EventArgs e) {
  base.OnMouseLeave(e);
  hovered = false;
  Invalidate();
 }

 protected override void OnMouseDown(MouseEventArgs e) {
  base.OnMouseDown(e);
  if (e.Button != MouseButtons.Left || scroll.LastPosition <= scroll.Minimum) return;
  var thumb = Thumb;
  if (e.Y < thumb.Top || e.Y >= thumb.Bottom) {
   MoveTo(scroll.Position + (e.Y < thumb.Top ? -1L : 1L) * Math.Max(1, scroll.Page));
   return;
  }
  dragOffset = e.Y - thumb.Top;
  dragging = true;
  Capture = true;
  Invalidate();
 }

 protected override void OnMouseMove(MouseEventArgs e) {
  base.OnMouseMove(e);
  if (!dragging) return;
  int inset = Math.Max(1, Width / 6);
  int travel = Height - 2 * inset - Thumb.Height;
  if (travel <= 0) return;
  double fraction = (e.Y - dragOffset - inset) / (double)travel;
  MoveTo(scroll.Minimum + (long)Math.Round(fraction * (scroll.LastPosition - (double)scroll.Minimum)));
 }

 protected override void OnMouseUp(MouseEventArgs e) {
  base.OnMouseUp(e);
  if (e.Button == MouseButtons.Left && dragging) Capture = false;
 }

 protected override void OnMouseCaptureChanged(EventArgs e) {
  base.OnMouseCaptureChanged(e);
  if (Capture) return;
  dragging = false;
  hovered = ClientRectangle.Contains(PointToClient(MousePosition));
  Invalidate();
 }

 protected override void WndProc(ref Message message) {
  if (message.Msg == MouseWheelMessage && view.IsHandleCreated) {
   message.Result = SendMessage(view.Handle, message.Msg, message.WParam, message.LParam);
   return;
  }
  base.WndProc(ref message);
 }

 void MoveTo(long position) {
  long delta = Math.Min(scroll.LastPosition, Math.Max(scroll.Minimum, position)) - scroll.Position;
  if (delta == 0 || view.VirtualListSize == 0) return;
  // Report view exposes row units. Icon view exposes pixels.
  if (view.View == View.Details) delta *= view.GetItemRect(0).Height;
  SendMessage(view.Handle, ListScroll, IntPtr.Zero, (IntPtr)delta);
 }

 [StructLayout(LayoutKind.Sequential)]
 struct ScrollInfo {
  public int Size, Mask, Minimum, Maximum, Page, Position, TrackPosition;
  public long LastPosition { get { return (long)Maximum - Math.Max(0, Page - 1); } }
 }

 [StructLayout(LayoutKind.Sequential)]
 struct ScrollBarInfo {
  public int Size, Left, Top, Right, Bottom, LineButton, ThumbTop, ThumbBottom, Reserved;
  public int State, StartArrowState, StartTrackState, ThumbState, EndTrackState, EndArrowState;
 }

 [DllImport("user32.dll")]
 static extern bool GetScrollBarInfo(IntPtr handle, int objectId, ref ScrollBarInfo info);
 [DllImport("user32.dll")]
 static extern bool GetScrollInfo(IntPtr handle, int bar, ref ScrollInfo info);
 [DllImport("user32.dll")]
 static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
}
}
