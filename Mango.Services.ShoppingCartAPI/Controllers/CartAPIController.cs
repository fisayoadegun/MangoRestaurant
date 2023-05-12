using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.ShoppingCartAPI.Controllers
{
	[ApiController]
	[Route("api/cart")]
	public class CartAPIController : ControllerBase
	{
		protected ResponseDto _response;
		private readonly ICartRepository _cartRepository;		

		public CartAPIController(ICartRepository cartRepository)
		{
			_cartRepository = cartRepository;
			this._response = new ResponseDto();
		}

		[HttpGet("GetCart/{userId}")]
		public async Task<object> GetCart(string userId)
		{
			try
			{
				CartDto cartDto = await _cartRepository.GetCartByUserId(userId);
				_response.Result = cartDto;
			}
			catch (Exception ex)
			{

				_response.IsSuccess = false;
				_response.ErrorMessages = new List<string>() { ex.ToString() };
			}
			return _response;
		}

		[HttpPost("AddCart")]
		public async Task<object> AddCart(CartDto cartDto)
		{
			try
			{
				CartDto cart = await _cartRepository.CreateUpdateCart(cartDto);
				_response.Result = cartDto;
			}
			catch (Exception ex)
			{

				_response.IsSuccess = false;
				_response.ErrorMessages = new List<string>() { ex.ToString() };
			}
			return _response;
		}

		[HttpPost("UpdateCart")]
		public async Task<object> UpdateCart(CartDto cartDto)
		{
			try
			{
				CartDto cart = await _cartRepository.CreateUpdateCart(cartDto);
				_response.Result = cartDto;
			}
			catch (Exception ex)
			{

				_response.IsSuccess = false;
				_response.ErrorMessages = new List<string>() { ex.ToString() };
			}
			return _response;
		}
		
		[HttpPost("RemoveCart")]
		public async Task<object> RemoveCart([FromBody]int cartId)
		{
			try
			{
				bool isSuccess = await _cartRepository.RemoveFromCart(cartId);
				_response.Result = isSuccess;
			}
			catch (Exception ex)
			{

				_response.IsSuccess = false;
				_response.ErrorMessages = new List<string>() { ex.ToString() };
			}
			return _response;
		}

		[HttpPost("ClearCart")]
		public async Task<object> ClearCart([FromBody] string userId)
		{
			try
			{
				bool isSuccess = await _cartRepository.ClearCart(userId);
				_response.Result = isSuccess;
			}
			catch (Exception ex)
			{

				_response.IsSuccess = false;
				_response.ErrorMessages = new List<string>() { ex.ToString() };
			}
			return _response;
		}

		[HttpPost("ApplyCoupon")]
		public async Task<object> ApplyCoupon([FromBody] CartDto cartDto)
		{
			try
			{
				bool isSuccess = await _cartRepository.ApplyCoupon(cartDto.CartHeader.UserId,
					cartDto.CartHeader.CouponCode);
				_response.Result = isSuccess;
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.ErrorMessages = new List<string>() { ex.ToString() };
			}
			return _response;
		}

		[HttpPost("RemoveCoupon")]
		public async Task<object> RemoveCoupon([FromBody] string userId)
		{
			try
			{
				bool isSuccess = await _cartRepository.RemoveCoupon(userId);
				_response.Result = isSuccess;
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.ErrorMessages = new List<string>() { ex.ToString() };
			}
			return _response;
		}
	}
}
