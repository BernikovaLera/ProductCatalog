using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Catalog.Rabbit;

public class RabbitMqService : IRabbitMqService
{
    public void SendMessage(object obj)
    {
        var message = JsonSerializer.Serialize(obj);
        SendMessage(message);
    }

    public void SendMessage(string message)
    {
        var factory = new ConnectionFactory
        {
            HostName = "http://localhost:15672/#/",
            UserName = "guest",
            Password = "guest",
            Port = 15672
        };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(queue: "MyQueue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchange: "",
            routingKey: "MyQueue",
            basicProperties: null,
            body: body);
    }
}