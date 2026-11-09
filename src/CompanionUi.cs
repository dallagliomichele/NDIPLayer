using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageNdi {
public partial class MainWindow {
 readonly CompanionTcp companionTcp=new CompanionTcp();
 readonly Label companionStatus=new Label{Name="CompanionStatus",AutoSize=true,MaximumSize=new Size(600,32)};
 readonly NumericUpDown escClearFade=Number(1,60000,500);
 readonly EscapeGesture escapeGesture=new EscapeGesture();
 readonly System.Diagnostics.Stopwatch escapeClock=System.Diagnostics.Stopwatch.StartNew();
 Playout companionProgram;
 void InitializeCompanion(FlowLayoutPanel top){escClearFade.Name="EscClearDuration";escClearFade.Value=Math.Max(1,Math.Min(60000,settings.EscClearMs));try{companionTcp.Configure(settings.Companion);}catch(Exception e){settings.Companion=new CompanionSettings();companionTcp.Configure(settings.Companion);companionStatus.Text="Companion disabilitato: "+e.Message;}AddButton(top,"Companion TCP…",ShowCompanionDialog);top.Controls.Add(companionStatus);escClearFade.ValueChanged+=delegate{if(!loadingProject)Save();};}
 void BindCompanion(Playout stream){if(companionProgram!=null)companionProgram.PresenceChanged-=CompanionPresence;companionProgram=stream;if(stream!=null)stream.PresenceChanged+=CompanionPresence;}
 void CompanionPresence(object sender,ProgramPresenceEventArgs e){companionTcp.Presence(e.HasImage);}
 void RefreshCompanionStatus(){companionStatus.Text=companionTcp.Status;companionStatus.ForeColor=companionTcp.Status.Contains("ERRORE")?Color.Firebrick:Color.DimGray;}
 void ApplyCompanionSettings(){companionTcp.Configure(settings.Companion);RefreshCompanionStatus();}
 void ShowCompanionDialog(){using(var dialog=new Form{Text="Bitfocus Companion · TCP",Width=680,Height=330,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
  var panel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=8,Padding=new Padding(12)};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,210));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  var enabled=new CheckBox{Text="Abilita notifiche Companion",Checked=settings.Companion.Enabled,AutoSize=true};var address=new TextBox{Text=settings.Companion.Address,Dock=DockStyle.Fill};var port=Number(1,65535,Math.Max(1,Math.Min(65535,settings.Companion.Port)));var onAir=new TextBox{Text=settings.Companion.OnAirCommand,Dock=DockStyle.Fill,MaxLength=4096};var empty=new TextBox{Text=settings.Companion.EmptyCommand,Dock=DockStyle.Fill,MaxLength=4096};
  panel.Controls.Add(enabled,0,0);panel.SetColumnSpan(enabled,2);panel.Controls.Add(new Label{Text="Indirizzo IP",AutoSize=true},0,1);panel.Controls.Add(address,1,1);panel.Controls.Add(new Label{Text="Porta TCP",AutoSize=true},0,2);panel.Controls.Add(port,1,2);panel.Controls.Add(new Label{Text="Da vuoto alla prima foto",AutoSize=true},0,3);panel.Controls.Add(onAir,1,3);panel.Controls.Add(new Label{Text="Clear completato: nessuna foto",AutoSize=true},0,4);panel.Controls.Add(empty,1,4);
  var help=new Label{Text="Un comando per riga. Il terminatore TCP viene aggiunto automaticamente.\r\nStringa vuota = nessun invio per quell'evento. Nessun messaggio a ogni cambio foto.",AutoSize=true};panel.Controls.Add(help,0,5);panel.SetColumnSpan(help,2);
  Func<CompanionSettings> read=()=>new CompanionSettings{Enabled=enabled.Checked,Address=address.Text.Trim(),Port=(int)port.Value,OnAirCommand=CompanionSettings.Line(onAir.Text),EmptyCommand=CompanionSettings.Line(empty.Text)};
  var testStatus=new Label{AutoSize=true,MaximumSize=new Size(620,40)};panel.Controls.Add(testStatus,0,6);panel.SetColumnSpan(testStatus,2);
  var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true};AddButton(buttons,"Test connessione",delegate{try{var config=read();config.Validate();companionTcp.Test(config);testStatus.Text=companionTcp.Status;}catch(Exception e){MessageBox.Show(e.Message,"Companion");}});
  AddButton(buttons,"Applica",delegate{try{var config=read();config.Validate();settings.Companion=config;ApplyCompanionSettings();Save();dialog.DialogResult=DialogResult.OK;}catch(Exception e){MessageBox.Show(e.Message,"Companion");}});var cancel=new Button{Text="Annulla",DialogResult=DialogResult.Cancel,AutoSize=true};buttons.Controls.Add(cancel);dialog.CancelButton=cancel;panel.Controls.Add(buttons,0,7);panel.SetColumnSpan(buttons,2);dialog.Controls.Add(panel);using(var refresh=new Timer{Interval=100}){refresh.Tick+=delegate{testStatus.Text=companionTcp.Status;};refresh.Start();dialog.ShowDialog(this);}
 }}
}
}
