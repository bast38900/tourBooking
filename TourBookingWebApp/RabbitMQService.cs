using RabbitMQ.Client;
using System.Text;
using System.Collections.Generic;

public class RabbitMqService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqService()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare the exchanges
        _channel.ExchangeDeclare
        (
            exchange: "tourBookingExchange", 
            type: ExchangeType.Topic
        );

        _channel.ExchangeDeclare
        (
            exchange: "deadLetterExchange", 
            type: ExchangeType.Direct
        );

        // Declare the dead letter queue and bind it to the dead letter exchange with the routing key "dead-letter"
        _channel.QueueDeclare
        (
            queue: "deadLetterQueue", 
            durable: true, 
            exclusive: false, 
            autoDelete: false, 
            arguments: null
        );

        _channel.QueueBind
        (
            queue: "deadLetterQueue", 
            exchange: "deadLetterExchange", 
            routingKey: "dead-letter"
        );

        // Declare the email queue and bind it to the exchange with the routing key "tours.booked" and dead letter exchange
        var emailQueueArguments = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "deadLetterExchange" },
            { "x-dead-letter-routing-key", "dead-letter" },
            { "x-message-ttl", 5000 } // TTL of 5 seconds
        };

        _channel.QueueDeclare
        (
            queue: "emailServiceQueue", 
            exclusive: false, 
            autoDelete: false, 
            arguments: emailQueueArguments
        );

        _channel.QueueBind
        (
            queue: "emailServiceQueue", 
            exchange: "tourBookingExchange", 
            routingKey: "tours.booked"
        );

        // Declare the backoffice queue and bind it to the exchange with the routing key "tours.*" and dead letter exchange
        var backofficeQueueArguments = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "deadLetterExchange" },
            { "x-dead-letter-routing-key", "dead-letter" },
            { "x-message-ttl", 5000 } // TTL of 5 seconds
        };

        _channel.QueueDeclare
        (
            queue: "backOfficeQueue", 
            durable: true, 
            exclusive: false, 
            autoDelete: false, 
            arguments: backofficeQueueArguments
        );

        _channel.QueueBind
        (
            queue: "backOfficeQueue", 
            exchange: "tourBookingExchange",
            routingKey: "tours.*"
        );
    }

    // Send messages to RabbitMQ
    public void SendMessage(string message, string action)
    {
        var routingKey = $"tours.{action}";

        var body = Encoding.UTF8.GetBytes(message);
        
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish
        (
            exchange: "tourBookingExchange", 
            routingKey: routingKey, 
            basicProperties: properties, 
            body: body
        );

        Console.WriteLine($" [x] Sent: '{routingKey}':'{message}'");
    }

    // Dispose the connection and channel
    public void Dispose()
    {
        _channel.Close();
        _connection.Close();
    }
}