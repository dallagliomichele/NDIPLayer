using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ImageNdi {
internal sealed class BrowserListView:ListView {
 public Func<Keys,bool> Navigation;
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
 void SelectIndex(int index){view.SelectedIndices.Clear();view.SelectedIndices.Add(index);view.Items[index].Focused=true;view.EnsureVisible(index);}
 public bool NavigateKey(Keys key){if(!ThumbnailsEnabled||(key!=Keys.Left&&key!=Keys.Right))return false;Navigate(key==Keys.Right?1:-1);return true;}
 public void SortByColumn(int column){if(column<0||column>3)return;sortDescending=sortColumn==column?!sortDescending:false;sortColumn=column;ReorderView();}
 public void RestoreManualOrder(){sortColumn=-1;sortDescending=false;ReorderView();}
 void ReorderView(){var chosen=SelectedPaths;string focus=SelectedPath;SetFiles(new List<string>(sourceFiles));SelectPaths(chosen);if(focus!=null){int at=paths.FindIndex(p=>String.Equals(p,focus,StringComparison.OrdinalIgnoreCase));if(at>=0){view.Items[at].Focused=true;view.EnsureVisible(at);}}for(int i=0;i<4;i++)view.Columns[i].Text=ColumnNames[i]+(i==sortColumn?(sortDescending?" ▼":" ▲"):"");}
 List<string> OrderFiles(List<string> input){if(sortColumn<0)return new List<string>(input);return input.OrderBy(p=>p,Comparer<string>.Create(CompareFiles)).ToList();}
 int CompareFiles(string a,string b){int result;if(sortColumn==1){long na,nb;bool ha=Int64.TryParse(NumberFor(a),out na),hb=Int64.TryParse(NumberFor(b),out nb);if(ha!=hb)return ha?-1:1;result=ha?na.CompareTo(nb):0;}else{string va=sortColumn==0?Path.GetFileName(a):sortColumn==2?sortStates[a]:a;string vb=sortColumn==0?Path.GetFileName(b):sortColumn==2?sortStates[b]:b;result=StringComparer.OrdinalIgnoreCase.Compare(va,vb);}return sortDescending?-result:result;}
}
}
