using Mango.MessageBus;

namespace Mango.Services.ShoppingCartAPI.RabbitMQSender
{
    public interface IRabbitMQCartMessageSender
    {
        void SendMessage(BaseMessage baseMessage, String queueName);
    }
}
