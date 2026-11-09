using System;
using System.Windows.Forms;

namespace ImageNdi {
public partial class MainWindow {
 Playout previewOutput;int previewW,previewH,previewFps;string previewSourceName;bool updatingPreviewToggle;
 readonly CheckBox sendPreview=new CheckBox{Name="SendPreviewNdi",Text="Invia Preview in regia",AutoSize=true};
 readonly Label previewOutputStatus=new Label{Name="PreviewNdiStatus",Text="Preview NDI: spento",AutoSize=true};
 void InitializePreviewOutput(FlowLayoutPanel top){top.Controls.Add(sendPreview);top.Controls.Add(previewOutputStatus);sendPreview.CheckedChanged+=delegate{if(updatingPreviewToggle)return;try{if(sendPreview.Checked)StartPreviewOutput();else StopPreviewOutput();}catch(Exception e){updatingPreviewToggle=true;sendPreview.Checked=false;updatingPreviewToggle=false;previewOutputStatus.Text="Preview NDI: "+e.Message;SetOutputControlLock();}};}
 void SetOutputControlLock(){bool free=output==null&&previewOutput==null;width.Enabled=height.Enabled=fps.Enabled=channelName.Enabled=free;}
 void StartPreviewOutput(){
  previewSourceName=NdiChannel.PreviewName(channelName.Text);previewW=FrameWidth;previewH=FrameHeight;previewFps=output==null?(int)fps.Value:activeProgramFps;
  if(previewW%2!=0)throw new ArgumentException("La larghezza deve essere pari.");
  previewOutput=new Playout(new NdiSender(previewSourceName),previewW,previewH,previewFps);SetOutputControlLock();PublishPreview();previewOutputStatus.Text="Preview NDI: "+previewSourceName;
 }
 void StopPreviewOutput(){bool running=previewOutput!=null;if(running){previewOutput.Dispose();previewOutput=null;}previewOutputStatus.Text="Preview NDI: spento";SetOutputControlLock();if(output==null)configNotice.Text="";if(running&&!Disposing&&!IsDisposed)RefreshPlacementForActiveFormat();}
 void RefreshPlacementForActiveFormat(){Placement placement;if(selected==null||!settings.Images.TryGetValue(selected,out placement))placement=new Placement();ApplyPlacement(placement,false);}
 void PublishPreview(){if(previewOutput==null)return;byte[] frame=pending;if(frame==null||frame.Length!=previewW*previewH*4)frame=new byte[previewW*previewH*4];previewOutput.Cue(frame,0,selected);}
 void CheckPreviewOutput(){if(previewOutput!=null&&previewOutput.Error!=null)previewOutputStatus.Text="Preview NDI FERMO: "+previewOutput.Error;}
 void ClearProgramImmediately(){preview.CancelInteraction();if(output==null){status.Text="Programma NDI arrestato.";return;}if(output.Error!=null){status.Text="Invio fermo: arresta e riavvia NDI.";return;}output.Cue(new byte[outW*outH*4],0);status.Text="ESC: Programma trasparente (cut)";}
 protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==Keys.Escape){ClearProgramImmediately();return true;}return base.ProcessCmdKey(ref msg,keyData);}
}
}
