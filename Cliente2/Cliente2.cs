using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

class Cliente
{
    private static readonly ConnectionFactory factory = new ConnectionFactory() { HostName = "localhost" };

    static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("Insira o seu ID:");
            string clientId = Console.ReadLine();

            var response = CallServer($"ID:{clientId}");

            Console.WriteLine($" [.] Recebido {response}");
            HandleServerResponse(response, clientId);
        }
    }

    private static string CallServer(string message)
    {
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            var replyQueue = channel.QueueDeclare().QueueName;
            var corrId = Guid.NewGuid().ToString();
            var props = channel.CreateBasicProperties();
            props.ReplyTo = replyQueue;
            props.CorrelationId = corrId;

            var messageBytes = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: props, body: messageBytes);

            var consumer = new EventingBasicConsumer(channel);
            var response = string.Empty;

            consumer.Received += (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == corrId)
                {
                    response = Encoding.UTF8.GetString(ea.Body.ToArray());
                }
            };

            channel.BasicConsume(consumer: consumer, queue: replyQueue, autoAck: true);

            while (string.IsNullOrEmpty(response))
            {
                // Waiting for the response
            }

            return response;
        }
    }

    private static void HandleServerResponse(string response, string clientId)
    {
        if (response.Contains("Terminou a tarefa? (Sim/Nao)"))
        {
            Console.WriteLine("Terminou a tarefa? (Sim/Nao)");
            string taskCompleted = Console.ReadLine();

            var nextResponse = CallServer(taskCompleted);
            Console.WriteLine($" [.] Recebido {nextResponse}");

            if (taskCompleted.Equals("Sim", StringComparison.InvariantCultureIgnoreCase))
            {
                if (nextResponse.Contains("Pretende alocar uma nova tarefa? (Sim/Nao)"))
                {
                    Console.WriteLine("Pretende alocar uma nova tarefa? (Sim/Nao)");
                    string allocateNewTask = Console.ReadLine();

                    nextResponse = CallServer($"Alocar:{clientId}");
                    Console.WriteLine($" [.] Recebido {nextResponse}");
                }
            }
        }
        else if (response.Contains("Pretende alocar uma nova tarefa? (Sim/Nao)"))
        {
            Console.WriteLine("Pretende alocar uma nova tarefa? (Sim/Nao)");
            string allocateNewTask = Console.ReadLine();

            var nextResponse = CallServer(allocateNewTask);
            Console.WriteLine($" [.] Recebido {nextResponse}");

            if (allocateNewTask.Equals("Sim", StringComparison.InvariantCultureIgnoreCase))
            {
                nextResponse = CallServer($"Alocar:{clientId}");
                Console.WriteLine($" [.] Recebido {nextResponse}");
            }
        }
        else
        {
            Console.WriteLine(response);
        }
    }
}