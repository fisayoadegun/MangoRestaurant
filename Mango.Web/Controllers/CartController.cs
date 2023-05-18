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
		private readonly ICouponService _couponService;

		public CartController(ICartService cartService, IProductService productService, ICouponService couponService)
		{
			_cartService = cartService;
			_productService = productService;
			_couponService = couponService;
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

		[HttpGet]
		public async Task<IActionResult> Checkout()
		{
			return View(await LoadCartDtoBasedOnLoggedInUser());
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
				if (!string.IsNullOrEmpty(cartDto.CartHeader.CouponCode))
				{
					var coupon = await _couponService.GetCoupon<ResponseDto>(cartDto.CartHeader.CouponCode, accessToken);
					if (coupon != null && coupon.IsSuccess)
					{
						var couponObj = JsonConvert.DeserializeObject<CouponDto>(Convert.ToString(coupon.Result));
						cartDto.CartHeader.DiscountTotal = couponObj.DiscountAmount;
					}
				}
				foreach (var detail in cartDto.CartDetails)
				{
					cartDto.CartHeader.OrderTotal += (detail.Product.Price * detail.Count);
				}
				cartDto.CartHeader.OrderTotal -= cartDto.CartHeader.DiscountTotal;
			}
			return cartDto;

		}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCart(CartDto cartDto)
        {           
			var accessToken = await HttpContext.GetTokenAsync("access_token");

			var cartHeader = await _cartService.GetCartByUserIdAsync<ResponseDto>(cartDto.CartHeader.UserId, accessToken); 
			var cartHeaderObj = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(cartHeader.Result));
			if (cartHeader != null) 
			{
				cartDto.CartDetails = cartHeaderObj.CartDetails;
			}
            
			var response = await _cartService.UpdateCartAsync<ResponseDto>(cartDto, accessToken);
            if (response != null && response.IsSuccess)
            {
                return RedirectToAction(nameof(CartIndex));
            }
           
            return RedirectToAction(nameof(CartIndex));
        }

		[HttpPost]
		[ActionName("ApplyCoupon")]
		public async Task<IActionResult> ApplyCoupon(CartDto cartDto)
		{
			var userId = User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value;
			var accessToken = await HttpContext.GetTokenAsync("access_token");
			var response = await _cartService.ApplyCoupon<ResponseDto>(cartDto, accessToken);

			if (response != null && response.IsSuccess)
			{
				return RedirectToAction(nameof(CartIndex));
			}
			return View();
		}

		[HttpPost]
		[ActionName("RemoveCoupon")]
		public async Task<IActionResult> RemoveCoupon(CartDto cartDto)
		{
			var userId = User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value;
			var accessToken = await HttpContext.GetTokenAsync("access_token");
			var response = await _cartService.RemoveCoupon<ResponseDto>(cartDto.CartHeader.UserId, accessToken);

			if (response != null && response.IsSuccess)
			{
				return RedirectToAction(nameof(CartIndex));
			}
			return View();
		}

        [HttpPost]
        [ActionName("Checkout")]
        public async Task<IActionResult> Checkout(CartDto cartDto)
        {                        
			try
			{
                var accessToken = await HttpContext.GetTokenAsync("access_token");
				var response = await _cartService.Checkout<ResponseDto>(cartDto.CartHeader, accessToken);
				if (!response.IsSuccess)
				{
					TempData["Error"] = response.DisplayMessage;
					return RedirectToAction(nameof(Checkout));
				}
				return RedirectToAction(nameof(Confirmation));
            }
			catch (Exception e)
			{

				return View(cartDto);
			}
        }

		[HttpGet]
		public async Task<IActionResult> Confirmation()
		{
			return View();
		}
    }
}
