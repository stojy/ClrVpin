using ClrVpx.Scanner;

namespace ClrVpx
{
    public class Model
    {
        public Model()
        {
            Scanner = new Scanner.Scanner();
        }

        public Scanner.Scanner Scanner { get; set; }
    }
}