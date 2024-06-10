using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

class Admin1
{
    private static IConnection connection;
    private static IModel channel;
    private static string replyQueueName;
    private static EventingBasicConsumer consumer;
    private static string correlationId;
    private static string response;
    private static bool awaitingResponse;

    static void Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };

        using (connection = factory.CreateConnection())
        using (channel = connection.CreateModel())
        {
            replyQueueName = channel.QueueDeclare().QueueName;
            consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var responseMessage = Encoding.UTF8.GetString(body);
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    response = responseMessage;
                    awaitingResponse = false;
                    Console.WriteLine(" [.] Recebido {0}", response);
                }
            };

            channel.BasicConsume(
                consumer: consumer,
                queue: replyQueueName,
                autoAck: true);

            // Autenticação do Admin
            Console.Write("Admin ID: ");
            string adminId = Console.ReadLine();
            Console.Write("Password: ");
            string password = Console.ReadLine();

            Call($"Admin:{adminId}:{password}");

            while (awaitingResponse) { }

            if (response.Contains("autenticado com sucesso"))
            {
                Console.WriteLine("Admin autenticado com sucesso.");
            }
            else
            {
                Console.WriteLine("Falha na autenticação do admin.");
                return;
            }

            while (true)
            {
                Console.WriteLine("Admin: Insira o comando desejado:");
                Console.WriteLine("1. Criar nova tarefa");
                Console.WriteLine("2. Criar novo cliente");
                Console.WriteLine("3. Sair");
                string comando = Console.ReadLine();

                if (comando == "3")
                {
                    break;
                }

                switch (comando)
                {
                    case "1":
                        Console.Write("ID do serviço: ");
                        string servicoID = Console.ReadLine();
                        Console.Write("ID da tarefa: ");
                        string tarefaID = Console.ReadLine();
                        Console.Write("Descrição da tarefa: ");
                        string descricao = Console.ReadLine();
                        Call($"CriarTarefa:{servicoID}:{tarefaID}:{descricao}");
                        while (awaitingResponse) { }
                        Console.WriteLine(response);
                        break;

                    case "2":
                        Console.Write("Novo ID do cliente: ");
                        string novoClienteID = Console.ReadLine();
                        Console.Write("ID do serviço: ");
                        string novoServicoID = Console.ReadLine();
                        Call($"CriarCliente:{novoClienteID}:{novoServicoID}");
                        while (awaitingResponse) { }
                        Console.WriteLine(response);
                        break;

                    default:
                        Console.WriteLine("Comando inválido.");
                        break;
                }
            }
        }
    }

    private static void Call(string message)
    {
        var props = channel.CreateBasicProperties();
        correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueueName;

        var messageBytes = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(
            exchange: "",
            routingKey: "rpc_queue",
            basicProperties: props,
            body: messageBytes);

        awaitingResponse = true;
    }
}