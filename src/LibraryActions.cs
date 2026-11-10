using System;
using System.IO;
using System.Windows.Forms;

namespace ImageNdi {
public partial class MainWindow {
 void InitializeLibraryActions(){files.ContextRequested+=delegate(object sender,LibraryContextEventArgs e){BuildImageMenu(e);};}
 void AddImagesToBlock(PlaylistBlock block,string[] paths){ProjectStore.AddFiles(block,paths);RefreshBlocks();RefreshVisible();files.SelectPaths(paths);Save();status.Text="Foto aggiunte al blocco «"+block.Name+"».";}
 void SetImageNumber(string path,string value){ImageNumbers.Set(settings,path,value);RefreshVisible();files.SelectPaths(new[]{path});Save();}
 void BuildImageMenu(LibraryContextEventArgs request){
  var add=new ToolStripMenuItem("Aggiungi a blocco…"){Name="AddToBlock"};
  foreach(var block in settings.Blocks){var target=block;var item=new ToolStripMenuItem(block.Name){Tag=block.Id};item.Click+=delegate{AddImagesToBlock(target,request.Paths);};add.DropDownItems.Add(item);}
  if(add.DropDownItems.Count>0)add.DropDownItems.Add(new ToolStripSeparator());
  var create=new ToolStripMenuItem("Nuovo blocco…"){Name="AddToNewBlock"};create.Click+=delegate{string name=AskText("Nuovo blocco","Nome del blocco","");if(String.IsNullOrWhiteSpace(name))return;AddImagesToBlock(ProjectStore.AddBlock(settings,name),request.Paths);};add.DropDownItems.Add(create);request.Menu.Items.Add(add);
  string path=request.FocusedPath;string number;ImageNumbers.Scope(settings).TryGetValue(path,out number);
  var edit=new ToolStripMenuItem(request.Paths.Length==1?(ActiveBlock==null?"Numero in Tutta la libreria…":"Numero nel blocco «"+ActiveBlock.Name+"»…"):"Numero per «"+Path.GetFileName(path)+"»…"){Name="EditImageNumber"};edit.Click+=delegate{string value=AskText("Numero foto","Numero di riferimento (vuoto per rimuovere)",number??"");if(value==null)return;try{SetImageNumber(path,value);}catch(ArgumentException error){MessageBox.Show(error.Message,"Numero foto");}};request.Menu.Items.Add(edit);
  var automatic=new ToolStripMenuItem("Numera automaticamente la selezione"){Name="AutoNumberImages",Enabled=request.Paths.Length>0};automatic.Click+=delegate{try{int count=ImageNumbers.AutoNumber(settings,request.Paths);RefreshVisible();files.SelectPaths(request.Paths);Save();status.Text=count+" file numerati nell'ordine visualizzato ("+(ActiveBlock==null?"Tutta la libreria":ActiveBlock.Name)+").";}catch(ArgumentException error){MessageBox.Show(error.Message,"Numerazione automatica");}};request.Menu.Items.Add(automatic);
  var remove=new ToolStripMenuItem("Rimuovi numero"){Name="RemoveImageNumber",Enabled=!String.IsNullOrEmpty(number)};remove.Click+=delegate{SetImageNumber(path,"");};request.Menu.Items.Add(remove);
 }
}
}
