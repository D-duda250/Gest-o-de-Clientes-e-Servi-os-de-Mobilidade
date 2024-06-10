using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

class Cliente
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

            while (true)
            {
                Console.Write("Insira o seu ID: ");
                string clientId = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(clientId))
                {
                    continue;
                }

                Call($"ID:{clientId}");

                while (awaitingResponse) { }

                if (response.Contains("Terminou a tarefa? (Sim/Nao)"))
                {
                    Console.Write("Terminou a tarefa? (Sim/Nao): ");
                    string terminouTarefa = Console.ReadLine();
                    Call(terminouTarefa);

                    while (awaitingResponse) { }

                    if (response.Contains("Pretende alocar uma nova tarefa? (Sim/Nao)"))
                    {
                        Console.Write("Pretende alocar uma nova tarefa? (Sim/Nao): ");
                        string alocarTarefa = Console.ReadLine();
                        Call($"Alocar:{clientId}");

                        while (awaitingResponse) { }
                    }
                }

                if (response.Contains("Quer desassociar-se deste serviço? (Sim/Nao)"))
                {
                    Console.Write("Quer desassociar-se deste serviço? (Sim/Nao): ");
                    string desassociar = Console.ReadLine();
                    Call(desassociar);

                    while (awaitingResponse) { }

                    if (response.Contains("Deseja realmente desassociar-se deste serviço? Responda com 'Desassociar:{clientId}:Sim' ou 'Desassociar:{clientId}:Nao'"))
                    {
                        Console.Write("Deseja realmente desassociar-se deste serviço? (Desassociar:{clientId}:Sim/Desassociar:{clientId}:Nao): ");
                        string confirmacao = Console.ReadLine();
                        Call($"Desassociar:{clientId}:{confirmacao}");

                        while (awaitingResponse) { }
                    }
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