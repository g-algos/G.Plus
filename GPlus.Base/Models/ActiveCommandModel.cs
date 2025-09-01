namespace GPlus.Base.Models
{
    public static class ActiveCommandModel
    {
        public static UIApplication UIApplication { get; private set; }
        public static UIDocument UIDocument { get; private set; }
        public static Document Document { get; private set; }
        public static Autodesk.Revit.DB.View View { get; private set; }


        public static void Set (UIApplication app)
        {
            UIApplication = app;
            UIDocument = app.ActiveUIDocument;
            Document = UIDocument.Document;
            View = Document.ActiveView;
        }
    }
}
