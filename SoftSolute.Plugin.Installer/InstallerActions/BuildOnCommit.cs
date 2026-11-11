//using System.Collections;
//using System.ComponentModel;
//using System.Configuration.Install;
//using System.Diagnostics;
//using System.IO;

//namespace SoftSolute.Plugin.Common.InstallerActions;

//[RunInstaller(true)]
//public class BuildOnCommit : Installer
//{
//    public override void Commit(IDictionary savedState)
//    {
//        base.Commit(savedState);

//        // The installer console will pass TARGETDIR in CustomActionData:
//        string targetDir = Context.Parameters["targetdir"];

//        // Builds the folder where your build.bat is located
//        string workDir = Path.Combine(targetDir, "Plugin Installation", "deployment");

//        var psi = new ProcessStartInfo("cmd.exe", arguments: "/c build.bat -debug")
//        {
//            WorkingDirectory = workDir,
//            UseShellExecute = false,
//            CreateNoWindow = true
//        };

//        using (var proc = Process.Start(psi))
//        {
//            proc.WaitForExit();
//            if (proc.ExitCode != 0)
//                throw new InstallException($"build.bat exited with code {proc.ExitCode}");
//        }
//    }
//}
