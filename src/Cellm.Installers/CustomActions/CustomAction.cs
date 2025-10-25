using System;
using System.Linq;
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
        public static ActionResult RemoveOfficeRegistryKeys(Session session)
        {
            try
            {
                session.Log($"Begin {nameof(RemoveOfficeRegistryKeys)}");

                RemoveOfficeKeysContainingPath(session, "12.0", "Cellm-AddIn64-packed.xll");
                RemoveOfficeKeysContainingPath(session, "14.0", "Cellm-AddIn64-packed.xll");
                RemoveOfficeKeysContainingPath(session, "15.0", "Cellm-AddIn64-packed.xll");
                RemoveOfficeKeysContainingPath(session, "16.0", "Cellm-AddIn64-packed.xll");

                session.Log($"End {nameof(RemoveOfficeRegistryKeys)}");
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log($"Error in RemoveOfficeRegistryKeys: {ex.Message}");
                // Don't fail uninstall if cleanup fails
                return ActionResult.Success;
            }
        }

        private static void RemoveOfficeKeysContainingPath(Session session, string version, string xllFileName)
        {
            var optionsPath = $@"Software\Microsoft\Office\{version}\Excel\Options";

            try
            {
                var optionsKey = Registry.CurrentUser.OpenSubKey(optionsPath, true);

                if (optionsKey == null)
                {
                    session.Log($"Registry key not found: {optionsPath}");
                    return;
                }

                var valueNames = optionsKey.GetValueNames();
                foreach (var valueName in valueNames)
                {
                    var value = optionsKey.GetValue(valueName)?.ToString();
                    if (value != null && value.IndexOf(xllFileName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        optionsKey.DeleteValue(valueName, throwOnMissingValue: false);
                        session.Log($"Removed registry value '{valueName}' from Office {version}: {value}");
                    }
                }
            }
            catch (Exception ex)
            {
                session.Log($"Error removing registry keys from Office {version}: {ex.Message}");
            }
        }

    }
}
