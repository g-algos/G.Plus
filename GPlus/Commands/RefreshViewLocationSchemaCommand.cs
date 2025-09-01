using Autodesk.Revit.Attributes;
using GPlus.Base.Models;
using GPlus.Base.Schemas;
using System.Windows;

namespace GPlus.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class RefreshViewLocationSchemaCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements
        )
        {
            ActiveCommandModel.Set(commandData.Application);
            if (!ViewLocationSchema.TryGetLocalization(ActiveCommandModel.View, out LocalizationModel? localizationModel))
                return Result.Cancelled;
            using (Transaction transaction = new Transaction(ActiveCommandModel.Document, "Manage Localization Schemas"))
            {
                transaction.Start();
                try
                {
                    bool isValid = ActiveCommandModel.View is View3D || ActiveCommandModel.View is ViewPlan || ActiveCommandModel.View is ViewSection || ActiveCommandModel.View is ViewDrafting;
                    if(!isValid)
                        return Result.Cancelled;
                    ViewLocationSchema.Refresh(ActiveCommandModel.View, localizationModel);

                    transaction.Commit();
                    return Result.Succeeded;
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    MessageBox.Show(
                        ex.Message,
                        Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return Result.Failed;
                }
            }
        }
    }
}
