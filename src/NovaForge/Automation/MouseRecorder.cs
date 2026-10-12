using System.Collections.Concurrent;
using System.Windows.Automation;
using NovaForge.Core;
using UIA=System.Windows.Automation.Automation;
using System.Runtime.InteropServices;
using System.Windows;
namespace NovaForge.Automation;
// UIA callbacks run outside the input hook, so providers cannot block mouse delivery.
public sealed class MouseRecorder : IDisposable
{
 public static Task<MouseRecorder> Create(Func<int,bool> filter,Action<UiStep> onStep,HashSet<int>? extra=null,Action<string>? onError=null,bool captureOtherWindows=false)=>Task.Run(()=>new MouseRecorder(filter,onStep,extra,onError,captureOtherWindows));
 private readonly Func<int,bool> allowed;private readonly Action<UiStep> captured;private readonly Action<string> report;private readonly HashSet<int> external;private volatile bool otherWindows;public bool CaptureOtherWindows{get=>otherWindows;set=>otherWindows=value;}
 private readonly ConcurrentDictionary<string,(UiStep Step,int Pid)> selectors=new();private readonly ConcurrentDictionary<string,(UiStep Step,int Pid,Rect Bounds,long At)> targets=new();private readonly ConcurrentDictionary<string,long> mouseActions=new();private readonly ConcurrentDictionary<int,string> titles=new();private readonly ConcurrentDictionary<int,(string Name,string Hash)> identities=new();private volatile bool disposed;
 private readonly Thread inputThread;private readonly Hook inputHook;private readonly System.Threading.Timer hoverTimer;private IntPtr hookHandle;private uint hookThreadId;private int hoverBusy;
 private readonly AutomationEventHandler invoked,selected;private readonly AutomationPropertyChangedEventHandler toggled;private readonly AutomationFocusChangedEventHandler focused;
 public MouseRecorder(Func<int,bool> filter,Action<UiStep> onStep,HashSet<int>? extra=null,Action<string>? onError=null,bool captureOtherWindows=false)
 {
  allowed=filter;captured=onStep;external=extra??[];report=onError??(_=>{});otherWindows=captureOtherWindows;
  invoked=(sender,_)=>Handle((AutomationElement)sender,"Invoke",null);selected=(sender,_)=>Handle((AutomationElement)sender,"Select",null);toggled=(sender,e)=>Handle((AutomationElement)sender,"Toggle",e.NewValue);focused=(sender,_)=>Remember((AutomationElement)sender);
  inputHook=Input;inputThread=new Thread(Pump){IsBackground=true,Name="NovaForge input observer"};hoverTimer=new System.Threading.Timer(_=>Hover(),null,100,100);inputThread.Start();
  var cache=new CacheRequest{TreeScope=TreeScope.Element,AutomationElementMode=AutomationElementMode.Full};foreach(var property in new[]{AutomationElement.RuntimeIdProperty,AutomationElement.ProcessIdProperty,AutomationElement.NameProperty,AutomationElement.AutomationIdProperty,AutomationElement.ClassNameProperty,AutomationElement.ControlTypeProperty})cache.Add(property);
  try{using(cache.Activate()){UIA.AddAutomationFocusChangedEventHandler(focused);UIA.AddAutomationEventHandler(InvokePattern.InvokedEvent,AutomationElement.RootElement,TreeScope.Subtree,invoked);UIA.AddAutomationEventHandler(SelectionItemPattern.ElementSelectedEvent,AutomationElement.RootElement,TreeScope.Subtree,selected);UIA.AddAutomationPropertyChangedEventHandler(AutomationElement.RootElement,TreeScope.Subtree,toggled,TogglePattern.ToggleStateProperty);}Remember(AutomationElement.FocusedElement);}catch{Dispose();throw;}
 }
 private static string Key(AutomationElement element){try{return string.Join(",",(int[])element.GetCachedPropertyValue(AutomationElement.RuntimeIdProperty));}catch{return string.Join(",",element.GetRuntimeId());}}
 private static int Pid(AutomationElement element){try{return element.Current.ProcessId;}catch{return (int)element.GetCachedPropertyValue(AutomationElement.ProcessIdProperty);}}
 private bool Accept(int pid)=>pid!=Environment.ProcessId&&(allowed(pid)||otherWindows);
 private void Remember(AutomationElement element){if(disposed)return;try{var pid=Pid(element);if(!Accept(pid))return;var window=UiAutomationEngine.TopWindow(element);if(window!=null)titles[pid]=window.Current.Name;var step=UiAutomationEngine.Capture(element);if(step!=null){Bind(step,pid);var key=Key(element);selectors[key]=(step,pid);targets[key]=(step,pid,element.Current.BoundingRectangle,Environment.TickCount64);}}catch(ElementNotAvailableException){}catch(Exception e){report("Steuerelement nicht zugänglich: "+e.Message);}}
 private static string Signature(UiStep s)=>s.WindowTitle+"|"+s.AutomationId+"|"+s.Name+"|"+s.Action;
 private void Hover(){if(disposed||Interlocked.Exchange(ref hoverBusy,1)!=0)return;try{if(GetCursorPos(out var point))Remember(AutomationElement.FromPoint(new Point(point.X,point.Y)));}catch{}finally{Volatile.Write(ref hoverBusy,0);}}
 private IntPtr Input(int code,IntPtr message,IntPtr data){if(code>=0&&!disposed&&message.ToInt32()==0x0201){var point=Marshal.PtrToStructure<MouseData>(data);var handle=WindowFromPoint(new NativePoint{X=point.X,Y=point.Y});GetWindowThreadProcessId(handle,out var pid);var target=targets.Values.Where(x=>x.Pid==(int)pid&&x.Bounds.Contains(point.X,point.Y)&&Environment.TickCount64-x.At<1000).OrderByDescending(x=>x.At).ThenBy(x=>x.Bounds.Width*x.Bounds.Height).FirstOrDefault();if(target.Step!=null){var step=target.Step;mouseActions[Signature(step)]=Environment.TickCount64;ThreadPool.QueueUserWorkItem(_=>{if(!disposed)captured(step);});}}return CallNextHookEx(hookHandle,code,message,data);}
 private void Pump(){hookThreadId=GetCurrentThreadId();PeekMessage(out _,IntPtr.Zero,0,0,0);if(disposed)return;hookHandle=SetWindowsHookEx(14,inputHook,GetModuleHandle(null),0);if(hookHandle==IntPtr.Zero){report("Zusätzliche Klickbeobachtung nicht verfügbar; UI-Ereignisse und F7 bleiben verfügbar.");return;}try{while(!disposed&&GetMessage(out var message,IntPtr.Zero,0,0)>0){TranslateMessage(ref message);DispatchMessage(ref message);}}finally{UnhookWindowsHookEx(hookHandle);}}
 private void Bind(UiStep step,int pid){if(!identities.TryGetValue(pid,out var identity)){UiAutomationEngine.BindExternal(step,pid);identity=(step.ProcessName,step.ProcessImageSha256);identities[pid]=identity;}step.ProcessName=identity.Name;step.ProcessImageSha256=identity.Hash;}
 private UiStep Snapshot(AutomationElement element,int pid)
 {
  var key=Key(element);if(selectors.TryGetValue(key,out var prior))return System.Text.Json.JsonSerializer.Deserialize<UiStep>(System.Text.Json.JsonSerializer.Serialize(prior.Step,Project.JsonOptions),Project.JsonOptions)!;
  try{var step=UiAutomationEngine.Capture(element);if(step!=null)return step;}catch(ElementNotAvailableException){}
  if(!titles.TryGetValue(pid,out var title))throw new InvalidOperationException("Fenstertitel fehlt. Vor der Aktion das Steuerelement fokussieren oder F7 verwenden.");
  return new(){WindowTitle=title,AutomationId=(string)element.GetCachedPropertyValue(AutomationElement.AutomationIdProperty),Name=(string)element.GetCachedPropertyValue(AutomationElement.NameProperty),ClassName=(string)element.GetCachedPropertyValue(AutomationElement.ClassNameProperty),ControlTypeId=((ControlType)element.GetCachedPropertyValue(AutomationElement.ControlTypeProperty)).Id};
 }
 private void Handle(AutomationElement element,string action,object? value){if(disposed)return;try{var pid=Pid(element);if(!Accept(pid))return;var step=Snapshot(element,pid);if(action=="Invoke"&&step.Action is "Toggle" or "Select")return;step.Action=action;if(action=="Toggle"){var state=value is ToggleState typed?typed:(ToggleState)Convert.ToInt32(value);if(state==ToggleState.Indeterminate)return;step.Value=state==ToggleState.On?"On":"Off";}Bind(step,pid);if(mouseActions.TryGetValue(Signature(step),out var timestamp)&&Environment.TickCount64-timestamp<800)return;captured(step);}catch(ElementNotAvailableException){report("Fenster schloss vor der Aufnahme. Aktion bei Bedarf mit F7 vormerken.");}catch(Exception e){report("Aufnahmefehler: "+e.Message);}}
 public void CaptureFocusedAction(){var element=SecretTarget();var step=UiAutomationEngine.Capture(element)??throw new InvalidDataException("Kein unterstützter Knopf oder Auswahlfeld fokussiert.");Bind(step,Pid(element));captured(step);}
 public void CaptureFocusedText(){var element=SecretTarget();var step=UiAutomationEngine.Capture(element,true);if(step?.Action!="SetValue")throw new InvalidDataException("Textfeld nicht auslesbar. Vertrauliche Werte ausdrücklich über F9 eingeben.");Bind(step,Pid(element));captured(step);}
 public AutomationElement SecretTarget(){var element=AutomationElement.FocusedElement;if(!Accept(Pid(element)))throw new InvalidDataException("Dieses Fenster gehört nicht zur Aufnahme. Zusatzfenster verbinden.");return element;}
 public UiStep CaptureSecret(AutomationElement target,string value){var step=UiAutomationEngine.CaptureSecret(target,value);Bind(step,Pid(target));captured(step);return step;}
 public void Dispose(){disposed=true;hoverTimer.Dispose();if(hookThreadId!=0)PostThreadMessage(hookThreadId,0x0012,IntPtr.Zero,IntPtr.Zero);_ = Task.Run(()=>{try{UIA.RemoveAutomationFocusChangedEventHandler(focused);}catch{}try{UIA.RemoveAutomationEventHandler(InvokePattern.InvokedEvent,AutomationElement.RootElement,invoked);}catch{}try{UIA.RemoveAutomationEventHandler(SelectionItemPattern.ElementSelectedEvent,AutomationElement.RootElement,selected);}catch{}try{UIA.RemoveAutomationPropertyChangedEventHandler(AutomationElement.RootElement,toggled);}catch{}});}
 [DllImport("user32.dll")]private static extern bool PeekMessage(out NativeMessage message,IntPtr window,uint first,uint last,uint remove);
 private delegate IntPtr Hook(int code,IntPtr message,IntPtr data);
 [StructLayout(LayoutKind.Sequential)]private struct NativePoint{public int X,Y;}
 [StructLayout(LayoutKind.Sequential)]private struct MouseData{public int X,Y;public uint Mouse,Flags,Time;public IntPtr Extra;}
 [StructLayout(LayoutKind.Sequential)]private struct NativeMessage{public IntPtr Window;public uint Id;public UIntPtr WParam;public IntPtr LParam;public uint Time;public NativePoint Point;public uint Private;}
 [DllImport("user32.dll")]private static extern bool GetCursorPos(out NativePoint point);
 [DllImport("user32.dll")]private static extern IntPtr WindowFromPoint(NativePoint point);
 [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(IntPtr handle,out uint pid);
 [DllImport("user32.dll")]private static extern IntPtr SetWindowsHookEx(int id,Hook callback,IntPtr module,uint thread);
 [DllImport("user32.dll")]private static extern IntPtr CallNextHookEx(IntPtr handle,int code,IntPtr message,IntPtr data);
 [DllImport("user32.dll")]private static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]private static extern IntPtr GetModuleHandle(string? name);
 [DllImport("kernel32.dll")]private static extern uint GetCurrentThreadId();
 [DllImport("user32.dll")]private static extern int GetMessage(out NativeMessage message,IntPtr window,uint first,uint last);
 [DllImport("user32.dll")]private static extern bool TranslateMessage(ref NativeMessage message);
 [DllImport("user32.dll")]private static extern IntPtr DispatchMessage(ref NativeMessage message);
 [DllImport("user32.dll")]private static extern bool PostThreadMessage(uint thread,uint message,IntPtr w,IntPtr l);
}
