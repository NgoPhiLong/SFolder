using iText.Kernel.Pdf.Action;
using RevisionFileUpdater.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.Forms.MessageBox;
using WinForms = System.Windows.Forms;

namespace RevisionFileUpdater.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly RevisionService _service =
            new RevisionService();

        private ObservableCollection<string> _multiTargetPaths
    = new ObservableCollection<string>();

        public ObservableCollection<string> MultiTargetPaths
        {
            get => _multiTargetPaths;
            set
            {
                _multiTargetPaths = value;
                OnPropertyChanged();
            }
        }
        private string _selectedMultiTarget;

        public string SelectedMultiTarget
        {
            get => _selectedMultiTarget;
            set
            {
                _selectedMultiTarget = value;
                OnPropertyChanged();
            }
        }
        private bool _isMultiTargetEnabled;

        public bool IsMultiTargetEnabled
        {
            get => _isMultiTargetEnabled;
            set
            {
                _isMultiTargetEnabled = value;
                OnPropertyChanged();
            }
        }

        private string _sourcePath;
        public string SourcePath
        {
            get => _sourcePath;
            set
            {
                _sourcePath = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _targetPath;
        public string TargetPath
        {
            get => _targetPath;
            set
            {
                _targetPath = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand CheckPdfDwgCommand { get; }
        public ICommand AddTargetCommand { get; }
        public ICommand RemoveSelectedTargetCommand { get; }
        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseTargetCommand { get; }
        public ICommand ExecuteCommand { get; }

        public MainViewModel()
        {
            BrowseSourceCommand =
                new RelayCommand(BrowseSource);

            BrowseTargetCommand =
                new RelayCommand(BrowseTarget);

            ExecuteCommand =
                new RelayCommand(Execute, CanExecute);

            AddTargetCommand = new RelayCommand(AddTarget);
            RemoveSelectedTargetCommand = new RelayCommand(RemoveSelectedTarget);
            CheckPdfDwgCommand = new RelayCommand(CheckPdfDwg);
        }

        private void BrowseSource()
        {
            BrowseFolder(true);
        }

        private void BrowseTarget()
        {
            BrowseFolder(false);
        }

        private void BrowseFolder(bool isSource)
        {
            using (var dialog =
                new WinForms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog()
                    == WinForms.DialogResult.OK)
                {
                    if (isSource)
                        SourcePath = dialog.SelectedPath;
                    else
                        TargetPath = dialog.SelectedPath;
                }
            }
        }

        private bool CanExecute()
        {
            return Directory.Exists(SourcePath) &&
                   Directory.Exists(TargetPath);
        }

        //private void Execute()
        //{
        //    var result = _service.Execute(
        //        SourcePath,
        //        TargetPath);

        //    MessageBox.Show(
        //        $"Completed.\n\n" +
        //        $"Updated: {result.updated}\n" +
        //        $"Skipped: {result.skipped}");
        //}
        private void Execute()
        {
            if (string.IsNullOrEmpty(SourcePath))
            {
                MessageBox.Show("Please select source folder");
                return;
            }

            if (MultiTargetPaths.Count > 0)
            {
                foreach (var target in MultiTargetPaths)
                {
                    _service.Execute(SourcePath, target);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(TargetPath))
                {
                    MessageBox.Show("Please select target folder");
                    return;
                }

                _service.Execute(SourcePath, TargetPath);
            }

            MessageBox.Show("Done!");
        }
        private void AddTarget()
        {
            if (string.IsNullOrWhiteSpace(TargetPath))
            {
                MessageBox.Show("TargetPath is empty");
                return;
            }

            if (!Directory.Exists(TargetPath))
            {
                MessageBox.Show("TargetPath does not exist");
                return;
            }

            if (MultiTargetPaths.Contains(TargetPath))
            {
                MessageBox.Show("Target already added");
                return;
            }

            MultiTargetPaths.Add(TargetPath);

            IsMultiTargetEnabled = MultiTargetPaths.Count > 0;
        }

        private void RemoveSelectedTarget()
        {
            if (SelectedMultiTarget == null)
                return;

            MultiTargetPaths.Remove(SelectedMultiTarget);

            IsMultiTargetEnabled = MultiTargetPaths.Count > 0;
        }
        private void CheckPdfDwg()
        {
            if (string.IsNullOrWhiteSpace(SourcePath) ||
                string.IsNullOrWhiteSpace(TargetPath))
            {
                MessageBox.Show("Please select Source and Target folders.");
                return;
            }

            if (!Directory.Exists(SourcePath) ||
                !Directory.Exists(TargetPath))
            {
                MessageBox.Show("Folder does not exist.");
                return;
            }

            var sourceFiles = Directory.GetFiles(SourcePath);
            var targetFiles = Directory.GetFiles(TargetPath);

            // Lấy danh sách name không extension
            var sourceNames = sourceFiles
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var targetNames = targetFiles
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingList = new List<string>();

            // Tìm file có ở source nhưng không có ở target
            foreach (var name in sourceNames)
            {
                if (!targetNames.Contains(name))
                {
                    missingList.Add($"{name} (Missing in Target)");
                }
            }

            // Tìm file có ở target nhưng không có ở source
            foreach (var name in targetNames)
            {
                if (!sourceNames.Contains(name))
                {
                    missingList.Add($"{name} (Missing in Source)");
                }
            }

            if (missingList.Count == 0)
            {
                MessageBox.Show("All PDF/DWG files matched.");
            }
            else
            {
                string message = string.Join(Environment.NewLine, missingList);
                MessageBox.Show(
                    $"Mismatch files:\n\n{message}",
                    "PDF/DWG Check Result",
                    (MessageBoxButtons)MessageBoxButton.OK,
                    (MessageBoxIcon)MessageBoxImage.Warning);
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
        }
    }
}