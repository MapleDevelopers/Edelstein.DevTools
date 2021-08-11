using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Utils.FileSystem;
using Utils.Process;

namespace Edelstein.DevTools
{
    public static class Tools
    {
        private static string FindDotnetPath()
        {
            var fileName = "dotnet";
            var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (!string.IsNullOrEmpty(dotnetRoot))
                return Path.Combine(dotnetRoot, fileName);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const string defaultWinPath = @"C:\Program Files\dotnet\";
                fileName += ".exe";

                if (File.Exists(defaultWinPath + fileName))
                    return defaultWinPath + fileName;
            }

            var mainModule = Process.GetCurrentProcess().MainModule;
            if (mainModule == null) return string.Empty;
            if (string.IsNullOrEmpty(mainModule.FileName)) return string.Empty;

            var mainModuleFileName = Path.GetFileName(mainModule.FileName);
            if (mainModuleFileName == null) return string.Empty;

            return mainModuleFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)
                ? mainModule.FileName : string.Empty;
        }

        public static void PackageNxCopyToServer()
        {
            var dotNetPath = FindDotnetPath();

            const string pathToInput = "src/";
            const string pathToOutput = "bin/Server.nx";
            const string args = CmdArgs.Dotnet.RunProject +
                                " Scrapyard/Scrapyard.CLI " +
                                pathToInput +
                                " " +
                                pathToOutput +
                                CmdArgs.Dotnet.DelimitArgs +
                                CmdArgs.Dotnet.VerbosityMinimal;

            var pathToRepos = Environment.ExpandEnvironmentVariables("%USERPROFILE%") + @"\source\repos\";
            var pathToServerNX = pathToRepos + "Server.NX";

            var launcher = new Launcher(dotNetPath, args, pathToServerNX);
            launcher.Launch();

            while (!launcher.Finished && !launcher.NormalOutput.Contains("Finished packaging"))
            {
                //let it work
            }

            if (launcher.ErrorOutput.Any())
                throw new Exception("Errors occurred during packaging of NX files!");

            var pathToEdelstein = pathToRepos + @"EdelsteinBia\";
            var pathToServerData = pathToEdelstein + @"src\app\Edelstein.App.Standalone\bin\Debug\net5.0\data";
            var pathToPackagedNx = pathToServerNX + "\\" + pathToOutput;
            var pathToDestFile = pathToServerData + "\\Server.nx";

            try
            {
                if (!File.Exists(pathToPackagedNx))
                    throw new Exception($"Packaged NX file not found! fullPath: {pathToPackagedNx}");

                var file = new FileInfo(pathToPackagedNx);
                var destFile = new FileInfo(pathToDestFile);
                destFile.ExistsDelete();
                file.CopyTo(destFile.FullName);
            }
            catch (Exception ex)
            {
                throw new Exception("Errors occurred during file move from output to server data!", ex);
            }
        }
    }
}
