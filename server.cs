/* Author:  Andrew Graham
 * Date:    27 May 2014
 * Purpose: Threaded server implementation of the TwentyQuestions game. Services multiple
 *          clients, monitors client connectivity, and relays messages from players of the
 *          game.
 */

using System;
using System.Collections.Generic;
using System.Collections;
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
        public static Hashtable clientsList = new Hashtable();
        private TcpListener listener;
        private Thread listenerThread;
        private enum control {caller, others, all};
        private IntPtr host;

        /*
         * Constructor
         */
        public server (int port)
        {
            this.Port = port;
        }
        
        /*
         * Function to initialize the server, listener thread and get it listening 
         * on the designated port for any incoming IP address.
         */
        public void start ()
        {
            listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
            listener.Start();

            // Let's start a new thread to handle the listener()
            listenerThread = new Thread(new ThreadStart(Listener));
            listenerThread.Start();
        }

        /*
         * Shuts down the server and ends it's listener thread
         */
        public void stop()
        {
            byte[] data;
            string quitMessage = "Server shutting down...";

            quitMessage = quitMessage.PadRight(254);

            data = Encoding.ASCII.GetBytes(quitMessage);

            foreach (TcpClient clientTemp in clientsList.Values)
            {
                try
                {
                    Broadcast(data, control.caller, clientTemp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // Close the listener thread
            listener.Stop();
            listenerThread.Abort();
        }
        
        /*
         * Thread that listens to the port and initializes connections
         */
        private void Listener()
        {
            IntPtr handle;

            // Listen forever on PORT and accept any IP
            while(true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    
                    // Let's add this client to the hashTable
                    handle = client.Client.Handle;
                    clientsList.Add(handle, client);

                    // Start the HandleClient thread to monitor their commands
                    new Thread(new ThreadStart(() => HandleClient(client))).Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in Listener Thread:");
                    Console.WriteLine(ex.Message);
                }
            }

        }

        /*
         * Main client handler - directs commands to appropriate function
         */ 
        private void HandleClient(TcpClient client)
        {
            Console.WriteLine("New user connected to the server");
            byte[] packetCommand = new byte[2]; // The command from the client
            byte[] packetData = new byte[254];  // The remainder of the 256 allowance - data

            while(TestConnection(client))
            {
                // Try-Catch needed in case the client forcibly ends the connection
                try
                {
                    client.GetStream().Read(packetCommand, 0, 2);
                    string command = Encoding.ASCII.GetString(packetCommand);

                    // Try-catch needed in case the client forcibly ends the connection
                    try
                    {
                        client.GetStream().Read(packetData, 0, 254);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    // What command was issued to the server?
                    switch (command)
                    {
                        case "Q:":
                            Console.WriteLine("Received Command: {0}", command);
                            serverQuit(client);
                            break;

                        case "?:":
                            Console.WriteLine("Received Command: {0}", command);
                            serverQuestion(client, packetData);
                            break;

                        case "A:":
                            Console.WriteLine("Received Command: {0}", command);
                            serverAnswer(client, packetData);
                            break;

                        case "E:":
                            Console.WriteLine("Received Command: {0}", command);
                            serverEnd(client, packetData);
                            break;

                        case "S:":
                            Console.WriteLine("Received Command: {0}", command);
                            serverStart(client, packetData);
                            break;

                        default:
                            Console.WriteLine("Invalid Command: {0}", command);
                            serverInvalid(client);
                            break;
                    }

                    // Clear the stream just in case a packet that was too large arrived
                    client.GetStream().Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                
            }

            // Make sure we tidy up the hash table
            clientsList.Remove(client.Client.Handle);
            Console.WriteLine("Client disconnected from server");
        }

        /*
         * This is a function to help the server determine whether or
         * not some client has disconnected from it.
         */ 
        private static bool TestConnection(TcpClient client)
        {
            bool isConnected = true;

            // Check the status of the client IOT detect a forced exit
            if(client.Client.Poll(0, SelectMode.SelectRead))
            {
                if (!client.Connected)
                    isConnected = false;
                else
                {
                    byte[] b = new byte[1];

                    try
                    {
                        // If the client doesn't receive the test byte then they're gone
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

        /*
         * Client wants to exit the game and close the connect
         */
        private void serverQuit(TcpClient client)
        {
            byte[] data;
            string clientDisconnect = "A user has disconnected";

            data = Encoding.ASCII.GetBytes(clientDisconnect);

            Console.WriteLine("QUIT");

            Broadcast(data, control.all, client);
        }

        /*
         * Client wants to submit a question to the current game
         */
        private void serverQuestion(TcpClient client, byte[] data)
        {
            Console.WriteLine("QUESTION");

            Broadcast(data, control.all, client);
        }

        /*
         * Client wants to send an answer to the current game
         */
        private void serverAnswer(TcpClient client, byte[] data)
        {
            if (client.Client.Handle == host)
            {
                Console.WriteLine("ANSWER");

                Broadcast(data, control.all, client);
            }
            else
                serverNotHost(client);
        }

        /*
         * Client wants to start a game - sending client is now the host
         */
        private void serverStart(TcpClient client, byte[] data)
        {
            Console.WriteLine("START");
            host = client.Client.Handle;

            Broadcast(data, control.all, client);
        }

        /*
         * Client notifies game session that the current game has ended
         */
        private void serverEnd(TcpClient client, byte[] data)
        {
            if (client.Client.Handle == host)
            {
                Console.WriteLine("END");
                host = (IntPtr)0;

                Broadcast(data, control.all, client);
            }
            else
                serverNotHost(client);
        }

        /*
         * Client has sent a message in an invalid format - only notify sender
         */
        private void serverInvalid(TcpClient client)
        {
            byte[] data;
            string error = "Invalid packet received";

            data = Encoding.ASCII.GetBytes(error);

            Console.WriteLine("Invalid packet received");

            Broadcast(data, control.caller, client);
        }

        /*
         * A player that is not the host has tried to perform a priviliged action
         */
        private void serverNotHost(TcpClient client)
        {
            byte[] data;
            string error = "You aren't currently the host of this game!";

            data = Encoding.ASCII.GetBytes(error);

            Console.WriteLine("Command from non-Host!");

            Broadcast(data, control.caller, client);
        }

        /*
         * Function to transmit to specified clients
         */
        private void Broadcast (byte[] message, control flag, TcpClient client)
        {
            // Send these messages only to the person who sent the original packet
            if(flag == control.caller)
            {
                try
                {
                    client.GetStream().Write(message, 0, message.Length);
                    client.GetStream().Flush();
                    Console.WriteLine("Package sent to client: {0}", client.Client.Handle);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            // Send these messages to everyone BUT the person who sent the original packet
            else if(flag == control.others)
            {
                foreach (TcpClient clientTemp in clientsList.Values)
                {
                    if (client.Client.Handle != clientTemp.Client.Handle)
                    {
                        try
                        {
                            clientTemp.GetStream().Write(message, 0, message.Length);
                            clientTemp.GetStream().Flush();
                            Console.WriteLine("Package sent to client: {0}", clientTemp.Client.Handle);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            // Broadcast these messages to all active clients
            else // Control.all
            {
                foreach (TcpClient clientTemp in clientsList.Values)
                {
                    try
                    {
                        clientTemp.GetStream().Write(message, 0, message.Length);
                        clientTemp.GetStream().Flush();
                        Console.WriteLine("Package sent to client: {0}", clientTemp.Client.Handle);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        /*
         * Function to help the server package data into the proper
         * format that the clients will expect.
         */
        private byte[] packageData(string data)
        {
            byte[] message;

            message = Encoding.ASCII.GetBytes(data);

            return message;
        }


    }
}
