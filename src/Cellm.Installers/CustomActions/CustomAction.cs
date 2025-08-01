using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomActions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult GetOffice12NextOpen(Session session)
        {
            try
            {
                session.Log($"Begin {nameof(GetOffice12NextOpen)}");

                var office12ExcelOptions = "Software\\Microsoft\\Office\\12.0\\Excel\\Options";
                session.Log($"Office12ExcelOptions: {office12ExcelOptions}");

                session["Office12NextOpen"] = GetNextOpen(office12ExcelOptions);
                session.Log($"Office12NextOpen: {session["Office12NextOpen"]}");

                session.Log($"End {nameof(GetOffice12NextOpen)}");
            }
            catch (Exception ex)
            {
                session.Log($"Error in GetOffice12NextOpen: {ex.Message}");
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult GetOffice14NextOpen(Session session)
        {
            try
            {
                session.Log($"Begin {nameof(GetOffice14NextOpen)}");

                var office14ExcelOptions = "Software\\Microsoft\\Office\\14.0\\Excel\\Options";
                session.Log($"Office14ExcelOptions: {office14ExcelOptions}");

                session["Office14NextOpen"] = GetNextOpen(office14ExcelOptions);
                session.Log($"Office14NextOpen: {session["Office14NextOpen"]}");

                session.Log($"End {nameof(GetOffice14NextOpen)}");
            }
            catch (Exception ex)
            {
                session.Log($"Error in GetOffice14NextOpen: {ex.Message}");
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult GetOffice15NextOpen(Session session)
        {
            try
            {
                session.Log($"Begin {nameof(GetOffice15NextOpen)}");

                var office15ExcelOptions = "Software\\Microsoft\\Office\\15.0\\Excel\\Options";
                session.Log($"Office15ExcelOptions: {office15ExcelOptions}");

                session["Office15NextOpen"] = GetNextOpen(office15ExcelOptions);
                session.Log($"Office15NextOpen: {session["Office15NextOpen"]}");

                session.Log($"End {nameof(GetOffice15NextOpen)}");
            }
            catch (Exception ex)
            {
                session.Log($"Error in GetOffice15NextOpen: {ex.Message}");
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult GetOffice16NextOpen(Session session)
        {
            try
            {
                session.Log($"Begin {nameof(GetOffice16NextOpen)}");

                var office16ExcelOptions = "Software\\Microsoft\\Office\\16.0\\Excel\\Options";
                session.Log($"Office16ExcelOptions: {office16ExcelOptions}");

                session["Office16NextOpen"] = GetNextOpen(office16ExcelOptions);
                session.Log($"Office16NextOpen: {session["Office16NextOpen"]}");

                session.Log($"End {nameof(GetOffice16NextOpen)}");
            }
            catch (Exception ex)
            {
                session.Log($"Error in GetOffice16NextOpen: {ex.Message}");
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        private static string GetNextOpen(string optionsPath)
        {
            var optionsKeys = Registry.CurrentUser.OpenSubKey(optionsPath, false) ?? throw new Exception($"Registry key not found: {optionsPath}");

            var excelOptionsKeys = optionsKeys.GetValueNames() ?? Array.Empty<string>();

            // Find the highest number used in keys like "OPEN", "OPEN1", "OPEN2", etc.
            var maxOpenNumber = excelOptionsKeys
                .Where(key => key.StartsWith("OPEN"))
                .Select(key =>
                {
                    switch (key)
                    {
                        case "OPEN":
                            return 0;
                        case string s when s.StartsWith("OPEN") && int.TryParse(s.Substring(4), out var n):
                            return n;
                        default:
                            return -1;
                    }
                })
                .DefaultIfEmpty(-1)
                .Max();

            if (maxOpenNumber == -1)
            {
                // If no "OPEN" keys found, return "OPEN"
                return "OPEN";
            }

            // Find first unused slot in sequence (if any) 
            for (var i = 1; i <= maxOpenNumber; i++)
            {
                var openKey = $"OPEN{i}";
                if (!excelOptionsKeys.Contains(openKey))
                {
                    return openKey;
                }
            }

            // Get next incremented key
            return $"OPEN{maxOpenNumber + 1}";
        }

        [CustomAction]
        public static ActionResult InstallNodeJs(Session session)
        {
            session.Log("Begin InstallNodeJs (deferred)");

            var temporaryDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                // For deferred actions, read from CustomActionData
                var nodeParentDir = session.CustomActionData["NODE_PARENT_DIR"];
                var version = session.CustomActionData["NODEJS_VERSION"];

                if (string.IsNullOrWhiteSpace(nodeParentDir) || string.IsNullOrWhiteSpace(version))
                {
                    session.Log("ERROR: Required properties from CustomActionData are not set.");
                    return ActionResult.Failure;
                }

                if (!Directory.Exists(nodeParentDir))
                {
                    session.Log($"ERROR: Node parent directory {nodeParentDir} does not exist. This directory should be created by the installer first.");
                    return ActionResult.Failure;
                }

                var nodeDist = $"node-v{version}-win-x64";
                var nodeFileName = $"{nodeDist}.zip";
                var downloadUrl = $"https://nodejs.org/dist/v{version}/{nodeFileName}";
                var downloadPath = Path.Combine(temporaryDir, nodeFileName);
                var extractPath = Path.Combine(temporaryDir, "extract");
                var nodeDir = Path.Combine(nodeParentDir, "node");

                session.Log($"Node.js Version: {version}-x64");
                session.Log($"Download URL: {downloadUrl}");
                session.Log($"Temporary Download Path: {downloadPath}");
                session.Log($"Extract Directory: {extractPath}");
                session.Log($"Node parent directory: {nodeParentDir}");
                session.Log($"Node directory: {nodeDir}");

                // Download
                session.Log("Downloading Node.js ...");

                Directory.CreateDirectory(temporaryDir);

                using (var client = new HttpClient())
                {
                    var response = client.GetAsync(downloadUrl).Result;
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        response.Content.CopyToAsync(fs).Wait();
                    }
                }

                session.Log("Downloading Node.js ... Done");

                // Extract
                session.Log($"Extracting '{downloadPath}' to '{extractPath}' ...");
                ZipFile.ExtractToDirectory(downloadPath, extractPath);
                session.Log($"Extracting '{downloadPath}' to '{extractPath}' ... Done");

                // Copy extracted directory
                var extractedNodeDir = Path.Combine(extractPath, nodeDist);
                session.Log($"Copying {extractedNodeDir} to {nodeDir} ...");

                FileSystem.CopyDirectory(extractedNodeDir, nodeDir, true);

                session.Log($"Copying {extractedNodeDir} to {nodeDir} ... Done");

                // Verify
                var nodeExePath = Path.Combine(nodeParentDir, "node", "node.exe");
                if (!File.Exists(nodeExePath))
                {
                    session.Log($"ERROR: node.exe not found at '{nodeExePath}'.");
                    return ActionResult.Failure;
                }

                session.Log("Found node.exe");

                var npmCmdPath = Path.Combine(nodeParentDir, "node", "npm.cmd");
                if (!File.Exists(nodeExePath))
                {
                    session.Log($"ERROR: npm.cmd not found at '{npmCmdPath}'.");
                    return ActionResult.Failure;
                }

                session.Log("Found npm.cmd");

                var npxCmdPath = Path.Combine(nodeParentDir, "node", "npx.cmd");
                if (!File.Exists(nodeExePath))
                {
                    session.Log($"ERROR: npx.cmd not found at '{npxCmdPath}'.");
                    return ActionResult.Failure;
                }

                session.Log("Found npx.cmd");

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log($"FATAL ERROR in InstallNodeJs: {ex}");
                return ActionResult.Failure;
            }
            finally
            {
                if (Directory.Exists(temporaryDir))
                {
                    try
                    {
                        Directory.Delete(temporaryDir, true);
                        session.Log($"Cleaned up temporary directory: {temporaryDir}");
                    }
                    catch (Exception ex)
                    {
                        session.Log($"Warning: Failed to cleanup temporary directory {temporaryDir}: {ex}");
                    }
                }

                session.Log("End InstallNodeJs Custom Action (deferred)");
            }
        }

        [CustomAction]
        public static ActionResult InstallPlaywright(Session session)
        {
            session.Log("Begin InstallPlaywright Custom Action (deferred)");
            try
            {
                // For deferred actions, read from CustomActionData
                var nodeParentDir = session.CustomActionData["NODE_PARENT_DIR"];

                // The rest of the function remains the same...
                if (string.IsNullOrWhiteSpace(nodeParentDir))
                {
                    session.Log("ERROR: Node directory from CustomActionData is not set.");
                    return ActionResult.Failure;
                }

                var npxPath = Path.Combine(nodeParentDir, "node", "npx.cmd");

                if (!File.Exists(npxPath))
                {
                    session.Log($"ERROR: npx.cmd not found at {npxPath}. Ensure Node.js was installed correctly.");
                    return ActionResult.Failure;
                }

                var arguments = "-y playwright install --with-deps --only-shell chrome";

                session.Log($"Node.js Path: {nodeParentDir}");
                session.Log($"Executing: {npxPath} {arguments}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = npxPath,
                    Arguments = arguments,
                    WorkingDirectory = nodeParentDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Environment = { ["PATH"] = $"{nodeParentDir};{Environment.GetEnvironmentVariable("PATH")}" }
                };

                using (var process = Process.Start(startInfo))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    session.Log("Playwright standard output");
                    session.Log(output);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        session.Log("Playwright standard error");
                        session.Log(error);
                    }

                    if (process.ExitCode != 0)
                    {
                        session.Log($"ERROR: Playwright installation failed with exit code {process.ExitCode}.");
                        return ActionResult.Failure;
                    }
                }

                session.Log("Playwright installation completed successfully.");
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log($"FATAL ERROR in InstallPlaywright: {ex}");
                return ActionResult.Failure;
            }
            finally
            {
                session.Log("End InstallPlaywright Custom Action (deferred)");
            }
        }
    }
}
