using CommandLine;

namespace MaintainManufacturerKey
{
    internal class Options
    {
        [Option('o', "operation", Required = true, HelpText = "Operation to perform: insert or upsert")]
        public required string Operation { get; set; }

        [Option('f', "file", Required = true, HelpText = "Path to the Excel or CSV file")]
        public required string FilePath { get; set; }
    }
}