using System.Windows.Media;

namespace GPlus.UI.ViewsModels
{
    public class FillPatternImageVM
    {

        public FillPatternImageVM(ElementId id, string name, ImageSource image)
        {
            Id = id;
            Image = image;
            Name = name;
        }
        public ElementId Id { get; private set; }
        public ImageSource Image { get; private set; }

        public string Name { get; private set; }
    }
}
