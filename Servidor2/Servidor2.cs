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
    private static ConnectionFactory factory = new ConnectionFactory() { HostName = "localhost" };
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
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "rpc_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                string response = null;
                var body = ea.Body.ToArray();
                var props = ea.BasicProperties;
                var replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                try
                {
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" [.] Recebido ({0})", message);
                    response = ProcessMessage(message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(" [.] " + e.Message);
                    response = "";
                }
                finally
                {
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            };

            channel.BasicConsume(queue: "rpc_queue", autoAck: false, consumer: consumer);
            Console.WriteLine(" Pressione [enter] para sair.");
            Console.ReadLine();
        }
    }

    static string ProcessMessage(string message)
    {
        List<ClienteServicoAssociation> associations = CarregarAssociacoesCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv");

        if (message.StartsWith("ID:"))
        {
            string clienteID = message.Substring(3);
            string servicoAssociado = VerificarServicoAssociado(associations, clienteID);

            if (servicoAssociado == null)
            {
                int sA = 0, sB = 0, sC = 0, sD = 0;
                foreach (var assoc in associations)
                {
                    if (assoc.ServicoID == "Servico_A") sA++;
                    else if (assoc.ServicoID == "Servico_B") sB++;
                    else if (assoc.ServicoID == "Servico_C") sC++;
                    else if (assoc.ServicoID == "Servico_D") sD++;
                }

                string servicoMenosIDs = new[] { (sA, "Servico_A"), (sB, "Servico_B"), (sC, "Servico_C"), (sD, "Servico_D") }.OrderBy(t => t.Item1).First().Item2;
                int totalIDs = sA + sB + sC + sD + 1;

                AdicionarNovoID("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv", servicoMenosIDs, totalIDs);
                servicoAssociado = servicoMenosIDs;
            }

            var tarefas = CarregarTarefasCSV(servicoAssociado);
            var tarefasDisponiveis = tarefas.Count(t => t.Estado == "Nao alocado");
            var estadoUltimaTarefa = tarefas.LastOrDefault(t => t.ClienteID == clienteID)?.Estado;

            var resposta = $"O ClienteID '{clienteID}' está associado ao Servico '{servicoAssociado}'. Este serviço tem {tarefasDisponiveis} tarefas disponíveis.\n";
            if (estadoUltimaTarefa != null)
            {
                resposta += $"A última tarefa associada ao ClienteID '{clienteID}' possui o estado: {estadoUltimaTarefa}.\n";
                if (estadoUltimaTarefa == "Em curso")
                {
                    resposta += "Terminou a tarefa? (Sim/Nao)";
                }
            }
            else
            {
                resposta += "Não foi encontrada nenhuma tarefa associada ao ClienteID.";
            }

            return resposta;
        }
        else if (message.StartsWith("Desassociar:"))
        {
            string clienteID = message.Substring(12);
            string servicoAssociado = VerificarServicoAssociado(associations, clienteID);

            if (servicoAssociado != null)
            {
                var tarefas = CarregarTarefasCSV(servicoAssociado);
                var tarefaEmCurso = tarefas.Any(t => t.ClienteID == clienteID && t.Estado == "Em curso");

                if (tarefaEmCurso)
                {
                    return $"O ClienteID '{clienteID}' não pode ser desassociado porque possui uma tarefa em curso.";
                }
                else
                {
                    RemoverID("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv", clienteID);
                    return $"O ClienteID '{clienteID}' foi desassociado do Servico '{servicoAssociado}'.";
                }
            }
            else
            {
                return $"O ClienteID '{clienteID}' não está associado a nenhum serviço.";
            }
        }
        else if (message.StartsWith("Sim:"))
        {
            string clienteID = message.Substring(4);
            string servicoAssociado = VerificarServicoAssociado(associations, clienteID);
            var tarefas = CarregarTarefasCSV(servicoAssociado);
            var ultimaTarefa = tarefas.LastOrDefault(t => t.ClienteID == clienteID && t.Estado == "Em curso");

            if (ultimaTarefa != null)
            {
                ultimaTarefa.Estado = "Concluido";
                AtualizarTarefas(servicoAssociado, tarefas);
            }

            var proximaTarefaNaoAlocada = tarefas.FirstOrDefault(t => t.Estado == "Nao alocado");
            if (proximaTarefaNaoAlocada != null)
            {
                proximaTarefaNaoAlocada.ClienteID = clienteID;
                proximaTarefaNaoAlocada.Estado = "Em curso";
                AtualizarTarefas(servicoAssociado, tarefas);
                return $"ClienteID '{clienteID}' alocado à próxima tarefa '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
            }
            else
            {
                return "Não existem mais tarefas de momento.";
            }
        }

        return "Comando inválido.";
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

    static List<Tarefa> CarregarTarefasCSV(string servicoID)
    {
        string filePath = GetFilePath(servicoID);
        List<Tarefa> tarefas = new List<Tarefa>();
        using (StreamReader reader = new StreamReader(filePath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(',');
                if (parts.Length >= 4)
                {
                    tarefas.Add(new Tarefa(parts[0].Trim(), parts[1].Trim(), parts[2].Trim(), parts[3].Trim()));
                }
            }
        }
        return tarefas;
    }

    static string VerificarServicoAssociado(List<ClienteServicoAssociation> associations, string clienteID)
    {
        ClienteServicoAssociation association = associations.FirstOrDefault(a => a.ClienteID == clienteID);
        return association != null ? association.ServicoID : null;
    }

    static void AdicionarNovoID(string filePath, string servicoMenosIDs, int novoID)
    {
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine($"{servicoMenosIDs},{novoID}");
        }
    }

    static void RemoverID(string filePath, string clienteID)
    {
        var associations = CarregarAssociacoesCSV(filePath);
        var updatedAssociations = associations.Where(a => a.ClienteID != clienteID).ToList();
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var assoc in updatedAssociations)
            {
                writer.WriteLine($"{assoc.ServicoID},{assoc.ClienteID}");
            }
        }
    }

    static void AtualizarTarefas(string servicoID, List<Tarefa> tarefas)
    {
        string filePath = GetFilePath(servicoID);
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("TarefaID,Descricao,Estado,ClienteID");
            foreach (var tarefa in tarefas)
            {
                writer.WriteLine($"{tarefa.TarefaID},{tarefa.Descricao},{tarefa.Estado},{tarefa.ClienteID}");
            }
        }
    }

    static string GetFilePath(string servicoID)
    {
        string baseDirectory = @"C:\Users\Duarte Oliveira\source\repos\SDTP1\SDTP1";
        return Path.Combine(baseDirectory, $"{servicoID}.csv");
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

    class Tarefa
    {
        public string TarefaID { get; set; }
        public string Descricao { get; set; }
        public string Estado { get; set; }
        public string ClienteID { get; set; }

        public Tarefa(string tarefaID, string descricao, string estado, string clienteID)
        {
            TarefaID = tarefaID;
            Descricao = descricao;
            Estado = estado;
            ClienteID = clienteID;
        }
    }
}