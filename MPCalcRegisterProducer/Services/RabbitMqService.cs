using MPCalcRegisterProducer.Services;
using RabbitMQ.Client;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

public class RabbitMqService : IRabbitMqService
{
    private readonly string _queueName;
    private readonly string _hostName;
    private readonly string _user;
    private readonly string _password;
    private readonly int _port;
    private readonly string _dlqName;
    private readonly string _dlxName;

    public RabbitMqService(IConfiguration configuration)
    {
        _queueName = configuration["RabbitMQ:QueueName"] ?? "default_queue";
        _hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
        _port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
        _user = configuration["RabbitMQ:User"] ?? "guest";
        _password = configuration["RabbitMQ:Password"] ?? "guest";
        _dlqName = configuration["RabbitMQ:DLQName"] ?? $"{_queueName}-dlq"; // Ex.: mpcalc-register-queue-dlq
        _dlxName = configuration["RabbitMQ:DLXName"] ?? "mpcalc-dlx"; // Exchange para DLQ
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
            exchange: _dlxName,
            type: "direct",
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
            exchange: _dlxName,
            routingKey: _dlqName); // Usa o nome da DLQ como routing key

        // Argumentos para a fila principal com DLQ configurada
        var arguments = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", _dlxName },
            { "x-dead-letter-routing-key", _dlqName }
        };

        // Declara a fila principal com os argumentos de DLQ
        await channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true, // Recomendado para DLQ
            exclusive: false,
            autoDelete: false,
            arguments: arguments);

        // Publica a mensagem
        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: _queueName,
            mandatory: false,
            body: body);
    }
}