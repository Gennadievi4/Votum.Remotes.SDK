using System;
using System.Linq;
using System.Threading.Tasks;

namespace Votum.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            HIDReader hIDReader = new HIDReader();
            hIDReader.InitializeVotumAsync().Wait();

            var test_diapason = Enumerable.Range(1, 10).ToArray()[1..];
            foreach (var item in test_diapason)
            {
                Console.WriteLine(item);
            }

            Console.ReadKey(true);
        }
    }
}
