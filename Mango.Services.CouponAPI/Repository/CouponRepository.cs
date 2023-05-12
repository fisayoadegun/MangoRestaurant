using AutoMapper;
using Mango.Services.CouponAPI.DbContexts;
using Mango.Services.CouponAPI.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.CouponAPI.Repository
{
	public class CouponRepository : ICouponRepository
	{
		private readonly ApplicationDbContext _context;
		protected IMapper _mapper;
		public CouponRepository(ApplicationDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		public async Task<CouponDto> GetCouponByCode(string couponCode)
		{
			var coupon = await _context.Coupons.FirstOrDefaultAsync(u => u.CouponCode == couponCode);
			return _mapper.Map<CouponDto>(coupon);
		}
	}
}
