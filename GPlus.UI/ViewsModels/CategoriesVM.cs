using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace GPlus.UI.ViewsModels
{
    public partial class CategoriesVM: ObservableObject
    {
        public CategoriesVM(List<IdentityVM> allCategories)
        {
            AllCategories = new ObservableCollection<IdentityVM>(allCategories);
            SelectedCategory = AllCategories.FirstOrDefault();
        }

        public ObservableCollection<IdentityVM> AllCategories { get; set; }
        private IdentityVM? _selectedCategory;
        public IdentityVM? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    OnSelectedCategoryChanged(value);
                }
            }
        }
        public void OnSelectedCategoryChanged(IdentityVM? value)
        {
            if(SelectedCategory == null)
            {
                //do somethig
            }

        }
    }
}
