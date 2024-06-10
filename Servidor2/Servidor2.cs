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
                var body = ea.Body.ToArray();
                var props = ea.BasicProperties;
                var replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                string response = null;

                try
                {
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" [.] Recebido ({0})", message);
                    response = ProcessMessage(message, channel, props);
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

    static string ProcessMessage(string message, IModel channel, IBasicProperties props)
    {
        if (message.StartsWith("ID:"))
        {
            var clienteID = message.Substring(3).Trim();
            List<ClienteServicoAssociation> associations = CarregarAssociacoesCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv");
            string servicoAssociado = VerificarServicoAssociado(associations, clienteID);

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
                    return $"O ClienteID '{totalIDs}' está associado ao Servico '{servicoMenosIDs}'.";
                }
            }

            // Se o cliente já tem um serviço associado
            if (!string.IsNullOrEmpty(servicoAssociado))
            {
                string result = $"O ClienteID '{clienteID}' está associado ao Servico '{servicoAssociado}'." + Environment.NewLine;

                List<Tarefa> tarefas = CarregarTarefasCSV($"C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\{servicoAssociado}.csv");
                string estadoTarefa = VerificarEstadoTarefa(tarefas, clienteID);
                string ultimoEstado = null;
                Tarefa proximaTarefaNaoAlocada = null;

                foreach (var tarefa in tarefas)
                {
                    if (tarefa.ClienteID == clienteID)
                    {
                        ultimoEstado = tarefa.Estado;
                    }

                    if (tarefa.Estado == "Nao alocado" && proximaTarefaNaoAlocada == null)
                    {
                        proximaTarefaNaoAlocada = tarefa;
                    }
                }

                if (ultimoEstado != null)
                {
                    result += $"A última tarefa associada ao ClienteID '{clienteID}' possui o estado: {ultimoEstado}." + Environment.NewLine;

                    if (ultimoEstado == "Concluido")
                    {
                        result += "Pretende alocar uma nova tarefa? (Sim/Nao)";
                    }
                    else if (ultimoEstado == "Em curso")
                    {
                        result += "Terminou a tarefa? (Sim/Nao)";
                    }
                }
                else
                {
                    result += "Não foi encontrada nenhuma tarefa associada ao ClienteID.";
                }

                return result;
            }
        }
        else if (message.Equals("Sim", StringComparison.InvariantCultureIgnoreCase) && props.ReplyTo != null)
        {
            var response = "Pretende alocar uma nova tarefa? (Sim/Nao)";
            return response;
        }
        else if (message.Equals("Nao", StringComparison.InvariantCultureIgnoreCase))
        {
            return "Ok. Sem novas tarefas no momento.";
        }
        else if (message.StartsWith("Alocar:"))
        {
            var clienteID = message.Substring(7).Trim();
            List<ClienteServicoAssociation> associations = CarregarAssociacoesCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv");
            string servicoAssociado = VerificarServicoAssociado(associations, clienteID);

            if (!string.IsNullOrEmpty(servicoAssociado))
            {
                List<Tarefa> tarefas = CarregarTarefasCSV($"C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\{servicoAssociado}.csv");
                var proximaTarefaNaoAlocada = tarefas.FirstOrDefault(t => t.Estado == "Nao alocado");

                if (proximaTarefaNaoAlocada != null)
                {
                    proximaTarefaNaoAlocada.Estado = "Em curso";
                    proximaTarefaNaoAlocada.ClienteID = clienteID;
                    AtualizarTarefas($"C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\{servicoAssociado}.csv", tarefas);

                    return $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                }
                else
                {
                    return "Não existem mais tarefas de momento.";
                }
            }
        }

        return "Mensagem não reconhecida.";
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

    static List<Tarefa> CarregarTarefasCSV(string filePath)
    {
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

    static string VerificarEstadoTarefa(List<Tarefa> tarefas, string clienteID)
    {
        Tarefa tarefa = tarefas.FirstOrDefault(t => t.ClienteID == clienteID);
        return tarefa != null ? tarefa.Estado : null;
    }

    static void AtualizarTarefas(string filePath, List<Tarefa> tarefas)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("TarefaID,Descricao,Estado,ClienteID");
            foreach (var tarefa in tarefas)
            {
                writer.WriteLine($"{tarefa.TarefaID},{tarefa.Descricao},{tarefa.Estado},{tarefa.ClienteID}");
            }
        }
    }

    static void AdicionarNovoID(string filePath, string servicoMenosIDs, int novoID)
    {
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine($"{servicoMenosIDs},{novoID}");
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