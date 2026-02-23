#region Namespaces
using iText.Kernel.Pdf;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.TextFormatting;
using System.Xml;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
// Alias tránh trùng với WinForms
using Win32SaveFileDialog = Microsoft.Win32.SaveFileDialog;
#endregion

namespace NPL_Tools
{
    /// <summary>
    /// ViewModel xử lý check exception elements
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _pdfPath;
        public string PdfPath
        {
            get => _pdfPath;
            set { _pdfPath = value; OnPropertyChanged(); }
        }

        public ICommand BrowseCommand { get; }
        public ICommand ExtractCommand { get; }

        public MainViewModel()
        {
            BrowseCommand = new RelayCommand(BrowsePdf);
            ExtractCommand = new RelayCommand(ExtractAnnotations, CanExtract);
        }

        private void BrowsePdf()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                Title = "Select PDF File"
            };

            if (dialog.ShowDialog() == true)
            {
                PdfPath = dialog.FileName;
            }
        }

        private bool CanExtract() => !string.IsNullOrWhiteSpace(PdfPath);

        private void ExtractAnnotations()
        {
            PdfAnnotationExtractor.ExtractToJson(PdfPath);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class PdfAnnotationExtractor
    {
        public static List<object> ExtractAnnotations(string filePath)
        {
            var result = new List<object>();

            using (var reader = new PdfReader(filePath))
            using (var pdfDoc = new PdfDocument(reader))
            {
                int numberOfPages = pdfDoc.GetNumberOfPages();

                for (int i = 1; i <= numberOfPages; i++)
                {
                    var page = pdfDoc.GetPage(i);
                    var annotations = page.GetAnnotations();

                    foreach (var annot in annotations)
                    {
                        var subject = annot.GetPdfObject().GetAsString(PdfName.Subj)?.ToString();
                        var text = annot.GetContents()?.ToString();
                        var rect = annot.GetRectangle().ToString();
                        var callout = annot.GetPdfObject().GetAsArray(new PdfName("CalloutLine"));

                        result.Add(new
                        {
                            Page = i,
                            Subject = subject,
                            Text = text,
                            Rect = rect,
                            CalloutLine = callout?.ToString()
                        });
                    }
                }
            }
            return result;
        }

        public static void ExportToJson(List<object> annotations, string jsonPath)
        {
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(annotations, Newtonsoft.Json.Formatting.Indented));
        }

        public static void ExtractToJson(string pdfPath)
        {
            var annotations = ExtractAnnotations(pdfPath);

            var dialog = new Win32SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Title = "Save JSON File",
                FileName = Path.GetFileNameWithoutExtension(pdfPath) + ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                ExportToJson(annotations, dialog.FileName);
                MessageBox.Show("Export thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSource_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtSource.Text = fbd.SelectedPath;
            }
        }

        private void BtnTarget_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtTarget.Text = fbd.SelectedPath;
            }
        }

        private void BtnExecute_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(txtSource.Text) || !Directory.Exists(txtTarget.Text))
            {
                MessageBox.Show("Invalid folder path.");
                return;
            }

            var sourceFiles = Directory.GetFiles(txtSource.Text)
                .Where(f =>
                    (f.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase) ||
                     f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) &&
                    HasRevision(Path.GetFileName(f)))
                .ToArray();

            var targetFiles = Directory.GetFiles(txtTarget.Text)
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
                        Path.GetExtension(f).Equals(extension, StringComparison.OrdinalIgnoreCase));

                // Không tồn tại base trong Target → bỏ qua
                if (targetMatch == null)
                {
                    skipped++;
                    continue;
                }

                int targetRev = ExtractRevision(Path.GetFileName(targetMatch));

                try
                {
                    if (sourceRev > targetRev)
                    {
                        // Source lớn hơn → thay thế Target
                        File.Delete(targetMatch);

                        string newPath = Path.Combine(txtTarget.Text, fileName);
                        File.Move(src, newPath);

                        updated++;
                    }
                    else
                    {
                        // Source nhỏ hơn hoặc bằng → giữ Target, xóa Source
                        File.Delete(src);
                        skipped++;
                    }
                }
                catch
                {
                    skipped++;
                }

            }

            MessageBox.Show(
                $"Completed.\n\nUpdated: {updated}\nSkipped: {skipped}");
        }

        private bool HasRevision(string fileName)
        {
            string name = Path.GetFileNameWithoutExtension(fileName);
            return Regex.IsMatch(name, @"-REV[-_]\d+$", RegexOptions.IgnoreCase);
        }

        private string RemoveRevision(string fileName)
        {
            string name = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);

            string cleaned = Regex.Replace(name, @"-REV[-_]\d+$", "", RegexOptions.IgnoreCase);

            return cleaned + ext;
        }

        private int ExtractRevision(string fileName)
        {
            string name = Path.GetFileNameWithoutExtension(fileName);

            var match = Regex.Match(name, @"-REV[-_](\d+)$", RegexOptions.IgnoreCase);

            if (match.Success)
                return int.Parse(match.Groups[1].Value);

            return -1;
        }
    }

}
