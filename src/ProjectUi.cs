using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ImageNdi {
public partial class MainWindow {
 List<string> catalog=new List<string>();
 readonly ListBox blockList=new ListBox{Name="PlaylistBlocks",Dock=DockStyle.Fill,AllowDrop=true};
 readonly TextBox search=new TextBox{Name="ImageSearch",Width=210};
 readonly Label onAirLabel=new Label{Name="OnAirStatus",AutoSize=true,ForeColor=Color.Firebrick,MaximumSize=new Size(800,40)};
 readonly Label configNotice=new Label{AutoSize=true,ForeColor=Color.DarkOrange};
 bool loadingProject,refreshingBlocks;string projectPath,savedFingerprint,blockDragId;Point blockDragOrigin;
 PlaylistBlock ActiveBlock {get{return settings.Blocks.Find(b=>b.Id==settings.ActiveBlockId);}}
 void CaptureProjectControls(){if(loadingProject)return;settings.OutputWidth=(int)width.Value;settings.OutputHeight=(int)height.Value;settings.OutputFps=(int)fps.Value;settings.ChannelName=channelName.Text;settings.Dissolve=dissolve.Checked;settings.FadeMs=(int)fade.Value;}
 void UpdateProjectTitle(){if(savedFingerprint==null)return;Text="NDIPlayer — 0.5.1 · "+(projectPath==null?"Progetto locale":Path.GetFileName(projectPath))+(ProjectStore.Fingerprint(settings)==savedFingerprint?"":" *");}
 void InitializeProjects(FlowLayoutPanel top,Control sidebar){
  top.Height=112;
  AddButton(top,"Nuovo progetto",delegate{if(ConfirmProjectChange())ApplyProject(new Settings(),null);});
  AddButton(top,"Apri progetto",delegate{if(!ConfirmProjectChange())return;using(var d=new OpenFileDialog{Filter="Progetto NDIPlayer|*.ndiproj|JSON|*.json"})if(d.ShowDialog()==DialogResult.OK)try{ApplyProject(ProjectStore.Read(d.FileName),d.FileName);}catch(Exception e){MessageBox.Show(e.Message,"Progetto non caricato");}});
  AddButton(top,"Salva progetto",delegate{WriteProject(false);});AddButton(top,"Salva con nome",delegate{WriteProject(true);});
  AddButton(top,"Ricollega cartella",delegate{using(var d=new FolderBrowserDialog())if(d.ShowDialog()==DialogResult.OK){ProjectStore.Relocate(settings,d.SelectedPath);RefreshBlocks();Scan();Save();}});
  top.Controls.Add(configNotice);top.Controls.Add(onAirLabel);
  var side=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1};side.RowStyles.Add(new RowStyle(SizeType.Absolute,235));side.RowStyles.Add(new RowStyle(SizeType.Percent,100));sidebar.Controls.Remove(files);sidebar.Controls.Add(side);
  var blocks=new Panel{Dock=DockStyle.Fill};var tools=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=94,AutoScroll=true};
  AddButton(tools,"Nuovo blocco",delegate{string name=AskText("Nuovo blocco","Nome del blocco","");if(name==null)return;try{var b=ProjectStore.AddBlock(settings,name);settings.ActiveBlockId=b.Id;RefreshBlocks();RefreshVisible();Save();}catch(Exception e){MessageBox.Show(e.Message);}});
  AddButton(tools,"Rinomina",delegate{var b=ActiveBlock;if(b==null)return;string name=AskText("Rinomina blocco","Nome",b.Name);if(String.IsNullOrWhiteSpace(name))return;b.Name=name.Trim();RefreshBlocks();Save();});
  AddButton(tools,"Elimina blocco",delegate{var b=ActiveBlock;if(b==null)return;if(MessageBox.Show("Rimuovere il blocco «"+b.Name+"»? Le foto originali restano sul disco.","Blocco",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;settings.Blocks.Remove(b);settings.ActiveBlockId=null;RefreshBlocks();RefreshVisible();Save();});
  AddButton(tools,"Blocco ↑",delegate{MoveBlock(-1);});AddButton(tools,"Blocco ↓",delegate{MoveBlock(1);});
  AddButton(tools,"Aggiungi foto…",delegate{var paths=files.SelectedPaths;if(paths.Count==0){status.Text="Seleziona una o più foto (Ctrl / Maiusc).";return;}var target=ChooseBlock();if(target==null)return;ProjectStore.AddFiles(target,paths);RefreshBlocks();RefreshVisible();Save();});
  AddButton(tools,"Rimuovi foto",delegate{var b=ActiveBlock;if(b==null){status.Text="Scegli un blocco per rimuovere una foto dalla playlist.";return;}var selectedPaths=new HashSet<string>(files.SelectedPaths,StringComparer.OrdinalIgnoreCase);b.Files.RemoveAll(selectedPaths.Contains);RefreshBlocks();RefreshVisible();Save();});
  AddButton(tools,"Foto ↑",delegate{MovePhotos(-1);});AddButton(tools,"Foto ↓",delegate{MovePhotos(1);});
  blocks.Controls.Add(blockList);blocks.Controls.Add(tools);
  var find=new FlowLayoutPanel{Dock=DockStyle.Top,Height=31};find.Controls.Add(new Label{Text="Cerca",AutoSize=true});find.Controls.Add(search);AddButton(find,"×",delegate{search.Clear();});blocks.Controls.Add(find);
  side.Controls.Add(blocks,0,0);files.Dock=DockStyle.Fill;side.Controls.Add(files,0,1);
  blockList.SelectedIndexChanged+=delegate{if(refreshingBlocks)return;var b=blockList.SelectedItem as PlaylistBlock;settings.ActiveBlockId=b==null?null:b.Id;RefreshVisible();Save();};
  search.TextChanged+=delegate{if(!loadingProject)RefreshVisible();};
  blockList.MouseDown+=delegate(object sender,MouseEventArgs e){blockDragOrigin=e.Location;int at=blockList.IndexFromPoint(e.Location);var block=at<0?null:blockList.Items[at] as PlaylistBlock;blockDragId=block==null?null:block.Id;};
  blockList.MouseMove+=delegate(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Left||blockDragId==null||Math.Abs(e.X-blockDragOrigin.X)+Math.Abs(e.Y-blockDragOrigin.Y)<8)return;string id=blockDragId;blockDragId=null;blockList.DoDragDrop(new DataObject("NDIPlayer.Block",id),DragDropEffects.Move);};
  blockList.DragEnter+=delegate(object sender,DragEventArgs e){e.Effect=e.Data.GetDataPresent("NDIPlayer.Block")?DragDropEffects.Move:e.Data.GetDataPresent("NDIPlayer.Images")?DragDropEffects.Copy:DragDropEffects.None;};
  blockList.DragDrop+=delegate(object sender,DragEventArgs e){var point=blockList.PointToClient(new Point(e.X,e.Y));int index=blockList.IndexFromPoint(point);var target=index<0?null:blockList.Items[index] as PlaylistBlock;if(target==null)return;if(e.Data.GetDataPresent("NDIPlayer.Block")){var moving=settings.Blocks.Find(b=>b.Id==(string)e.Data.GetData("NDIPlayer.Block"));if(moving==null||moving==target)return;var rect=blockList.GetItemRectangle(index);bool after=point.Y>rect.Top+rect.Height/2;settings.Blocks.Remove(moving);settings.Blocks.Insert(settings.Blocks.IndexOf(target)+(after?1:0),moving);}else if(e.Data.GetDataPresent("NDIPlayer.Images"))ProjectStore.AddFiles(target,(string[])e.Data.GetData("NDIPlayer.Images"));RefreshBlocks();RefreshVisible();Save();};
  files.DropRequested+=delegate(object sender,LibraryDropEventArgs e){var b=ActiveBlock;if(b==null){status.Text="Scegli un blocco per riordinare le foto.";return;}ProjectStore.AddFiles(b,e.Paths);ProjectStore.MoveFiles(b,e.Paths,e.Before);RefreshBlocks();RefreshVisible();Save();};
  RefreshBlocks();savedFingerprint=ProjectStore.Fingerprint(settings);UpdateProjectTitle();onAirLabel.Text="In onda: nessuna immagine";
 }
 void RefreshBlocks(){refreshingBlocks=true;try{blockList.Items.Clear();blockList.Items.Add("Tutta la libreria");int selectedIndex=0;foreach(var b in settings.Blocks){blockList.Items.Add(b);if(b.Id==settings.ActiveBlockId)selectedIndex=blockList.Items.Count-1;}blockList.SelectedIndex=selectedIndex;}finally{refreshingBlocks=false;}}
 void RefreshVisible(){var all=catalog.Concat(settings.Blocks.SelectMany(b=>b.Files)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase);files.SetFiles(ProjectStore.Visible(settings,all,settings.ActiveBlockId,search.Text).Where(Library.Displayable).ToList());}
 void MoveBlock(int direction){var b=ActiveBlock;if(b==null)return;int old=settings.Blocks.IndexOf(b),next=old+direction;if(next<0||next>=settings.Blocks.Count)return;settings.Blocks.RemoveAt(old);settings.Blocks.Insert(next,b);RefreshBlocks();Save();}
 void MovePhotos(int direction){var b=ActiveBlock;if(b==null)return;var selectedSet=new HashSet<string>(files.SelectedPaths,StringComparer.OrdinalIgnoreCase);if(direction<0){for(int i=1;i<b.Files.Count;i++)if(selectedSet.Contains(b.Files[i])&&!selectedSet.Contains(b.Files[i-1])){string p=b.Files[i];b.Files[i]=b.Files[i-1];b.Files[i-1]=p;}}else{for(int i=b.Files.Count-2;i>=0;i--)if(selectedSet.Contains(b.Files[i])&&!selectedSet.Contains(b.Files[i+1])){string p=b.Files[i];b.Files[i]=b.Files[i+1];b.Files[i+1]=p;}}RefreshVisible();files.SelectPaths(selectedSet);Save();}
 PlaylistBlock ChooseBlock(){if(settings.Blocks.Count==0){string name=AskText("Nuovo blocco","Nome del blocco","");if(String.IsNullOrWhiteSpace(name))return null;return ProjectStore.AddBlock(settings,name);}using(var dialog=new Form{Text="Aggiungi le foto al blocco",Width=390,Height=155,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){var list=new ComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList};foreach(var b in settings.Blocks)list.Items.Add(b);list.SelectedIndex=ActiveBlock==null?0:settings.Blocks.IndexOf(ActiveBlock);var ok=new Button{Text="Aggiungi",Dock=DockStyle.Bottom,DialogResult=DialogResult.OK};dialog.Controls.Add(list);dialog.Controls.Add(ok);dialog.AcceptButton=ok;return dialog.ShowDialog(this)==DialogResult.OK?list.SelectedItem as PlaylistBlock:null;}}
 string AskText(string title,string caption,string value){using(var dialog=new Form{Text=title,Width=400,Height=155,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){var label=new Label{Text=caption,Dock=DockStyle.Top};var input=new TextBox{Text=value,Dock=DockStyle.Top,MaxLength=100};var row=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=35};var ok=new Button{Text="OK",DialogResult=DialogResult.OK};var cancel=new Button{Text="Annulla",DialogResult=DialogResult.Cancel};row.Controls.Add(ok);row.Controls.Add(cancel);dialog.Controls.Add(input);dialog.Controls.Add(label);dialog.Controls.Add(row);dialog.AcceptButton=ok;dialog.CancelButton=cancel;return dialog.ShowDialog(this)==DialogResult.OK?input.Text:null;}}
 bool WriteProject(bool choose){CaptureProjectControls();string path=projectPath;if(choose||path==null)using(var d=new SaveFileDialog{Filter="Progetto NDIPlayer|*.ndiproj",DefaultExt="ndiproj",FileName=path==null?"Spettacolo.ndiproj":Path.GetFileName(path)}){if(d.ShowDialog()!=DialogResult.OK)return false;path=d.FileName;}try{ProjectStore.Write(path,settings);projectPath=path;savedFingerprint=ProjectStore.Fingerprint(settings);UpdateProjectTitle();status.Text="Progetto salvato: "+Path.GetFileName(path);return true;}catch(Exception e){MessageBox.Show(e.Message,"Progetto non salvato");return false;}}
 bool ConfirmProjectChange(){CaptureProjectControls();if(savedFingerprint==null||ProjectStore.Fingerprint(settings)==savedFingerprint)return true;var answer=MessageBox.Show("Salvare le modifiche al progetto prima di continuare?","Progetto modificato",MessageBoxButtons.YesNoCancel);return answer==DialogResult.No||(answer==DialogResult.Yes&&WriteProject(false));}
 void ApplyProject(Settings data,string path){
  ProjectStore.Normalize(data);preview.CancelInteraction();loadingProject=true;
  try{settings=data;projectPath=path;width.Value=data.OutputWidth;height.Value=data.OutputHeight;fps.Value=data.OutputFps;fade.Value=Math.Max(0,Math.Min(10000,data.FadeMs));dissolve.Checked=data.Dissolve;channelName.Text=data.ChannelName;files.SetPreferences(data.Thumbnails,data.ThumbnailSize);search.Clear();RefreshBlocks();Scan();}finally{loadingProject=false;}
  CaptureProjectControls();savedFingerprint=ProjectStore.Fingerprint(settings);Save();UpdateProjectTitle();
  configNotice.Text=output==null?"":"Formato / nome progetto al prossimo avvio NDI";
  status.Text=output==null?"Progetto caricato":"Progetto caricato. La messa in onda corrente continua invariata.";
 }
}
}
