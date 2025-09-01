using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPlus.Base.Models;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;

namespace GPlus.UI.ViewsModels
{
    public partial class NewScheduleLinkVM : ObservableObject
    {
        public ObservableCollection<IdentityVM> Schedules { get; set; }
        [ObservableProperty] private IdentityVM _schedule;
        [ObservableProperty] private string _path;
        [ObservableProperty] private bool _isAbsolute;
        [ObservableProperty] private bool _isInstance;

        public NewScheduleLinkVM(List<IdentityVM> schedules)
        {
            Schedules = new ObservableCollection<IdentityVM> (schedules);
            IsAbsolute = true;
            IsInstance = true;
        }
        partial void OnIsAbsoluteChanged(bool oldValue, bool newValue)
        {
            if (String.IsNullOrEmpty(Path)) return;
            if (oldValue == false && newValue == true)
                Path = Base.Helpers.FileHelpers.ResolvePath(Path, ActiveCommandModel.Document);
            else if (oldValue == true && newValue == false)
                Path = Base.Helpers.FileHelpers.ToRelativePath(Path, ActiveCommandModel.Document);
        }

        [RelayCommand]
        private void OnLinking()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                AddExtension = true,
                FileName = $"{Schedule.Name}.xlsx",
                DefaultExt = "xlsx",
                OverwritePrompt = false
            };
            bool? result = dialog.ShowDialog();
            if (result != true) return;

            var validName = Schedule.Name;
            validName = Regex.Replace(validName, @"[^A-Za-z0-9_]", "_"); // excel doesn't accept certain chars
            if (char.IsDigit(validName[0])) validName = "_" + validName;  // excel doesn't accept starting with a digit
            if (validName.Length > 255)
                validName = validName.Substring(0, 255);// excel has a limit of 255 characters for sheet names

            string selectedPath = dialog.FileName;
            if (IsAbsolute)
                Path = $"{selectedPath}|{validName}";
            else
                Path = $"{Base.Helpers.FileHelpers.ToRelativePath(selectedPath, ActiveCommandModel.Document)}|{validName}";
        }

        public event EventHandler<bool?> RequestClose;
        [RelayCommand]
        private void OnSave()
        {
            if (Schedule== null)
            {
                System.Windows.MessageBox.Show(
                            Base.Resources.Localizations.Messages.NoSchedule,
                            Base.Resources.Localizations.Messages.Wait,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                return;
            }
            if (String.IsNullOrEmpty(Path))
            {
                System.Windows.MessageBox.Show(
                    Base.Resources.Localizations.Messages.MissingPath,
                    Base.Resources.Localizations.Messages.Wait,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }


            RequestClose?.Invoke(this, true);
        }

    }
}
