using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

namespace ImageNdi {
// Virtual items and a bounded thumbnail cache keep large folders responsive.
public sealed class LibraryBrowser:UserControl {
 const int CacheLimit=128;
 readonly ListView view=new ListView{Dock=DockStyle.Fill,VirtualMode=true,MultiSelect=true,HideSelection=false,ShowItemToolTips=true,AllowDrop=true,OwnerDraw=true};
 readonly ComboBox mode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=100};
 readonly TrackBar size=new TrackBar{Minimum=64,Maximum=256,Value=128,SmallChange=16,LargeChange=32,TickFrequency=64,Width=115,Height=30};
 readonly Label sizeLabel=new Label{AutoSize=true};
 readonly Dictionary<string,int> cache=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
 readonly HashSet<string> requested=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 readonly Queue<string> order=new Queue<string>();
 List<string> paths=new List<string>(); ImageList images; int generation;
 CancellationTokenSource cancellation; BlockingCollection<string> queue;
 public event EventHandler SelectionChanged;
 public event EventHandler ImageDoubleClicked;
 public event EventHandler PreferencesChanged;
 public event EventHandler<LibraryDropEventArgs> DropRequested;
 readonly HashSet<string> onAir=new HashSet<string>(StringComparer.OrdinalIgnoreCase);bool transitioning;
 public List<string> SelectedPaths {get{var result=new List<string>();foreach(int index in view.SelectedIndices)if(index>=0&&index<paths.Count)result.Add(paths[index]);return result;}}
 public string SelectedPath {get{if(view.SelectedIndices.Count==0)return null;int index=view.FocusedItem!=null&&view.FocusedItem.Selected?view.FocusedItem.Index:view.SelectedIndices[0];return index>=0&&index<paths.Count?paths[index]:null;}}
 public bool ThumbnailsEnabled {get{return mode.SelectedIndex==1;}}
 public int ThumbnailSize {get{return size.Value;}}
 public int Count {get{return paths.Count;}}
 public LibraryBrowser(){
  mode.Items.AddRange(new object[]{"Elenco","Miniature"});mode.SelectedIndex=0;
  var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=70,AutoScroll=true};
  bar.Controls.Add(mode);bar.Controls.Add(new Label{Text="Dimensione",AutoSize=true,Margin=new Padding(3,8,0,0)});bar.Controls.Add(size);bar.Controls.Add(sizeLabel);
  bar.Controls.Add(new Label{Text="Clic: seleziona · Doppio clic: manda in onda",AutoSize=true});
  Controls.Add(view);Controls.Add(bar);
  view.Columns.Add("Immagine",180);view.Columns.Add("Stato",170);view.Columns.Add("Percorso",400);view.View=View.Details;view.FullRowSelect=true;
  view.DrawColumnHeader+=delegate(object sender,DrawListViewColumnHeaderEventArgs e){e.DrawDefault=true;};view.DrawItem+=DrawItem;view.DrawSubItem+=DrawSubItem;
  view.ItemDrag+=delegate{if(SelectedPaths.Count>0)view.DoDragDrop(new DataObject("NDIPlayer.Images",SelectedPaths.ToArray()),DragDropEffects.Copy|DragDropEffects.Move);};
  view.DragEnter+=delegate(object sender,DragEventArgs e){e.Effect=e.Data.GetDataPresent("NDIPlayer.Images")?DragDropEffects.Move:DragDropEffects.None;};
  view.DragDrop+=delegate(object sender,DragEventArgs e){if(!e.Data.GetDataPresent("NDIPlayer.Images"))return;var hit=view.HitTest(view.PointToClient(new Point(e.X,e.Y)));string before=hit.Item==null?null:paths[hit.Item.Index];if(DropRequested!=null)DropRequested(this,new LibraryDropEventArgs((string[])e.Data.GetData("NDIPlayer.Images"),before));};
  view.RetrieveVirtualItem+=Retrieve;
  view.SelectedIndexChanged+=delegate{view.Invalidate();if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);};
  view.MouseDoubleClick+=delegate(object sender,MouseEventArgs e){
   if(e.Button!=MouseButtons.Left)return;var hit=view.HitTest(e.Location);
   if(hit.Item==null||hit.Item.Index<0||hit.Item.Index>=paths.Count)return;
   int index=hit.Item.Index;if(view.SelectedIndices.Count!=1||view.SelectedIndices[0]!=index){view.SelectedIndices.Clear();view.SelectedIndices.Add(index);}
   if(ImageDoubleClicked!=null)ImageDoubleClicked(this,EventArgs.Empty);
  };
  mode.SelectedIndexChanged+=delegate{view.View=ThumbnailsEnabled?View.LargeIcon:View.Details;size.Enabled=ThumbnailsEnabled;NotifyPreferences();view.Invalidate();};
  size.ValueChanged+=delegate{sizeLabel.Text=size.Value+" px";};
  size.MouseUp+=delegate{RebuildCache();NotifyPreferences();};
  size.KeyUp+=delegate{RebuildCache();NotifyPreferences();};
  sizeLabel.Text="128 px";size.Enabled=false;
 }
 public void SetPreferences(bool thumbnails,int pixels){size.Value=Math.Max(64,Math.Min(256,pixels));mode.SelectedIndex=thumbnails?1:0;RebuildCache();}
 public void SelectPaths(IEnumerable<string> selected){var set=new HashSet<string>(selected,StringComparer.OrdinalIgnoreCase);view.SelectedIndices.Clear();for(int i=0;i<paths.Count;i++)if(set.Contains(paths[i]))view.SelectedIndices.Add(i);}
 public string IndicatorsFor(string path){bool selected=SelectedPaths.Contains(path,StringComparer.OrdinalIgnoreCase);string text=(selected?"SELEZIONATA":"")+(onAir.Contains(path)?(selected?" · ":"")+(transitioning?"IN DISSOLVENZA":"IN ONDA"):"");if(!File.Exists(path))text+=(text.Length>0?" · ":"")+"FILE MANCANTE";return text;}
 public void SetOnAir(OnAirStatus status){var incoming=new HashSet<string>(status.Paths,StringComparer.OrdinalIgnoreCase);if(transitioning==status.Transitioning&&onAir.SetEquals(incoming))return;onAir.Clear();onAir.UnionWith(incoming);transitioning=status.Transitioning;view.Invalidate();}
 void NotifyPreferences(){if(PreferencesChanged!=null)PreferencesChanged(this,EventArgs.Empty);}
 public void SetFiles(List<string> files){string previous=SelectedPath;view.SelectedIndices.Clear();view.VirtualListSize=0;paths=new List<string>(files);RebuildCache();view.VirtualListSize=paths.Count;int index=previous==null?-1:paths.FindIndex(p=>String.Equals(p,previous,StringComparison.OrdinalIgnoreCase));if(index<0&&paths.Count>0)index=0;if(index>=0){view.SelectedIndices.Add(index);view.EnsureVisible(index);}view.Invalidate();}
 void Retrieve(object sender,RetrieveVirtualItemEventArgs e){
  if(e.ItemIndex<0||e.ItemIndex>=paths.Count){e.Item=new ListViewItem("");return;}
  string path=paths[e.ItemIndex];var item=new ListViewItem(Path.GetFileName(path));item.SubItems.Add(IndicatorsFor(path));item.SubItems.Add(path);item.ToolTipText=path+(File.Exists(path)?"":" · FILE MANCANTE");
  if(ThumbnailsEnabled){int index;item.ImageIndex=cache.TryGetValue(path,out index)?index:0;if(item.ImageIndex==0&&queue!=null&&requested.Add(path))queue.Add(path);}
  e.Item=item;
 }
 void DrawItem(object sender,DrawListViewItemEventArgs e){if(view.View==View.Details)return;string path=paths[e.ItemIndex];bool selected=view.SelectedIndices.Contains(e.ItemIndex),live=onAir.Contains(path);using(var background=new SolidBrush(selected?Color.FromArgb(221,241,255):Color.White))e.Graphics.FillRectangle(background,e.Bounds);
  var icon=view.GetItemRect(e.ItemIndex,ItemBoundsPortion.Icon);int index=e.Item.ImageIndex;if(images!=null&&index>=0&&index<images.Images.Count)using(var bitmap=images.Images[index])e.Graphics.DrawImage(bitmap,icon.Left+(icon.Width-size.Value)/2,icon.Top,size.Value,size.Value);
  var label=new Rectangle(e.Bounds.Left+5,Math.Min(icon.Bottom+1,e.Bounds.Bottom-19),Math.Max(1,e.Bounds.Width-10),18);TextRenderer.DrawText(e.Graphics,e.Item.Text,view.Font,label,File.Exists(path)?Color.Black:Color.Gray,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
  if(selected)using(var pen=new Pen(Color.DeepSkyBlue,3))e.Graphics.DrawRectangle(pen,e.Bounds.Left+2,e.Bounds.Top+2,Math.Max(1,e.Bounds.Width-5),Math.Max(1,e.Bounds.Height-5));
  if(live){using(var pen=new Pen(Color.Firebrick,3))e.Graphics.DrawRectangle(pen,e.Bounds.Left+6,e.Bounds.Top+6,Math.Max(1,e.Bounds.Width-13),Math.Max(1,e.Bounds.Height-13));var tag=new Rectangle(e.Bounds.Left+7,e.Bounds.Top+7,Math.Max(1,e.Bounds.Width-14),18);e.Graphics.FillRectangle(Brushes.Firebrick,tag);TextRenderer.DrawText(e.Graphics,transitioning?"DISSOLVENZA":"IN ONDA",view.Font,tag,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);}
 }
 void DrawSubItem(object sender,DrawListViewSubItemEventArgs e){if(view.View!=View.Details)return;string path=paths[e.ItemIndex];bool selected=view.SelectedIndices.Contains(e.ItemIndex),live=onAir.Contains(path);using(var brush=new SolidBrush(selected?Color.FromArgb(221,241,255):live?Color.FromArgb(255,230,230):Color.White))e.Graphics.FillRectangle(brush,e.Bounds);string text=e.ColumnIndex==1?IndicatorsFor(path):e.SubItem.Text;TextRenderer.DrawText(e.Graphics,text,view.Font,new Rectangle(e.Bounds.X+5,e.Bounds.Y,e.Bounds.Width-5,e.Bounds.Height),e.ColumnIndex==1&&live?Color.Firebrick:Color.Black,TextFormatFlags.EndEllipsis|TextFormatFlags.VerticalCenter);if(e.ColumnIndex==0){if(live)e.Graphics.FillRectangle(Brushes.Firebrick,e.Bounds.Left,e.Bounds.Top,4,e.Bounds.Height);if(selected)e.Graphics.DrawRectangle(Pens.DeepSkyBlue,e.Bounds.Left+1,e.Bounds.Top,Math.Max(1,e.Bounds.Width-2),Math.Max(1,e.Bounds.Height-1));}}
 void RebuildCache(){
  generation++;CancelWorker();
  cache.Clear();requested.Clear();order.Clear();
  var old=images;images=new ImageList{ColorDepth=ColorDepth.Depth32Bit,ImageSize=new Size(size.Value,size.Value)};
  var imageHandle=images.Handle;
  using(var placeholder=new Bitmap(size.Value,size.Value))using(var g=Graphics.FromImage(placeholder)){g.Clear(Color.FromArgb(50,54,60));g.DrawString("…",Font,Brushes.White,10,10);images.Images.Add(placeholder);}
  view.LargeImageList=images;if(old!=null)old.Dispose();
  cancellation=new CancellationTokenSource();queue=new BlockingCollection<string>();
  var tokenSource=cancellation;var work=queue;var token=tokenSource.Token;int version=generation,edge=size.Value;
  Task.Factory.StartNew(delegate{
   try{foreach(string path in work.GetConsumingEnumerable(token)){
    Bitmap thumb=null;try{thumb=Renderer.ToBitmap(Renderer.Render(path,edge,edge,new Placement()),edge,edge);}catch(Exception){thumb=new Bitmap(edge,edge);using(var g=Graphics.FromImage(thumb)){g.Clear(Color.DimGray);g.DrawString("Non leggibile",SystemFonts.DefaultFont,Brushes.White,4,edge/2);}}
    var ready=thumb;
    if(token.IsCancellationRequested){ready.Dispose();break;}
    var completed=new TaskCompletionSource<bool>();
    try{BeginInvoke((Action)delegate{
     try{if(IsDisposed||version!=generation)return;requested.Remove(path);int index;
      if(cache.Count>=CacheLimit){string evicted=order.Dequeue();index=cache[evicted];cache.Remove(evicted);images.Images[index]=ready;}else{images.Images.Add(ready);index=images.Images.Count-1;}
      cache[path]=index;order.Enqueue(path);view.Invalidate();
     }finally{ready.Dispose();completed.TrySetResult(true);}
    });completed.Task.Wait(token);}catch(InvalidOperationException){ready.Dispose();break;}
   }}catch(OperationCanceledException){}finally{work.Dispose();tokenSource.Dispose();}
  },CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);
  view.Invalidate();
 }
 void CancelWorker(){if(cancellation!=null)try{cancellation.Cancel();}catch(ObjectDisposedException){}}
 protected override void Dispose(bool disposing){if(disposing){generation++;CancelWorker();view.LargeImageList=null;if(images!=null){images.Dispose();images=null;}}base.Dispose(disposing);}
}
public sealed class LibraryDropEventArgs:EventArgs {public readonly string[] Paths;public readonly string Before;public LibraryDropEventArgs(string[] paths,string before){Paths=paths;Before=before;}}
}
