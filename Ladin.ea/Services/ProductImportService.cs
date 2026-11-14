using Ladin.ea.Models;

namespace Ladin.ea.Services
{
    public class ProductImportService : IProductImportService
    {
        private readonly IProductRepository _repository;
        private const string RequiredHeader = "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number";

        public ProductImportService(IProductRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public ImportResult ImportFromCsv(string filePath)
        {
            var result = new ImportResult();

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = "Файл не найден" });
                return result;
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                return ImportFromLines(lines);
            }
            catch (Exception)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = "Ошибка чтения файла" });
                return result;
            }
        }

        public ImportResult ImportFromLines(IEnumerable<string> lines)
        {
            var result = new ImportResult();
            var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

            if (nonEmptyLines.Count == 0)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = "Данные для импорта отсутствуют" });
                return result;
            }

            var headerLine = nonEmptyLines[0];
            var headers = CsvLineParser.Parse(headerLine);

            var requiredColumns = RequiredHeader.Split(',');
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Count; i++)
            {
                if (!columnMap.ContainsKey(headers[i]))
                {
                    columnMap[headers[i]] = i;
                }
            }

            if (!requiredColumns.All(c => columnMap.ContainsKey(c)))
            {
                result.Errors.Add(new ImportError { RowNumber = 1, Message = "Неверные заголовки CSV" });
                return result;
            }

            var importedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < nonEmptyLines.Count; i++)
            {
                var line = nonEmptyLines[i];
                int rowNumber = i + 1;
                result.TotalRows++;

                var values = CsvLineParser.Parse(line);
                if (values.Count != headers.Count)
                {
                    result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = "Неверное количество полей в строке" });
                    continue;
                }

                string GetValue(string col)
                {
                    return columnMap.TryGetValue(col, out int index) ? values[index].Trim() : string.Empty;
                }

                var rowErrors = new List<ImportError>();

                string sku = GetValue("sku");
                if (string.IsNullOrWhiteSpace(sku)) rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле sku не может быть пустым" });

                string name = GetValue("name");
                if (string.IsNullOrWhiteSpace(name)) rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле name не может быть пустым" });

                decimal price;
                if (!decimal.TryParse(GetValue("price"), out price) || price <= 0) rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле price должно быть числом больше 0" });

                int stock;
                if (!int.TryParse(GetValue("stock"), out stock) || stock < 0) rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Остаток на складе должен быть целым числом не меньше 0" });

                string isActiveStr = GetValue("is_active").ToLower();
                bool isActive;
                if (isActiveStr == "true" || isActiveStr == "1") isActive = true;
                else if (isActiveStr == "false" || isActiveStr == "0") isActive = false;
                else
                {
                    isActive = false;
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле is_active должно иметь значение true, false, 1 или 0" });
                }

                string brand = GetValue("brand");
                if (string.IsNullOrWhiteSpace(brand)) rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле brand обязательно для варианта «Электроника»" });

                int warranty;
                if (!int.TryParse(GetValue("warranty_months"), out warranty) || warranty < 0 || warranty > 60) rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Гарантия должна быть целым числом от 0 до 60 месяцев" });

                if (rowErrors.Count > 0)
                {
                    result.Errors.AddRange(rowErrors);
                    continue;
                }

                if (_repository.GetBySku(sku) != null || importedSkus.Contains(sku))
                {
                    result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = "Артикул товара уже существует" });
                    continue;
                }

                string currency = GetValue("currency").ToUpper();
                if (string.IsNullOrWhiteSpace(currency)) currency = "RUB";

                string category = GetValue("category_name");
                if (string.IsNullOrWhiteSpace(category)) category = "Без категории";

                var product = new Product
                {
                    Sku = sku,
                    Name = name,
                    Description = GetValue("description"),
                    Price = price,
                    Currency = currency,
                    CategoryName = category,
                    StockQuantity = stock,
                    ImageUrl = GetValue("image_url"),
                    IsActive = isActive,
                    Brand = brand,
                    WarrantyMonths = warranty,
                    ModelNumber = GetValue("model_number")
                };

                _repository.Add(product);
                importedSkus.Add(sku);
                result.ImportedCount++;
            }

            if (result.TotalRows == 0 && result.Errors.Count == 0)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = "Данные для импорта отсутствуют" });
            }

            return result;
        }
    }
}