namespace ClrVpin.Models.Settings
{
    public class Settings
    {
        public Settings()
        {
            // default settings - will be overwritten AFTER ctor by the deserialized settings if they exist
            TableFolder = @"C:\vp\tables\vpx";
        }

        public string TableFolder { get; set; }
    }
}