using Mango.Services.Email.Messages;
using Mango.Services.Email.Repository;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.Email.Messaging
{
	public class RabbitMQPaymentConsumer : BackgroundService
	{
		private IConnection _connection;
		private IModel _channel;
		private readonly IConfiguration _configuration;
		private readonly string orderpaymentProcessTopic;
		private const string ExchangeName = "PublishSubscribePaymentUpdate_Exchange";
		private const string DirectExchange = "DirectPaymentUpdate_Exchange";
		private const string PaymentEmailUpdateQueueName = "PaymentEmailUpdateQueueName";
		private readonly EmailRepository _emailRepo;
		string queueName = "";
		public RabbitMQPaymentConsumer(IConfiguration configuration, EmailRepository emailRepo)
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
			_channel.QueueDeclare(PaymentEmailUpdateQueueName, false, false, false, null);
			//_channel.QueueBind(queueName, ExchangeName, "");
			_channel.QueueBind(PaymentEmailUpdateQueueName, DirectExchange, "PaymentEmail");
			_emailRepo = emailRepo;
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
			_channel.BasicConsume(PaymentEmailUpdateQueueName, false, consumer);

			return Task.CompletedTask;
		}

		private async Task HandleMessage(UpdatePaymentResultMessage updatePaymentResultMessage)
		{			
			try
			{
				await _emailRepo.SendAndLogEmail(updatePaymentResultMessage);				
			}
			catch (Exception e)
			{
				throw;
			}

		}
	}
}
