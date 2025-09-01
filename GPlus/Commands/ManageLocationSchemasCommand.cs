using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.ExtensibleStorage;
using GPlus.Base.Extensions;
using GPlus.Base.Models;
using GPlus.Base.Schemas;
using System.Collections.ObjectModel;
using System.Windows;

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
                    viewModel.AddSchema = (vm) => AddSchema(vm, ActiveCommandModel.Document.ProjectInformation, viewModel);
                    viewModel.RemoveSchema = (vm) => RemoveSchema(vm, ActiveCommandModel.Document.ProjectInformation, viewModel);
                    viewModel.RefreshSchema = (vm) => RefreshSchema(vm, ActiveCommandModel.Document.ProjectInformation, viewModel);
                    viewModel.UpdateSchema = (vm) => UpdateSchema(vm, ActiveCommandModel.Document.ProjectInformation, viewModel);
                    var window = new ManageLocationSchemasView(viewModel);
                    _ = window.ShowDialog();

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
        private (bool result, string message) UpdateSchema((string, object) e, ProjectInfo project, ManageLocationSchemasVM viewModel)
        {
            using (SubTransaction transaction = new SubTransaction(ActiveCommandModel.Document))
            {
                try
                {
                    transaction.Start();
                    var schema = Schema.Lookup(viewModel.SelectedSchema.Id);
                    if (schema == null) return (true, string.Empty);
                    var entity = project.GetEntity(schema);
                    if (entity == null || !entity.IsValid()) return (true, string.Empty);
                    var localizationSchema = new LocationSchema(project, entity, true);
                    var values = e.Item2;
                    if (e.Item1 == nameof(LocalizationVM.Items))
                    {
                        if (e.Item2 is not List<LocationItemVM> items)
                            return (true, string.Empty);
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
                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    return (false, ex.Message);
                }
            }
        }
        private static (bool result, string message) RefreshSchema(LocalizationVM vm, ProjectInfo project, ManageLocationSchemasVM viewModel)
        {
            using (SubTransaction transaction = new SubTransaction(ActiveCommandModel.Document))
            {
                try
                {
                    transaction.Start();
                    var schema = Schema.Lookup(vm.Id);
                    if (schema == null) return (true, string.Empty);
                    var entity = project.GetEntity(schema);
                    if (entity == null || !entity.IsValid()) return (true, string.Empty);
                    var localizationSchema = new LocationSchema(project, entity, true);
                    viewModel.LocalizationSchemaItems = new ObservableCollection<LocationItemVM>(localizationSchema.Items.Select(e => new LocationItemVM(e.Value, e.Color, _patterns.FirstOrDefault(p => p.Id == new ElementId(e.FillPattern)))));
                    transaction.Commit();
                    return (true, string.Empty);
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
                    transaction.RollBack();
                    return (false, ex.Message);
                }
            }
        }
        private static (bool result, string message) RemoveSchema(LocalizationVM vm, ProjectInfo project, ManageLocationSchemasVM viewModel)
        {
            using (SubTransaction transaction = new SubTransaction(ActiveCommandModel.Document))
            {
                try
                {
                    transaction.Start();
                    var schema = Schema.Lookup(vm.Id);
                    var projectInfo = project;
                    projectInfo.DeleteEntity(schema);

                    viewModel.AllSchemas.Remove(vm);
                    viewModel.LocalizationSchemas.Remove(vm);
                    viewModel.SelectedSchema = null;
                    ProjectLocationsShema.RemoveLocalizationModel(project, schema.GUID);
                    transaction.Commit();
                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    return (false, ex.Message);
                }
            }
        }
        private static (bool result, string message) AddSchema(NewLocationVM vm, ProjectInfo project, ManageLocationSchemasVM viewModel)
        {
            using (SubTransaction transaction = new SubTransaction(ActiveCommandModel.Document))
            {
                try
                {
                    transaction.Start();
                    LocationSchema localizationSC = new LocationSchema(project,
                    vm.Name,
                    vm.SelectedCategories.Select(c => c.Id).ToList(),
                    vm.SelectedParameter.Id,
                    vm.ByValue,
                    vm.IncludeLinks,
                    vm.Step);


                    LocalizationVM newLocalization = new LocalizationVM(
                        vm.Id,
                        vm.Name,
                        vm.SelectedCategories.ToList(),
                        vm.SelectedParameter,
                        vm.IncludeLinks,
                        vm.ByValue,
                        vm.Step
                    );

                    newLocalization.Items = new ObservableCollection<LocationItemVM>(localizationSC.Items.Select(e => new LocationItemVM(e.Value, e.Color, _patterns.FirstOrDefault(p => p.Id == new ElementId(e.FillPattern)))));
                    ProjectLocationsShema.AddLocalizationModel(project, localizationSC.Id);
                    viewModel.AllSchemas.Add(newLocalization);
                    viewModel.LocalizationSchemas.Add(newLocalization);
                    //TODO scrool to the schema in the list
                    viewModel.SelectedSchema = newLocalization;
                    transaction.Commit();
                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    return (false, ex.Message);
                }
            }
        }
    }
}
