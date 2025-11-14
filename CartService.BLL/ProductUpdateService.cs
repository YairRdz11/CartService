using CartService.Transversal.Interfaces.DAL;

namespace CartService.BLL
{
    public class ProductUpdateService
    {
        private readonly ICartRepository _repo;

        public ProductUpdateService(ICartRepository repo)
        {
            _repo = repo;
        }

        public int ApplyProductUpdate(Guid productId, string? name, decimal? price, Guid? categoryId)
        {
            return _repo.UpdateProductInfo(productId, name, price, categoryId);
        }
    }
}
