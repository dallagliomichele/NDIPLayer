using System.Drawing;
using System.Windows.Forms;
namespace ImageNdi {
public partial class MainWindow {
 readonly Label programCountdown=new Label{Name="ProgramCountdown",Dock=DockStyle.Bottom,Height=56,Font=new Font(SystemFonts.DefaultFont.FontFamily,17,FontStyle.Bold),TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.FromArgb(30,32,35),ForeColor=Color.White,Text="Program · nessun video"};
 void InitializeCountdown(){var layout=(TableLayoutPanel)program.Parent;layout.Controls.Remove(program);var host=new Panel{Dock=DockStyle.Fill};host.Controls.Add(program);host.Controls.Add(programCountdown);layout.Controls.Add(host,1,0);}
 void UpdateProgramCountdown(){var clock=output==null?null:output.ReadSentPlayback();programCountdown.Text=clock==null?"Program · nessun video":clock.Display();programCountdown.ForeColor=clock!=null&&clock.Ready&&!clock.Ended&&clock.Duration>0&&clock.Remaining<=5?Color.DarkOrange:Color.White;}
}
}
