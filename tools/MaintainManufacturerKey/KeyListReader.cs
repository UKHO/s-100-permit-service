using System.Text;
using ExcelDataReader;

namespace MaintainManufacturerKey
{
    internal class KeyListReader
    {
        public IEnumerable<(string ManufacturerId, string ManufacturerKey)> ReadFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".csv")
            {
                return ReadCsv(filePath);
            }

            if (extension == ".xlsx" || extension == ".xls")
            {
                return ReadExcel(filePath);
            }

            throw new NotSupportedException($"Unsupported file extension: {extension}");
        }

        private static IEnumerable<(string ManufacturerId, string ManufacturerKey)> ReadExcel(string filePath)
        {
            var results = new List<(string, string)>();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            ReadRows(reader, results);
            return results;
        }

        private static IEnumerable<(string ManufacturerId, string ManufacturerKey)> ReadCsv(string filePath)
        {
            var results = new List<(string, string)>();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var config = new ExcelReaderConfiguration
            {
                AutodetectSeparators = new[] { ',', ';' },
                FallbackEncoding = Encoding.UTF8
            };

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateCsvReader(stream, config);

            ReadRows(reader, results);

            return results;
        }

        private static void ReadRows(IExcelDataReader reader, List<(string, string)> results)
        {
            var headerRead = false;

            while (reader.Read())
            {
                if (!headerRead)
                {
                    headerRead = true;
                    continue; // skip header row
                }

                var manufacturerId = reader.GetString(0);
                var manufacturerKey = reader.GetString(1);

                if (!string.IsNullOrWhiteSpace(manufacturerId) && !string.IsNullOrWhiteSpace(manufacturerKey))
                {
                    results.Add((manufacturerId, manufacturerKey));
                }
            }
        }
    }
}