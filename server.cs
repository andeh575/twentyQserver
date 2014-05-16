using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace twentyQserver
{
    class server
    {
        public int Port { get; set; }
        public TcpListener listener;
        private Thread listenerThread;

        public server (int port)
        {
            this.Port = port;
        }
        
        public void start ()
        {
            listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
            listener.Start();
            listenerThread = new Thread(new ThreadStart(Listener));
            listenerThread.Start();
        }

        public void stop()
        {
            listener.Stop();
            listenerThread.Abort();
        }
        
        private void Listener()
        {
            while(true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    new Thread(new ThreadStart(() => HandleClient(client))).Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in Listener Thread:");
                    Console.WriteLine(ex.Message);
                }
            }

        }

        private void HandleClient(TcpClient client)
        {
            
        }
    }
}
