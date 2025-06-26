using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPlus.UI.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Windows.Data;

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
        public event EventHandler<ElementId> RemoveLink;
        public event EventHandler<ElementId> Push;
        public event EventHandler<ElementId> Pull;
        public event EventHandler<ElementId> Sync;
        public event EventHandler<Tuple<ElementId, DataTable?, DataTable?>?> Merge;

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
            RemoveLink?.Invoke(this, SelectedLink.Schedule.Id);
            AllLinks.Remove(SelectedLink!);
            SelectedLink = null;
        }


        [RelayCommand]
        private void OnPush()
        {
            if (SelectedLink == null) return;
            Push?.Invoke(this, SelectedLink.Schedule.Id);
        }
        [RelayCommand]
        private void OnPull()
        {
            if (SelectedLink == null) return;
            Pull?.Invoke(this, SelectedLink.Schedule.Id);
        }
        [RelayCommand]
        private void OnSync()
        {
            if (SelectedLink == null) return;
            Sync?.Invoke(this, SelectedLink.Schedule.Id);
        }

        public Func<ElementId, Tuple<DataTable?, DataTable?>>? GetComparisonTable { get; set; }
        [RelayCommand]
        private void OnOpen()
        {
            if (SelectedLink == null || GetComparisonTable == null) return;

            var tables = GetComparisonTable.Invoke(SelectedLink.Schedule.Id);
            var rvtTable = tables.Item1;
            var xlsTable = tables.Item2;

            if (rvtTable == null || rvtTable.Rows.Count == 0 || xlsTable == null || xlsTable.Rows.Count == 0)
            {
                Autodesk.Revit.UI.TaskDialog.Show(Base.Resources.Localizations.Messages.Wait, Base.Resources.Localizations.Messages.NoChanges);
                return;
            }
            var viewModel = new DataLinkConflictsVM(rvtTable, xlsTable);
            viewModel.RequestMerge += (s, e) => Merge.Invoke(this, new(SelectedLink.Schedule.Id, e.Item1, e.Item2));
            var viewer = new DataLinkConflictsView(viewModel);
            viewer.ShowDialog();
        }

        partial void OnSearchTextChanged(string value) => Links.Refresh();
    }
}
