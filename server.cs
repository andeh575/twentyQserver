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

        /**
         * Main client handler - directs commands to appropriate function
         */ 
        private void HandleClient(TcpClient client)
        {
            Console.WriteLine("New user connected to the server");
            byte[] packetCommand = new byte[2]; // The command from the client
            byte[] packetData = new byte[254];  // The remainder of the 256 allowance - data

            // Try-Catch needed in case the client forcibly ends the connection
            try
            {
                client.GetStream().Read(packetCommand, 0, 2);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            

            string command = BitConverter.ToString(packetCommand);

            while(TestConnection(client))
            {
                client.GetStream().Read(packetData, 2, 254);
                switch(command)
                {
                    case "Q:":
                        Console.WriteLine("Received Command: {0}", command);
                        serverQuit(client, packetData);
                        break;

                    case "?:":
                        Console.WriteLine("Received Command: {0}", command);
                        serverQuestion(client, packetData);
                        client.GetStream().Read(packetCommand, 0, 2);
                        break;

                    case "A:":
                        Console.WriteLine("Received Command: {0}", command);
                        serverAnswer(client, packetData);
                        client.GetStream().Read(packetCommand, 0, 2);
                        break;

                    case "E:":
                        Console.WriteLine("Received Command: {0}", command);
                        serverEnd(client, packetData);
                        client.GetStream().Read(packetCommand, 0, 2);
                        break;

                    case "S:":
                        Console.WriteLine("Received Command: {0}", command);
                        serverStart(client, packetData);
                        client.GetStream().Read(packetCommand, 0, 2);
                        break;

                    default:
                        Console.WriteLine("Invalid Command: {0}", command);
                        serverInvalid(client);
                        client.GetStream().Read(packetCommand, 0, 2);
                        break;

                }
                
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

        /**
         * Client wants to exist the game and close the connect
         */
        private void serverQuit(TcpClient client, byte[] data)
        {
            Console.WriteLine("QUIT");
        }

        /**
         * Client wants to submit a question to the current game
         */
        private void serverQuestion(TcpClient client, byte[] data)
        {
            Console.WriteLine("QUESTION");
        }

        /**
         * Client wants to send an answer to the current game
         */
        private void serverAnswer(TcpClient client, byte[] data)
        {
            Console.WriteLine("ANSWER");
        }

        /**
         * Client wants to start a game - sending client is now the host
         */
        private void serverStart(TcpClient client, byte[] data)
        {
            Console.WriteLine("START");
        }

        /**
         * Client notifies game session that the current game has ended
         */
        private void serverEnd(TcpClient client, byte[] data)
        {
            Console.WriteLine("END");
        }

        /**
         * Client has sent a message in an invalid format - only notify sender
         */
        private void serverInvalid(TcpClient client)
        {
            Console.WriteLine("Invalid packet received");
        }
    }
}
