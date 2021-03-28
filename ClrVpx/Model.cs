using ClrVpx.Scanner;

namespace ClrVpx
{
    public class Model
    {
        public Model()
        {
            Scanner = new ScannerModel();
        }

        public ScannerModel Scanner { get; set; }
    }
}