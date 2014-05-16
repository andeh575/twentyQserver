using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace twentyQserver
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 14000;
            server server = new server(port);

            server.start();

            Console.WriteLine("Started twentyQ server on port number: {0}", port);
            Console.WriteLine("Press [q] to stop the server");

            while(true)
            {
                ConsoleKeyInfo c = Console.ReadKey(true);

                if(c.Key == ConsoleKey.Q)
                {
                    Console.WriteLine("Closing server...");
                    server.stop();
                    return;
                }
            }
        }
    }
}
