using Mango.Services.ProductAPI.Models.Dtos;

namespace Mango.Services.ProductAPI.Repository
{
	public interface IProductRepository
	{
		Task<IEnumerable<ProductDto>> GetProducts();
		Task<ProductDto> GetProductById(int ProductId);
		Task<ProductDto> CreateUpdateProduct(ProductDto productDto);
		Task<bool> DeleteProduct(int productId);
	}
}
