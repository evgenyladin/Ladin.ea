using Ladin.ea.Models;
using Ladin.ea.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ladin.ea.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductRepository _repository;
        private readonly IProductImportService _importService;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(
            IProductRepository repository,
            IProductImportService importService,
            IWebHostEnvironment environment)
        {
            _repository = repository;
            _importService = importService;
            _environment = environment;
        }

        public IActionResult Index()
        {
            var viewModel = new ProductsIndexViewModel
            {
                Products = _repository.GetAll()
            };
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View(new ImportViewModel { FilePath = "/csv/sample_products.txt" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Import(string filePath)
        {
            string resolvedPath = ResolveFilePath(filePath);

            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                var errorModel = new ImportViewModel
                {
                    FilePath = filePath ?? string.Empty,
                    Result = new ImportResult()
                };
                errorModel.Result.Errors.Add(new ImportError { RowNumber = 0, Message = "Укажите путь к CSV-файлу" });
                return View(errorModel);
            }

            var result = _importService.ImportFromCsv(resolvedPath);
            return View(new ImportViewModel { FilePath = filePath, Result = result });
        }

        private string ResolveFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return string.Empty;

            if (filePath.StartsWith("/"))
            {
                var relativePath = filePath.TrimStart('/');
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath);
                return fullPath;
            }

            return filePath;
        }
    }
}