namespace ClrVpx
{
    public class Model
    {
        public Model()
        {
            Scanner = new Scanner.Scanner();
            Settings = new Settings.Settings();
        }

        public Scanner.Scanner Scanner { get; set; }
        public Settings.Settings Settings { get; set; }
    }
}