namespace UKHO.S100PermitService.Common.IO
{
    public interface ISchemaValidator
    {
        bool ValidateSchema(string permitXml, string xsdPath);
    }
}