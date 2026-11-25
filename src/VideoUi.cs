using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace ImageNdi {
public partial class MainWindow {
 readonly CheckBox videoLoop=new CheckBox{Name="VideoLoop",Text="Loop video",AutoSize=true,Enabled=false};
 readonly Label videoStatus=new Label{Name="VideoStatus",AutoSize=true};
 readonly VideoTransportBar previewTransport=new VideoTransportBar{Name="PreviewVideoTransport",Dock=DockStyle.Right};readonly VideoTransportBar programTransport=new VideoTransportBar{Name="ProgramVideoTransport",Dock=DockStyle.Right};
 System.Threading.Tasks.Task<double> previewDuration;
 VideoSession selectionVideo;long selectionVideoSequence;bool videoPending;DateTime videoDue;
 Task<VideoSession> programPreroll;
 byte[] previewPoster;
 int prerollGeneration;
 void DiscardProgramPreroll(){
  Interlocked.Increment(ref prerollGeneration);
  var task=programPreroll;programPreroll=null;
  if(task!=null)task.ContinueWith(completed=>{
   if(completed.Status==TaskStatus.RanToCompletion){if(completed.Result!=null)completed.Result.Dispose();}
   else if(completed.IsFaulted){var error=completed.Exception;}
  },TaskScheduler.Default);
 }
 void PrepareProgramVideo(){
  DiscardProgramPreroll();
  string path=SourcePath(selected);
  if(output==null||output.Error!=null||previewPoster==null||!VideoTools.IsVideo(path)||pointerEditing||controlClosing)return;
  string asset=selected;
  var cue=CueFor(asset);
  var placement=PlacementNow();
  int width=outW,height=outH,rate=activeProgramFps,generation=prerollGeneration;
  var initial=previewPoster;
  var bus=ProgramAudio;
  double gain=GainFor(asset),speed=SpeedFor(asset);
  programPreroll=Task.Run(()=>{
   if(Volatile.Read(ref prerollGeneration)!=generation)return null;
   return new VideoSession(path,width,height,rate,placement,placement.Loop,initial,bus,gain,speed,
    !cue.AutoStart,cue.InSeconds,cue.OutSeconds,asset,true);
  });
 }
 VideoSession TakeProgramVideo(){
  var task=programPreroll;programPreroll=null;
  VideoSession source=null;
  if(task!=null){
   try{source=task.GetAwaiter().GetResult();}catch(Exception){/* Retry via the normal cue path. */}
   if(source!=null&&source.Error!=null){source.Dispose();source=null;}
  }
  if(source==null){
   var cue=CueFor(selected);
   var initial=previewPoster;
   if(initial==null||initial.Length!=outW*outH*4)initial=Renderer.RenderImage(selectedSource,outW,outH,PlacementNow());
   source=new VideoSession(SourcePath(selected),outW,outH,activeProgramFps,PlacementNow(),videoLoop.Checked,
    initial,ProgramAudio,GainFor(selected),SpeedFor(selected),!cue.AutoStart,cue.InSeconds,cue.OutSeconds,selected,true);
  }
  source.SetAudioGain(GainFor(selected));
  return source;
 }
 readonly NumericUpDown videoSpeed=new NumericUpDown{Name="VideoSpeed",Minimum=.5m,Maximum=5m,Increment=.1m,DecimalPlaces=1,Value=1,Width=72,Enabled=false,AccessibleName="Velocità video"};bool loadingSpeed;
 readonly NumericUpDown videoIn=new NumericUpDown{Name="VideoInSeconds",Minimum=0,Maximum=86400,DecimalPlaces=2,Increment=.1m,Width=78};readonly NumericUpDown videoOut=new NumericUpDown{Name="VideoOutSeconds",Minimum=0,Maximum=86400,DecimalPlaces=2,Increment=.1m,Width=78};readonly NumericUpDown videoSeek=new NumericUpDown{Name="VideoSeekSeconds",Minimum=0,Maximum=86400,DecimalPlaces=2,Increment=.1m,Width=78};readonly CheckBox videoAutoStart=new CheckBox{Name="VideoAutoStart",Text="Autostart",Checked=true,AutoSize=true};bool loadingVideoCue;readonly FlowLayoutPanel videoCueControls=new FlowLayoutPanel{Name="VideoCueControls",Dock=DockStyle.Fill,WrapContents=false};
 VideoCue CueFor(string path){VideoCue value;return path!=null&&settings.VideoCues.TryGetValue(path,out value)?value:new VideoCue();}
 void LoadPreviewDuration(){preview.HeadingDetail="";preview.Invalidate();previewDuration=null;string path=SourcePath(selected);if(!VideoTools.IsVideo(path))return;preview.HeadingDetail=" · durata…";previewDuration=System.Threading.Tasks.Task.Run(()=>{try{return VideoMetadata.Duration(path);}catch{return 0;}});}
 void UpdatePreviewDuration(){var task=previewDuration;if(task==null||!task.IsCompleted)return;previewDuration=null;double seconds=task.Status==System.Threading.Tasks.TaskStatus.RanToCompletion?task.Result:0;var time=TimeSpan.FromSeconds(seconds);preview.HeadingDetail=seconds>0?String.Format(" · durata {0:00}:{1:00}:{2:00}",(int)time.TotalHours,time.Minutes,time.Seconds):" · durata non disponibile";preview.Invalidate();}
 void LoadVideoCue(){loadingVideoCue=true;try{bool enabled=VideoTools.IsVideo(SourcePath(selected));var cue=CueFor(selected);videoIn.Enabled=videoOut.Enabled=videoSeek.Enabled=videoAutoStart.Enabled=enabled;videoIn.Value=(decimal)Math.Min(86400,cue.InSeconds);videoOut.Value=(decimal)Math.Min(86400,cue.OutSeconds);videoSeek.Value=videoIn.Value;videoAutoStart.Checked=cue.AutoStart;}finally{loadingVideoCue=false;}}
 void SaveVideoCue(){if(loadingVideoCue||!VideoTools.IsVideo(SourcePath(selected)))return;var cue=new VideoCue{InSeconds=(double)videoIn.Value,OutSeconds=(double)videoOut.Value,AutoStart=videoAutoStart.Checked};try{cue.Validate();double duration=VideoMetadata.Duration(SourcePath(selected));if(duration>0&&(cue.InSeconds>=duration||cue.OutSeconds>duration+.01))throw new ArgumentException("Entrata e uscita devono rientrare nella durata del video.");settings.VideoCues[selected]=cue;PrepareProgramVideo();Save();if(selectionVideo!=null)SeekPreview(cue.InSeconds);}catch(Exception e){status.Text=e.Message;LoadVideoCue();}}
 double PreviewPosition(){if(selectionVideo==null)return (double)videoSeek.Value;VideoPlaybackStatus playback;selectionVideo.ReadPlaybackFrame(out playback);return selectionVideo.StartSeconds+playback.Position;}
 void MarkVideoPoint(bool entry){if(selectionVideo==null)return;decimal position=(decimal)Math.Min(86400,Math.Max(0,PreviewPosition()));if(entry)videoIn.Value=position;else videoOut.Value=position;}
 void SeekPreview(double seconds){if(selectionVideo==null)return;var cue=CueFor(selected);if(seconds<cue.InSeconds||cue.OutSeconds>0&&seconds>=cue.OutSeconds){status.Text="Il seek deve restare tra entrata e uscita.";return;}VideoSession replacement=null;try{var previous=selectionVideo;replacement=previous.Seek(seconds,previous.Paused,GainFor(selected));replacement.ActivateAudio(0);selectionVideo=replacement;replacement=null;previous.Dispose();selectionVideoSequence=0;PublishPreview();previewTransport.SetVideo(selectionVideo);}catch(Exception e){if(replacement!=null)replacement.Dispose();status.Text="Seek video: "+e.Message;}}
 double SpeedFor(string path){double value;return path!=null&&settings.VideoSpeeds.TryGetValue(path,out value)?value:1;}
 void LoadVideoSpeed(){LoadPreviewDuration();LoadVideoCue();loadingSpeed=true;try{videoSpeed.Enabled=VideoTools.IsVideo(SourcePath(selected));videoSpeed.Value=(decimal)Math.Max(.5,Math.Min(5,SpeedFor(selected)));}finally{loadingSpeed=false;}}
 void InitializeVideo(Control bar){bar.Height=100;var row=new FlowLayoutPanel{Name="VideoControls",Width=1000,Height=34,AutoScroll=true};bar.Controls.Add(row);row.Controls.Add(videoLoop);row.Controls.Add(videoStatus);var cueRow=videoCueControls;AddField(cueRow,"Entrata s",videoIn);AddButton(cueRow,"Segna entrata",delegate{MarkVideoPoint(true);});AddField(cueRow,"Uscita s (0 = fine)",videoOut);AddButton(cueRow,"Segna uscita",delegate{MarkVideoPoint(false);});AddField(cueRow,"Seek s",videoSeek);AddButton(cueRow,"Vai",delegate{SeekPreview((double)videoSeek.Value);});cueRow.Controls.Add(videoAutoStart);bar.Controls.Add(cueRow);videoIn.ValueChanged+=delegate{SaveVideoCue();};videoOut.ValueChanged+=delegate{SaveVideoCue();};videoAutoStart.CheckedChanged+=delegate{SaveVideoCue();};videoLoop.CheckedChanged+=delegate{if(!loading&&VideoTools.IsVideo(SourcePath(selected)))ApplyPlacement(PlacementNow(),true);};previewTransport.PlayPauseRequested+=delegate{ToggleVideo(false);};programTransport.PlayPauseRequested+=delegate{ToggleVideo(true);};previewTransport.RestartRequested+=delegate{RestartVideo(false,null);};programTransport.RestartRequested+=delegate{RestartVideo(true,null);};videoSpeed.ValueChanged+=delegate{if(loadingSpeed||selected==null||!VideoTools.IsVideo(SourcePath(selected)))return;double speed=(double)videoSpeed.Value;settings.VideoSpeeds[selected]=speed;PrepareProgramVideo();mutePreview.Checked=true;if(selectionVideo!=null)RestartVideo(false,speed);Save();};Disposed+=delegate{DiscardProgramPreroll();StopSelectionVideo();};}
 void StopSelectionVideo(){videoPending=false;if(selectionVideo!=null){selectionVideo.Dispose();selectionVideo=null;}selectionVideoSequence=0;previewTransport.SetVideo(null);}
 void ScheduleSelectionVideo(){videoLoop.Enabled=VideoTools.IsVideo(SourcePath(selected));if(videoLoop.Enabled&&pending!=null){videoPending=true;videoDue=DateTime.UtcNow.AddMilliseconds(150);}}
 void StartSelectionVideo(){StopSelectionVideo();videoLoop.Enabled=VideoTools.IsVideo(SourcePath(selected));if(!videoLoop.Enabled||pending==null)return;selectionVideo=new VideoSession(SourcePath(selected),FrameWidth,FrameHeight,output==null?previewOutput==null?(int)fps.Committed:previewFps:activeProgramFps,PlacementNow(),videoLoop.Checked,pending,PreviewAudio,GainFor(selected),SpeedFor(selected),false,CueFor(selected).InSeconds,CueFor(selected).OutSeconds,selected);selectionVideo.ActivateAudio(0);PublishPreview();previewTransport.SetVideo(selectionVideo);}
 void ToggleVideo(bool onProgram){var video=onProgram?(output==null?null:output.CurrentVideo):selectionVideo;if(video==null)return;if(video.Ended&&!video.Paused){RestartVideo(onProgram,null);return;}video.SetPaused(!video.Paused);(onProgram?programTransport:previewTransport).SetVideo(video);}
 void RestartVideo(bool onProgram,double? newSpeed){RestartVideoCore(onProgram,newSpeed,true);}
 bool RestartVideoCore(bool onProgram,double? newSpeed,bool showError){var video=onProgram?(output==null?null:output.CurrentVideo):selectionVideo;if(video==null)return false;VideoSession replacement=null;bool adopted=false;try{double speed=newSpeed??video.Speed;replacement=onProgram?video.Restart(speed,video.Paused,GainFor(video.AssetKey)):video.RestartAt(CueFor(video.AssetKey).InSeconds,speed,video.Paused,GainFor(video.AssetKey));if(onProgram){System.Threading.SpinWait.SpinUntil(()=>replacement.DecodedFrames>0||replacement.Error!=null,1500);output.CueSource(replacement,replacement.ReadFrame(),0,video.AssetKey);adopted=true;}else{replacement.ActivateAudio(0);selectionVideo=replacement;adopted=true;selectionVideoSequence=0;video.Dispose();pending=replacement.ReadFrame();preview.SetFrame(Renderer.ToBitmap(pending,FrameWidth,FrameHeight),FrameWidth,FrameHeight);PublishPreview();}(onProgram?programTransport:previewTransport).SetVideo(replacement);return true;}catch(Exception e){status.Text="Controllo video: "+e.Message;if(showError)MessageBox.Show(e.Message,"Controllo video");(onProgram?programTransport:previewTransport).SetVideo(onProgram?(output==null?null:output.CurrentVideo):selectionVideo);return false;}finally{if(!adopted&&replacement!=null)replacement.Dispose();}}
 void UpdateSelectionVideo(){UpdatePreviewDuration();programTransport.SetVideo(output==null?null:output.CurrentVideo);if(videoPending&&DateTime.UtcNow>=videoDue){try{StartSelectionVideo();}catch(Exception e){videoStatus.Text=e.Message;return;}}if(selectionVideo==null){videoStatus.Text="";return;}previewTransport.SetVideo(selectionVideo);if(!videoSeek.ContainsFocus)videoSeek.Value=(decimal)Math.Min((double)videoSeek.Maximum,Math.Max(0,PreviewPosition()));videoStatus.Text=selectionVideo.Error??(selectionVideo.Ended?"Video terminato · ultimo fotogramma":selectionVideo.Paused?"Video in pausa":videoLoop.Checked?"Video in loop · "+(PreviewAudio==null?"audio Preview spento":"audio Preview abilitato"):"Video in riproduzione · "+(PreviewAudio==null?"audio Preview spento":"audio Preview abilitato"));long sequence=System.Threading.Interlocked.Read(ref selectionVideo.DecodedFrames);if(sequence==selectionVideoSequence)return;selectionVideoSequence=sequence;pending=selectionVideo.ReadFrame();preview.SetFrame(Renderer.ToBitmap(pending,FrameWidth,FrameHeight),FrameWidth,FrameHeight);}
}
}
