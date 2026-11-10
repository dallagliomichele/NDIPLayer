using System;
using System.Drawing;
using System.Windows.Forms;
namespace ImageNdi {
public partial class MainWindow {
 readonly CheckBox videoLoop=new CheckBox{Name="VideoLoop",Text="Loop video",AutoSize=true,Enabled=false};
 readonly Label videoStatus=new Label{Name="VideoStatus",AutoSize=true};
 VideoSession selectionVideo;long selectionVideoSequence;bool videoPending;DateTime videoDue;
 void InitializeVideo(Control bar){bar.Height=100;var row=new FlowLayoutPanel{Name="VideoControls",Width=1000,Height=34,AutoScroll=true};bar.Controls.Add(row);row.Controls.Add(videoLoop);var replay=new Button{Text="Riavvia anteprima video",AutoSize=true};row.Controls.Add(replay);row.Controls.Add(videoStatus);replay.Click+=delegate{if(VideoTools.IsVideo(selected))RenderPreview();};videoLoop.CheckedChanged+=delegate{if(!loading&&VideoTools.IsVideo(selected))ApplyPlacement(PlacementNow(),true);};Disposed+=delegate{StopSelectionVideo();};}
 void StopSelectionVideo(){videoPending=false;if(selectionVideo!=null){selectionVideo.Dispose();selectionVideo=null;}selectionVideoSequence=0;}
 void ScheduleSelectionVideo(){videoLoop.Enabled=VideoTools.IsVideo(selected);if(videoLoop.Enabled&&pending!=null){videoPending=true;videoDue=DateTime.UtcNow.AddMilliseconds(150);}}
 void StartSelectionVideo(){StopSelectionVideo();videoLoop.Enabled=VideoTools.IsVideo(selected);if(!videoLoop.Enabled||pending==null)return;selectionVideo=new VideoSession(selected,FrameWidth,FrameHeight,output==null?previewOutput==null?(int)fps.Value:previewFps:activeProgramFps,PlacementNow(),videoLoop.Checked,pending,PreviewAudio,GainFor(selected));selectionVideo.ActivateAudio(0);PublishPreview();}
 void UpdateSelectionVideo(){if(videoPending&&DateTime.UtcNow>=videoDue){try{StartSelectionVideo();}catch(Exception e){videoStatus.Text=e.Message;return;}}if(selectionVideo==null){videoStatus.Text="";return;}videoStatus.Text=selectionVideo.Error??(selectionVideo.Ended?"Video terminato · ultimo fotogramma":videoLoop.Checked?"Video in loop · "+(PreviewAudio==null?"audio Preview spento":"audio Preview abilitato"):"Video in riproduzione · "+(PreviewAudio==null?"audio Preview spento":"audio Preview abilitato"));long sequence=System.Threading.Interlocked.Read(ref selectionVideo.DecodedFrames);if(sequence==selectionVideoSequence)return;selectionVideoSequence=sequence;pending=selectionVideo.ReadFrame();preview.SetFrame(Renderer.ToBitmap(pending,FrameWidth,FrameHeight),FrameWidth,FrameHeight);}
}
}
