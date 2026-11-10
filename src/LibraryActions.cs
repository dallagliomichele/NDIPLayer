using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace ImageNdi {
public partial class MainWindow {
 void InitializeLibraryActions(){files.ContextRequested+=delegate(object sender,LibraryContextEventArgs e){BuildImageMenu(e);};}
 void AddImagesToBlock(PlaylistBlock block,string[] paths){ProjectStore.AddFiles(block,paths);RefreshBlocks();RefreshVisible();files.SelectPaths(paths);Save();status.Text="Foto aggiunte al blocco «"+block.Name+"».";}
 void SetImageNumber(string path,string value){ImageNumbers.Set(settings,path,value);RefreshVisible();files.SelectPaths(new[]{path});Save();}
 void NumberImagesFrom(string[] paths,int first){int count=ImageNumbers.AutoNumber(settings,paths,first);RefreshVisible();files.SelectPaths(paths);Save();status.Text=count+" file numerati da "+first+" nel blocco corrente.";}
 void ShowNumberStartDialog(string[] paths){using(var dialog=new Form{Text="Numerazione della selezione",Width=510,Height=235,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,StartPosition=FormStartPosition.CenterParent}){var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(12)};panel.Controls.Add(new Label{Text=paths.Length+" file · "+(ActiveBlock==null?"Tutta la libreria":ActiveBlock.Name),AutoSize=true});var first=Number(0,999999999,1);first.Width=125;first.Name="AutoNumberStart";ShellField(panel,"Numero iniziale",first);var range=new Label{AutoSize=true};Action update=delegate{range.Text="Sequenza: "+first.Value+" → "+(first.Value+paths.Length-1)+" · ordine visualizzato";};first.ValueChanged+=delegate{update();};update();panel.Controls.Add(range);panel.Controls.Add(new Label{Text="I numeri dei file non selezionati e degli altri blocchi restano invariati.",AutoSize=true,MaximumSize=new Size(460,0)});var buttons=ShellRow();buttons.Dock=DockStyle.None;buttons.AutoSize=true;buttons.AutoSizeMode=AutoSizeMode.GrowAndShrink;var apply=new Button{Text="Numera",AutoSize=true};apply.Click+=delegate{try{NumberImagesFrom(paths,(int)first.Value);dialog.DialogResult=DialogResult.OK;}catch(ArgumentException e){MessageBox.Show(dialog,e.Message,"Numerazione non applicata");}};buttons.Controls.Add(apply);var cancel=new Button{Text="Annulla",AutoSize=true,DialogResult=DialogResult.Cancel};buttons.Controls.Add(cancel);panel.Controls.Add(buttons);dialog.Controls.Add(panel);dialog.AcceptButton=apply;dialog.CancelButton=cancel;dialog.ShowDialog(this);}}
 void BuildImageMenu(LibraryContextEventArgs request){
  var add=new ToolStripMenuItem("Aggiungi a blocco…"){Name="AddToBlock"};
  foreach(var block in settings.Blocks){var target=block;var item=new ToolStripMenuItem(block.Name){Tag=block.Id};item.Click+=delegate{AddImagesToBlock(target,request.Paths);};add.DropDownItems.Add(item);}
  if(add.DropDownItems.Count>0)add.DropDownItems.Add(new ToolStripSeparator());
  var create=new ToolStripMenuItem("Nuovo blocco…"){Name="AddToNewBlock"};create.Click+=delegate{string name=AskText("Nuovo blocco","Nome del blocco","");if(String.IsNullOrWhiteSpace(name))return;AddImagesToBlock(ProjectStore.AddBlock(settings,name),request.Paths);};add.DropDownItems.Add(create);request.Menu.Items.Add(add);
  string path=request.FocusedPath;string number;ImageNumbers.Scope(settings).TryGetValue(path,out number);
  var edit=new ToolStripMenuItem(request.Paths.Length==1?(ActiveBlock==null?"Numero in Tutta la libreria…":"Numero nel blocco «"+ActiveBlock.Name+"»…"):"Numero per «"+Path.GetFileName(path)+"»…"){Name="EditImageNumber"};edit.Click+=delegate{string value=AskText("Numero foto","Numero di riferimento (vuoto per rimuovere)",number??"");if(value==null)return;try{SetImageNumber(path,value);}catch(ArgumentException error){MessageBox.Show(error.Message,"Numero foto");}};request.Menu.Items.Add(edit);
  var automatic=new ToolStripMenuItem("Numera automaticamente la selezione"){Name="AutoNumberImages",Enabled=request.Paths.Length>0};automatic.Click+=delegate{try{int count=ImageNumbers.AutoNumber(settings,request.Paths);RefreshVisible();files.SelectPaths(request.Paths);Save();status.Text=count+" file numerati nell'ordine visualizzato ("+(ActiveBlock==null?"Tutta la libreria":ActiveBlock.Name)+").";}catch(ArgumentException error){MessageBox.Show(error.Message,"Numerazione automatica");}};request.Menu.Items.Add(automatic);
  var from=new ToolStripMenuItem("Numera partendo da…"){Name="NumberImagesFrom",Enabled=request.Paths.Length>0};from.Click+=delegate{ShowNumberStartDialog(request.Paths);};request.Menu.Items.Add(from);
  var remove=new ToolStripMenuItem("Rimuovi numero"){Name="RemoveImageNumber",Enabled=!String.IsNullOrEmpty(number)};remove.Click+=delegate{SetImageNumber(path,"");};request.Menu.Items.Add(remove);
 }
}
}
