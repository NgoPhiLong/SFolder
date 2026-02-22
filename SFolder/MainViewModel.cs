#region Namespaces
using System.IO;
using System.Windows;

using Newtonsoft.Json;

// Alias tránh trùng với WinForms
using Win32SaveFileDialog = Microsoft.Win32.SaveFileDialog;

using Path = System.IO.Path;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using iText.Kernel.Pdf;
using Microsoft.Win32;
using System.Xml;
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
    }

}
