using Mango.Web.Models;
using Mango.Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Mango.Web.Controllers
{
	public class CartController : Controller
	{
		private readonly IProductService _productService;
		private readonly ICartService _cartService;

		public CartController(ICartService cartService, IProductService productService)
		{
			_cartService = cartService;
			_productService = productService;
		}

		public async Task<IActionResult> CartIndex()
		{
			return View(await LoadCartDtoBasedOnLoggedInUser());
		}

        public async Task<IActionResult> Remove(int cartDetailsId)
        {
            var userId = User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value;
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var response = await _cartService.RemoveFromCartAsync<ResponseDto>(cartDetailsId, accessToken);

            CartDto cartDto = new();
            if (response != null && response.IsSuccess)
            {
				return RedirectToAction(nameof(CartIndex));
            }
			return View();
        }

        public async Task<IActionResult> ClearCart()
        {
            var userId = User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value;
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var response = await _cartService.ClearCartAsync<ResponseDto>(userId, accessToken);

            CartDto cartDto = new();
            if (response != null && response.IsSuccess)
            {
                return RedirectToAction(nameof(CartIndex));
            }
            return View();
        }

        private async Task<CartDto> LoadCartDtoBasedOnLoggedInUser()
		{
			var userId = User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value;
			var accessToken = await HttpContext.GetTokenAsync("access_token");
			var response = await _cartService.GetCartByUserIdAsync<ResponseDto>(userId, accessToken);

			CartDto cartDto = new();
			if(response != null && response.IsSuccess)
			{
				cartDto = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(response.Result));
			}

			if(cartDto.CartHeader != null)
			{
				foreach (var detail in cartDto.CartDetails)
				{
					cartDto.CartHeader.OrderTotal += (detail.Product.Price * detail.Count);
				}
			}
			return cartDto;

		}

        public async Task<IActionResult> DetailsPost(ProductDto productDto)
        {
            CartDto cartDto = new()
            {
                CartHeader = new CartHeaderDto
                {
                    UserId = User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value
                }
            };

            CartDetailsDto cartDetails = new CartDetailsDto()
            {
                Count = productDto.Count,
                ProductId = productDto.ProductId,
            };

            var response = await _productService.GetAllProductByIdAsync<ResponseDto>(productDto.ProductId, "");
            if (response != null && response.IsSuccess)
            {
                cartDetails.Product = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(response.Result));
            }
            List<CartDetailsDto> cartDetailsDtos = new();
            cartDetailsDtos.Add(cartDetails);
            cartDto.CartDetails = cartDetailsDtos;

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var addToCartResp = await _cartService.AddToCartAsync<ResponseDto>(cartDto, accessToken);
            if (addToCartResp != null && addToCartResp.IsSuccess)
            {
                return RedirectToAction(nameof(Index));
            }
            return View(productDto);
        }
    }
}
