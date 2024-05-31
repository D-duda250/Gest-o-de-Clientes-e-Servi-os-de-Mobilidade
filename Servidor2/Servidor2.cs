using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

class Servidor
{
    private static Mutex mut = new Mutex();
    private const int numThreads = 3;

    static void Main(string[] args)
    {
        for (int i = 0; i < numThreads; i++)
        {
            Thread newThread = new Thread(new ThreadStart(Server));
            newThread.Name = string.Format("Thread{0}", i + 1);
            newThread.Start();
        }
    }

    static void Server()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "cliente_fila",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                mut.WaitOne();
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" [x] Recebido {0}", message);

                    // Processar mensagem
                    ProcessMessage(message);
                }
                finally
                {
                    mut.ReleaseMutex();
                }
            };

            channel.BasicConsume(queue: "cliente_fila",
                                 autoAck: true,
                                 consumer: consumer);

            Console.WriteLine(" Pressione [enter] para sair.");
            Console.ReadLine();
        }
    }

    static void ProcessMessage(string message)
    {
        // Aqui você pode adicionar a lógica de processamento da mensagem
        // que anteriormente estava dentro do loop de leitura do stream no servidor.
        List<ClienteServicoAssociation> associations = CarregarAssociacoesCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv");
        string clienteID = message;
        string servicoAssociado = VerificarServicoAssociado(associations, message);

        if (servicoAssociado == null)
        {
            int sA = 0;
            int sB = 0;
            int sC = 0;
            int sD = 0;

            foreach (var ID in associations)
            {
                if (ID.ServicoID == "Servico_A") sA++;
                else if (ID.ServicoID == "Servico_B") sB++;
                else if (ID.ServicoID == "Servico_C") sC++;
                else if (ID.ServicoID == "Servico_D") sD++;
            }

            string servicoMenosIDs = null;
            int menorNumeroIDs = int.MaxValue;

            if (sA < menorNumeroIDs) { menorNumeroIDs = sA; servicoMenosIDs = "Servico_A"; }
            if (sB < menorNumeroIDs) { menorNumeroIDs = sB; servicoMenosIDs = "Servico_B"; }
            if (sC < menorNumeroIDs) { menorNumeroIDs = sC; servicoMenosIDs = "Servico_C"; }
            if (sD < menorNumeroIDs) { menorNumeroIDs = sD; servicoMenosIDs = "Servico_D"; }

            int totalIDs = sA + sB + sC + sD + 1;

            if (servicoMenosIDs != null)
            {
                AdicionarNovoID("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv", servicoMenosIDs, totalIDs);
                Console.WriteLine($"O ClienteID '{totalIDs}' está associado ao Servico '{servicoMenosIDs}'.");
            }
        }
    }

    static List<ClienteServicoAssociation> CarregarAssociacoesCSV(string filePath)
    {
        List<ClienteServicoAssociation> associations = new List<ClienteServicoAssociation>();
        using (StreamReader reader = new StreamReader(filePath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(',');
                if (parts.Length == 2)
                {
                    string clienteID = parts[0].Trim();
                    string servicoID = parts[1].Trim();
                    associations.Add(new ClienteServicoAssociation(clienteID, servicoID));
                }
            }
        }
        return associations;
    }

    static string VerificarServicoAssociado(List<ClienteServicoAssociation> associations, string clienteID)
    {
        ClienteServicoAssociation association = associations.FirstOrDefault(a => a.ClienteID == clienteID);
        return association != null ? association.ServicoID : null;
    }

    static void AdicionarNovoID(string filePath, string servicoMenosIDs, int novoID)
    {
        mut.WaitOne();
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine($"{servicoMenosIDs},{novoID}");
            }
        }
        finally
        {
            mut.ReleaseMutex();
        }
    }

    class ClienteServicoAssociation
    {
        public string ClienteID { get; set; }
        public string ServicoID { get; set; }

        public ClienteServicoAssociation(string clienteID, string servicoID)
        {
            ClienteID = clienteID;
            ServicoID = servicoID;
        }
    }
}