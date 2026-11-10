using System.Drawing;
using System.Windows.Forms;
namespace ImageNdi {
// Paint the whole countdown atomically; a native Label can erase visibly between timer ticks.
public sealed class CountdownLabel:Control {
 string displayText="";public ContentAlignment TextAlign{get;set;} public override string Text{get{return displayText??"";}set{string next=value??"";if(displayText==next)return;displayText=next;Invalidate();}}
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
 void InitializeCountdown(){var panel=(Panel)program.Parent;panel.Controls.Remove(program);var host=new ProgramMonitorPanel{Name="ProgramMonitorSurface",Dock=DockStyle.Fill};host.Controls.Add(program);host.Controls.Add(programCountdown);panel.Controls.Add(host);}
 void UpdateProgramCountdown(){var clock=output==null?null:output.ReadSentPlayback();string text=clock==null?"Program · nessun video":clock.Display();Color color=clock!=null&&clock.Ready&&(clock.Loop||clock.Duration>0)?settings.Countdown.ColorFor(clock.Remaining,clock.Loop):Color.White;programCountdown.ShowCountdown(text,color);}
}
}
