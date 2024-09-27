namespace UKHO.S100PermitService.Common.Configuration
{
    public class WaitAndRetryConfiguration
    {
        public string RetryCount { get; set; }
        public string SleepDurationInSeconds { get; set; }
    }
}
