using System.Text;
using ExcelDataReader;
using ExcelDataReader.Exceptions;
using Serilog;

namespace MaintainManufacturerKey
{
    internal static class KeyListReader
    {
        public static IEnumerable<(string ManufacturerId, string ManufacturerKey)> ReadFile(
            string filePath, 
            string idColumnName, 
            string keyColumnName,
            string? configuredPassword = null,
            int maxRowsToSearchForHeader = 50)
        {
            Log.Information("Reading file from disk: {FilePath}", Path.GetFullPath(filePath));

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".csv")
            {
                return ReadCsv(filePath, idColumnName, keyColumnName, maxRowsToSearchForHeader);
            }

            if (extension == ".xlsx" || extension == ".xls")
            {
                return ReadExcel(filePath, idColumnName, keyColumnName, configuredPassword, maxRowsToSearchForHeader);
            }

            throw new NotSupportedException($"Unsupported file extension: {extension}");
        }

        private static IEnumerable<(string ManufacturerId, string ManufacturerKey)> ReadExcel(
            string filePath, 
            string idColumnName, 
            string keyColumnName,
            string? configuredPassword = null,
            int maxRowsToSearchForHeader = 50)
        {
            var results = new List<(string, string)>();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            const int maxPasswordAttempts = 3;
            string? password = !string.IsNullOrWhiteSpace(configuredPassword) ? configuredPassword : null;
            bool triedConfiguredPassword = password != null;

            for (int attempt = 0; attempt < maxPasswordAttempts; attempt++)
            {
                try
                {
                    using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                    using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration
                    {
                        Password = password
                    });

                    ReadRows(reader, results, idColumnName, keyColumnName, maxRowsToSearchForHeader);

                    if (triedConfiguredPassword && attempt == 0)
                    {
                        Log.Information("Successfully opened password-protected file using configured password");
                    }

                    return results;
                }
                catch (InvalidPasswordException)
                {
                    if (attempt < maxPasswordAttempts - 1)
                    {
                        if (attempt == 0)
                        {
                            if (triedConfiguredPassword)
                            {
                                Log.Warning("The configured password is incorrect for '{FileName}'", Path.GetFileName(filePath));
                            }
                            else
                            {
                                Log.Warning("The Excel file '{FileName}' is password-protected", Path.GetFileName(filePath));
                            }
                        }
                        else
                        {
                            Log.Warning("Invalid password. Please try again");
                        }

                        Console.Write($"Enter password (attempt {attempt + 1}/{maxPasswordAttempts}): ");
                        password = ReadPassword();
                        Console.WriteLine();
                        triedConfiguredPassword = false; // Mark that we're now using user input
                    }
                    else
                    {
                        throw new NotSupportedException($"Failed to open the Excel file '{Path.GetFileName(filePath)}' after {maxPasswordAttempts} attempts. The file may be password-protected or corrupted.");
                    }
                }
            }

            return results;
        }

        private static string ReadPassword()
        {
            var password = new StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            while (key.Key != ConsoleKey.Enter);

            return password.ToString();
        }

        private static IEnumerable<(string ManufacturerId, string ManufacturerKey)> ReadCsv(
            string filePath, 
            string idColumnName, 
            string keyColumnName,
            int maxRowsToSearchForHeader = 50)
        {
            var results = new List<(string, string)>();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var config = new ExcelReaderConfiguration
            {
                AutodetectSeparators = [',', ';'],
                FallbackEncoding = Encoding.UTF8
            };

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateCsvReader(stream, config);

            ReadRows(reader, results, idColumnName, keyColumnName, maxRowsToSearchForHeader);

            return results;
        }

        private static void ReadRows(
            IExcelDataReader reader, 
            List<(string, string)> results, 
            string idColumnName, 
            string keyColumnName,
            int maxRowsToSearch = 50)
        {
            int? idColumnIndex = null;
            int? keyColumnIndex = null;
            var rowCount = 0;
            var foundColumns = new List<string>();
            bool headerFound = false;
            int rowsSearched = 0;

            // Search for header row by reading rows until we find our column names
            while (reader.Read() && !headerFound && rowsSearched < maxRowsToSearch)
            {
                rowsSearched++;
                foundColumns.Clear();
                idColumnIndex = null;
                keyColumnIndex = null;

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var headerValue = reader.GetValue(i)?.ToString()?.Trim();

                    if (!string.IsNullOrWhiteSpace(headerValue))
                    {
                        foundColumns.Add(headerValue);

                        if (string.Equals(headerValue, idColumnName, StringComparison.OrdinalIgnoreCase))
                        {
                            idColumnIndex = i;
                        }

                        if (string.Equals(headerValue, keyColumnName, StringComparison.OrdinalIgnoreCase))
                        {
                            keyColumnIndex = i;
                        }
                    }
                }

                // Check if we found both columns in this row
                if (idColumnIndex.HasValue && keyColumnIndex.HasValue)
                {
                    headerFound = true;
                    Log.Information("Found header row at row {RowNumber}", rowsSearched);
                    Log.Debug("Found {IdColumnName} at index {IdIndex}", idColumnName, idColumnIndex.Value);
                    Log.Debug("Found {KeyColumnName} at index {KeyIndex}", keyColumnName, keyColumnIndex.Value);
                }
            }

            if (!headerFound)
            {
                // TEMP DEBUG: Print all found columns from the last row checked
                Log.Information("=== DEBUG: Searched {RowCount} rows. Last row had {Count} columns ===", rowsSearched, foundColumns.Count);
                for (int i = 0; i < foundColumns.Count; i++)
                {
                    Log.Information("  Column {Index}: '{ColumnName}' (Length: {Length}, Bytes: [{Bytes}])", 
                        i, 
                        foundColumns[i], 
                        foundColumns[i].Length,
                        string.Join(", ", foundColumns[i].Select(c => ((int)c).ToString())));
                }
                Log.Information("=== Looking for ID column: '{IdColumn}' (Length: {Length}) ===", idColumnName, idColumnName.Length);
                Log.Information("=== Looking for Key column: '{KeyColumn}' (Length: {Length}) ===", keyColumnName, keyColumnName.Length);
                Log.Information("===========================================");

                var availableColumns = string.Join(", ", foundColumns.Select(c => $"'{c}'"));

                if (!idColumnIndex.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Required column '{idColumnName}' not found in file after searching {rowsSearched} rows. " +
                        $"Last row columns: {availableColumns}. " +
                        $"Update the 'ManufacturerIdColumnName' setting in appsettings.json to match one of the available columns.");
                }

                if (!keyColumnIndex.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Required column '{keyColumnName}' not found in file after searching {rowsSearched} rows. " +
                        $"Last row columns: {availableColumns}. " +
                        $"Update the 'ManufacturerKeyColumnName' setting in appsettings.json to match one of the available columns.");
                }
            }

            // Read data rows (all rows after the header)
            while (reader.Read())
            {
                // Use GetValue to handle different data types (strings, numbers, etc.)
                var manufacturerIdValue = reader.GetValue(idColumnIndex!.Value);
                var manufacturerKeyValue = reader.GetValue(keyColumnIndex!.Value);

                var manufacturerId = manufacturerIdValue?.ToString()?.Trim() ?? string.Empty;
                var manufacturerKey = manufacturerKeyValue?.ToString()?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(manufacturerId) && !string.IsNullOrWhiteSpace(manufacturerKey))
                {
                    results.Add((manufacturerId, manufacturerKey));
                    rowCount++;
                }
            }

            Log.Debug("Read {RowCount} data rows from file", rowCount);
        }
    }
}