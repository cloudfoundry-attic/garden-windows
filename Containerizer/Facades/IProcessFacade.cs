#region

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;

#endregion

namespace Containerizer.Facades
{
    public interface IProcessFacade
    {
        ISite Site { get; set; }
        IContainer Container { get; }
        int BasePriority { get; }
        int ExitCode { get; }
        bool HasExited { get; }
        DateTime ExitTime { get; }
        IntPtr Handle { get; }
        int HandleCount { get; }
        int Id { get; }
        string MachineName { get; }
        IntPtr MainWindowHandle { get; }
        string MainWindowTitle { get; }
        ProcessModule MainModule { get; }
        IntPtr MaxWorkingSet { get; set; }
        IntPtr MinWorkingSet { get; set; }
        ProcessModuleCollection Modules { get; }
        int NonpagedSystemMemorySize { get; }
        long NonpagedSystemMemorySize64 { get; }
        int PagedMemorySize { get; }
        long PagedMemorySize64 { get; }
        int PagedSystemMemorySize { get; }
        long PagedSystemMemorySize64 { get; }
        int PeakPagedMemorySize { get; }
        long PeakPagedMemorySize64 { get; }
        int PeakWorkingSet { get; }
        long PeakWorkingSet64 { get; }
        int PeakVirtualMemorySize { get; }
        long PeakVirtualMemorySize64 { get; }
        bool PriorityBoostEnabled { get; set; }
        ProcessPriorityClass PriorityClass { get; set; }
        int PrivateMemorySize { get; }
        long PrivateMemorySize64 { get; }
        TimeSpan PrivilegedProcessorTime { get; }
        string ProcessName { get; }
        IntPtr ProcessorAffinity { get; set; }
        bool Responding { get; }
        int SessionId { get; }
        ProcessStartInfo StartInfo { get; set; }
        DateTime StartTime { get; }
        ISynchronizeInvoke SynchronizingObject { get; set; }
        ProcessThreadCollection Threads { get; }
        TimeSpan TotalProcessorTime { get; }
        TimeSpan UserProcessorTime { get; }
        int VirtualMemorySize { get; }
        long VirtualMemorySize64 { get; }
        bool EnableRaisingEvents { get; set; }
        StreamWriter StandardInput { get; }
        StreamReader StandardOutput { get; }
        StreamReader StandardError { get; }
        int WorkingSet { get; }
        long WorkingSet64 { get; }
        object GetLifetimeService();
        object InitializeLifetimeService();
        ObjRef CreateObjRef(Type requestedType);
        void Dispose();
        event EventHandler Disposed;
        bool CloseMainWindow();
        void Close();
        void Refresh();
        bool Start();
        void Kill();
        bool WaitForExit(int milliseconds);
        void WaitForExit();
        bool WaitForInputIdle(int milliseconds);
        bool WaitForInputIdle();
        void BeginOutputReadLine();
        void BeginErrorReadLine();
        void CancelOutputRead();
        void CancelErrorRead();
        event DataReceivedEventHandler OutputDataReceived;
        event DataReceivedEventHandler ErrorDataReceived;
        event EventHandler Exited;
    }
}