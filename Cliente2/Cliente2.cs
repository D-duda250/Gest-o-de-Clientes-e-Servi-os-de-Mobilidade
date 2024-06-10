using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;

class Cliente
{
    private static string AskQuestion(string question)
    {
        Console.WriteLine(question);
        return Console.ReadLine();
    }

    public static void Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            var replyQueueName = channel.QueueDeclare().QueueName;
            var consumer = new EventingBasicConsumer(channel);
            string response = null;
            var correlationId = Guid.NewGuid().ToString();
            var props = channel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var responseCorrId = ea.BasicProperties.CorrelationId;
                if (responseCorrId == correlationId)
                {
                    response = Encoding.UTF8.GetString(body);
                    Console.WriteLine($" [.] Recebido {response}");
                }
            };

            channel.BasicConsume(consumer: consumer, queue: replyQueueName, autoAck: true);

            var message = AskQuestion("Digite o comando (ID:<id> para associar, Desassociar:<id> para desassociar):");
            var messageBytes = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: props, body: messageBytes);

            while (response == null)
            {
                Thread.Sleep(100);
            }

            if (response.Contains("Terminou a tarefa? (Sim/Nao)"))
            {
                var answer = AskQuestion("Terminou a tarefa? (Sim/Nao)");
                messageBytes = Encoding.UTF8.GetBytes($"{answer}:{message.Substring(3)}");
                response = null;
                channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: props, body: messageBytes);

                while (response == null)
                {
                    Thread.Sleep(100);
                }
                Console.WriteLine($" [.] Recebido {response}");
            }

            if (!response.Contains("O ClienteID"))
            {
                Console.WriteLine("Digite o comando para desassociar (Desassociar:<id>):");
                var desassociarCommand = Console.ReadLine();
                messageBytes = Encoding.UTF8.GetBytes(desassociarCommand);
                response = null;
                channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: props, body: messageBytes);

                while (response == null)
                {
                    Thread.Sleep(100);
                }
                Console.WriteLine($" [.] Recebido {response}");
            }
        }
    }
}