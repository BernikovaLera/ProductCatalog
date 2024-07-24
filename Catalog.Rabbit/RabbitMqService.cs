using System.Text;
using RabbitMQ.Client;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Catalog.Rabbit;

public class RabbitMqService : IRabbitMqService
{
    
    public void SendMessage(object obj)
    {
        var message = JsonSerializer.Serialize(obj);
        SendMessage(message); 
    }

    /// <summary>
    /// Создание канала рэбита
    /// </summary>
    public void SendMessage(string message)
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost"
        };
        using var connection = factory.CreateConnection(); 
        using var channel = connection.CreateModel();
        channel.QueueDeclare(queue: "MyQueue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchange: string.Empty,
            routingKey: "MyQueue",
            basicProperties: null,
            body: body);
        Console.WriteLine($" [x] Sent {message}");

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
}