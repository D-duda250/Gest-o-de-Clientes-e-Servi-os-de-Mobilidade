using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CsvHelper.Configuration.Attributes;

class Cliente
{
    public static void Main(string[] args)
    {


        string ID;
        Console.Write("Qual e o seu ID?\n");
        //Console.Write("   ");
        ID = Console.ReadLine();

        Connect("127.0.0.1", ID);
        static void Connect(String server, String ID)
        {
            try
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer
                // connected to the same address as specified by the server, port
                // combination.
                Int32 port = 13000;

                // Prefer a using declaration to ensure the instance is Disposed later.
                using TcpClient client = new TcpClient(server, port);

                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] bytes = System.Text.Encoding.ASCII.GetBytes(ID);

                String data = null;
                // Get a client stream for reading and writing.
                NetworkStream stream = client.GetStream();


                // Send the message to the connected TcpServer.
                //stream.Write(data, 0, data.Length);
                //Console.WriteLine("Sent: {0}", ID);


                // Byte[] bytes = new Byte[256];
               
               

                int i;

                // Loop to receive all the data sent by the server.
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine("Received: {0}", data);

                    // Process the data sent by the client.
                    
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                    // Send back a response.
                    stream.Write(msg, 0, msg.Length);
                    Console.WriteLine("{0}", data);

                    //stream.Write(msg, 0, msg.Length);
                    //Console.WriteLine("Já terminou a tarefa?", data);

                }
                // Receive the server response.

                // Buffer to store the response bytes.
                //data = new Byte[256];

                //// String to store the response ASCII representation.
                //String responseData = String.Empty;

                //// Read the first batch of the TcpServer response bytes.
                //Int32 bytes = stream.Read(data, 0, data.Length);
                //responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Console.WriteLine("Received: {0}", responseData);


                //Int32 bytes = stream.Read(data, 0, data.Length);
                //responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Console.WriteLine("{0}", responseData);

                // Explicit close is not necessary since TcpClient.Dispose() will be
                // called automatically.
                // stream.Close();
                // client.Close();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }
    }
}