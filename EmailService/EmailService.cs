using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

class Program
{
  static void Main(string[] args)
  {
    var factory = new ConnectionFactory() { HostName = "localhost" };
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    // Declare the dead letter exchange and queue and bind the queue to the exchange with the routing key "dead-letter"
    var arguments = new Dictionary<string, object>
    {
      { "x-dead-letter-exchange", "deadLetterExchange" },
      { "x-dead-letter-routing-key", "dead-letter" },
      { "x-message-ttl", 5000 }
    };

    channel.QueueDeclare
    (
      queue: "emailServiceQueue", 
      exclusive: false, 
      autoDelete: false, 
      arguments: arguments
    );

    // create a consumer to receive messages from the email queue
    var emailConsumer = new EventingBasicConsumer(channel);

    emailConsumer.Received += (model, ea) =>
    {
      var body = ea.Body.ToArray();
      var message = Encoding.UTF8.GetString(body);
    
      try
      {
        // Check if the email contains '@'
        if (!message.Contains("@"))
        {
          throw new Exception("Invalid email format");
        }

        // Receive, process and acknowledge the message
        Console.WriteLine("Received: {0}", message);
        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
      }
      catch (Exception ex)
      {
        // Log the error and publish the message to the dead letter exchange
        Console.WriteLine("Error processing: {0} || Sending to deadLetterExchange", ex.Message);

        channel.BasicPublish
        (
          exchange: "deadLetterExchange", 
          routingKey: "dead-letter", 
          basicProperties: null, 
          body: body
        );
      
        // Reject the message and requeue it to the dead letter exchange
        channel.BasicReject(deliveryTag: ea.DeliveryTag, requeue: false);
      }
    };

    channel.BasicConsume
    (
      queue: "emailServiceQueue", 
      autoAck: false, 
      consumer: emailConsumer
    );

    // Keep the application running to listen for messages
    Console.WriteLine(" Welcome to BackOffice:\n\n Press [enter] to exit.");
    Console.ReadLine();
  }
}