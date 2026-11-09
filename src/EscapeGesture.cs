namespace ImageNdi {
public enum EscapeAction {None,Fade,Cut}
public sealed class EscapeGesture {
 long previous=-100000;
 public const int DoublePressMilliseconds=350;
 public EscapeAction Press(long milliseconds,bool repeat){if(repeat)return EscapeAction.None;bool twice=milliseconds>=previous&&milliseconds-previous<=DoublePressMilliseconds;previous=twice?-100000:milliseconds;return twice?EscapeAction.Cut:EscapeAction.Fade;}
 public void Reset(){previous=-100000;}
}
}
