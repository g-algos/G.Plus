using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI.Events;
using G.DB;
using GPlus.Base.Models;
using GPlus.Base.Schemas;
using Microsoft.Win32;
using OfficeOpenXml;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Application = Autodesk.Revit.ApplicationServices.Application;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using View = Autodesk.Revit.DB.View;

namespace GPlus
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            var culture = GetCultureInfo(app);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            CreateRibbon(app);

            app.Idling += OnIdlingOnce;
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.StartsWith("G.wpf"))
                {
                    string dllPath = Path.Combine(dir, "G.wpf.dll");
                    return File.Exists(dllPath) ? Assembly.LoadFrom(dllPath) : null;
                }
                return null;
            };


            ProjectLocationsShema.Create();
            ViewLocationSchema.Create();
            DataLinkSchema.Create();

            ExcelPackage.License.SetNonCommercialOrganization("etc-tec");

            app.ViewActivated += OnViewActivated;
            app.ControlledApplication.ViewPrinting += OnViewPrinting;

            return Result.Succeeded;
        }
        public Result OnShutdown(UIControlledApplication app)
        {
            app.ViewActivated -= OnViewActivated;
            app.ControlledApplication.ViewPrinting -= OnViewPrinting;
            return Result.Succeeded;
        }
        internal void CreateRibbon(UIControlledApplication app)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            string tabName = Base.Resources.Localizations.Content.GPlus;
            string dataManagement = Base.Resources.Localizations.Content.DataManagement;
            string dataVisualisation = Base.Resources.Localizations.Content.DataVisualization;
            string extra = "●●●";

            app.CreateRibbonTab(tabName);

            RibbonPanel dataManagementPanel = app.CreateRibbonPanel(tabName, dataManagement);

            PushButtonData btn1 = new PushButtonData("ManageLinks", Base.Resources.Localizations.Content.ManageLinks, assemblyPath, typeof(Commands.ManageLinksCommand).FullName);
            btn1.LargeImage = new BitmapImage(new Uri("pack://application:,,,/GPlus;component/Resources/RibbonButtons/manageLinks_32.png"));
            btn1.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url,"https://g-algos.com/G.plus/DataLinks/"));
            //PushButtonData btn2 = new PushButtonData("DataSchemas", Base.Resources.Localizations.Content.DataSchemas, assemblyPath, typeof(Commands.DataSchemasCommand).FullName);
            //btn2.LargeImage = new BitmapImage(new Uri("pack://application:,,,/GPlus;component/Resources/RibbonButtons/GA.png"));

            dataManagementPanel.AddItem(btn1);
            //dataManagementPanel.AddItem(btn2);

            RibbonPanel dataVisualizationPanel = app.CreateRibbonPanel(tabName, dataVisualisation);

            PushButtonData btn3 = new PushButtonData("LocalizationSchemas", Base.Resources.Localizations.Content.LocationSchemas, assemblyPath, typeof(Commands.ManageLocationSchemasCommand).FullName);
            btn3.LargeImage = new BitmapImage(new Uri("pack://application:,,,/GPlus;component/Resources/RibbonButtons/localizations_32.png"));
            btn3.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://g-algos.com/G.plus/LocalizationSchemas/"));

            PushButtonData btn4 = new PushButtonData("ApplySchema", Base.Resources.Localizations.Content.ApplySchema, assemblyPath, typeof(Commands.ApplyLocationSchemaCommand).FullName);
            btn4.LargeImage = new BitmapImage(new Uri("pack://application:,,,/GPlus;component/Resources/RibbonButtons/applyLoc_32.png"));
            btn4.AvailabilityClassName = typeof(Helpers.Availability.ModelViewAvailability).FullName;
            btn4.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://g-algos.com/G.plus/LocalizationSchemas/#apply-scheme-to-view"));
            PushButtonData refreshLocalization = new PushButtonData("Refresh", Base.Resources.Localizations.Content.Refresh, assemblyPath, typeof(Commands.RefreshViewLocationSchemaCommand).FullName);
            refreshLocalization.LargeImage = new BitmapImage(new Uri("pack://application:,,,/GPlus;component/Resources/RibbonButtons/refreshLoc_32.png"));
            refreshLocalization.AvailabilityClassName = typeof(Helpers.Availability.LocalizationViewAvailability).FullName;
            refreshLocalization.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://g-algos.com/G.plus/LocalizationSchemas/#apply-scheme-to-view"));

            dataVisualizationPanel.AddItem(btn3);
            dataVisualizationPanel.AddItem(btn4);
            dataVisualizationPanel.AddItem(refreshLocalization);

            RibbonPanel extraPanel = app.CreateRibbonPanel(tabName, extra);

            PushButtonData btn5 = new PushButtonData("About", Base.Resources.Localizations.Content.About, assemblyPath, typeof(Commands.AboutCommand).FullName);
            btn5.Image = new BitmapImage(new Uri("pack://application:,,,/GPlus;component/Resources/RibbonButtons/Galgo_16.png"));
            btn5.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://g-algos.com/G.plus/changelog/"));
            PushButtonData btn6 = new PushButtonData("TakeBreak", Base.Resources.Localizations.Content.TakeBreak, assemblyPath, typeof(Commands.TakeBreakCommand).FullName);
            btn6.Image = new BitmapImage(new Uri("pack://application:,,,/GPlus;component/Resources/RibbonButtons/takeABreak_16.png"));
            btn6.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://g-algos.com/G.plus/Intro/"));
            PushButtonData btn7 = new PushButtonData("PayMeCoffee", Base.Resources.Localizations.Content.PayMeCoffee, assemblyPath, typeof(Commands.PayMeCoffeeCommand).FullName);
            btn7.Image = new BitmapImage(new Uri("pack://application:,,,/GPlus;component/Resources/RibbonButtons/coffe_16.png"));
            btn7.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://g-algos.com/"));

            extraPanel.AddStackedItems(btn5, btn6, btn7);
            //PushButtonData btn8 = new PushButtonData("QuickTest", "QuickTest", assemblyPath, typeof(Commands.QuickCommand).FullName);
            //extraPanel.AddItem(btn8);
        }
        internal CultureInfo GetCultureInfo(UIControlledApplication app)
        {
            LanguageType languageType = app.ControlledApplication.Language;
            string language = languageType switch
            {
                LanguageType.French => "fr-FR",
                LanguageType.Brazilian_Portuguese => "pt-PT",
                _ => "en",
            };

            return new CultureInfo("en");
        }
        internal void OnIdlingOnce(object sender, IdlingEventArgs e)
        {
            const string RegistryPath = @"Software\EtcTec\Gplus";
            UIApplication uiApp = sender as UIApplication;

            if (uiApp == null)
            {
                uiApp.Idling -= OnIdlingOnce;
                return;
            }

            try
            {
                string loginId = uiApp.Application.LoginUserId;
                Application app = uiApp.Application;

                string userId = app.LoginUserId;
                string userName = app.Username;
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        return;
                    }
                }
                ConnectionManager conn = new ConnectionManager();
                conn.LogUserToDatabase(userId, userName, "G.plus");
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    key.SetValue("LoginUserId", userId);
                    key.SetValue("UserName", userName);
                    key.SetValue("RegisteredAt", DateTime.UtcNow.ToString("o"));
                    key.SetValue("Product", "G.plus");
                }

            }
            catch { }
            finally
            {
                uiApp.Idling -= OnIdlingOnce;
            }
        }
        private void OnViewActivated(object sender, ViewActivatedEventArgs e)
        {

            View newView = e.CurrentActiveView;
            if (newView != null)
                ApplyLocalizationSchema(newView);

        }
        private void OnViewPrinting(object sender, ViewPrintingEventArgs e)
        {
            View view = e.View;
            if (view != null)
                ApplyLocalizationSchema(view);
        }
        private void ApplyLocalizationSchema(View newView)
        {
            bool isValid = newView is View3D || newView is ViewPlan || newView is ViewSection || newView is ViewDrafting;
            if (!isValid)
                return;
            if (!ViewLocationSchema.TryGetLocalization(newView, out LocalizationModel? localization))
                return;
            if(!localization!.Valid)
            {
                    TaskDialog dialog = new TaskDialog(Base.Resources.Localizations.Messages.OOOps)
                    {
                        MainInstruction = Base.Resources.Localizations.Messages.Error,
                        MainContent = Base.Resources.Localizations.Messages.LocalizationNotValid,
                        CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
                    };
                var result = dialog.Show();
                if (result == TaskDialogResult.No)
                    return;
                ProjectLocationsShema.RemoveLocalizationModel(newView.Document.ProjectInformation, localization.Id);
            }
            using (Transaction transaction = new Transaction(newView.Document, "refresh view"))
            {
                transaction.Start();
                try
                {

                    ViewLocationSchema.Refresh(newView, localization!);
                    transaction.Commit();
                }
                catch
                {
                    transaction.RollBack();
                }
            }
        }
    }
}
