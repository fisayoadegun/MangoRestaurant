using Mango.MessageBus;
using Mango.Services.PaymentAPI.Messages;
using Mango.Services.PaymentAPI.RabbitMQSender;
using Newtonsoft.Json;
using PaymentProcessor;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace Mango.Services.PaymentAPI.Messaging
{
	public class RabbitMQPaymentConsumer : BackgroundService
	{		
		private IConnection _connection;
		private IModel _channel;
		private readonly IConfiguration _configuration;
		private readonly string orderpaymentProcessTopic;
		private readonly string orderupdatepaymentresulttopic;
		private readonly IRabbitMQPaymentMessageSender _rabbitMQPaymentMessageSender;
		private readonly IProcessPayment _processPayment;
		public RabbitMQPaymentConsumer(IConfiguration configuration, IRabbitMQPaymentMessageSender rabbitMQPaymentMessageSender, IProcessPayment processPayment)
		{
			_configuration = configuration;
			orderpaymentProcessTopic = configuration.GetValue<string>("OrderPaymentProcessTopics");
			orderupdatepaymentresulttopic = configuration.GetValue<string>("OrderUpdatePaymentResultTopic");

			var factory = new ConnectionFactory
			{
				HostName = "localhost",
				UserName = "guest",
				Password = "guest"
			};

			_connection = factory.CreateConnection();
			_channel = _connection.CreateModel();
			_channel.QueueDeclare(queue: orderpaymentProcessTopic, false, false, false, arguments: null);
			_rabbitMQPaymentMessageSender = rabbitMQPaymentMessageSender;
			_processPayment = processPayment;
		}
		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			stoppingToken.ThrowIfCancellationRequested();

			var consumer = new EventingBasicConsumer(_channel);
			consumer.Received += (ch, ea) =>
			{
				var content = Encoding.UTF8.GetString(ea.Body.ToArray());
				PaymentRequestMessage paymentRequestMessage = JsonConvert.DeserializeObject<PaymentRequestMessage>(content);
				HandleMessage(paymentRequestMessage).GetAwaiter().GetResult();

				_channel.BasicAck(ea.DeliveryTag, false);
			};
			_channel.BasicConsume(orderpaymentProcessTopic, false, consumer);

			return Task.CompletedTask;
		}

		private async Task HandleMessage(PaymentRequestMessage paymentRequestMessage)
		{
			var result = _processPayment.PaymentProcessor();

			UpdatePaymentResultMessage updatePaymentResultMessage = new()
			{
				Status = result,
				OrderId = paymentRequestMessage.OrderId,
				Email = paymentRequestMessage.Email
			};


			try
			{
				_rabbitMQPaymentMessageSender.SendMessage(updatePaymentResultMessage);
				//await _messageBus.PublishMessage(updatePaymentResultMessage, orderupdatepaymentresulttopic);
				//await args.CompleteMessageAsync(args.Message);
			}
			catch (Exception e)
			{
				throw;
			}

		}
	}
}
