using Mango.MessageBus;
using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dtos;
using Mango.Services.OrderAPI.RabbitMQSender;
using Mango.Services.OrderAPI.Repository;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace Mango.Services.OrderAPI.Messaging
{
	public class RabbitMQCheckoutConsumer : BackgroundService
	{
		private readonly OrderRepository _orderRepository;
		private IConnection _connection;
		private IModel _channel;
		private readonly IConfiguration _configuration;
		private readonly string checkoutMessageTopic;
		private readonly string orderPaymentProcessTopic;
		private readonly string orderEmailProcessTopic;
		private readonly IRabbitMQOrderMessageSender _rabbitMQOrderMessageSender;
		public RabbitMQCheckoutConsumer(OrderRepository orderRepository, IConfiguration configuration, IRabbitMQOrderMessageSender rabbitMQOrderMessageSender)
		{
			_configuration = configuration;
			_orderRepository = orderRepository;
			checkoutMessageTopic = configuration.GetValue<string>("CheckoutMessageTopic");
			orderPaymentProcessTopic = configuration.GetValue<string>("OrderPaymentProcessTopic");
			orderEmailProcessTopic = configuration.GetValue<string>("OrderEmailProcessTopic");


			var factory = new ConnectionFactory
			{
				HostName = "localhost",
				UserName = "guest",
				Password = "guest"
			};

			_connection = factory.CreateConnection();
			_channel = _connection.CreateModel();
			_channel.QueueDeclare(queue: checkoutMessageTopic, false, false, false, arguments: null);
			_rabbitMQOrderMessageSender = rabbitMQOrderMessageSender;
		}
		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			stoppingToken.ThrowIfCancellationRequested();

			var consumer = new EventingBasicConsumer(_channel);
			consumer.Received += (ch, ea) =>
			{
				var content = Encoding.UTF8.GetString(ea.Body.ToArray());
				CheckoutHeaderDto checkoutHeaderDto = JsonConvert.DeserializeObject<CheckoutHeaderDto>(content);
				HandleMessage(checkoutHeaderDto).GetAwaiter().GetResult();

				_channel.BasicAck(ea.DeliveryTag, false);
			};
			_channel.BasicConsume(checkoutMessageTopic, false, consumer);

			return Task.CompletedTask;
		}

		private async Task HandleMessage(CheckoutHeaderDto checkoutHeaderDto)
		{
			OrderHeader orderHeader = new()
			{
				UserId = checkoutHeaderDto.UserId,
				FirstName = checkoutHeaderDto.FirstName,
				LastName = checkoutHeaderDto.LastName,
				OrderDetails = new List<OrderDetails>(),
				CardNumber = checkoutHeaderDto.CardNumber,
				CouponCode = checkoutHeaderDto.CouponCode,
				CVV = checkoutHeaderDto.CVV,
				DiscountTotal = checkoutHeaderDto.DiscountTotal,
				Email = checkoutHeaderDto.Email,
				ExpiryMonthYear = checkoutHeaderDto.ExpiryMonthYear,
				OrderTime = DateTime.Now,
				OrderTotal = checkoutHeaderDto.OrderTotal,
				PaymentStatus = false,
				Phone = checkoutHeaderDto.Phone,
				PickupDateTime = checkoutHeaderDto.PickupDateTime
			};
			foreach (var detailList in checkoutHeaderDto.CartDetails)
			{
				OrderDetails orderDetails = new()
				{
					ProductId = detailList.ProductId,
					ProductName = detailList.Product.Name,
					Price = detailList.Product.Price,
					Count = detailList.Count					
				};

				orderHeader.CartTotalItems += detailList.Count;
				orderHeader.OrderDetails.Add(orderDetails);
			}

			await _orderRepository.AddOrder(orderHeader);

			EmailOrderHeader emailOrder = new()
			{
				UserId = checkoutHeaderDto.UserId,
				FirstName = checkoutHeaderDto.FirstName,
				LastName = checkoutHeaderDto.LastName,
				OrderDetails = new List<OrderDetails>(),
				CardNumber = checkoutHeaderDto.CardNumber,
				CouponCode = checkoutHeaderDto.CouponCode,
				CVV = checkoutHeaderDto.CVV,
				DiscountTotal = checkoutHeaderDto.DiscountTotal,
				Email = checkoutHeaderDto.Email,
				ExpiryMonthYear = checkoutHeaderDto.ExpiryMonthYear,
				OrderTime = DateTime.Now,
				OrderTotal = checkoutHeaderDto.OrderTotal,
				PaymentStatus = false,
				Phone = checkoutHeaderDto.Phone,
				PickupDateTime = checkoutHeaderDto.PickupDateTime
			};

			foreach (var detailList in checkoutHeaderDto.CartDetails)
			{
				OrderDetails orderDetails = new()
				{
					ProductId = detailList.ProductId,
					ProductName = detailList.Product.Name,
					Price = detailList.Product.Price,
					Count = detailList.Count,
					ProductImage = detailList.Product.ImageUrl
				};

				emailOrder.CartTotalItems += detailList.Count;
				emailOrder.OrderDetails.Add(orderDetails);
			}

			PaymentRequestMessage paymentRequestMessage = new()
			{
				Name = orderHeader.FirstName + " " + orderHeader.LastName,
				CardNumber = orderHeader.CardNumber,
				CVV = orderHeader.CVV,
				ExpiryMonthYear = orderHeader.ExpiryMonthYear,
				OrderId = orderHeader.OrderHeaderId,
				OrderTotal = orderHeader.OrderTotal,
				Email = orderHeader.Email
			};

			try
			{				
				_rabbitMQOrderMessageSender.SendMessage(paymentRequestMessage, orderPaymentProcessTopic);
				_rabbitMQOrderMessageSender.SendMessage(emailOrder, orderEmailProcessTopic);
			}
			catch (Exception e)
			{

				throw;
			}
		}
	}
}
