using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RevisionFileUpdater.Services
{
    public class RevisionService
    {
        public (int updated, int skipped) Execute(
            string sourcePath,
            string targetPath)
        {
            var sourceFiles = Directory.GetFiles(sourcePath)
                .Where(f =>
                    (f.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase) ||
                     f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) &&
                    HasRevision(Path.GetFileName(f)))
                .ToArray();

            var targetFiles = Directory.GetFiles(targetPath)
                .Where(f =>
                    f.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            int updated = 0;
            int skipped = 0;

            foreach (var src in sourceFiles)
            {
                string fileName = Path.GetFileName(src);
                string baseName = RemoveRevision(fileName);
                string extension = Path.GetExtension(src);
                int sourceRev = ExtractRevision(fileName);

                var targetMatch = targetFiles
                    .FirstOrDefault(f =>
                        RemoveRevision(Path.GetFileName(f)) == baseName &&
                        Path.GetExtension(f)
                        .Equals(extension, StringComparison.OrdinalIgnoreCase));

                if (targetMatch == null)
                {
                    skipped++;
                    continue;
                }

                int targetRev =
                    ExtractRevision(Path.GetFileName(targetMatch));

                try
                {
                    if (sourceRev > targetRev)
                    {
                        File.Delete(targetMatch);
                        File.Move(src,
                            Path.Combine(targetPath, fileName));
                        updated++;
                    }
                    else
                    {
                        File.Delete(src);
                        skipped++;
                    }
                }
                catch
                {
                    skipped++;
                }
            }

            return (updated, skipped);
        }

        private bool HasRevision(string fileName)
        {
            string name =
                Path.GetFileNameWithoutExtension(fileName);

            return Regex.IsMatch(name,
                @"-REV[-_]\d+$",
                RegexOptions.IgnoreCase);
        }

        private string RemoveRevision(string fileName)
        {
            string name =
                Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);

            string cleaned =
                Regex.Replace(name,
                    @"-REV[-_]\d+$",
                    "",
                    RegexOptions.IgnoreCase);

            return cleaned + ext;
        }

        private int ExtractRevision(string fileName)
        {
            string name =
                Path.GetFileNameWithoutExtension(fileName);

            var match = Regex.Match(name,
                @"-REV[-_](\d+)$",
                RegexOptions.IgnoreCase);

            return match.Success
                ? int.Parse(match.Groups[1].Value)
                : -1;
        }
    }
}