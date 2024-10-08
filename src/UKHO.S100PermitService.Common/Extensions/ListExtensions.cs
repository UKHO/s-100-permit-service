namespace UKHO.S100PermitService.Common.Extensions
{
    public static class ListExtensions
    {
        public static bool IsNullOrEmpty<T>(List<T>? list)
        {
            return list is null || list.Count == 0;
        }
    }
}