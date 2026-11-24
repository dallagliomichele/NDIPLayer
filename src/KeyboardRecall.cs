using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ImageNdi {
public partial class MainWindow {
 readonly TextBox recallNumber=new TextBox{Name="KeyboardRecallNumber",Width=90,MaxLength=9};
 readonly Label recallStatus=new Label{Name="KeyboardRecallStatus",AutoSize=true};
 bool recallingNumber,updatingRecall;
 Control InitializeKeyboardRecall(){
  var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true};
  panel.Controls.Add(new Label{Text="Numero → Invio",AutoSize=true});panel.Controls.Add(recallNumber);panel.Controls.Add(recallStatus);
  recallNumber.TextChanged+=delegate{if(!updatingRecall)ResolveNumberRecall();};
  recallNumber.KeyPress+=delegate(object sender,KeyPressEventArgs e){if(!Char.IsControl(e.KeyChar)&&(e.KeyChar<'0'||e.KeyChar>'9'))e.Handled=true;};
  files.SelectionChanged+=delegate{if(!recallingNumber)ResetNumberRecall();};ResetNumberRecall();return panel;
 }
 void ResetNumberRecall(){updatingRecall=true;try{recallNumber.Clear();}finally{updatingRecall=false;}recallStatus.ForeColor=Color.DimGray;recallStatus.Text=ActiveBlock==null?"Tutta la libreria":"Blocco: "+ActiveBlock.Name;}
 bool ResolveNumberRecall(){string message;bool valid=SelectNumber(recallNumber.Text,out message);recallStatus.ForeColor=valid?Color.ForestGreen:Color.Firebrick;recallStatus.Text=valid?"Pronta · Invio per mandare in onda":message;return valid;}
 bool SelectNumber(string number,out string message){
  message="";if(number.Length==0){message="Digita un numero";return false;}
  System.Collections.Generic.List<string> matches;
  try{matches=ImageNumbers.Find(settings,ActiveBlock==null?AllAssetKeys():ActiveBlock.Files,number);}catch(ArgumentException){message="Solo cifre (massimo 9)";return false;}
  if(matches.Count!=1){message=matches.Count==0?"Numero assente nel blocco":"Numero duplicato: correggi le assegnazioni";return false;}
  string path=matches[0];if(!System.IO.File.Exists(SourcePath(path))||!Library.Displayable(SourcePath(path))){message="File non disponibile";return false;}
  recallingNumber=true;try{if(search.Text.Length>0)search.Clear();RefreshVisible();files.SelectPaths(new[]{path});}finally{recallingNumber=false;}
  bool valid=String.Equals(selected,path,StringComparison.OrdinalIgnoreCase)&&pending!=null&&selectedSource!=null;if(!valid)message="Immagine non leggibile";return valid;
 }
 static Control KeyboardFocus(Control parent){foreach(Control child in parent.Controls){if(child.Focused)return child;if(child.ContainsFocus)return KeyboardFocus(child);}return parent.Focused?parent:null;}
 bool TryProcessRecallKey(Keys keyData,Control focused,bool repeat){
  if((keyData&Keys.Modifiers)!=Keys.None)return false;
  bool own=focused==recallNumber;
  if(!own&&(focused is TextBoxBase||focused is NumericUpDown||focused is ComboBox||focused!=null&&focused.Parent is UpDownBase))return false;
  int key=(int)(keyData&Keys.KeyCode);int digit=key>=(int)Keys.D0&&key<=(int)Keys.D9?key-(int)Keys.D0:key>=(int)Keys.NumPad0&&key<=(int)Keys.NumPad9?key-(int)Keys.NumPad0:-1;
  if(digit>=0&&!own){if(!repeat){recallNumber.Text=digit.ToString();recallNumber.Focus();recallNumber.SelectionStart=recallNumber.TextLength;}return true;}
  if(keyData!=Keys.Enter)return false;
  if(repeat)return true;
  if(recallNumber.TextLength>0){if(!ResolveNumberRecall())return true;if(output==null||output.Error!=null){Cue(false);return true;}Cue(false);ResetNumberRecall();preview.Focus();return true;}
  if(pending!=null&&selectedSource!=null)Cue(false);return true;
 }
 bool IsBrowserChild(Control control){while(control!=null){if(control==files)return true;control=control.Parent;}return false;}
}
}
