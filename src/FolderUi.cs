using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageNdi {
public partial class MainWindow {
 readonly Label folderNotification=new Label{Name="FolderChanges",Text="●",ForeColor=Color.Red,AutoSize=true,Visible=false,Margin=new Padding(3,7,4,0),AccessibleName="Modifiche cartella sorgente"};
 readonly ToolTip folderTooltip=new ToolTip();
 readonly ContextMenuStrip sourceFolders=new ContextMenuStrip();
 FileSystemWatcher folderWatcher;
 Dictionary<string,Tuple<long,long,long,FileAttributes>> folderBaseline;
 Task<Dictionary<string,Tuple<long,long,long,FileAttributes>>> folderCheck;
 int folderChanged,folderWatchFailed;
 DateTime folderCheckDue;

 List<string> ScanObservedFolder(List<string> warnings){
  if(folderWatcher!=null){folderWatcher.Dispose();folderWatcher=null;}
  folderCheck=null;
  folderCheckDue=DateTime.UtcNow;
  Interlocked.Exchange(ref folderChanged,0);
  Interlocked.Exchange(ref folderWatchFailed,0);
  string root=settings.Root;
  if(!String.IsNullOrEmpty(root)){
   try{
    folderWatcher=new FileSystemWatcher(root){IncludeSubdirectories=true,NotifyFilter=NotifyFilters.FileName|NotifyFilters.DirectoryName|NotifyFilters.LastWrite|NotifyFilters.CreationTime|NotifyFilters.Size|NotifyFilters.Attributes};
    folderWatcher.Created+=FolderChanged;
    folderWatcher.Deleted+=FolderChanged;
    folderWatcher.Changed+=FolderChanged;
    folderWatcher.Renamed+=FolderChanged;
    folderWatcher.Error+=delegate{Interlocked.Exchange(ref folderWatchFailed,1);};
    folderWatcher.EnableRaisingEvents=true;
   }catch(ArgumentException){Interlocked.Exchange(ref folderWatchFailed,1);}
   catch(IOException){Interlocked.Exchange(ref folderWatchFailed,1);}
   catch(UnauthorizedAccessException){Interlocked.Exchange(ref folderWatchFailed,1);}
  }
  var paths=ReadFolder(root,warnings);
  folderBaseline=ReadFolderStamps(paths);
  SetFolderNotification(0);
  return paths;
 }
 static List<string> ReadFolder(string root,List<string> warnings){
  return String.IsNullOrEmpty(root)||!Directory.Exists(root)?new List<string>():Library.Scan(root,warnings);
 }
 static Dictionary<string,Tuple<long,long,long,FileAttributes>> ReadFolderStamps(IEnumerable<string> paths){
  var stamps=new Dictionary<string,Tuple<long,long,long,FileAttributes>>(StringComparer.OrdinalIgnoreCase);
  foreach(string path in paths){
   try{var file=new FileInfo(path);stamps[path]=Tuple.Create(file.Length,file.LastWriteTimeUtc.Ticks,file.CreationTimeUtc.Ticks,file.Attributes);}
   catch(IOException){}
   catch(UnauthorizedAccessException){}
  }
  return stamps;
 }
 void FolderChanged(object sender,FileSystemEventArgs args){Interlocked.Exchange(ref folderChanged,1);}
 void UpdateFolderNotification(){
  if(folderBaseline==null||String.IsNullOrEmpty(settings.Root))return;
  if(folderCheck!=null){
   if(!folderCheck.IsCompleted)return;
   if(folderCheck.Status==TaskStatus.RanToCompletion){
    var current=folderCheck.Result;
    int changed=current.Count(p=>!folderBaseline.ContainsKey(p.Key)||!p.Value.Equals(folderBaseline[p.Key]));
    changed+=folderBaseline.Keys.Count(p=>!current.ContainsKey(p));
    SetFolderNotification(changed);
   }else{var error=folderCheck.Exception;Interlocked.Exchange(ref folderWatchFailed,1);}
   folderCheck=null;
  }
  var now=DateTime.UtcNow;
  if(now<folderCheckDue)return;
  bool polling=Interlocked.CompareExchange(ref folderWatchFailed,0,0)!=0;
  if(Interlocked.Exchange(ref folderChanged,0)==0&&!polling)return;
  string root=settings.Root;
  folderCheckDue=now.AddMilliseconds(polling?5000:500);
  folderCheck=Task.Run(()=>ReadFolderStamps(ReadFolder(root,new List<string>())));
 }
 void SetFolderNotification(int count){
  string message=count==1?"1 file da aggiornare":count+" file da aggiornare";
  folderTooltip.SetToolTip(folderNotification,message);
  folderNotification.AccessibleDescription=message;
  folderNotification.Visible=count>0;
 }
 void DisposeFolderTools(){
  if(folderWatcher!=null)folderWatcher.Dispose();
  folderTooltip.Dispose();
  sourceFolders.Dispose();
 }

 List<string> FavoriteFolders{
  get{
   if(windowPreferences==null)windowPreferences=new WindowPreferences();
   if(windowPreferences.FavoriteFolders==null)windowPreferences.FavoriteFolders=new List<string>();
   return windowPreferences.FavoriteFolders;
  }
 }
 void ShowSourceFolders(string buttonText,bool relocate){
  var button=FindButton(this,buttonText);
  foreach(ToolStripItem item in sourceFolders.Items.Cast<ToolStripItem>().ToArray())item.Dispose();
  sourceFolders.Items.Clear();
  sourceFolders.Items.Add("Sfoglia…",null,delegate{
   string folder;
   try{folder=ExplorerFolderDialog.Pick(Handle,relocate?"Ricollega cartella":"Apri cartella",settings.Root);}
   catch(COMException error){status.Text="Impossibile aprire il selettore cartelle ("+error.ErrorCode.ToString("X8")+").";return;}
   if(folder!=null)ApplySourceFolder(folder,relocate);
  });
  sourceFolders.Items.Add(new ToolStripSeparator());
  var favorites=FavoriteFolders;
  var add=sourceFolders.Items.Add("Aggiungi cartella corrente ai preferiti",null,delegate{
   favorites.Add(settings.Root);SaveWindowPreferences();
  });
  add.Enabled=Directory.Exists(settings.Root)&&!favorites.Contains(settings.Root,StringComparer.OrdinalIgnoreCase);
  if(favorites.Count>0){
   var remove=new ToolStripMenuItem("Rimuovi dai preferiti");
   foreach(string folder in favorites){
    var favorite=sourceFolders.Items.Add(folder,null,delegate{ApplySourceFolder(folder,relocate);});
    favorite.Enabled=Directory.Exists(folder);
    remove.DropDownItems.Add(folder,null,delegate{favorites.Remove(folder);SaveWindowPreferences();});
   }
   sourceFolders.Items.Add(remove);
  }
  if(button!=null)sourceFolders.Show(button,new Point(0,button.Height));
 }
 void ApplySourceFolder(string folder,bool relocate){
  if(!Directory.Exists(folder)){status.Text="Cartella non disponibile.";return;}
  if(relocate){ProjectStore.Relocate(settings,folder);RefreshBlocks();}
  else settings.Root=folder;
  Scan();
  Save();
 }
}
}
