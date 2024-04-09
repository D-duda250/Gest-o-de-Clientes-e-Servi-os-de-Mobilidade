using CsvHelper.Configuration.Attributes;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Servidor
{
    private static Mutex mut = new Mutex();
    private const int numIterations = 1;
    private const int numThreads = 3;


    //Servico.Csv

    [Name("ServicoId")]
    public string ServicoID { get; set; }



    //Servico_A
    //TarefaID,Descricao,Estado,ClienteID

    [Name("TarefaID")]
    public string TarefaID { get; set; }


    [Name("Descricao")]
    public string Descricao { get; set; }


    [Name("Estado")]
    public string Estado { get; set; }


    [Name("ClienteID")]
    public string ClienteID { get; set; }


    static void Main()
    {
        // Create the threads that will use the protected resource.
        for (int i = 0; i < numThreads; i++)
        {
            Thread newThread = new Thread(new ThreadStart(Mota));
            newThread.Name = String.Format("Thread{0}", i + 1);
            newThread.Start();
        }

        // The main thread exits, but the application continues to
        // run until all foreground threads have exited.
    }


    public static void Mota()
    {
        var path = @"C:/Users/Octav/Desktop/Nova pasta/Sist Distribuidos 2024/TP1/ficheiros_de_exemplo/Ficheiros de exemplo/Servico.csv";
        var servA = @"C:/Users/Octav/Desktop/Nova pasta/Sist Distribuidos 2024/TP1/ficheiros_de_exemplo/Ficheiros de exemplo/Servico_A.csv";

        var Reader = new StreamReader(File.OpenRead(path)); 
        var line = Reader.ReadLine();
        var columns = line.Split(",");


        TcpListener server = null;
        try
        {
            //Console.WriteLine("{0} is requesting the mutex",
            //              Thread.CurrentThread.Name);
            //mut.WaitOne();

            //Console.WriteLine("{0} has entered the protected area",
            //                  Thread.CurrentThread.Name);

            // Set the TcpListener on port 13000.
            Int32 port = 13000;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");

            // TcpListener server = new TcpListener(port);
            server = new TcpListener(localAddr, port);

            // Start listening for client requests.
            server.Start();

            // Buffer for reading data
            Byte[] bytes = new Byte[256];
            String data = null;

            // Enter the listening loop.
            while (true)
            {
                Console.Write("Waiting for a connection... \n");

                // Perform a blocking call to accept requests.
                // You could also use server.AcceptSocket() here.
                using TcpClient client = server.AcceptTcpClient();
                //Console.WriteLine("100 OK!");

                data = null;

                // Get a stream object for reading and writing
                NetworkStream stream = client.GetStream();




                int i;

                // Loop to receive all the data sent by the client.
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    //Console.WriteLine("Received: {0}", data);

                    // Process the data sent by the client.
                    //data = data.ToUpper();

                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                    // Send back a response.
                    stream.Write(msg, 0, msg.Length);
                    Console.WriteLine("O ID {0} possui uma tarefa alocada.", data);

                    stream.Write(msg, 0, msg.Length);
                    Console.WriteLine("Já terminou a tarefa?", data);

                }
                //// Release the Mutex.
                //mut.ReleaseMutex();
                //Console.WriteLine("{0} has released the mutex",
                //    Thread.CurrentThread.Name);
            }
            //Console.WriteLine("{0} is leaving the protected area",
            //Thread.CurrentThread.Name);


        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
        finally
        {
            server.Stop();
        }

        Console.WriteLine("\nHit enter to continue...");
        Console.Read();

    }
}