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
            Console.WriteLine("New user connected to the server");

            while(TestConnection(client))
            {
                // Parse commands from clients here?
            }

            Console.WriteLine("Client disconnected from server");
        }

        /**
         * This is a function to help the server determine whether or
         * not some client has disconnected from it.
         */ 
        private static bool TestConnection(TcpClient client)
        {
            bool isConnected = true;

            // Check the status of the client
            if(client.Client.Poll(0, SelectMode.SelectRead))
            {
                if (!client.Connected)
                    isConnected = false;
                else
                {
                    byte[] b = new byte[1];

                    try
                    {
                        // If we get a zero then the client has disconnected
                        if (client.Client.Receive(b, SocketFlags.Peek) == 0)
                            isConnected = false;
                    }
                    catch
                    {
                        // If the process fails then the client has disconnected
                        isConnected = false;
                    }
                }
            }

            return isConnected;
        }
    }
}
