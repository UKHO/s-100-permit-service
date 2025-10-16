namespace UKHO.S100PermitService.API.FunctionalTests.Models
{
    public class S100PermitServiceRequestModel
    {
        public class ProductModel
        {
            public string ?productName { get; set; }
            public int editionNumber { get; set; }
            public string ?permitExpiryDate { get; set; }
        }

        public class RequestBodyModel
        {
            public List<ProductModel> ?products { get; set; }
            public List<UserPermitModel> ?userPermits { get; set; }
        }

        public class UserPermitModel
        {
            public string ?title { get; set; }
            public string ?upn { get; set; }
        }
    }
}
