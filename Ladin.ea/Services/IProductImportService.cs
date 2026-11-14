using Ladin.ea.Models;

namespace Ladin.ea.Services
{
    public interface IProductImportService
    {
        ImportResult ImportFromCsv(string filePath);
        ImportResult ImportFromLines(IEnumerable<string> lines);
    }
}