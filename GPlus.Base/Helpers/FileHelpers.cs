using System.IO;
using System.Windows;

namespace GPlus.Base.Helpers
{
    public static class FileHelpers
    {
        public static string ResolvePath(string inputPath, Document doc)
        {
            if (Path.IsPathRooted(inputPath))
                return Path.GetFullPath(inputPath);

            if (TryDocPath(doc, out string baseFolder)!)
            { 
                baseFolder = Path.GetDirectoryName(doc.PathName);
                if (string.IsNullOrEmpty(baseFolder))
                    return null;
            }

            return Path.GetFullPath(Path.Combine(baseFolder, inputPath));
        }
        public static string ToRelativePath(string inputPath, Document doc)
        {
          if(TryDocPath(doc, out string baseFolder)!) return inputPath;
            Uri baseUri = new Uri(baseFolder + System.IO.Path.DirectorySeparatorChar);
            Uri fileUri = new Uri(inputPath);
            string relativePath = Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString()).Replace('/', System.IO.Path.DirectorySeparatorChar);

            return relativePath;
        }

        public static bool TryDocPath(Document doc, out string baseFolder)
        {
            baseFolder = doc.PathName;
            if (doc.IsWorkshared)
            {
                ModelPath modelPath = doc.GetWorksharingCentralModelPath();
                if (modelPath != null)
                {
                    string centralPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);

                    if (!centralPath.StartsWith("BIM 360", StringComparison.OrdinalIgnoreCase) &&
                        !centralPath.StartsWith("Autodesk Docs", StringComparison.OrdinalIgnoreCase) &&
                        !centralPath.StartsWith("Revit Server", StringComparison.OrdinalIgnoreCase))
                        baseFolder = Path.GetDirectoryName(centralPath);
                }
            }
            if (string.IsNullOrEmpty(baseFolder))
            {
                System.Windows.MessageBox.Show(Resources.Localizations.Messages.FirstSaveFile, Resources.Localizations.Messages.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }



    }
}
