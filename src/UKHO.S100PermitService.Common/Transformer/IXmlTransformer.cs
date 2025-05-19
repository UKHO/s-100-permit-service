namespace UKHO.S100PermitService.Common.Transformer
{
    public interface IXmlTransformer
    {
        Task<string> SerializeToXml<T>(T obj);
    }
}