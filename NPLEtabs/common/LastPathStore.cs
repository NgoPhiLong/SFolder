using System;
using System.IO;
using System.Text;
using Path = System.IO.Path;

static class LastPathStore
{
    private static readonly string SettingsFile =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "SBIMApp", "lastjson.txt");

    public static string Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
                return File.ReadAllText(SettingsFile).Trim();
        }
        catch { }
        return string.Empty;
    }

    public static void Save(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(SettingsFile, path ?? string.Empty, new UTF8Encoding(false));
        }
        catch { }
    }

    public static string GetInitialDirectory(string lastPath)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(lastPath))
            {
                var dir = Path.GetDirectoryName(lastPath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    return dir;
            }
        }
        catch { }
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

}