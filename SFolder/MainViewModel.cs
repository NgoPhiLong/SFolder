using RevisionFileUpdater.Services;
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

        private void Execute()
        {
            var result = _service.Execute(
                SourcePath,
                TargetPath);

            MessageBox.Show(
                $"Completed.\n\n" +
                $"Updated: {result.updated}\n" +
                $"Skipped: {result.skipped}");
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