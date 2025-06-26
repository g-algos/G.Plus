using Autodesk.Revit.Attributes;
using GPlus.Base.Models;
using GPlus.Base.Schemas;

namespace GPlus.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ApplyLocationSchemaCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements
        )
        {
            ActiveCommandModel.Set(commandData.Application);
            using (Transaction transaction = new Transaction(ActiveCommandModel.Document, "Manage Localization Schemas"))
            {
                transaction.Start();
                try
                {
                    bool isValid = ActiveCommandModel.View is View3D || ActiveCommandModel.View is ViewPlan || ActiveCommandModel.View is ViewSection || ActiveCommandModel.View is ViewDrafting;
                    if (!isValid)
                        return Result.Cancelled;

                    List<IdentityGuidVM> listVM = ProjectLocationsShema.GetLocalizations(ActiveCommandModel.Document.ProjectInformation)
                                                .Select(e=> new IdentityGuidVM() { Id = e.Id, Name = e.Name })
                                                .ToList();
                    IdentityGuidVM selectedVM = null;
                    if(ViewLocationSchema.TryGetLocalization(ActiveCommandModel.View, out LocalizationModel localization))
                        selectedVM = new IdentityGuidVM() { Id =localization.Id, Name = localization.Name };



                    var viewModel = new ApplyLocationSchemaVM(listVM, selectedVM); 

                    viewModel.ApplySchema += (s, e) => { ApplyLocalization(e, listVM); };
                    var window = new ApplyLocationView(viewModel);
                    _ = window.ShowDialog();

                    transaction.Commit();
                    return Result.Succeeded;
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    var dialog = new TaskDialog(Base.Resources.Localizations.Messages.OOOps)
                    {
                        MainInstruction = Base.Resources.Localizations.Messages.Error,
                        MainContent = ex.Message,
                        CommonButtons = TaskDialogCommonButtons.Close
                    };
                    dialog.Show();
                    return Result.Failed;
                }
            }
        }

        private void ApplyLocalization(Guid? localization, List<IdentityGuidVM> localizations)
        {
            using (SubTransaction transaction = new SubTransaction(ActiveCommandModel.Document))
            {

                try
                {
                    transaction.Start();

                    if (localization == null)
                    {
                        ViewLocationSchema.RemoveLocalization(ActiveCommandModel.View);
                        return;
                    }

                    var schema = localizations.FirstOrDefault(e => e.Id == localization);
                    if (schema == null)
                        return;

                    ViewLocationSchema.SetLocalization(ActiveCommandModel.View, schema.Id);

                    if (!ViewLocationSchema.TryGetLocalization(ActiveCommandModel.View, out LocalizationModel? localizationModel))
                        return;
                    ViewLocationSchema.Refresh(ActiveCommandModel.View, localizationModel);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    var dialog = new TaskDialog(Base.Resources.Localizations.Messages.OOOps)
                    {
                        MainInstruction = Base.Resources.Localizations.Messages.Error,
                        MainContent = ex.Message,
                        CommonButtons = TaskDialogCommonButtons.Close
                    };
                    dialog.Show();
                    return;
                }
            }
        }
    }
}
