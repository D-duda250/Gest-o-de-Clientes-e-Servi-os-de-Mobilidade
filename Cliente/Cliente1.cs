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
        string msg = null;

        Connect("127.0.0.1", ID);
        static void Connect(string server, string ID)
        {
            try
            {
                int port = 13000;

                using TcpClient client = new TcpClient(server, port);
                NetworkStream stream = client.GetStream();

                byte[] data = Encoding.ASCII.GetBytes(ID);

                // Envie o ID para o servidor
                stream.Write(data, 0, data.Length);
                //Console.WriteLine("Mensagem enviada: {0}", ID);

                // Loop para receber e processar as mensagens do servidor
                while (true)
                {
                    data = new byte[1024];
                    int bytesReceived = stream.Read(data, 0, data.Length);
                    string respostaData = Encoding.ASCII.GetString(data, 0, bytesReceived);


                    // Verifique se a mensagem recebida é "400 OK"
                    if (respostaData.Trim() == "400 BYE")
                    {
                        Console.WriteLine("BYE. Encerrando a conexão.");
                        break; // Saia do loop
                    }

                    // Se não for "400 OK", envie outra mensagem (se necessário)
                    // Aqui você pode adicionar lógica para enviar mensagens adicionais se desejado
                    Console.WriteLine("{0}", respostaData);
                    string respostaMsg = Console.ReadLine();
                    byte[] responseBytes = Encoding.ASCII.GetBytes(respostaMsg);
                    stream.Write(responseBytes, 0, responseBytes.Length);


                }

                // Feche o stream e o cliente após terminar de enviar e receber mensagens
                stream.Close();
                client.Close();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            Console.WriteLine("\nPressione Enter para continuar...");
            Console.Read();
        }
    }
}

