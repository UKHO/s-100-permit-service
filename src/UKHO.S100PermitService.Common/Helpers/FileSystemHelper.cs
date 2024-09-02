using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class FileSystemHelper : IFileSystemHelper
    {
        public void CreateFile(string fileContent, string filePath)
        {
            try
            {
                var directoryPath = Path.GetDirectoryName(filePath);
                if(!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                var fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
                var streamWriter = new StreamWriter(fileStream);
                streamWriter.WriteLine(fileContent);
                streamWriter.Flush();
                streamWriter.Close();
                fileStream.Close();
            }
            catch(Exception)
            {
                throw;
            }
        }
    }
}