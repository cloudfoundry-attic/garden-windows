#region

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;

#endregion

#pragma warning disable 618

namespace Containerizer.Facades
{
    public class ProcessFacade : IProcessFacade
    {
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private Process process;

        public ProcessFacade()
        {
            process = new Process();
        }

        public object GetLifetimeService()
        {
            return process.GetLifetimeService();
        }

        public object InitializeLifetimeService()
        {
            return process.InitializeLifetimeService();
        }

        public ObjRef CreateObjRef(Type requestedType)
        {
            return process.CreateObjRef(requestedType);
        }

        public void Dispose()
        {
            process.Dispose();
        }

        public ISite Site
        {
            get { return process.Site; }
            set { process.Site = value; }
        }

        public IContainer Container
        {
            get { return process.Container; }
        }

        public event EventHandler Disposed
        {
            add { process.Disposed += value; }
            remove { process.Disposed -= value; }
        }

        public bool CloseMainWindow()
        {
            return process.CloseMainWindow();
        }

        public void Close()
        {
            process.Close();
        }

        public void Refresh()
        {
            process.Refresh();
        }

        public bool Start()
        {
            return process.Start();
        }

        public void Kill()
        {
            process.Kill();
        }

        public bool WaitForExit(int milliseconds)
        {
            return process.WaitForExit(milliseconds);
        }

        public void WaitForExit()
        {
            process.WaitForExit();
        }

        public bool WaitForInputIdle(int milliseconds)
        {
            return process.WaitForInputIdle(milliseconds);
        }

        public bool WaitForInputIdle()
        {
            return process.WaitForInputIdle();
        }

        public void BeginOutputReadLine()
        {
            process.BeginOutputReadLine();
        }

        public void BeginErrorReadLine()
        {
            process.BeginErrorReadLine();
        }

        public void CancelOutputRead()
        {
            process.CancelOutputRead();
        }

        public void CancelErrorRead()
        {
            process.CancelErrorRead();
        }

        public int BasePriority
        {
            get { return process.BasePriority; }
        }

        public int ExitCode
        {
            get { return process.ExitCode; }
        }

        public bool HasExited
        {
            get { return process.HasExited; }
        }

        public DateTime ExitTime
        {
            get { return process.ExitTime; }
        }

        public IntPtr Handle
        {
            get { return process.Handle; }
        }

        public int HandleCount
        {
            get { return process.HandleCount; }
        }

        public int Id
        {
            get { return process.Id; }
        }

        public string MachineName
        {
            get { return process.MachineName; }
        }

        public IntPtr MainWindowHandle
        {
            get { return process.MainWindowHandle; }
        }

        public string MainWindowTitle
        {
            get { return process.MainWindowTitle; }
        }

        public ProcessModule MainModule
        {
            get { return process.MainModule; }
        }

        public IntPtr MaxWorkingSet
        {
            get { return process.MaxWorkingSet; }
            set { process.MaxWorkingSet = value; }
        }

        public IntPtr MinWorkingSet
        {
            get { return process.MinWorkingSet; }
            set { process.MinWorkingSet = value; }
        }

        public ProcessModuleCollection Modules
        {
            get { return process.Modules; }
        }

        public int NonpagedSystemMemorySize
        {
            get { return process.NonpagedSystemMemorySize; }
        }

        public long NonpagedSystemMemorySize64
        {
            get { return process.NonpagedSystemMemorySize64; }
        }

        public int PagedMemorySize
        {
            get { return process.PagedMemorySize; }
        }

        public long PagedMemorySize64
        {
            get { return process.PagedMemorySize64; }
        }

        public int PagedSystemMemorySize
        {
            get { return process.PagedSystemMemorySize; }
        }

        public long PagedSystemMemorySize64
        {
            get { return process.PagedSystemMemorySize64; }
        }

        public int PeakPagedMemorySize
        {
            get { return process.PeakPagedMemorySize; }
        }

        public long PeakPagedMemorySize64
        {
            get { return process.PeakPagedMemorySize64; }
        }

        public int PeakWorkingSet
        {
            get { return process.PeakWorkingSet; }
        }

        public long PeakWorkingSet64
        {
            get { return process.PeakWorkingSet64; }
        }

        public int PeakVirtualMemorySize
        {
            get { return process.PeakVirtualMemorySize; }
        }

        public long PeakVirtualMemorySize64
        {
            get { return process.PeakVirtualMemorySize64; }
        }

        public bool PriorityBoostEnabled
        {
            get { return process.PriorityBoostEnabled; }
            set { process.PriorityBoostEnabled = value; }
        }

        public ProcessPriorityClass PriorityClass
        {
            get { return process.PriorityClass; }
            set { process.PriorityClass = value; }
        }

        public int PrivateMemorySize
        {
            get { return process.PrivateMemorySize; }
        }

        public long PrivateMemorySize64
        {
            get { return process.PrivateMemorySize64; }
        }

        public TimeSpan PrivilegedProcessorTime
        {
            get { return process.PrivilegedProcessorTime; }
        }

        public string ProcessName
        {
            get { return process.ProcessName; }
        }

        public IntPtr ProcessorAffinity
        {
            get { return process.ProcessorAffinity; }
            set { process.ProcessorAffinity = value; }
        }

        public bool Responding
        {
            get { return process.Responding; }
        }

        public int SessionId
        {
            get { return process.SessionId; }
        }

        public ProcessStartInfo StartInfo
        {
            get { return process.StartInfo; }
            set { process.StartInfo = value; }
        }

        public DateTime StartTime
        {
            get { return process.StartTime; }
        }

        public ISynchronizeInvoke SynchronizingObject
        {
            get { return process.SynchronizingObject; }
            set { process.SynchronizingObject = value; }
        }

        public ProcessThreadCollection Threads
        {
            get { return process.Threads; }
        }

        public TimeSpan TotalProcessorTime
        {
            get { return process.TotalProcessorTime; }
        }

        public TimeSpan UserProcessorTime
        {
            get { return process.UserProcessorTime; }
        }

        public int VirtualMemorySize
        {
            get { return process.VirtualMemorySize; }
        }

        public long VirtualMemorySize64
        {
            get { return process.VirtualMemorySize64; }
        }

        public bool EnableRaisingEvents
        {
            get { return process.EnableRaisingEvents; }
            set { process.EnableRaisingEvents = value; }
        }

        public StreamWriter StandardInput
        {
            get { return process.StandardInput; }
        }

        public StreamReader StandardOutput
        {
            get { return process.StandardOutput; }
        }

        public StreamReader StandardError
        {
            get { return process.StandardError; }
        }

        public int WorkingSet
        {
            get { return process.WorkingSet; }
        }

        public long WorkingSet64
        {
            get { return process.WorkingSet64; }
        }

        public event DataReceivedEventHandler OutputDataReceived
        {
            add { process.OutputDataReceived += value; }
            remove { process.OutputDataReceived -= value; }
        }

        public event DataReceivedEventHandler ErrorDataReceived
        {
            add { process.ErrorDataReceived += value; }
            remove { process.ErrorDataReceived -= value; }
        }

        public event EventHandler Exited
        {
            add { process.Exited += value; }
            remove { process.Exited -= value; }
        }
    }
}

#pragma warning restore 618