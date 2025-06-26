using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.ExtensibleStorage;
using GPlus.Base.Extensions;
using GPlus.Base.Models;
using GPlus.Base.Schemas;
using System.Collections.ObjectModel;

namespace GPlus.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ManageLocationSchemasCommand : IExternalCommand
    {
        private static List<FillPatternImageVM> _patterns;
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

                    List<LocalizationVM> listVM = new();
                    _patterns = ActiveCommandModel.Document.GetfillPatterns().Select(fp => new FillPatternImageVM(fp.Id, fp.Name, fp.GetFillPatternImage())).ToList();

                    List<LocalizationModel> listLocM = ProjectLocationsShema.GetLocalizations(ActiveCommandModel.Document.ProjectInformation) ?? new();
                    LocalizationVM selectedVM = null;
                    if (ViewLocationSchema.TryGetLocalization(ActiveCommandModel.View, out LocalizationModel? selectedModel))
                        selectedVM = new LocalizationVM(
                            selectedModel.Id,
                            selectedModel.Name,
                            selectedModel.Categories.Select(e => new IdentityVM() 
                            { 
                                Id = e,
#if V2023
                                Name = LabelUtils.GetLabelFor((BuiltInCategory)e.IntegerValue)
#else
                                Name = LabelUtils.GetLabelFor((BuiltInCategory)e.Value)
#endif
                            }).ToList(),
                            new IdentityVM() { Name = ActiveCommandModel.Document.GetParameterName(selectedModel.Parameter), Id = selectedModel.Parameter },
                            selectedModel.IncludeElementsFromLinks,
                            selectedModel.ByValue,
                            selectedModel.Step);

                    foreach (var model in listLocM)
                    {
                        var parameter = new IdentityVM() { Name = ActiveCommandModel.Document.GetParameterName(model.Parameter), Id = model.Parameter };
                        var modelCategories = model.Categories.Select(c => new IdentityVM() 
                        { 
                            Id = c,
#if V2023
                            Name = LabelUtils.GetLabelFor((BuiltInCategory)c.IntegerValue)
#else
                            Name = LabelUtils.GetLabelFor((BuiltInCategory)c.Value)
#endif
                        }).ToList();
                        var vm = new LocalizationVM(model.Id, model.Name, modelCategories, parameter, model.IncludeElementsFromLinks, model.ByValue, model.Step);
                        foreach (var item in model.Items)
                        {
                            vm.Items.Add(new LocationItemVM(item.Value, item.Color, _patterns.FirstOrDefault(e => e.Id == new ElementId(item.FillPattern))));
                        }

                        listVM.Add(vm);
                    }

                    ManageLocationSchemasVM viewModel = new ManageLocationSchemasVM(listVM, selectedVM, _patterns);
                    viewModel.AddSchema += (s, e) => { AddSchema(e, ActiveCommandModel.Document.ProjectInformation, viewModel); };
                    viewModel.RemoveSchema += (s, e) => { RemoveSchema(e, ActiveCommandModel.Document.ProjectInformation, viewModel); };
                    viewModel.RefreshSchema += (s, e) => { RefreshSchema(e, ActiveCommandModel.Document.ProjectInformation, viewModel); };
                    viewModel.UpdateSchema += (s, e) => { UpdateSchema(e, ActiveCommandModel.Document.ProjectInformation, viewModel); };
                    var window = new ManageLocationSchemasView(viewModel);
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
        private void UpdateSchema((string, object) e, ProjectInfo project, ManageLocationSchemasVM viewModel)
        {
            using (SubTransaction transaction = new SubTransaction(ActiveCommandModel.Document))
            {
                try
                {
                    transaction.Start();
                    var schema = Schema.Lookup(viewModel.SelectedSchema.Id);
                    if (schema == null) return;
                    var entity = project.GetEntity(schema);
                    if (entity == null || !entity.IsValid()) return;
                    var localizationSchema = new LocationSchema(project, entity, true);
                    var values = e.Item2;
                    if (e.Item1 == nameof(LocalizationVM.Items))
                    {
                        if (e.Item2 is not List<LocationItemVM> items)
                            return;
                        values = items.Select(
#if V2023
                            e => new LocalizationItemModel(e.Value, e.Color, e.FillPattern!.Id.IntegerValue))
#else
                            e => new LocalizationItemModel(e.Value, e.Color, e.FillPattern!.Id.Value))
#endif
                            .ToList();
                    }
                    localizationSchema.EditField(project, e.Item1, values);
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
        private static void RefreshSchema(LocalizationVM e, ProjectInfo project, ManageLocationSchemasVM viewModel)
        {
            using (SubTransaction transaction = new SubTransaction(ActiveCommandModel.Document))
            {
                try
                {
                    transaction.Start();
                    var schema = Schema.Lookup(e.Id);
                    if (schema == null) return;
                    var entity = project.GetEntity(schema);
                    if (entity == null || !entity.IsValid()) return;
                    var localizationSchema = new LocationSchema(project, entity, true);
                    viewModel.LocalizationSchemaItems = new ObservableCollection<LocationItemVM>(localizationSchema.Items.Select(e => new LocationItemVM(e.Value, e.Color, _patterns.FirstOrDefault(p => p.Id == new ElementId(e.FillPattern)))));
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
        private static void RemoveSchema(LocalizationVM e, ProjectInfo project, ManageLocationSchemasVM viewModel)
        {
            using (SubTransaction transaction = new SubTransaction(ActiveCommandModel.Document))
            {
                try
                {
                    transaction.Start();
                    var schema = Schema.Lookup(e.Id);
                    var projectInfo = project;
                    projectInfo.DeleteEntity(schema);

                    viewModel.AllSchemas.Remove(e);
                    viewModel.LocalizationSchemas.Remove(e);
                    viewModel.SelectedSchema = null;
                    ProjectLocationsShema.RemoveLocalizationModel(project, schema.GUID);
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
        private static void AddSchema(NewLocationVM e, ProjectInfo project, ManageLocationSchemasVM viewModel)
        {
            using (SubTransaction transaction = new SubTransaction(ActiveCommandModel.Document))
            {
                try
                {
                    transaction.Start();
                    LocationSchema localizationSC = new LocationSchema(project,
                    e.Name,
                    e.SelectedCategories.Select(c => c.Id).ToList(),
                    e.SelectedParameter.Id,
                    e.ByValue,
                    e.IncludeLinks,
                    e.Step);


                    LocalizationVM newLocalization = new LocalizationVM(
                        e.Id,
                        e.Name,
                        e.SelectedCategories.ToList(),
                        e.SelectedParameter,
                        e.IncludeLinks,
                        e.ByValue,
                        e.Step
                    );

                    newLocalization.Items = new ObservableCollection<LocationItemVM>(localizationSC.Items.Select(e => new LocationItemVM(e.Value, e.Color, _patterns.FirstOrDefault(p => p.Id == new ElementId(e.FillPattern)))));
                    ProjectLocationsShema.AddLocalizationModel(project, localizationSC.Id);
                    viewModel.AllSchemas.Add(newLocalization);
                    viewModel.LocalizationSchemas.Add(newLocalization);
                    //TODO scrool to the schema in the list
                    viewModel.SelectedSchema = newLocalization;
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
