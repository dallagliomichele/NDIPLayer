using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ImageNdi {
internal sealed class BrowserListView:ListView {
 const int LeftButtonDown=0x0201,GetItemState=0x102C,SelectedState=0x0002;
 [System.Runtime.InteropServices.DllImport("user32.dll")]
 static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr index,IntPtr mask);
 // Query the native selection without retrieving the virtual item again.
 public bool IsIndexSelected(int index){return IsHandleCreated&&(SendMessage(Handle,GetItemState,(IntPtr)index,(IntPtr)SelectedState).ToInt64()&SelectedState)!=0;}
 public Func<Keys,bool> Navigation;
 public Func<System.Drawing.Point,bool> BeforeLeftClick;
 protected override void WndProc(ref Message message){
  if(message.Msg==LeftButtonDown&&BeforeLeftClick!=null){
   long point=message.LParam.ToInt64();
   if(BeforeLeftClick(new System.Drawing.Point((short)point,(short)(point>>16))))return;
  }
  base.WndProc(ref message);
 }
 protected override bool ProcessCmdKey(ref Message message,Keys key){if(Navigation!=null&&Navigation(key))return true;return base.ProcessCmdKey(ref message,key);}
}
public sealed partial class LibraryBrowser {
 Dictionary<string,string> sortStates=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 List<string> sourceFiles=new List<string>();int sortColumn=-1;bool sortDescending;
 static readonly string[] ColumnNames={"Immagine","Numero","Stato","Percorso"};
 public bool Sorted {get{return sortColumn>=0;}}
 public List<string> PathsInView {get{return new List<string>(paths);}}
 public bool Navigate(int direction){if(paths.Count==0)return false;int at=paths.FindIndex(p=>String.Equals(p,SelectedPath,StringComparison.OrdinalIgnoreCase));int next=at<0?0:at+Math.Sign(direction);if(next<0||next>=paths.Count)return false;SelectIndex(next);return true;}
 public void SelectFirst(){if(paths.Count>0)SelectIndex(0);}
 void SelectIndex(int index){allSelected=false;view.SelectedIndices.Clear();view.SelectedIndices.Add(index);view.Items[index].Focused=true;view.EnsureVisible(index);}
 bool allSelected,updatingSelection;
 string previewBeforeSelectAll;
 public void SelectAllVisible(){
  if(paths.Count==0||allSelected)return;
  previewBeforeSelectAll=SelectedPath;
  allSelected=true;
  UpdateSelection(delegate{for(int i=0;i<paths.Count;i++)view.SelectedIndices.Add(i);});
 }
 void UpdateSelection(Action change){
  updatingSelection=true;
  try{change();}finally{updatingSelection=false;view.Invalidate();}
 }
 bool BeforeLibraryClick(System.Drawing.Point location){
  if(!allSelected)return false;
  if(view.HitTest(location).Item!=null){allSelected=false;return false;}
  string previous=previewBeforeSelectAll;
  UpdateSelection(delegate{SelectPaths(previous==null?new string[0]:new[]{previous});});
  return true;
 }
 public bool NavigateKey(Keys key){if(key==(Keys.Control|Keys.A)){SelectAllVisible();return true;}if(!ThumbnailsEnabled||(key!=Keys.Left&&key!=Keys.Right))return false;Navigate(key==Keys.Right?1:-1);return true;}
 public void SortByColumn(int column){if(column<0||column>3)return;sortDescending=sortColumn==column?!sortDescending:false;sortColumn=column;ReorderView();}
 public void RestoreManualOrder(){sortColumn=-1;sortDescending=false;ReorderView();}
 void ReorderView(){var chosen=SelectedPaths;string focus=SelectedPath;SetFiles(new List<string>(sourceFiles));SelectPaths(chosen);if(focus!=null){int at=paths.FindIndex(p=>String.Equals(p,focus,StringComparison.OrdinalIgnoreCase));if(at>=0){view.Items[at].Focused=true;view.EnsureVisible(at);}}for(int i=0;i<4;i++)view.Columns[i].Text=ColumnNames[i]+(i==sortColumn?(sortDescending?" ▼":" ▲"):"");}
 List<string> OrderFiles(List<string> input){if(sortColumn<0)return new List<string>(input);return input.OrderBy(p=>p,Comparer<string>.Create(CompareFiles)).ToList();}
 int CompareFiles(string a,string b){int result;if(sortColumn==1){long na,nb;bool ha=Int64.TryParse(NumberFor(a),out na),hb=Int64.TryParse(NumberFor(b),out nb);if(ha!=hb)return ha?-1:1;result=ha?na.CompareTo(nb):0;}else{string va=sortColumn==0?AssetTitle(a):sortColumn==2?sortStates[a]:SourcePath(a);string vb=sortColumn==0?AssetTitle(b):sortColumn==2?sortStates[b]:SourcePath(b);result=StringComparer.OrdinalIgnoreCase.Compare(va,vb);}return sortDescending?-result:result;}
}
}
