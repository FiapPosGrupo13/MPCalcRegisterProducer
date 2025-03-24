namespace MPCalcRegisterProducer.Services
{
    public interface IRabbitMqService
    {
        Task PublishMessageAsync(string message);
    }
}
