using System.Runtime.InteropServices;
using System.Windows.Automation;
using NovaForge.Core;
namespace NovaForge.Automation;
public sealed class MouseRecorder : IDisposable
{
 private readonly Func<int,bool> allowed;private readonly Action<UiStep> captured;private readonly Hook callback;private readonly HashSet<int> external;private IntPtr handle;
 public MouseRecorder(Func<int,bool> filter,Action<UiStep> onStep,HashSet<int>? extra=null){allowed=filter;captured=onStep;external=extra??[];callback=OnMouse;handle=SetWindowsHookEx(14,callback,GetModuleHandle(null),0);if(handle==IntPtr.Zero)throw new IOException("Mausbeobachtung konnte nicht gestartet werden.");}
 private void Emit(UiStep step,int pid){if(external.Contains(pid))UiAutomationEngine.BindExternal(step,pid);captured(step);}
 public void CaptureFocusedText(){var element=AutomationElement.FocusedElement;if(allowed(element.Current.ProcessId)){var step=UiAutomationEngine.Capture(element,true);if(step?.Action=="SetValue")Emit(step,element.Current.ProcessId);}}
 public AutomationElement SecretTarget(){var element=AutomationElement.FocusedElement;if(!allowed(element.Current.ProcessId))throw new InvalidDataException("Dieses Fenster gehört nicht zur Aufnahme. Zusatzfenster zuerst verbinden.");return element;}
 public UiStep CaptureSecret(AutomationElement target,string value){var step=UiAutomationEngine.CaptureSecret(target,value);Emit(step,target.Current.ProcessId);return step;}
 private IntPtr OnMouse(int code,IntPtr message,IntPtr data){if(code>=0&&message.ToInt32()==0x0201){try{var point=Marshal.PtrToStructure<MouseData>(data);var element=AutomationElement.FromPoint(new System.Windows.Point(point.X,point.Y));if(allowed(element.Current.ProcessId)){var step=UiAutomationEngine.Capture(element);if(step!=null)Emit(step,element.Current.ProcessId);}}catch{}}return CallNextHookEx(handle,code,message,data);}
 public void Dispose(){if(handle!=IntPtr.Zero){UnhookWindowsHookEx(handle);handle=IntPtr.Zero;}}
 private delegate IntPtr Hook(int code,IntPtr message,IntPtr data);
 [StructLayout(LayoutKind.Sequential)]private struct MouseData{public int X,Y;public uint Mouse,Flags,Time;public IntPtr Extra;}
 [DllImport("user32.dll")]private static extern IntPtr SetWindowsHookEx(int id,Hook callback,IntPtr module,uint thread);
 [DllImport("user32.dll")]private static extern bool UnhookWindowsHookEx(IntPtr handle);
 [DllImport("user32.dll")]private static extern IntPtr CallNextHookEx(IntPtr handle,int code,IntPtr message,IntPtr data);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]private static extern IntPtr GetModuleHandle(string? name);
}
