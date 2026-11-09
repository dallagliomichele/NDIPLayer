using System.Drawing;
using System.Windows.Forms;
namespace ImageNdi {
// Paint the whole countdown atomically; a native Label can erase visibly between timer ticks.
public sealed class CountdownLabel:Label {
 public CountdownLabel(){AutoSize=false;DoubleBuffered=true;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Opaque|ControlStyles.ResizeRedraw,true);}
 protected override void OnPaintBackground(PaintEventArgs e){}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,ForeColor,BackColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);}
 public void ShowCountdown(string text,Color color){if(ForeColor!=color)ForeColor=color;if(Text!=text)Text=text;}
}
sealed class ProgramMonitorPanel:Panel {
 public ProgramMonitorPanel(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.FromArgb(30,32,35);}
}
public partial class MainWindow {
 readonly CountdownLabel programCountdown=new CountdownLabel{Name="ProgramCountdown",Dock=DockStyle.Bottom,Height=56,Font=new Font(SystemFonts.DefaultFont.FontFamily,17,FontStyle.Bold),TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.FromArgb(30,32,35),ForeColor=Color.White,Text="Program · nessun video"};
 void InitializeCountdown(){var layout=(TableLayoutPanel)program.Parent;layout.Controls.Remove(program);var host=new ProgramMonitorPanel{Dock=DockStyle.Fill};host.Controls.Add(program);host.Controls.Add(programCountdown);layout.Controls.Add(host,1,0);}
 void UpdateProgramCountdown(){var clock=output==null?null:output.ReadSentPlayback();string text=clock==null?"Program · nessun video":clock.Display();Color color=clock!=null&&clock.Ready&&!clock.Ended&&clock.Duration>0&&clock.Remaining<=5?Color.DarkOrange:Color.White;programCountdown.ShowCountdown(text,color);}
}
}
