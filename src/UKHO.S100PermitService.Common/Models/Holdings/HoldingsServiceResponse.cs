namespace UKHO.S100PermitService.Common.Models.Holdings
{
    public class HoldingsServiceResponse
    {
        public string ProductCode { get; set; }
        public string ProductTitle { get; set; }
        public DateTime ExpiryDate { get; set; }
        public List<Cell> Cells { get; set; }
    }
}
