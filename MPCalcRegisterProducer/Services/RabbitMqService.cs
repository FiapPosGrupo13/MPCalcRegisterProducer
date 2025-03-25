using MPCalcRegisterProducer.Services;
using RabbitMQ.Client;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

public class RabbitMqService : IRabbitMqService
{
    private readonly string _routingKey;
    private readonly string _queueName;
    private readonly string _hostName;
    private readonly string _user;
    private readonly string _password;
    private readonly int _port;
    private readonly string _dlqName;
    private readonly string _dlxName;

    public RabbitMqService(IConfiguration configuration)
    {
        _routingKey = configuration["RabbitMQ:RoutingKey"] ?? "default_route";
        _queueName = configuration["RabbitMQ:QueueName"] ?? "default_queue";
        _hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
        _port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
        _user = configuration["RabbitMQ:User"] ?? "guest";
        _password = configuration["RabbitMQ:Password"] ?? "guest";
        _dlqName = configuration["RabbitMQ:DLQName"] ?? $"{_queueName}.error"; // Ex.: mpcalc-register-queue-dlq
        _dlxName = configuration["RabbitMQ:DLXName"] ?? $"{_queueName}.dlx"; // Exchange para DLQ
    }

    protected virtual async Task<IConnection> CreateConnectionAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _hostName,
            Port = _port,
            UserName = _user,
            Password = _password
        };

        return await factory.CreateConnectionAsync();
    }

    public async Task PublishMessageAsync(string message)
    {
        await using var connection = await CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Declara o Dead Letter Exchange (DLX)
        await channel.ExchangeDeclareAsync(
            exchange: "topic_exchange",
            type: "topic",
            durable: true,
            autoDelete: false);

        // Declara a Dead Letter Queue (DLQ)
        await channel.QueueDeclareAsync(
            queue: _dlqName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Vincula a DLQ ao DLX
        await channel.QueueBindAsync(
            queue: _dlqName,
            exchange: "topic_exchange",
            routingKey: "contract.*.error"); // Usa o nome da DLQ como routing key

        // Declara a fila principal com os argumentos de DLQ
        await channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true, // Recomendado para DLQ
            exclusive: false,
            autoDelete: false);
        
        await channel.QueueBindAsync(
            queue: _queueName,
            exchange: "topic_exchange",
            routingKey: "contact.*");

        // Publica a mensagem
        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync(
            exchange: "topic_exchange",
            routingKey: _routingKey,
            mandatory: false,
            body: body);
    }
}