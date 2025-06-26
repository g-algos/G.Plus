using System.Collections;

namespace GPlus.UI.ViewsModels
{

    public interface IIdentityVM<T> : IEqualityComparer
    {
        public T Id { get; set; }
        public string Name { get; set; }
    }
    public class IdentityVM : IIdentityVM<ElementId>
    {
        public ElementId Id { get; set; }
        public string Name { get; set; }

        public new bool Equals(object? x, object? y)
        {
            return ((IdentityVM)x).Id == ((IdentityVM)y).Id;
        }


        public int GetHashCode(object obj)
        {
            return ((IdentityVM)obj).Id.GetHashCode();
        }
    }

    public class ParameterIdentityVM : IdentityVM
    {
        public StorageType StorageType { get; set; }
       
    }

    public class IdentityGuidVM : IIdentityVM<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public new bool Equals(object? x, object? y)
        {
            return ((IdentityGuidVM)x).Id == ((IdentityGuidVM)y).Id;
        }


        public int GetHashCode(object obj)
        {
            return ((IdentityGuidVM)obj).Id.GetHashCode();
        }
    }

    public class IdentityForgeVM : IIdentityVM<ForgeTypeId>
    {
        public ForgeTypeId Id { get; set; }
        public string Name { get; set; }

        public new bool Equals(object? x, object? y)
        {
            return ((IdentityForgeVM)x).Name == ((IdentityForgeVM)y).Name;
        }
        public int GetHashCode(object obj)
        {
            return ((IdentityForgeVM)obj).Name.GetHashCode();
        }
    }


}
