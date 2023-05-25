using Mango.Services.Email.Models;
using Mango.Services.Email.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace Mango.Services.Email.Messaging
{
	public class RabbitMQOrderConsumer : BackgroundService
	{
		private readonly string orderEmailProcessTopic;
		private readonly IConfiguration _configuration;
		private IConnection _connection;
		private IModel _channel;
		private readonly EmailRepository _emailRepository;
		public RabbitMQOrderConsumer(IConfiguration configuration, EmailRepository emailRepository)
		{
			orderEmailProcessTopic = configuration.GetValue<string>("OrderEmailProcessTopic");
			_configuration = configuration;

			var factory = new ConnectionFactory
			{
				HostName = "localhost",
				UserName = "guest",
				Password = "guest"
			};

			_connection = factory.CreateConnection();
			_channel = _connection.CreateModel();
			_channel.QueueDeclare(queue: orderEmailProcessTopic, false, false, false, arguments: null);			
			_emailRepository = emailRepository;
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			stoppingToken.ThrowIfCancellationRequested();

			var consumer = new EventingBasicConsumer(_channel);
			consumer.Received += (ch, ea) =>
			{
				var content = Encoding.UTF8.GetString(ea.Body.ToArray());
				EmailOrderHeader orderHeaderDto = JsonConvert.DeserializeObject<EmailOrderHeader>(content);
				HandleMessage(orderHeaderDto).GetAwaiter().GetResult();

				_channel.BasicAck(ea.DeliveryTag, false);
			};
			_channel.BasicConsume(orderEmailProcessTopic, false, consumer);

			return Task.CompletedTask;
		}

		private async Task HandleMessage(EmailOrderHeader orderHeaderDto)
		{
			EmailOrderHeader orderHeader = new()
			{
				UserId = orderHeaderDto.UserId,
				FirstName = orderHeaderDto.FirstName,
				LastName = orderHeaderDto.LastName,
				OrderDetails = new List<OrderDetails>(),
				CardNumber = orderHeaderDto.CardNumber,
				CouponCode = orderHeaderDto.CouponCode,
				CVV = orderHeaderDto.CVV,
				DiscountTotal = orderHeaderDto.DiscountTotal,
				Email = orderHeaderDto.Email,
				ExpiryMonthYear = orderHeaderDto.ExpiryMonthYear,
				OrderTime = DateTime.Now,
				OrderTotal = orderHeaderDto.OrderTotal,
				PaymentStatus = false,
				Phone = orderHeaderDto.Phone,
				PickupDateTime = orderHeaderDto.PickupDateTime
			};
			foreach (var detailList in orderHeaderDto.OrderDetails)
			{
				OrderDetails orderDetails = new()
				{
					ProductId = detailList.ProductId,
					ProductName = detailList.ProductName,
					Price = detailList.Price,
					Count = detailList.Count,
					ProductImage = detailList.ProductImage
				};

				orderHeader.CartTotalItems += detailList.Count;
				orderHeader.OrderDetails.Add(orderDetails);
			}

			await _emailRepository.SendOrderDetailsEmail(orderHeaderDto);						
		}
	}
}
