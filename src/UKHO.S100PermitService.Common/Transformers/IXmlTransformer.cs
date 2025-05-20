namespace UKHO.S100PermitService.Common.Transformers
{
    public interface IXmlTransformer
    {
        Task<string> SerializeToXml<T>(T obj);
    }
}