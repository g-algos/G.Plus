namespace GPlus.Base.Enums;

/// <summary>
/// It determines the type of action applied to an Element, the order is relevant, in case of a log we will only store the higher transformation.
/// </summary>
public enum ElementAction
{
    None = 0,
    Edited = 1,
    Created = 2,
    Deleted = 3,
}
