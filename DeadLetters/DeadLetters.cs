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

        channel.QueueDeclare
        (
            queue: "deadLetterQueue", 
            durable: true, 
            exclusive: false, 
            autoDelete: false, 
            arguments: null
        );

        var deadLetterconsumer = new EventingBasicConsumer(channel);
        
        deadLetterconsumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var deathReasonBytes = ea.BasicProperties.Headers["x-first-death-reason"] as byte[];
            var deathReason = Encoding.UTF8.GetString(deathReasonBytes);
            Console.WriteLine($"Deadletter: {message} => Reason: {deathReason}");
        };

        channel.BasicConsume("deadLetterQueue", true, consumer: deadLetterconsumer);

        Console.WriteLineLine("Waiting for dead letters. Press any key to exit.");
        Console.ReadLine();

        channel.Close();
        connection.Close();
    }
}