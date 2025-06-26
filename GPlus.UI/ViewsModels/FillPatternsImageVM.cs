using System.Collections.ObjectModel;

namespace GPlus.UI.ViewsModels
{
    public class FillPatternsImageVM
    {
        private ReadOnlyCollection<FillPatternImageVM> _fillPatterns;

        public FillPatternsImageVM(List<FillPatternImageVM> fillPatterns)
        {
            _fillPatterns = new ReadOnlyCollection<FillPatternImageVM>(fillPatterns);
        }

        public ReadOnlyCollection<FillPatternImageVM> FillPatterns
        {
            get { return _fillPatterns; }
        }
    }
}
