namespace UKHO.S100PermitService.Common.Models
{
    public class HoldingsServiceResponse
    {
        public string ProductCode { get; set; }
        public string ProductTitle { get; set; }
        public DateTime expiryDate { get; set; }
        public List<Cell> cells { get; set; }
        
        public class Cell
        {
            public string cellCode { get; set; }
            public string cellTitle { get; set; }
            public string latestEditionNumber { get; set; }
            public string latestUpdateNumber { get; set; }
        }
    }
}
