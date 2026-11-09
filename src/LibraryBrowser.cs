using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageNdi {
// Virtual items and a bounded thumbnail cache keep large folders responsive.
public sealed class LibraryBrowser:UserControl {
 const int CacheLimit=128;
 readonly ListView view=new ListView{Dock=DockStyle.Fill,VirtualMode=true,MultiSelect=false,HideSelection=false,ShowItemToolTips=true};
 readonly ComboBox mode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=100};
 readonly TrackBar size=new TrackBar{Minimum=64,Maximum=256,Value=128,SmallChange=16,LargeChange=32,TickFrequency=64,Width=115,Height=30};
 readonly Label sizeLabel=new Label{AutoSize=true};
 readonly Dictionary<string,int> cache=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
 readonly HashSet<string> requested=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 readonly Queue<string> order=new Queue<string>();
 List<string> paths=new List<string>(); ImageList images; int generation;
 CancellationTokenSource cancellation; BlockingCollection<string> queue;
 public event EventHandler SelectionChanged;
 public event EventHandler PreferencesChanged;
 public string SelectedPath {get{return view.SelectedIndices.Count==0?null:paths[view.SelectedIndices[0]];}}
 public bool ThumbnailsEnabled {get{return mode.SelectedIndex==1;}}
 public int ThumbnailSize {get{return size.Value;}}
 public int Count {get{return paths.Count;}}
 public LibraryBrowser(){
  mode.Items.AddRange(new object[]{"Elenco","Miniature"});mode.SelectedIndex=0;
  var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=70,AutoScroll=true};
  bar.Controls.Add(mode);bar.Controls.Add(new Label{Text="Dimensione",AutoSize=true,Margin=new Padding(3,8,0,0)});bar.Controls.Add(size);bar.Controls.Add(sizeLabel);
  Controls.Add(view);Controls.Add(bar);
  view.Columns.Add("Immagine",180);view.Columns.Add("Percorso",400);view.View=View.Details;view.FullRowSelect=true;
  view.RetrieveVirtualItem+=Retrieve;
  view.SelectedIndexChanged+=delegate{if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);};
  mode.SelectedIndexChanged+=delegate{view.View=ThumbnailsEnabled?View.LargeIcon:View.Details;size.Enabled=ThumbnailsEnabled;NotifyPreferences();view.Invalidate();};
  size.ValueChanged+=delegate{sizeLabel.Text=size.Value+" px";};
  size.MouseUp+=delegate{RebuildCache();NotifyPreferences();};
  size.KeyUp+=delegate{RebuildCache();NotifyPreferences();};
  sizeLabel.Text="128 px";size.Enabled=false;
 }
 public void SetPreferences(bool thumbnails,int pixels){size.Value=Math.Max(64,Math.Min(256,pixels));mode.SelectedIndex=thumbnails?1:0;RebuildCache();}
 void NotifyPreferences(){if(PreferencesChanged!=null)PreferencesChanged(this,EventArgs.Empty);}
 public void SetFiles(List<string> files){string previous=SelectedPath;view.SelectedIndices.Clear();view.VirtualListSize=0;paths=new List<string>(files);RebuildCache();view.VirtualListSize=paths.Count;int index=previous==null?-1:paths.FindIndex(p=>String.Equals(p,previous,StringComparison.OrdinalIgnoreCase));if(index<0&&paths.Count>0)index=0;if(index>=0){view.SelectedIndices.Add(index);view.EnsureVisible(index);}view.Invalidate();}
 void Retrieve(object sender,RetrieveVirtualItemEventArgs e){
  if(e.ItemIndex<0||e.ItemIndex>=paths.Count){e.Item=new ListViewItem("");return;}
  string path=paths[e.ItemIndex];var item=new ListViewItem(Path.GetFileName(path));item.SubItems.Add(path);item.ToolTipText=path;
  if(ThumbnailsEnabled){int index;item.ImageIndex=cache.TryGetValue(path,out index)?index:0;if(item.ImageIndex==0&&queue!=null&&requested.Add(path))queue.Add(path);}
  e.Item=item;
 }
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
}
