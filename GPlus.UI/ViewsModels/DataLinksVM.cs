using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPlus.UI.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Data;
using MessageBox = System.Windows.MessageBox;

namespace GPlus.UI.ViewsModels
{
    public partial class DataLinksVM: ObservableObject
    {
        public ObservableCollection<ScheduleLinkVM> AllLinks { get; set; }
        private ObservableCollection<IdentityVM> _allSchedules { get; set; }

        [ObservableProperty] private ICollectionView _links;
        [ObservableProperty] private ScheduleLinkVM? _selectedLink;
        [ObservableProperty] private string _searchText = string.Empty;

        [ObservableProperty] private ICollectionView _notConnectedSchedules;

        [ObservableProperty] private bool _hasLinksToAdd;


        public Func<NewScheduleLinkVM, bool>? AddLinkCallback;
        public Func<ElementId, (bool, string)> RemoveLink;
        public Func<ElementId, (bool, string)> Push;
        public Func<ElementId, (bool, string)> Pull;
        public Func<ElementId, (bool, string)> Sync;
        public Func<Tuple<ElementId, DataTable?, DataTable?>?, (bool, string)> Merge;

        [ObservableProperty]
        private ScheduleLinkVM? _linkToScroll;
        public DataLinksVM(List<ScheduleLinkVM> allLinks, ScheduleLinkVM? selectedLink, List<IdentityVM> schedules)
        {
            AllLinks = new ObservableCollection<ScheduleLinkVM>(allLinks);

            Links = CollectionViewSource.GetDefaultView(AllLinks);
            Links.SortDescriptions.Add(new SortDescription("Schedule.Name", ListSortDirection.Ascending));
            Links.Filter = item =>
            {
                if (item is not ScheduleLinkVM link) return false;
                if (string.IsNullOrWhiteSpace(SearchText)) return true;
                return link.Schedule.Name.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase);
            };
            AllLinks.CollectionChanged += (s, e) =>
            {
                Links.Refresh();
                NotConnectedSchedules.Refresh();
                HasLinksToAdd = _allSchedules.FirstOrDefault(e=> !AllLinks.Any(l=> l.Schedule.Id == e.Id)) != null;
            };
            SelectedLink = selectedLink;

            _allSchedules = new ObservableCollection<IdentityVM>(schedules);
            HasLinksToAdd = _allSchedules.FirstOrDefault(e => !AllLinks.Any(l => l.Schedule.Id == e.Id)) != null;
            NotConnectedSchedules = CollectionViewSource.GetDefaultView(_allSchedules);
            NotConnectedSchedules.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            NotConnectedSchedules.Filter = item =>
            {
                if (item is not IdentityVM schedule) return false;
                if (!AllLinks.Any()) return true;
                return !AllLinks.Any(e=> e.Schedule.Id == schedule.Id);
            };
        }

        [RelayCommand] private void OnAddLink()
        {
            var notConnected = NotConnectedSchedules.Cast<IdentityVM>().ToList();
            var viewModel = new NewScheduleLinkVM(notConnected);
            var window = new NewScheduleLinkView(viewModel);
            var result = window.ShowDialog();
            if (result == true)
            {
                bool success = AddLinkCallback?.Invoke(viewModel) ?? false;
                if(!success) return;
                ScheduleLinkVM newLink = new ScheduleLinkVM()
                {
                    LastSync = DateTime.UtcNow,
                    LinkType = viewModel.IsInstance ? Base.Resources.Localizations.Content.Instance : Base.Resources.Localizations.Content.Type,
                    Path = viewModel.Path,
                    PathType = viewModel.IsAbsolute ? Base.Resources.Localizations.Content.Absolute : Base.Resources.Localizations.Content.Relative,
                    Schedule = viewModel.Schedule,
                    Status = true
                };
                AllLinks.Add(newLink);
                SelectedLink = newLink;
                LinkToScroll = newLink;
            }
        }

        [RelayCommand] private void OnRemoveLink()
        {
            if (SelectedLink == null) return;
            (bool success, string message) result =  RemoveLink?.Invoke(SelectedLink.Schedule.Id) ?? (false, "");
            if (!result.success)
                MessageBox.Show(
                    result.message,
                    Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            else
            {
                AllLinks.Remove(SelectedLink!);
                SelectedLink = null;
            }
        }


        [RelayCommand]
        private void OnPush()
        {
            if (SelectedLink == null) return;
            (bool success , string message) result = Push?.Invoke(SelectedLink.Schedule.Id)??(false,"");
            if(!result.success)
                MessageBox.Show(
                    result.message,
                    Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
        }
        [RelayCommand]
        private void OnPull()
        {
            if (SelectedLink == null) return;
            (bool success, string message) result = Pull?.Invoke(SelectedLink.Schedule.Id) ?? (false, "");
            if (!result.success)
                MessageBox.Show(
                    result.message,
                    Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
        }
        [RelayCommand]
        private void OnSync()
        {
            if (SelectedLink == null) return;
            (bool success, string message) result = Sync?.Invoke(SelectedLink.Schedule.Id) ?? (false, "");
            if (!result.success)
                MessageBox.Show(
                    result.message,
                    Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
        }

        public Func<ElementId, (DataTable?, DataTable?, bool, string)>? GetComparisonTable { get; set; }
        [RelayCommand]
        private void OnOpen()
        {
            if (SelectedLink == null || GetComparisonTable == null) return;


            (DataTable? rvtTable, DataTable? xlsTable, bool sucess, string message) result = GetComparisonTable.Invoke(SelectedLink.Schedule.Id);
           
            if(result.sucess == false)
            {
                MessageBox.Show(
                    result.message,
                    Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }
            if (result.rvtTable == null || result. rvtTable.Rows.Count == 0 || result.xlsTable == null || result.xlsTable.Rows.Count == 0)
            {
                Autodesk.Revit.UI.TaskDialog.Show(Base.Resources.Localizations.Messages.Wait, Base.Resources.Localizations.Messages.NoChanges);
                return;
            }
            var viewModel = new DataLinkConflictsVM(result.rvtTable, result.xlsTable);


            viewModel.RequestMerge += (s, e) =>
            {
                if (Merge != null && e != null && SelectedLink?.Schedule != null)
                {
                    (bool success, string message) result = Merge(new Tuple<ElementId, DataTable?, DataTable?>(
                        SelectedLink.Schedule.Id,
                        e.Item1,
                        e.Item2));

                    if (!result.success)
                        MessageBox.Show(
                            result.message,
                            Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                }
            };
            var viewer = new DataLinkConflictsView(viewModel);
            viewer.ShowDialog();
        }

        partial void OnSearchTextChanged(string value) => Links.Refresh();
    }
}
