using RabbitMQ.Client;
using System;
using System.Text;

class Cliente
{
    public static void Main(string[] args)
    {
        Console.Write("Qual é o seu ID?\n");
        string ID = Console.ReadLine();

        SendMessageToQueue(ID);
    }

    static void SendMessageToQueue(string message)
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

            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "",
                                 routingKey: "cliente_fila",
                                 basicProperties: null,
                                 body: body);
            Console.WriteLine(" [x] Enviado {0}", message);
        }

        Console.WriteLine("\nPressione Enter para sair...");
        Console.ReadLine();
    }
}
