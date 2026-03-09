using CommandLine;

namespace MaintainManufacturerKey
{
    internal class Options
    {
        [Option('f', "file", Required = false, HelpText = "Path to the Excel or CSV file")]
        public string? FilePath { get; set; }

        [Option('u', "undo", Required = false, HelpText = "Path to the change log CSV file to undo operations")]
        public string? UndoFilePath { get; set; }
    }
}