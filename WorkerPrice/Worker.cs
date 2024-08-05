using Catalog.Data;
using EasyNetQ;
using Microsoft.Extensions.Hosting;
using MimeKit;
using MailKit.Net.Smtp;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace WorkerPrice;

public class Worker (IBus bus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await bus.PubSub.SubscribeAsync<Price>("MyQueue", HandlePriceChangeMessage, cancellationToken: stoppingToken);
    }

    private async Task HandlePriceChangeMessage(Price message)
    {
        // Формируем email-сообщение
        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress("Sender Name", "sender@example.com"));
        emailMessage.To.Add(new MailboxAddress("Recipient Name", "recipient@example.com"));
        emailMessage.Subject = "Изменение цены";
        emailMessage.Body = new TextPart("plain")
        {
            Text = $"{message.Cost}\n" +
                   $"{message.StartDate}\n" +
                   $"{message.EndDate}"
        };

        // Отправка email
        using (var client = new SmtpClient())
        {
            client.Connect("localhost", 25, false); // Порт по умолчанию для MailHog
            client.Send(emailMessage);
            client.Disconnect(true);
        }
    }
}