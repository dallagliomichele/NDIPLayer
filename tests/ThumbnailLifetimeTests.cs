using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using ImageNdi;
static class ThumbnailLifetimeTests {
 public static void Run(Action<bool,string> check){
  using(var browser=new LibraryBrowser()){
   browser.SetPreferences(true,128);browser.SetFiles(new List<string>{"missing-test.png"});
   var type=typeof(LibraryBrowser);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   var old=(BlockingCollection<string>)type.GetField("queue",flags).GetValue(browser);
   type.GetMethod("CancelWorker",flags).Invoke(browser,null);
   check(type.GetField("queue",flags).GetValue(browser)==null&&type.GetField("cancellation",flags).GetValue(browser)==null,"annullamento miniature scollega subito coda e token dal browser");
   check(SpinWait.SpinUntil(()=>{try{var done=old.IsAddingCompleted;return false;}catch(ObjectDisposedException){return true;}},2000),"worker miniature libera la vecchia coda dopo annullamento");
   var request=new RetrieveVirtualItemEventArgs(0);type.GetMethod("Retrieve",flags).Invoke(browser,new object[]{null,request});
   check(request.Item!=null,"richiesta tardiva della lista non usa BlockingCollection eliminata");
   browser.SetPreferences(true,192);request=new RetrieveVirtualItemEventArgs(0);type.GetMethod("Retrieve",flags).Invoke(browser,new object[]{null,request});
   check(request.Item!=null&&type.GetField("queue",flags).GetValue(browser)!=null,"ricostruzione miniature riparte con nuova coda utilizzabile");
   browser.Dispose();request=new RetrieveVirtualItemEventArgs(0);type.GetMethod("Retrieve",flags).Invoke(browser,new object[]{null,request});check(request.Item!=null,"richiesta virtuale durante chiusura resta sicura");
  }
 }
}
