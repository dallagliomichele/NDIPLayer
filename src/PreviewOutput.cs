using System;
using System.Windows.Forms;
using System.Drawing;

namespace ImageNdi {
public partial class MainWindow {
 Playout previewOutput;int previewW,previewH,previewFps;string previewSourceName,activeProgramSourceName,previewStartError,programStartError;
 readonly Button previewStart=new Button{Name="PreviewStartStop",Text="Avvia Preview",AutoSize=true};
 readonly Label programLamp=new Label{Name="ProgramEnabledStatus",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};
 readonly Label previewLamp=new Label{Name="PreviewEnabledStatus",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};
 readonly Label programSource=new Label{Name="ProgramSourceName",Dock=DockStyle.Fill,AutoEllipsis=true};
 readonly Label previewOutputStatus=new Label{Name="PreviewNdiStatus",Text="Preview NDI: spento",AutoSize=true};
 void InitializePreviewOutput(FlowLayoutPanel top){
  top.Controls.Remove(start);start.Name="ProgramStartStop";previewOutputStatus.AutoSize=false;previewOutputStatus.Dock=DockStyle.Fill;previewOutputStatus.AutoEllipsis=true;
  var sections=new TableLayoutPanel{Name="OutputSections",Dock=DockStyle.Top,Height=88,ColumnCount=2,RowCount=1};sections.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));sections.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));sections.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  sections.Controls.Add(OutputSection("Program",start,programLamp,programSource),0,0);sections.Controls.Add(OutputSection("Preview",previewStart,previewLamp,previewOutputStatus),1,0);
  var header=new Panel{Dock=DockStyle.Top,Height=top.Height+sections.Height};Controls.Remove(top);top.Dock=DockStyle.Fill;header.Controls.Add(top);header.Controls.Add(sections);Controls.Add(header);
  previewStart.Click+=delegate{try{previewStartError=null;if(previewOutput==null)StartPreviewOutput();else StopPreviewOutput();}catch(Exception e){previewStartError=e.Message;SetOutputControlLock();}RefreshOutputIndicators();};
  channelName.TextChanged+=delegate{RefreshOutputIndicators();};RefreshOutputIndicators();
 }
 static GroupBox OutputSection(string title,Button button,Label lamp,Label source){var group=new GroupBox{Name=title+"OutputSection",Text=title,Dock=DockStyle.Fill,Padding=new Padding(8,5,8,5)};var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2};content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,145));content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.Absolute,32));content.RowStyles.Add(new RowStyle(SizeType.Percent,100));button.Dock=DockStyle.Fill;content.Controls.Add(button,0,0);content.Controls.Add(lamp,1,0);content.Controls.Add(source,0,1);content.SetColumnSpan(source,2);group.Controls.Add(content);return group;}
 static void OutputLamp(Label label,Playout stream,string startError){string error=stream==null?startError:stream.Error;label.Text=error!=null?"● ERRORE":stream==null?"● SPENTO":stream.Frames==0?"● AVVIO":"● ACCESO";label.ForeColor=error!=null?Color.Firebrick:stream==null?Color.DimGray:stream.Frames==0?Color.DarkOrange:Color.ForestGreen;}
 void RefreshOutputIndicators(){OutputLamp(programLamp,output,programStartError);OutputLamp(previewLamp,previewOutput,previewStartError);start.Text=output==null?"Avvia Program":"Arresta Program";previewStart.Text=previewOutput==null?"Avvia Preview":"Arresta Preview";programSource.Text=programStartError!=null&&output==null?programStartError:"NDI: "+(output!=null?activeProgramSourceName??channelName.Text:channelName.Text);if(previewOutput!=null)previewOutputStatus.Text=previewOutput.Error??("NDI: "+previewSourceName);else if(previewStartError!=null)previewOutputStatus.Text=previewStartError;else{try{previewOutputStatus.Text="NDI: "+NdiChannel.PreviewName(channelName.Text);}catch(ArgumentException){previewOutputStatus.Text="Nome derivato dal canale Program";}}}
 void SetOutputControlLock(){bool free=output==null&&previewOutput==null;width.Enabled=height.Enabled=fps.Enabled=channelName.Enabled=free;RefreshOutputIndicators();}
 void StartPreviewOutput(){
  previewSourceName=NdiChannel.PreviewName(channelName.Text);previewW=FrameWidth;previewH=FrameHeight;previewFps=output==null?(int)fps.Value:activeProgramFps;
  if(previewW%2!=0)throw new ArgumentException("La larghezza deve essere pari.");
  previewOutput=new Playout(new NdiSender(previewSourceName),previewW,previewH,previewFps);SetOutputControlLock();PublishPreview();previewOutputStatus.Text="Preview NDI: "+previewSourceName;
 }
 void StopPreviewOutput(){bool running=previewOutput!=null;if(running){previewOutput.Dispose();previewOutput=null;}previewOutputStatus.Text="Preview NDI: spento";SetOutputControlLock();if(output==null)configNotice.Text="";if(running&&!Disposing&&!IsDisposed)RefreshPlacementForActiveFormat();}
 void RefreshPlacementForActiveFormat(){Placement placement;if(selected==null||!settings.Images.TryGetValue(selected,out placement))placement=new Placement();ApplyPlacement(placement,false);}
 void PublishPreview(){if(previewOutput==null)return;byte[] frame=pending;if(frame==null||frame.Length!=previewW*previewH*4)frame=new byte[previewW*previewH*4];previewOutput.Cue(frame,0,selected);}
 void CheckPreviewOutput(){RefreshOutputIndicators();}
 void ClearProgramImmediately(){preview.CancelInteraction();if(output==null){status.Text="Programma NDI arrestato.";return;}if(output.Error!=null){status.Text="Invio fermo: arresta e riavvia NDI.";return;}output.Cue(new byte[outW*outH*4],0);status.Text="ESC: Programma trasparente (cut)";}
 protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==Keys.Escape){ClearProgramImmediately();return true;}return base.ProcessCmdKey(ref msg,keyData);}
}
}
