using Ladin.ea.Models;

namespace Ladin.ea.Services
{
    public interface IProductRepository
    {
        void Add(Product product);
        IReadOnlyList<Product> GetAll();
        Product? GetBySku(string sku);
        int Count { get; }
        void Clear();
    }
}