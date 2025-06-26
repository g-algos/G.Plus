using Autodesk.Revit.Attributes;

namespace GPlus.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class PayMeCoffeeCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements
        )
        {
            PayMeACoffeView view = new PayMeACoffeView();
            view.Show();
            return Result.Succeeded;
        }
    }
}
