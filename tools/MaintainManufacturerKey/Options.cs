using CommandLine;

namespace MaintainManufacturerKey
{
    internal class Options
    {
        [Option('f', "file", Required = true, HelpText = "Path to the Excel or CSV file")]
        public required string FilePath { get; set; }
    }
}