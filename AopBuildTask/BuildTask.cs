using AopInterceptionTaskLib;
using System.Diagnostics;

namespace AopBuildTask
{
    public class BuildTask: Microsoft.Build.Utilities.Task
    {
        [Microsoft.Build.Framework.Required]
        public string AssemblyFile
        {
            get;
            set;
        }

        //[Microsoft.Build.Framework.Required]
        public string TaskFile
        {
            get;
            set;
        }

        public override bool Execute()
        {
            if(string.IsNullOrWhiteSpace(AssemblyFile))
            {
                Log.LogError("Build task failed, argument AssemblyFile is required.");
                return false;
            }

            Log.LogMessage($"Injecting {AssemblyFile}.");

            // Decide the injectiont way.
            // If TaskFile is empty or null, uses the AopInterceptionTask class directly.
            // Or, use your cmd tool specified by TaskFile.
            if (string.IsNullOrWhiteSpace(TaskFile))
            {
                IAopInterceptionTask interceptionTask = new AopInterceptionTask(AssemblyFile);
                interceptionTask.Run();
            }
            else
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    p.StandardInput.AutoFlush = true;
                    p.StandardInput.WriteLine(TaskFile + " " + AssemblyFile + "\r\n");
                    p.StandardInput.WriteLine("exit");
                    var output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    Log.LogMessage(output);
                }
            }

            Log.LogMessage($"Injecting {AssemblyFile} is finished.");
            return true;
        }
    }
}
