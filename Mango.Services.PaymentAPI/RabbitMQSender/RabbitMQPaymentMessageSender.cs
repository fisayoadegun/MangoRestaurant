using Mango.MessageBus;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace Mango.Services.PaymentAPI.RabbitMQSender
{
    public class RabbitMQPaymentMessageSender : IRabbitMQPaymentMessageSender
    {
        private readonly string _hostname;
        private readonly string _password;
        private readonly string _username;
        private IConnection _connection;
		private const string ExchangeName = "PublishSubscribePaymentUpdate_Exchange";
		private const string DirectExchangeName = "DirectPaymentUpdate_Exchange";
		private const string PaymentEmailUpdateQueueName = "PaymentEmailUpdateQueueName";
		private const string PaymentOrderUpdateQueueName = "PaymentOrderUpdateQueueName";
		public RabbitMQPaymentMessageSender()
        {
            _hostname = "127.0.0.1";
            _password = "guest";
            _username = "guest";
        }

        public void SendMessage(BaseMessage message)
        {
			if (ConnectionExists())
			{
				using var channel = _connection.CreateModel();
				// Fanout exchange
				//channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: false);

				// Direct Exchange
				channel.ExchangeDeclare(DirectExchangeName, ExchangeType.Direct, durable: false);
				channel.QueueDeclare(PaymentOrderUpdateQueueName, false, false, false, null);
				channel.QueueDeclare(PaymentEmailUpdateQueueName, false, false, false, null);
				channel.QueueBind(PaymentEmailUpdateQueueName, DirectExchangeName, "PaymentEmail");
				channel.QueueBind(PaymentOrderUpdateQueueName, DirectExchangeName, "PaymentOrder");
				var json = JsonConvert.SerializeObject(message);
				var body = Encoding.UTF8.GetBytes(json);
				// Direct Publish
				channel.BasicPublish(exchange: DirectExchangeName, "PaymentEmail", basicProperties: null, body: body);
				channel.BasicPublish(exchange: DirectExchangeName, "PaymentOrder", basicProperties: null, body: body);

				// FanOut Publis
				//channel.BasicPublish(exchange: ExchangeName, "",basicProperties: null, body: body);
			}            
        }

		private void CreateConnection()
		{
			try
			{
				var factory = new ConnectionFactory
				{
					HostName = _hostname,
					UserName = _username,
					Password = _password
				};
				_connection = factory.CreateConnection();
			}
			catch (Exception)
			{

				throw;
			}
		}

		private bool ConnectionExists()
		{
			if (_connection != null)
			{
				return true;
			}
			CreateConnection();
			return _connection != null;
		}
	}
}
