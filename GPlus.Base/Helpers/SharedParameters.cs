using System.IO;
using System.Text;
using Binding = Autodesk.Revit.DB.Binding;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace GPlus.Base.Helpers
{
    public class SharedParameters
    {
        public static List<Definition> AddSharedParameterToTempFile(Application app, List<(string Name, ForgeTypeId SpecType)> parameters)
        {
            string filePath = Path.Combine(Path.GetTempPath(), "g_sharedparams_temp.txt");
            if (!File.Exists(filePath))
            {
                // Criar ficheiro do zero
                string header =
@"# This is a Revit shared parameter file.
# Do not edit manually.
*META	VERSION	MINVERSION
META	2	1
*GROUP	ID	NAME
GROUP	1	temp
*PARAM	GUID	NAME	DATATYPE	DATACATEGORY	GROUP	VISIBLE	DESCRIPTION	USERMODIFIABLE	HIDEWHENNOVALUE
";
                File.WriteAllText(filePath, header, Encoding.Unicode);
            }
            app.SharedParametersFilename = filePath;

            DefinitionFile defFile = app.OpenSharedParameterFile();
            if (defFile == null)
                return new();

            DefinitionGroup group = defFile.Groups.get_Item("temp") ?? defFile.Groups.Create("temp");

            List<Definition> definitions = new();
            foreach (var parameter in parameters)
            {
                ForgeTypeId forgeType = parameter.SpecType;

                ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(parameter.Name, forgeType)
                {
                    Visible = true,
                    UserModifiable = true
                };

                Definition def = group.Definitions.Create(options);
                definitions.Add(def);
            }
            return definitions;
        }


        public static bool TryCreateSharedParameter(
                Document doc,
                Definition definition,
                bool isInstance,
                CategorySet catSet,
                out ElementId id)
        {
            try
            {


                Binding binding = isInstance
                    ? (Binding)doc.Application.Create.NewInstanceBinding(catSet)
                    : (Binding)doc.Application.Create.NewTypeBinding(catSet);
                bool inserted = doc.ParameterBindings.Insert(definition, binding, GroupTypeId.General);
                if (!inserted)
                  doc.ParameterBindings.ReInsert(definition, binding, GroupTypeId.General);

                id = new FilteredElementCollector(doc).OfClass(typeof(SharedParameterElement)).FirstOrDefault(e=> e.Name == definition.Name)?.Id;
                if (id == null) return false;
                return true;
            }
            catch (Exception ex)
            {
                id = ElementId.InvalidElementId;
                return false;
            }
        }

    }
}
