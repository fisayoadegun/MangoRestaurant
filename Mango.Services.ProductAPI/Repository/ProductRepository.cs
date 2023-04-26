using AutoMapper;
using Mango.Services.ProductAPI.DbContexts;
using Mango.Services.ProductAPI.Models;
using Mango.Services.ProductAPI.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ProductAPI.Repository
{
	public class ProductRepository : IProductRepository
	{
		private ApplicationDbContext _context;
		private IMapper _mapper;
        public ProductRepository(ApplicationDbContext context, IMapper mapper)
        {
			_context = context;
			_mapper = mapper;
		}
        public async Task<ProductDto> CreateUpdateProduct(ProductDto productDto)
		{
			Product product = _mapper.Map<ProductDto, Product>(productDto);
			if (product.ProductId > 0)
			{
				_context.Products.Update(product);
			}
			else
			{
				_context.Add(product);
			}
			await _context.SaveChangesAsync();
			return _mapper.Map<Product, ProductDto>(product);
		}

		public async Task<bool> DeleteProduct(int productId)
		{
			try
			{
                Product product = await _context.Products.Where(x => x.ProductId == productId)
                .FirstOrDefaultAsync();
				if (product == null)
				{
					return false;
				}

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
				return true;
            }
			catch (Exception)
			{

				throw;
			}
		}

		public async Task<ProductDto> GetProductById(int ProductId)
		{
			Product product = await _context.Products.Where(x => x.ProductId == ProductId)
				.FirstOrDefaultAsync();
			return _mapper.Map<ProductDto>(product);
		}

		public async Task<IEnumerable<ProductDto>> GetProducts()
		{
			IEnumerable<Product> productList = await _context.Products.ToListAsync();
			return _mapper.Map<List<ProductDto>>(productList);
		}
	}
}
