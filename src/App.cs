using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ImageNdi {
public class Settings {public string Root {get;set;} public Dictionary<string,Placement> Images {get;set;} public bool Thumbnails {get;set;} public int ThumbnailSize {get;set;} public string ChannelName {get;set;} public List<PlaylistBlock> Blocks {get;set;} public string ActiveBlockId {get;set;} public int OutputWidth {get;set;} public int OutputHeight {get;set;} public int OutputFps {get;set;} public bool Dissolve {get;set;} public int FadeMs {get;set;} public Settings(){Images=new Dictionary<string,Placement>(StringComparer.OrdinalIgnoreCase);ThumbnailSize=128;ChannelName=NdiChannel.DefaultName;Blocks=new List<PlaylistBlock>();OutputWidth=1920;OutputHeight=1080;OutputFps=50;FadeMs=500;}}
public partial class MainWindow:Form {
 LibraryBrowser files=new LibraryBrowser(); PreviewCanvas preview=new PreviewCanvas{InteractionEnabled=true,Footer="Trascina: posizione · Rotellina: scala · INVIA per applicare"}; PreviewCanvas program=new PreviewCanvas{Heading="PROGRAMMA · NDI arrestato",Footer="Nessuna trasmissione",BorderColor=Color.FromArgb(245,100,80)}; Label status=new Label();
 NumericUpDown scale=Number(1,400,100), x=Number(-8192,16384,960), y=Number(-8192,16384,540), width=Number(16,3840,1920), height=Number(16,2160,1080), fps=Number(1,60,50), fade=Number(0,10000,500);
 CheckBox dissolve=new CheckBox{Text="Dissolvenza",AutoSize=true}; Button start=new Button{Text="Avvia NDI",AutoSize=true};
 TextBox channelName=new TextBox{Name="NdiChannelName",Width=210,MaxLength=128};
 Settings settings=new Settings(); string selected; byte[] pending; bool loading; Playout output; int outW,outH; long programSequence; readonly string state;
 bool pointerEditing;decimal dragCenterX,dragCenterY;
 Bitmap selectedSource;
 public MainWindow():this(null){}
 public MainWindow(string preferencesPath){state=preferencesPath??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WatchoutImageNDI","settings.json");Text="NDIPlayer — 0.4.0";Width=1600;Height=900;MinimumSize=new Size(1150,650);
  x.Name="ImageCenterX";y.Name="ImageCenterY";scale.Name="ImageScale";preview.Name="SelectionPreview";program.Name="ProgramPreview";files.Name="ImageLibrary";width.Name="OutputWidth";height.Name="OutputHeight";x.DecimalPlaces=y.DecimalPlaces=scale.DecimalPlaces=1;
  try{if(File.Exists(state))settings=new JavaScriptSerializer{MaxJsonLength=16*1024*1024}.Deserialize<Settings>(File.ReadAllText(state));if(settings==null||settings.Images==null)settings=new Settings();}catch(Exception){status.Text="Preferenze non leggibili: ripristinate.";}
  try{ProjectStore.Normalize(settings);}catch{settings=new Settings();}
  width.Value=Math.Max(16,Math.Min(3840,settings.OutputWidth));height.Value=Math.Max(16,Math.Min(2160,settings.OutputHeight));fps.Value=Math.Max(1,Math.Min(60,settings.OutputFps));fade.Value=Math.Max(0,Math.Min(10000,settings.FadeMs));dissolve.Checked=settings.Dissolve;x.Value=width.Value/2;y.Value=height.Value/2;
  channelName.Text=String.IsNullOrWhiteSpace(settings.ChannelName)?NdiChannel.DefaultName:settings.ChannelName;
  channelName.TextChanged+=delegate{settings.ChannelName=channelName.Text;};channelName.Leave+=delegate{Save();};
  files.SetPreferences(settings.Thumbnails,settings.ThumbnailSize);
  files.PreferencesChanged+=delegate{settings.Thumbnails=files.ThumbnailsEnabled;settings.ThumbnailSize=files.ThumbnailSize;Save();};
  var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=82,AutoScroll=true};Controls.Add(top);
  AddButton(top,"Apri cartella",delegate{using(var dialog=new FolderBrowserDialog()){if(dialog.ShowDialog()==DialogResult.OK){settings.Root=dialog.SelectedPath;Scan();Save();}}});
  AddButton(top,"Aggiorna",delegate{Scan();}); AddField(top,"Larghezza",width);AddField(top,"Altezza",height);AddField(top,"fps",fps);AddField(top,"Nome canale NDI",channelName);top.Controls.Add(start);start.Click+=delegate{Toggle();};
  var ndiLink=new LinkLabel{Text="NDI® · ndi.video",AutoSize=true};ndiLink.LinkClicked+=delegate{System.Diagnostics.Process.Start("https://ndi.video/");};top.Controls.Add(ndiLink);
  AddButton(top,"Info",delegate{MessageBox.Show("NDI® is a registered trademark of Vizrt NDI AB.\r\nhttps://ndi.video/\r\nRuntime distribuito tramite il redistributable ufficiale, con licenza propria.","NDIPlayer — Informazioni");});
  var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=100};Controls.Add(bottom);AddField(bottom,"Scala %",scale);AddField(bottom,"Centro X px",x);AddField(bottom,"Centro Y px",y);bottom.Controls.Add(dissolve);AddField(bottom,"Durata ms",fade);
  AddButton(bottom,"INVIA selezione",delegate{Cue(false);});AddButton(bottom,"CLEAR trasparente",delegate{Cue(true);});AddButton(bottom,"Reset posizione",delegate{ApplyPlacement(new Placement(),true);});
  status.AutoSize=true;bottom.Controls.Add(status);
  var split=new SplitContainer{Width=1500,Dock=DockStyle.Fill,SplitterDistance=350};Controls.Add(split);split.BringToFront();files.Dock=DockStyle.Fill;split.Panel1.Controls.Add(files);
  var monitors=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};monitors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));monitors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));monitors.RowStyles.Add(new RowStyle(SizeType.Percent,100));preview.Dock=DockStyle.Fill;program.Dock=DockStyle.Fill;monitors.Controls.Add(preview,0,0);monitors.Controls.Add(program,1,0);split.Panel2.Controls.Add(monitors);
  files.SelectionChanged+=delegate{preview.CancelInteraction();selected=files.SelectedPath;if(selectedSource!=null){selectedSource.Dispose();selectedSource=null;}Placement p;if(selected==null||!settings.Images.TryGetValue(selected,out p))p=new Placement();string error=null;if(selected!=null)try{selectedSource=Renderer.LoadBitmap(selected);}catch(Exception e){error=e.Message;}ApplyPlacement(p,false);if(error!=null)status.Text=error;};
  files.ImageDoubleClicked+=delegate{Cue(false);};
  foreach(var n in new[]{scale,x,y})n.ValueChanged+=delegate{if(loading)return;ApplyPlacement(PlacementNow(),!pointerEditing);};
  foreach(var n in new[]{width,height})n.ValueChanged+=delegate{if(output==null){preview.CancelInteraction();Placement p;if(selected==null||!settings.Images.TryGetValue(selected,out p))p=new Placement();ApplyPlacement(p,false);}};
  preview.DragStarted+=delegate{pointerEditing=true;dragCenterX=x.Value;dragCenterY=y.Value;};
  preview.DragMoved+=delegate(object sender,PreviewDragEventArgs e){ApplyCenter(dragCenterX+(decimal)e.Delta.X,dragCenterY+(decimal)e.Delta.Y,scale.Value,false);};
  preview.DragCompleted+=delegate{pointerEditing=false;Save();};
  preview.ZoomRequested+=delegate(object sender,PreviewZoomEventArgs e){ApplyCenter(x.Value,y.Value,PlacementCoordinates.ZoomPercent(scale.Value,e.Delta,e.Fine),true);};
  InitializeProjects(top,split.Panel1);
  foreach(var n in new[]{width,height,fps,fade})n.ValueChanged+=delegate{if(!loadingProject)Save();};dissolve.CheckedChanged+=delegate{if(!loadingProject)Save();};
  var timer=new Timer{Interval=100};timer.Tick+=delegate{UpdateProgram();};timer.Start();
  FormClosing+=delegate(object sender,FormClosingEventArgs e){if(!ConfirmProjectChange()){e.Cancel=true;return;}timer.Stop();if(output!=null)output.Dispose();Save();};Disposed+=delegate{timer.Dispose();if(selectedSource!=null){selectedSource.Dispose();selectedSource=null;}};
  Shown+=delegate{Scan();};
 }
 static NumericUpDown Number(int min,int max,int value){return new NumericUpDown{Minimum=min,Maximum=max,Value=value,Width=72};}
 static void AddField(Control parent,string label,Control field){parent.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(6,8,0,0)});parent.Controls.Add(field);}
 static void AddButton(Control parent,string text,Action action){var b=new Button{Text=text,AutoSize=true};b.Click+=delegate{action();};parent.Controls.Add(b);}
 int FrameWidth {get{return output==null?(int)width.Value:outW;}}
 int FrameHeight {get{return output==null?(int)height.Value:outH;}}
 Placement PlacementNow(){return PlacementCoordinates.FromCenter(x.Value,y.Value,FrameWidth,FrameHeight,(double)scale.Value/100);}
 void ApplyCenter(decimal cx,decimal cy,decimal percent,bool persist){cx=Math.Max(x.Minimum,Math.Min(x.Maximum,cx));cy=Math.Max(y.Minimum,Math.Min(y.Maximum,cy));ApplyPlacement(PlacementCoordinates.FromCenter(cx,cy,FrameWidth,FrameHeight,(double)percent/100),persist);}
 void ApplyPlacement(Placement p,bool persist){var center=PlacementCoordinates.Center(p,FrameWidth,FrameHeight);loading=true;try{scale.Value=Math.Max(scale.Minimum,Math.Min(scale.Maximum,(decimal)p.Scale*100));x.Value=Math.Max(x.Minimum,Math.Min(x.Maximum,(decimal)center.X));y.Value=Math.Max(y.Minimum,Math.Min(y.Maximum,(decimal)center.Y));}finally{loading=false;}if(selected!=null&&(persist||pointerEditing||settings.Images.ContainsKey(selected)))settings.Images[selected]=PlacementNow();RenderPreview();if(persist)Save();}
 void Scan(){var warnings=new List<string>();catalog=String.IsNullOrEmpty(settings.Root)||!Directory.Exists(settings.Root)?new List<string>():Library.Scan(settings.Root,warnings);RefreshVisible();status.Text=files.Count+" immagini visibili · "+warnings.Count+" cartelle non accessibili";}
 void RenderPreview(){int w=output==null?(int)width.Value:outW,h=output==null?(int)height.Value:outH;try{pending=selectedSource==null?null:Renderer.RenderImage(selectedSource,w,h,PlacementNow());preview.SetFrame(pending==null?null:Renderer.ToBitmap(pending,w,h),w,h);status.Text="Anteprima pronta · premi INVIA";}catch(Exception e){pending=null;preview.SetFrame(null,w,h);status.Text=e.Message;}}
 void UpdateProgram(){if(output==null)return;var sent=output.ReadSentFrame(ref programSequence);if(sent!=null)program.SetFrame(Renderer.ToBitmap(sent,outW,outH),outW,outH);var live=output.ReadOnAirStatus();files.SetOnAir(live);onAirLabel.Text=(output.Error==null?"In onda: ":"Ultimo inviato (ERRORE): ")+(live.Paths.Length==0?"trasparente":String.Join(" + ",Array.ConvertAll(live.Paths,Path.GetFileName)))+(live.Transitioning?" · dissolvenza":"");program.Heading=output.Error==null?"PROGRAMMA · frame inviati":"PROGRAMMA · ERRORE / fermo";program.Footer=output.Error==null?"Monitor locale · non è il ritorno da WATCHOUT":"Ultimo frame inviato · "+output.Error;program.Invalidate();status.Text=output.Error==null?"NDI attivo · frame "+output.Frames:"ERRORE NDI: "+output.Error;}
 void Toggle(){if(output!=null){output.Dispose();output=null;start.Text="Avvia NDI";width.Enabled=height.Enabled=fps.Enabled=channelName.Enabled=true;status.Text="NDI arrestato";files.SetOnAir(new OnAirStatus());onAirLabel.Text="In onda: nessuna immagine";configNotice.Text="";program.Heading="PROGRAMMA · NDI arrestato";program.Footer="Nessuna trasmissione";program.SetFrame(null,outW,outH);return;}try{string sourceName=NdiChannel.Validate(channelName.Text);channelName.Text=sourceName;settings.ChannelName=sourceName;Save();outW=(int)width.Value;outH=(int)height.Value;if(outW%2!=0)throw new ArgumentException("La larghezza deve essere pari.");programSequence=0;program.Heading="PROGRAMMA · avvio NDI";program.Footer="In attesa del primo frame inviato";program.SetFrame(null,outW,outH);output=new Playout(new NdiSender(sourceName),outW,outH,(int)fps.Value);start.Text="Arresta NDI";width.Enabled=height.Enabled=fps.Enabled=channelName.Enabled=false;RenderPreview();}catch(Exception e){program.Heading="PROGRAMMA · NDI non avviato";program.Footer=e.Message;program.Invalidate();MessageBox.Show(e.Message,"NDI");}}
 void Cue(bool clear){if(output==null){status.Text="Avvia prima NDI.";return;}if(output.Error!=null){status.Text="Invio fermo: arresta e riavvia NDI.";return;}if(!clear&&pending==null){status.Text="Seleziona un'immagine valida.";return;}output.Cue(clear?new byte[outW*outH*4]:pending,dissolve.Checked?(double)fade.Value/1000:0,clear?null:selected);}
 void Save(){CaptureProjectControls();UpdateProjectTitle();try{Directory.CreateDirectory(Path.GetDirectoryName(state));string temp=state+".tmp";File.WriteAllText(temp,new JavaScriptSerializer{MaxJsonLength=16*1024*1024}.Serialize(settings));if(File.Exists(state))File.Replace(temp,state,null);else File.Move(temp,state);}catch(Exception e){status.Text="Preferenze non salvate: "+e.Message;}}
 [STAThread] public static void Main(){Application.EnableVisualStyles();Application.Run(new MainWindow());}
}
}
