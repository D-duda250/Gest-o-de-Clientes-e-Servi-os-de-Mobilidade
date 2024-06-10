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
    private static readonly string baseDirectory = @"C:\Users\Duarte Oliveira\source\repos\SDTP1\SDTP1";
    private static readonly string desassociadosFilePath = Path.Combine(baseDirectory, "Desassociados.csv");

    static void Main(string[] args)
    {
        for (int i = 0; i < numThreads; i++)
        {
            Thread newThread = new Thread(new ThreadStart(Server));
            newThread.Name = string.Format("Thread{0}", i + 1);
            newThread.Start();
        }
    }

    static string GetFilePath(string servicoID)
    {
        return Path.Combine(baseDirectory, $"{servicoID}.csv");
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
        var parts = message.Split(':');
        var action = parts[0];
        var clientID = parts.Length > 1 ? parts[1] : null;

        switch (action)
        {
            case "ID":
                return HandleClientID(clientID);
            case "Sim":
            case "Nao":
                return HandleClientResponse(action, clientID);
            case "Alocar":
                return HandleAlocarRequest(clientID);
            case "Desassociar":
                return HandleDesassociar(clientID, parts.Length > 2 && parts[2] == "Sim");
            case "Admin":
                return HandleAdminAuthentication(clientID, parts.Length > 2 ? parts[2] : null);
            case "CriarTarefa":
                if (IsAdmin(clientID))
                {
                    return HandleCriarTarefa(parts);
                }
                return "Permissão negada.";
            case "CriarCliente":
                if (IsAdmin(clientID))
                {
                    return HandleCriarCliente(parts);
                }
                return "Permissão negada.";
            default:
                return "Comando não reconhecido.";
        }
    }

    static bool IsAdmin(string clientID)
    {
        return clientID.StartsWith("A");
    }

    static string HandleClientID(string clientID)
    {
        var associations = CarregarAssociacoesCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv");
        string servicoAssociado = VerificarServicoAssociado(associations, clientID);

        if (servicoAssociado == null)
        {
            servicoAssociado = AtribuirNovoServico(associations, clientID);
        }

        if (servicoAssociado != null)
        {
            var tarefas = CarregarTarefasCSV(servicoAssociado);
            string ultimoEstado = VerificarEstadoTarefa(tarefas, clientID);
            int tarefasDisponiveis = tarefas.Count(t => t.Estado == "Nao alocado");

            var response = $"O ClienteID '{clientID}' está associado ao Servico '{servicoAssociado}'.\n";
            response += $"Este serviço tem {tarefasDisponiveis} tarefas disponíveis.\n";
            if (ultimoEstado != null)
            {
                response += $"A última tarefa associada ao ClienteID '{clientID}' possui o estado: {ultimoEstado}.\n";
                if (ultimoEstado == "Em curso")
                {
                    response += "Terminou a tarefa? (Sim/Nao)";
                }
                else
                {
                    response += "Quer desassociar-se deste serviço? (Sim/Nao)";
                }
            }

            return response;
        }

        return "Não foi encontrada nenhuma tarefa associada ao ClienteID.";
    }

    static string HandleClientResponse(string response, string clientID)
    {
        if (response == "Sim")
        {
            var servicoAssociado = VerificarServicoAssociado(CarregarAssociacoesCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv"), clientID);
            var tarefas = CarregarTarefasCSV(servicoAssociado);
            var tarefaAtual = tarefas.FirstOrDefault(t => t.ClienteID == clientID && t.Estado == "Em curso");

            if (tarefaAtual != null)
            {
                tarefaAtual.Estado = "Concluido";
                AtualizarTarefas(servicoAssociado, tarefas);
                return "Pretende alocar uma nova tarefa? (Sim/Nao)";
            }
            else
            {
                return $"Deseja realmente desassociar-se deste serviço? Responda com 'Desassociar:{clientID}:Sim' ou 'Desassociar:{clientID}:Nao'";
            }
        }
        else if (response == "Nao")
        {
            return "Insira o seu ID:";
        }

        return "Comando não reconhecido.";
    }

    static string HandleAlocarRequest(string clientID)
    {
        var servicoAssociado = VerificarServicoAssociado(CarregarAssociacoesCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv"), clientID);
        var tarefas = CarregarTarefasCSV(servicoAssociado);
        var novaTarefa = tarefas.FirstOrDefault(t => t.Estado == "Nao alocado");

        if (novaTarefa != null)
        {
            novaTarefa.ClienteID = clientID;
            novaTarefa.Estado = "Em curso";
            AtualizarTarefas(servicoAssociado, tarefas);
            return $"ClienteID '{clientID}' alocado à tarefa '{novaTarefa.TarefaID}' - '{novaTarefa.Descricao}'.";
        }

        return "Não existem mais tarefas de momento.";
    }

    static string HandleDesassociar(string clienteID, bool confirmar)
    {
        if (!confirmar)
        {
            return "Insira o seu ID:";
        }

        var associations = CarregarAssociacoesCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv");
        var servicoAssociado = VerificarServicoAssociado(associations, clienteID);
        var tarefas = CarregarTarefasCSV(servicoAssociado);
        var estadoTarefa = VerificarEstadoTarefa(tarefas, clienteID);

        if (estadoTarefa == "Em curso")
        {
            return $"O ClienteID '{clienteID}' não pode se desassociar pois possui uma tarefa em curso.";
        }

        // Adicionar o cliente ao arquivo de desassociados
        AdicionarClienteDesassociado(clienteID);

        // Remover o cliente da lista de associações
        RemoverID("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv", clienteID);
        return $"O ClienteID '{clienteID}' foi desassociado do serviço '{servicoAssociado}'.";
    }

    static string HandleCriarTarefa(string[] parts)
    {
        if (parts.Length < 5)
        {
            return "Formato da mensagem inválido. Use: CriarTarefa:<servicoID>:<tarefaID>:<descricao>";
        }

        string servicoID = parts[1];
        string tarefaID = parts[2];
        string descricao = parts[3];
        string estado = "Nao alocado";
        string clienteID = "";

        string filePath = GetFilePath(servicoID);
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine($"{tarefaID},{descricao},{estado},{clienteID}");
        }

        return $"Tarefa '{tarefaID}' criada com sucesso no serviço '{servicoID}'.";
    }

    static string HandleCriarCliente(string[] parts)
    {
        if (parts.Length < 3)
        {
            return "Formato da mensagem inválido. Use: CriarCliente:<clientID>:<servicoID>";
        }

        string clientID = parts[1];
        string servicoID = parts[2];

        string filePath = "C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv";
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine($"{clientID},{servicoID}");
        }

        return $"Cliente '{clientID}' criado com sucesso no serviço '{servicoID}'.";
    }

    static string HandleAdminAuthentication(string adminID, string password)
    {
        var adminAssociations = CarregarAssociacoesCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Admin.csv");
        var adminAssociation = adminAssociations.FirstOrDefault(a => a.ClienteID == adminID);

        if (adminAssociation != null && password == "admin_password") // Substitua "admin_password" pelo método real de verificação de senha
        {
            return $"Admin '{adminID}' autenticado com sucesso.";
        }

        return "Falha na autenticação do admin.";
    }

    static void AdicionarClienteDesassociado(string clienteID)
    {
        using (StreamWriter writer = new StreamWriter(desassociadosFilePath, true))
        {
            writer.WriteLine(clienteID);
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
                if (parts.Length == 4)
                {
                    string tarefaID = parts[0].Trim();
                    string descricao = parts[1].Trim();
                    string estado = parts[2].Trim();
                    string clienteID = parts[3].Trim();
                    tarefas.Add(new Tarefa(tarefaID, descricao, estado, clienteID));
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

    static void RemoverID(string filePath, string clienteID)
    {
        var associations = CarregarAssociacoesCSV(filePath);
        var updatedAssociations = associations.Where(a => a.ClienteID != clienteID).ToList();
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var assoc in updatedAssociations)
            {
                writer.WriteLine($"{assoc.ClienteID},{assoc.ServicoID}");
            }
        }
    }

    static void AtualizarTarefas(string servicoID, List<Tarefa> tarefas)
    {
        string filePath = GetFilePath(servicoID);
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var tarefa in tarefas)
            {
                writer.WriteLine($"{tarefa.TarefaID},{tarefa.Descricao},{tarefa.Estado},{tarefa.ClienteID}");
            }
        }
    }

    static string AtribuirNovoServico(List<ClienteServicoAssociation> associations, string clientID)
    {
        int sA = 0, sB = 0, sC = 0, sD = 0;

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

        if (servicoMenosIDs != null)
        {
            AdicionarNovoID("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv", clientID, servicoMenosIDs);
            return servicoMenosIDs;
        }

        return null;
    }

    static void AdicionarNovoID(string filePath, string clienteID, string servicoMenosIDs)
    {
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine($"{clienteID},{servicoMenosIDs}");
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