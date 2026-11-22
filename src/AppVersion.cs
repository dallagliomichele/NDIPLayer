namespace ImageNdi {
public static class AppVersion {
 public static string Value {get{return typeof(AppVersion).Assembly.GetName().Version.ToString(3);}}
}
}
