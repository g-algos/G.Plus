namespace GPlus.UI.ViewsModels
{
    public class AboutVM
    {
        public string Product { get; set; }
        public string Version { get; set; }
        public string Launching { get; set; }
        public string Vendor { get; set; } = "etc-tec";
        public string Contact { get; set; } = "contact@g-algos.com";
        public string Site { get; set; } = "http://g-algos.com";

        public AboutVM()
        {
        }
    }
}
