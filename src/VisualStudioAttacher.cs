﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsDebugger
{
    /// <summary>
    /// I found this gem originally on PasteBin (http://pastebin.com/pHWMP3EQ) and (http://pastebin.com/KKyBpWQW).
    /// Since it was dropped there anonymously, it had little documentation, and I made a change, I thought I'd migrate it to a gist.
    /// I found this snippet of code extremely useful, it flows much better than the alternative Debugger.Launch() call.
    ///
    /// Example usage:
    /// /// <summary>
    /// /// Attaches a debugger, if built in with DEBUG symbol
    /// /// </summary>
    /// [Conditional("DEBUG")] 
    /// private static void AttachDebugger()
    /// {
    /// 	if (!Debugger.IsAttached)
    /// 	{
    /// 		// do a search for any Visual Studio processes that also have our solution currently open
    /// 		var vsProcess =
    /// 			VisualStudioAttacher.GetVisualStudioForSolutions(
    /// 				new List<string>() { "FooBar2012.sln", "FooBar.sln" });
    /// 				
    /// 		if (vsProcess != null) 
    /// 		{
    /// 			VisualStudioAttacher.AttachVisualStudioToProcess(proc, Process.GetCurrentProcess());
    /// 		}
    /// 		else
    /// 		{
    /// 			// try and attach the old fashioned way
    /// 			Debugger.Launch();
    /// 		}
    /// 
    /// 		if (Debugger.IsAttached)
    /// 		{
    /// 			Console.WriteLine("\t>>> Attach sucessful");
    /// 		}
    /// 	}
    /// }
    /// </summary>
    using System.IO;
    using EnvDTE;
    using DTEProcess = EnvDTE.Process;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Process = System.Diagnostics.Process;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System.Runtime.InteropServices;

    using System.Diagnostics;

    namespace AppController
    {
        #region Classes

        public static class VisualStudioAttacher
        {
            #region Public Methods

            [DllImport("User32")]
            private static extern int ShowWindow(int hwnd, int nCmdShow);

            [DllImport("ole32.dll")]
            public static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

            [DllImport("ole32.dll")]
            public static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);


            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr SetFocus(IntPtr hWnd);


            public static string GetSolutionForVisualStudio(Process visualStudioProcess)
            {
                _DTE visualStudioInstance;
                if (TryGetVsInstance(visualStudioProcess.Id, out visualStudioInstance))
                {
                    try
                    {
                        return visualStudioInstance.Solution.FullName;
                    }
                    catch (Exception)
                    {
                    }
                }
                return null;
            }

            public static Process GetAttachedVisualStudio(Process applicationProcess)
            {
                IEnumerable<Process> visualStudios = GetVisualStudioProcesses();

                foreach (Process visualStudio in visualStudios)
                {
                    _DTE visualStudioInstance;
                    if (TryGetVsInstance(visualStudio.Id, out visualStudioInstance))
                    {
                        try
                        {
                            foreach (Process debuggedProcess in visualStudioInstance.Debugger.DebuggedProcesses)
                            {
                                if (debuggedProcess.Id == applicationProcess.Id)
                                {
                                    return debuggedProcess;
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                return null;
            }

            public static void AttachVisualStudioToProcess(Process visualStudioProcess, Process applicationProcess)
            {
                _DTE visualStudioInstance;

                if (TryGetVsInstance(visualStudioProcess.Id, out visualStudioInstance))
                {
                    //Find the process you want the VS instance to attach to...
                    DTEProcess processToAttachTo = visualStudioInstance.Debugger.LocalProcesses.Cast<DTEProcess>().FirstOrDefault(process => process.ProcessID == applicationProcess.Id);

                    //Attach to the process.
                    if (processToAttachTo != null)
                    {
                        processToAttachTo.Attach();

                        ShowWindow((int)visualStudioProcess.MainWindowHandle, 3);
                        SetForegroundWindow(visualStudioProcess.MainWindowHandle);
                    }
                    else
                    {
                        throw new InvalidOperationException("Visual Studio process cannot find specified application '" + applicationProcess.Id + "'");
                    }
                }
            }

            public static Process GetVisualStudioForSolutions(List<string> solutionNames)
            {
                foreach (string solution in solutionNames)
                {
                    Process visualStudioForSolution = GetVisualStudioForSolution(solution);
                    if (visualStudioForSolution != null)
                    {
                        return visualStudioForSolution; ;
                    }
                }
                return null;
            }


            public static Process GetVisualStudioForSolution(string solutionName)
            {
                IEnumerable<Process> visualStudios = GetVisualStudioProcesses();

                foreach (Process visualStudio in visualStudios)
                {
                    _DTE visualStudioInstance;
                    if (TryGetVsInstance(visualStudio.Id, out visualStudioInstance))
                    {
                        try
                        {
                            string actualSolutionName = Path.GetFileName(visualStudioInstance.Solution.FullName);

                            if (string.Compare(actualSolutionName, solutionName, StringComparison.InvariantCultureIgnoreCase) == 0)
                            {
                                return visualStudio;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                return null;
            }

            #endregion

            #region Private Methods

            private static IEnumerable<Process> GetVisualStudioProcesses()
            {
                Process[] processes = Process.GetProcesses();
                return processes.Where(o => o.ProcessName.Contains("devenv"));
            }

            private static bool TryGetVsInstance(int processId, out _DTE instance)
            {
                IntPtr numFetched = IntPtr.Zero;
                IRunningObjectTable runningObjectTable;
                IEnumMoniker monikerEnumerator;
                IMoniker[] monikers = new IMoniker[1];

                GetRunningObjectTable(0, out runningObjectTable);
                runningObjectTable.EnumRunning(out monikerEnumerator);
                monikerEnumerator.Reset();

                while (monikerEnumerator.Next(1, monikers, numFetched) == 0)
                {
                    IBindCtx ctx;
                    CreateBindCtx(0, out ctx);

                    string runningObjectName;
                    monikers[0].GetDisplayName(ctx, null, out runningObjectName);

                    object runningObjectVal;
                    runningObjectTable.GetObject(monikers[0], out runningObjectVal);

                    if (runningObjectVal is _DTE && runningObjectName.StartsWith("!VisualStudio"))
                    {
                        int currentProcessId = int.Parse(runningObjectName.Split(':')[1]);

                        if (currentProcessId == processId)
                        {
                            instance = (_DTE)runningObjectVal;
                            return true;
                        }
                    }
                }

                instance = null;
                return false;
            }

            #endregion
        }

        #endregion
    }
}