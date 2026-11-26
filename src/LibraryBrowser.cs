using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

namespace ImageNdi {
// Virtual items and a bounded thumbnail cache keep large folders responsive.
public sealed partial class LibraryBrowser:UserControl {
 public Func<string,bool> IsModified;
 public void RefreshIndicators(){view.Invalidate();}
 const int CacheLimit=128;
 readonly BrowserListView view=new BrowserListView{Dock=DockStyle.Fill,VirtualMode=true,MultiSelect=true,HideSelection=false,ShowItemToolTips=true,AllowDrop=true,OwnerDraw=true};
 readonly ComboBox mode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=100};
 readonly TrackBar size=new TrackBar{AutoSize=false,Minimum=64,Maximum=256,Value=128,SmallChange=16,LargeChange=32,TickFrequency=64,Width=115,Height=30};
 readonly Label sizeLabel=new Label{AutoSize=true};
 readonly FlowLayoutPanel bar=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,AutoScroll=false,WrapContents=true};
 public Control Toolbar {get{return bar;}}
 sealed class MemoryThumbnail {internal int Index;internal string Key;}
 readonly Dictionary<string,MemoryThumbnail> cache=new Dictionary<string,MemoryThumbnail>(StringComparer.OrdinalIgnoreCase);
 readonly HashSet<string> stale=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 readonly HashSet<string> requested=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 readonly Queue<string> order=new Queue<string>();
 readonly ContextMenuStrip imageMenu=new ContextMenuStrip{Name="ImageContextMenu"};bool contextOutside;string contextPath;
 Dictionary<string,string> numbers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 List<string> paths=new List<string>(); ImageList images; bool shuttingDown;
 readonly ThumbnailWorker thumbnails;
 Dictionary<string,List<int>> thumbnailRows=new Dictionary<string,List<int>>(StringComparer.OrdinalIgnoreCase);
 string thumbnailDirectory;
 string[] thumbnailSources=new string[0];
 bool playerIdle=true;
 public event EventHandler SelectionChanged;
 public event EventHandler ImageDoubleClicked;
 public event EventHandler PreferencesChanged;
 public event EventHandler<LibraryDropEventArgs> DropRequested;
 public event EventHandler<LibraryContextEventArgs> ContextRequested;
 readonly HashSet<string> onAir=new HashSet<string>(StringComparer.OrdinalIgnoreCase);bool transitioning;
 bool darkMode;
 public void SetDarkMode(bool enabled){darkMode=enabled;Color background=enabled?Color.FromArgb(32,34,38):SystemColors.Window;Color foreground=enabled?Color.Gainsboro:SystemColors.WindowText;BackColor=background;ForeColor=foreground;view.BackColor=background;view.ForeColor=foreground;view.Invalidate();}
 internal Func<string,string> SourcePath=p=>p;
 internal Func<string,string> AssetTitle=Path.GetFileName;
 public List<string> SelectedPaths {get{var result=new List<string>();foreach(int index in view.SelectedIndices)if(index>=0&&index<paths.Count)result.Add(paths[index]);return result;}}
 public string SelectedPath {get{if(allSelected)return previewBeforeSelectAll;if(view.SelectedIndices.Count==0)return null;int index=view.FocusedItem!=null&&view.FocusedItem.Selected?view.FocusedItem.Index:view.SelectedIndices[0];return index>=0&&index<paths.Count?paths[index]:null;}}
 public bool ThumbnailsEnabled {get{return mode.SelectedIndex==1;}}
 public int ThumbnailSize {get{return size.Value;}}
 public int Count {get{return paths.Count;}}
 public LibraryBrowser(){
  thumbnails=new ThumbnailWorker(delegate{BeginInvoke((Action)ReceiveThumbnail);});
  mode.Items.AddRange(new object[]{"Elenco","Miniature"});mode.SelectedIndex=0;
  bar.Controls.Add(mode);bar.Controls.Add(new Label{Text="Dimensione",AutoSize=true,Margin=new Padding(3,8,0,0)});bar.Controls.Add(size);bar.Controls.Add(sizeLabel);

  var manual=new Button{Text="Ordine manuale",AutoSize=true};manual.Click+=delegate{RestoreManualOrder();};bar.Controls.Add(manual);view.Navigation=NavigateKey;view.BeforeLeftClick=BeforeLibraryClick;view.ColumnClick+=delegate(object sender,ColumnClickEventArgs e){SortByColumn(e.Column);};
  Controls.Add(view);Controls.Add(bar);
  var scrollBar=new LibraryScrollBar(view);Controls.Add(scrollBar);scrollBar.BringToFront();
  view.Columns.Add("Immagine",180);view.Columns.Add("Numero",75);view.Columns.Add("Stato",170);view.Columns.Add("Percorso",400);view.View=View.Details;view.FullRowSelect=true;
  view.ContextMenuStrip=imageMenu;
  view.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Right)return;var hit=view.HitTest(e.Location);contextOutside=hit.Item==null;contextPath=contextOutside?null:paths[hit.Item.Index];if(hit.Item==null)return;if(allSelected&&hit.Item.Selected)return;allSelected=false;if(!hit.Item.Selected){view.SelectedIndices.Clear();view.SelectedIndices.Add(hit.Item.Index);}hit.Item.Focused=true;if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);};
  imageMenu.Opening+=delegate(object sender,System.ComponentModel.CancelEventArgs e){string target=contextPath??SelectedPath;contextPath=null;if(contextOutside||target==null){contextOutside=false;e.Cancel=true;return;}foreach(ToolStripItem item in imageMenu.Items.Cast<ToolStripItem>().ToArray())item.Dispose();imageMenu.Items.Clear();if(ContextRequested!=null)ContextRequested(this,new LibraryContextEventArgs(imageMenu,SelectedPaths.ToArray(),target));e.Cancel=imageMenu.Items.Count==0;};
  view.DrawColumnHeader+=delegate(object sender,DrawListViewColumnHeaderEventArgs e){if(!darkMode){e.DrawDefault=true;return;}using(var fill=new SolidBrush(Color.FromArgb(47,49,54)))e.Graphics.FillRectangle(fill,e.Bounds);TextRenderer.DrawText(e.Graphics,e.Header.Text,view.Font,new Rectangle(e.Bounds.Left+6,e.Bounds.Top,e.Bounds.Width-8,e.Bounds.Height),Color.Gainsboro,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);using(var line=new Pen(Color.FromArgb(70,72,78)))e.Graphics.DrawLine(line,e.Bounds.Right-1,e.Bounds.Top,e.Bounds.Right-1,e.Bounds.Bottom);};view.DrawItem+=DrawItem;view.DrawSubItem+=DrawSubItem;
  view.ItemDrag+=delegate{if(SelectedPaths.Count>0)view.DoDragDrop(new DataObject("NDIPlayer.Images",SelectedPaths.ToArray()),DragDropEffects.Copy|DragDropEffects.Move);};
  view.DragEnter+=delegate(object sender,DragEventArgs e){e.Effect=e.Data.GetDataPresent("NDIPlayer.Images")?DragDropEffects.Move:DragDropEffects.None;};
  view.DragDrop+=delegate(object sender,DragEventArgs e){if(!e.Data.GetDataPresent("NDIPlayer.Images"))return;var hit=view.HitTest(view.PointToClient(new Point(e.X,e.Y)));string before=hit.Item==null?null:paths[hit.Item.Index];if(DropRequested!=null)DropRequested(this,new LibraryDropEventArgs((string[])e.Data.GetData("NDIPlayer.Images"),before));};
  view.RetrieveVirtualItem+=Retrieve;
  view.SelectedIndexChanged+=delegate{if(updatingSelection)return;allSelected=false;view.Invalidate();if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);};
  view.MouseDoubleClick+=delegate(object sender,MouseEventArgs e){
   if(e.Button!=MouseButtons.Left)return;var hit=view.HitTest(e.Location);
   if(hit.Item==null||hit.Item.Index<0||hit.Item.Index>=paths.Count)return;
   int index=hit.Item.Index;allSelected=false;if(view.SelectedIndices.Count!=1||view.SelectedIndices[0]!=index){view.SelectedIndices.Clear();view.SelectedIndices.Add(index);}
   if(ImageDoubleClicked!=null)ImageDoubleClicked(this,EventArgs.Empty);
  };
  mode.SelectedIndexChanged+=delegate{view.View=ThumbnailsEnabled?View.LargeIcon:View.Details;size.Enabled=ThumbnailsEnabled;if(!ThumbnailsEnabled)ResetThumbnailRequests();UpdatePrefetch();NotifyPreferences();view.Invalidate();};
  size.ValueChanged+=delegate{sizeLabel.Text=size.Value+" px";};
  size.MouseUp+=delegate{UpdateThumbnailSize();NotifyPreferences();};
  size.KeyUp+=delegate{UpdateThumbnailSize();NotifyPreferences();};
  sizeLabel.Text="128 px";size.Enabled=false;
  UpdateThumbnailSize();
 }
 public void SetPreferences(bool thumbnails,int pixels){size.Value=Math.Max(64,Math.Min(256,pixels));UpdateThumbnailSize();mode.SelectedIndex=thumbnails?1:0;}
 public void SetNumbers(IDictionary<string,string> values){numbers=new Dictionary<string,string>(values,StringComparer.OrdinalIgnoreCase);view.Invalidate();}
 public string NumberFor(string path){string number;return numbers.TryGetValue(path,out number)?number:"";}
 public string DisplayLabel(string path){string number=NumberFor(path);return (number.Length>0?"#"+number+" · ":"")+AssetTitle(path);}
 public void SelectPaths(IEnumerable<string> selected){allSelected=false;var set=new HashSet<string>(selected,StringComparer.OrdinalIgnoreCase);view.SelectedIndices.Clear();for(int i=0;i<paths.Count;i++)if(set.Contains(paths[i]))view.SelectedIndices.Add(i);}
 public string IndicatorsFor(string path){int index=paths.FindIndex(p=>String.Equals(p,path,StringComparison.OrdinalIgnoreCase));return IndicatorsFor(path,index>=0&&view.IsIndexSelected(index));}
 string IndicatorsFor(string path,bool selected){string text=(selected?"SELEZIONATA":"")+(onAir.Contains(path)?(selected?" · ":"")+(transitioning?"IN DISSOLVENZA":"IN ONDA"):"");if(IsModified!=null&&IsModified(path))text+=(text.Length>0?" · ":"")+"MODIFICATA";if(!File.Exists(SourcePath(path)))text+=(text.Length>0?" · ":"")+"FILE MANCANTE";return text;}
 public void SetOnAir(OnAirStatus status){var incoming=new HashSet<string>(status.Paths,StringComparer.OrdinalIgnoreCase);if(transitioning==status.Transitioning&&onAir.SetEquals(incoming))return;onAir.Clear();onAir.UnionWith(incoming);transitioning=status.Transitioning;view.Invalidate();}
 void NotifyPreferences(){if(PreferencesChanged!=null)PreferencesChanged(this,EventArgs.Empty);}
 public void SetFiles(List<string> files){
  if(sortColumn==2){
   var chosen=new HashSet<string>(SelectedPaths,StringComparer.OrdinalIgnoreCase);
   sortStates=files.Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(p=>p,p=>IndicatorsFor(p,chosen.Contains(p)),StringComparer.OrdinalIgnoreCase);
  }
  string previous=SelectedPath;
  view.BeginUpdate();
  try{
   allSelected=false;
   view.SelectedIndices.Clear();
   view.VirtualListSize=0;
   sourceFiles=new List<string>(files);
   paths=OrderFiles(sourceFiles);
   ResetThumbnailRequests();
   IndexThumbnailRows();
   view.VirtualListSize=paths.Count;
   int index=previous==null?-1:paths.FindIndex(p=>String.Equals(p,previous,StringComparison.OrdinalIgnoreCase));
   if(index<0&&paths.Count>0)index=0;
   if(index>=0){view.SelectedIndices.Add(index);view.EnsureVisible(index);}
  }finally{view.EndUpdate();}
  view.Invalidate();
 }
 void Retrieve(object sender,RetrieveVirtualItemEventArgs e){
  if(e.ItemIndex<0||e.ItemIndex>=paths.Count){e.Item=new ListViewItem("");return;}
  string path=paths[e.ItemIndex],source=SourcePath(path);var item=new ListViewItem(AssetTitle(path));item.SubItems.Add(NumberFor(path));item.SubItems.Add(IndicatorsFor(path,view.IsIndexSelected(e.ItemIndex)));item.SubItems.Add(source);item.ToolTipText=DisplayLabel(path)+"\n"+source+(File.Exists(source)?"":" · FILE MANCANTE");
  if(ThumbnailsEnabled){
   MemoryThumbnail thumbnail;
   cache.TryGetValue(source,out thumbnail);
   item.ImageIndex=thumbnail==null?0:thumbnail.Index;
   if(!shuttingDown&&IsHandleCreated&&(thumbnail==null||stale.Contains(source))&&requested.Add(source))
    thumbnails.Request(source,thumbnail==null?null:thumbnail.Key);
  }
  e.Item=item;
 }
 void DrawItem(object sender,DrawListViewItemEventArgs e){if(view.View==View.Details)return;string path=paths[e.ItemIndex];bool selected=view.IsIndexSelected(e.ItemIndex),live=onAir.Contains(path);using(var background=new SolidBrush(darkMode?(selected?Color.FromArgb(49,73,91):Color.FromArgb(32,34,38)):(selected?Color.FromArgb(221,241,255):Color.White)))e.Graphics.FillRectangle(background,e.Bounds);
  var icon=view.GetItemRect(e.ItemIndex,ItemBoundsPortion.Icon);int index=e.Item.ImageIndex;if(images!=null&&index>=0&&index<images.Images.Count)using(var bitmap=images.Images[index])e.Graphics.DrawImage(bitmap,icon.Left+(icon.Width-size.Value)/2,icon.Top,size.Value,size.Value);
  var label=new Rectangle(e.Bounds.Left+5,Math.Min(icon.Bottom+1,e.Bounds.Bottom-19),Math.Max(1,e.Bounds.Width-10),18);TextRenderer.DrawText(e.Graphics,DisplayLabel(path),view.Font,label,File.Exists(SourcePath(path))?(darkMode?Color.Gainsboro:Color.Black):Color.Gray,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
  if(IsModified!=null&&IsModified(path))using(var pen=new Pen(Color.Orange,2))e.Graphics.DrawRectangle(pen,icon.Left+1,icon.Top+1,Math.Max(1,icon.Width-3),Math.Max(1,icon.Height-3));
  if(selected)using(var pen=new Pen(Color.DeepSkyBlue,3))e.Graphics.DrawRectangle(pen,e.Bounds.Left+2,e.Bounds.Top+2,Math.Max(1,e.Bounds.Width-5),Math.Max(1,e.Bounds.Height-5));
  if(live){using(var pen=new Pen(Color.Firebrick,3))e.Graphics.DrawRectangle(pen,e.Bounds.Left+6,e.Bounds.Top+6,Math.Max(1,e.Bounds.Width-13),Math.Max(1,e.Bounds.Height-13));var tag=new Rectangle(e.Bounds.Left+7,e.Bounds.Top+7,Math.Max(1,e.Bounds.Width-14),18);e.Graphics.FillRectangle(Brushes.Firebrick,tag);TextRenderer.DrawText(e.Graphics,transitioning?"DISSOLVENZA":"IN ONDA",view.Font,tag,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);}
 }
 void DrawSubItem(object sender,DrawListViewSubItemEventArgs e){if(view.View!=View.Details)return;string path=paths[e.ItemIndex];bool selected=view.IsIndexSelected(e.ItemIndex),live=onAir.Contains(path);Color background=darkMode?(selected?Color.FromArgb(49,73,91):live?Color.FromArgb(70,39,43):VideoTools.IsVideo(SourcePath(path))?Color.FromArgb(42,44,48):Color.FromArgb(32,34,38)):(selected?Color.FromArgb(221,241,255):live?Color.FromArgb(255,230,230):VideoTools.IsVideo(SourcePath(path))?Color.FromArgb(246,246,246):Color.White);using(var brush=new SolidBrush(background))e.Graphics.FillRectangle(brush,e.Bounds);string text=e.ColumnIndex==2?IndicatorsFor(path,selected):e.ColumnIndex==1?NumberFor(path):e.SubItem.Text;TextRenderer.DrawText(e.Graphics,text,view.Font,new Rectangle(e.Bounds.X+5,e.Bounds.Y,e.Bounds.Width-5,e.Bounds.Height),e.ColumnIndex==2&&live?(darkMode?Color.FromArgb(255,125,125):Color.Firebrick):(darkMode?Color.Gainsboro:Color.Black),TextFormatFlags.EndEllipsis|TextFormatFlags.VerticalCenter);if(e.ColumnIndex==0){if(live)e.Graphics.FillRectangle(Brushes.Firebrick,e.Bounds.Left,e.Bounds.Top,4,e.Bounds.Height);if(selected)e.Graphics.DrawRectangle(Pens.DeepSkyBlue,e.Bounds.Left+1,e.Bounds.Top,Math.Max(1,e.Bounds.Width-2),Math.Max(1,e.Bounds.Height-1));}}
 void IndexThumbnailRows(){
  thumbnailRows.Clear();
  for(int i=0;i<paths.Count;i++){
   string source=SourcePath(paths[i]);
   List<int> rows;
   if(!thumbnailRows.TryGetValue(source,out rows))thumbnailRows[source]=rows=new List<int>();
   rows.Add(i);
  }
 }

 internal void SetThumbnailLibrary(string directory,IEnumerable<string> sources){
  bool changed=!String.Equals(directory,thumbnailDirectory,StringComparison.OrdinalIgnoreCase);
  thumbnailDirectory=directory;
  thumbnailSources=sources.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
  ConfigureThumbnails();
  if(changed)ResetThumbnailImages();
  else stale.UnionWith(cache.Keys);
  view.Invalidate();
 }

 internal void SetThumbnailPlayerIdle(bool idle){
  playerIdle=idle;
  UpdatePrefetch();
 }

 void UpdatePrefetch(){thumbnails.SetPrefetchAllowed(playerIdle&&ThumbnailsEnabled);}

 void ConfigureThumbnails(){
  requested.Clear();
  thumbnails.Configure(thumbnailDirectory,size.Value,thumbnailSources);
 }

 void ResetThumbnailRequests(){
  requested.Clear();
  thumbnails.ResetVisible();
 }

 void UpdateThumbnailSize(){
  if(images!=null&&images.ImageSize.Width==size.Value)return;
  ConfigureThumbnails();
  ResetThumbnailImages();
  view.Invalidate();
 }

 internal Task ClearThumbnailCache(){
  var cleared=thumbnails.Clear();
  requested.Clear();
  ResetThumbnailImages();
  view.Invalidate();
  return cleared;
 }

 void ResetThumbnailImages(){
  cache.Clear();
  stale.Clear();
  order.Clear();
  var old=images;
  images=new ImageList{ColorDepth=ColorDepth.Depth32Bit,ImageSize=new Size(size.Value,size.Value)};
  var handle=images.Handle;
  using(var placeholder=new Bitmap(size.Value,size.Value))
  using(var graphics=Graphics.FromImage(placeholder)){
   graphics.Clear(Color.FromArgb(50,54,60));
   graphics.DrawString("…",Font,Brushes.White,10,10);
   images.Images.Add(placeholder);
  }
  view.LargeImageList=images;
  if(old!=null)old.Dispose();
 }

 void ReceiveThumbnail(){
  using(var ready=thumbnails.TakeResult()){
   if(ready==null||shuttingDown)return;
   requested.Remove(ready.Source);
   stale.Remove(ready.Source);
   if(ready.Unchanged){
    if(!cache.ContainsKey(ready.Source))InvalidateThumbnail(ready.Source);
    return;
   }
   MemoryThumbnail thumbnail;
   if(!cache.TryGetValue(ready.Source,out thumbnail)){
    thumbnail=new MemoryThumbnail();
    if(cache.Count>=CacheLimit){
     string evicted=order.Dequeue();
     thumbnail.Index=cache[evicted].Index;
     cache.Remove(evicted);
     stale.Remove(evicted);
     InvalidateThumbnail(evicted);
    }
    cache.Add(ready.Source,thumbnail);
    order.Enqueue(ready.Source);
   }
   thumbnail.Key=ready.Key;
   if(ready.Image==null){
    using(var failure=new Bitmap(images.ImageSize.Width,images.ImageSize.Height))
    using(var graphics=Graphics.FromImage(failure)){
     graphics.Clear(Color.DimGray);
     graphics.DrawString("Non leggibile",SystemFonts.DefaultFont,Brushes.White,4,failure.Height/2);
     SetThumbnailImage(thumbnail,failure);
    }
   }else SetThumbnailImage(thumbnail,ready.Image);
   InvalidateThumbnail(ready.Source);
  }
 }

 void SetThumbnailImage(MemoryThumbnail thumbnail,Bitmap bitmap){
  if(thumbnail.Index>0)images.Images[thumbnail.Index]=bitmap;
  else{
   images.Images.Add(bitmap);
   thumbnail.Index=images.Images.Count-1;
  }
 }

 void InvalidateThumbnail(string source){
  List<int> rows;
  if(!ThumbnailsEnabled||!view.IsHandleCreated||!thumbnailRows.TryGetValue(source,out rows))return;
  foreach(int row in rows)view.Invalidate(view.GetItemRect(row));
 }

 protected override void Dispose(bool disposing){
  if(disposing){
   shuttingDown=true;
   thumbnails.Dispose();
   imageMenu.Dispose();
   view.LargeImageList=null;
   if(images!=null){images.Dispose();images=null;}
  }
  base.Dispose(disposing);
 }
}
public sealed class LibraryDropEventArgs:EventArgs {public readonly string[] Paths;public readonly string Before;public LibraryDropEventArgs(string[] paths,string before){Paths=paths;Before=before;}}
public sealed class LibraryContextEventArgs:EventArgs {public readonly ContextMenuStrip Menu;public readonly string[] Paths;public readonly string FocusedPath;public LibraryContextEventArgs(ContextMenuStrip menu,string[] paths,string focused){Menu=menu;Paths=paths;FocusedPath=focused;}}
}
