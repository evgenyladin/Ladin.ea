namespace Ladin.ea.Models
{
    public class ImportViewModel
    {
        public string FilePath { get; set; } = string.Empty;
        public ImportResult? Result { get; set; }
    }

    public class ProductsIndexViewModel
    {
        public IReadOnlyList<Product> Products { get; set; } = new List<Product>();
        public int TotalCount => Products.Count;
        public decimal TotalValue => Products.Sum(p => p.Price);
    }
}