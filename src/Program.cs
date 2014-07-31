using System;
using System.Diagnostics;
using System.Linq;
using VsDebugger.AppController;

namespace VsDebugger
{
    class Program
    {
        public static void Exit(string message, params object[] args)
        {
            Console.WriteLine(message, args);
            Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            var solutionName = args[0];
            if (string.IsNullOrEmpty(solutionName))
            {
                Exit("Solution name must me provided as a first argument");
            }

            var vsProcess = VisualStudioAttacher.GetVisualStudioForSolution(solutionName);
            if (vsProcess == null)
            {
                Exit("Cannot find devenv process with '{0}'", solutionName);
            }

            var processName = args[1];
            if (string.IsNullOrEmpty(processName))
            {
                Exit("Target process name must me provided as a second argument");
            }
            var appProcessList = Process.GetProcessesByName(processName);
            if (appProcessList.Length == 0)
            {
                Exit("No w3p processes are running");
            }
            else
            {
                foreach (var appProsess in appProcessList)
                {
                    VisualStudioAttacher.AttachVisualStudioToProcess(vsProcess, appProsess);
                }
            }
            Console.WriteLine("devenv({0}) debugger is attached to {1}({2})", vsProcess.Id, processName, String.Join(",", appProcessList.Select(p => p.Id)));
        }
    }
}
