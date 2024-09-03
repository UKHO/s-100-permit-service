namespace UKHO.S100PermitService.Common.Helpers
{
    public static class CommonHelper
        {
            public static Guid CorrelationID { get; set; } = Guid.NewGuid();

            public static string GetCorrelationId(string? correlationId)
            {
                return string.IsNullOrEmpty(correlationId) ? CorrelationID.ToString() : correlationId;
            }
        }
}
