using Autodesk.Revit.Attributes;
using System.Reflection;


namespace GPlus.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class AboutCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements
        )
        {
            var assembly = typeof(AboutCommand).Assembly;

            var viewModel = new AboutVM();
            viewModel.Product = assembly
                .GetCustomAttributes(typeof(AssemblyProductAttribute), false)
                .OfType<AssemblyProductAttribute>()
                .FirstOrDefault()?.Product ?? "G.plus";
            viewModel.Version = assembly.GetName().Version.ToString();
            viewModel.Launching = assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "ReleaseDate")?.Value ?? "Unknown";
            AboutView view = new AboutView(viewModel);
            view.ShowDialog();
            return Result.Succeeded;
        }
    }
}
