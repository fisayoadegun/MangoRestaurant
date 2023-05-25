using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Repository;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.OrderAPI.Messaging
{
	public class RabbitMQPaymentConsumer : BackgroundService
	{
		private IConnection _connection;
		private IModel _channel;
		private readonly IConfiguration _configuration;
		private readonly string orderpaymentProcessTopic;
		private readonly OrderRepository _orderRepository;
		private const string ExchangeName = "PublishSubscribePaymentUpdate_Exchange";
		private const string DirectExchange = "DirectPaymentUpdate_Exchange";
		private const string PaymentOrderUpdateQueueName = "PaymentOrderUpdateQueueName";
		string queueName = "";
		public RabbitMQPaymentConsumer(IConfiguration configuration, OrderRepository orderRepository)
		{
			_configuration = configuration;
			orderpaymentProcessTopic = configuration.GetValue<string>("OrderPaymentProcessTopics");

			var factory = new ConnectionFactory
			{
				HostName = "localhost",
				UserName = "guest",
				Password = "guest"
			};

			_connection = factory.CreateConnection();
			_channel = _connection.CreateModel();
			// Fanout
			//_channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout);

			// Direct 
			_channel.ExchangeDeclare(DirectExchange, ExchangeType.Direct);
			//queueName = _channel.QueueDeclare().QueueName;
			_channel.QueueDeclare(PaymentOrderUpdateQueueName, false, false, false, null);
			//_channel.QueueBind(queueName, ExchangeName, "");
			_orderRepository = orderRepository;
		}
		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			stoppingToken.ThrowIfCancellationRequested();

			var consumer = new EventingBasicConsumer(_channel);
			consumer.Received += (ch, ea) =>
			{
				var content = Encoding.UTF8.GetString(ea.Body.ToArray());
				UpdatePaymentResultMessage updatePaymentResultMessage = JsonConvert.DeserializeObject<UpdatePaymentResultMessage>(content);
				HandleMessage(updatePaymentResultMessage).GetAwaiter().GetResult();

				_channel.BasicAck(ea.DeliveryTag, false);
			};
			//Fanout
			//_channel.BasicConsume(queueName, false, consumer);
			//Direct
			_channel.BasicConsume(PaymentOrderUpdateQueueName, false, consumer);

			return Task.CompletedTask;
		}

		private async Task HandleMessage(UpdatePaymentResultMessage updatePaymentResultMessage)
		{
			try
			{
				await _orderRepository.UpdateOrderPaymentStatus(updatePaymentResultMessage.OrderId, updatePaymentResultMessage.Status);
			}
			catch (Exception e)
			{
				throw;
			}

		}
	}
}
