using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ImageNdi {
public class Settings {public string Root {get;set;} public Dictionary<string,Placement> Images {get;set;} public Settings(){Images=new Dictionary<string,Placement>(StringComparer.OrdinalIgnoreCase);}}
public class MainWindow:Form {
 ListBox files=new ListBox(); PictureBox preview=new PictureBox(); Label status=new Label();
 NumericUpDown scale=Number(1,400,100), x=Number(-8192,8192,0), y=Number(-8192,8192,0), width=Number(16,3840,1920), height=Number(16,2160,1080), fps=Number(1,60,50), fade=Number(0,10000,500);
 CheckBox dissolve=new CheckBox{Text="Dissolvenza",AutoSize=true}; Button start=new Button{Text="Avvia NDI",AutoSize=true};
 Settings settings=new Settings(); string selected; byte[] pending; bool loading; Playout output; int outW,outH; readonly string state=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WatchoutImageNDI","settings.json");
 public MainWindow(){Text="Watchout Image NDI — 0.1";Width=1280;Height=820;MinimumSize=new Size(1000,650);
  try{if(File.Exists(state))settings=new JavaScriptSerializer().Deserialize<Settings>(File.ReadAllText(state));}catch(Exception){status.Text="Preferenze non leggibili: ripristinate.";}
  var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=82,AutoScroll=true};Controls.Add(top);
  AddButton(top,"Apri cartella",delegate{using(var dialog=new FolderBrowserDialog()){if(dialog.ShowDialog()==DialogResult.OK){settings.Root=dialog.SelectedPath;Scan();Save();}}});
  AddButton(top,"Aggiorna",delegate{Scan();}); AddField(top,"Larghezza",width);AddField(top,"Altezza",height);AddField(top,"fps",fps);top.Controls.Add(start);start.Click+=delegate{Toggle();};
  var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=100};Controls.Add(bottom);AddField(bottom,"Scala %",scale);AddField(bottom,"X px",x);AddField(bottom,"Y px",y);bottom.Controls.Add(dissolve);AddField(bottom,"Durata ms",fade);
  AddButton(bottom,"INVIA selezione",delegate{Cue(false);});AddButton(bottom,"CLEAR trasparente",delegate{Cue(true);});AddButton(bottom,"Reset posizione",delegate{scale.Value=100;x.Value=0;y.Value=0;});
  status.AutoSize=true;bottom.Controls.Add(status);
  var split=new SplitContainer{Width=1200,Dock=DockStyle.Fill,SplitterDistance=360};Controls.Add(split);split.BringToFront();files.Dock=DockStyle.Fill;split.Panel1.Controls.Add(files);preview.Dock=DockStyle.Fill;preview.SizeMode=PictureBoxSizeMode.Zoom;preview.BackgroundImage=Checker();preview.BackgroundImageLayout=ImageLayout.Tile;split.Panel2.Controls.Add(preview);
  files.SelectedIndexChanged+=delegate{selected=files.SelectedItem as string;if(selected==null)return;loading=true;Placement p;if(!settings.Images.TryGetValue(selected,out p))p=new Placement();scale.Value=Math.Max(1,Math.Min(400,(decimal)p.Scale*100));x.Value=p.X;y.Value=p.Y;loading=false;RenderPreview();};
  foreach(var n in new[]{scale,x,y})n.ValueChanged+=delegate{if(loading||selected==null)return;settings.Images[selected]=PlacementNow();RenderPreview();Save();};
  foreach(var n in new[]{width,height})n.ValueChanged+=delegate{if(output==null)RenderPreview();};
  var timer=new Timer{Interval=500};timer.Tick+=delegate{if(output!=null)status.Text=output.Error==null?"NDI attivo · frame "+output.Frames:"ERRORE NDI: "+output.Error;};timer.Start();
  FormClosing+=delegate{timer.Stop();if(output!=null)output.Dispose();Save();};
  Shown+=delegate{Scan();};
 }
 static NumericUpDown Number(int min,int max,int value){return new NumericUpDown{Minimum=min,Maximum=max,Value=value,Width=72};}
 static void AddField(Control parent,string label,Control field){parent.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(6,8,0,0)});parent.Controls.Add(field);}
 static void AddButton(Control parent,string text,Action action){var b=new Button{Text=text,AutoSize=true};b.Click+=delegate{action();};parent.Controls.Add(b);}
 static Bitmap Checker(){var b=new Bitmap(24,24);using(var g=Graphics.FromImage(b)){g.Clear(Color.DimGray);g.FillRectangle(Brushes.Gray,0,0,12,12);g.FillRectangle(Brushes.Gray,12,12,12,12);}return b;}
 Placement PlacementNow(){return new Placement{Scale=(double)scale.Value/100,X=(int)x.Value,Y=(int)y.Value};}
 void Scan(){if(String.IsNullOrEmpty(settings.Root)||!Directory.Exists(settings.Root))return;var warnings=new List<string>();files.DataSource=Library.Scan(settings.Root,warnings);status.Text=files.Items.Count+" immagini · "+warnings.Count+" cartelle non accessibili";}
 void RenderPreview(){try{int w=output==null?(int)width.Value:outW,h=output==null?(int)height.Value:outH;pending=Renderer.Render(selected,w,h,PlacementNow());var b=new Bitmap(w,h,PixelFormat.Format32bppArgb);var d=b.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{Marshal.Copy(pending,0,d.Scan0,pending.Length);}finally{b.UnlockBits(d);}var old=preview.Image;preview.Image=b;if(old!=null)old.Dispose();status.Text="Anteprima pronta · premi INVIA";}catch(Exception e){pending=null;status.Text=e.Message;}}
 void Toggle(){if(output!=null){output.Dispose();output=null;start.Text="Avvia NDI";width.Enabled=height.Enabled=fps.Enabled=true;status.Text="NDI arrestato";return;}try{outW=(int)width.Value;outH=(int)height.Value;if(outW%2!=0)throw new ArgumentException("La larghezza deve essere pari.");output=new Playout(new NdiSender("Watchout Image NDI"),outW,outH,(int)fps.Value);start.Text="Arresta NDI";width.Enabled=height.Enabled=fps.Enabled=false;RenderPreview();}catch(Exception e){MessageBox.Show(e.Message,"NDI");}}
 void Cue(bool clear){if(output==null){status.Text="Avvia prima NDI.";return;}if(!clear&&pending==null){status.Text="Seleziona un'immagine valida.";return;}output.Cue(clear?new byte[outW*outH*4]:pending,dissolve.Checked?(double)fade.Value/1000:0);}
 void Save(){try{Directory.CreateDirectory(Path.GetDirectoryName(state));string temp=state+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(settings));if(File.Exists(state))File.Replace(temp,state,null);else File.Move(temp,state);}catch(Exception e){status.Text="Preferenze non salvate: "+e.Message;}}
 [STAThread] public static void Main(){Application.EnableVisualStyles();Application.Run(new MainWindow());}
}
}
