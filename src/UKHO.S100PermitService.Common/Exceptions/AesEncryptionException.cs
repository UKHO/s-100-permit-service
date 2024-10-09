using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Exceptions
{
    [ExcludeFromCodeCoverage]
    public class AesEncryptionException : Exception
    {
        public EventId EventId { get; set; }

        public object[] MessageArguments { get; }

        public AesEncryptionException(EventId eventId, string message, params object[] messageArguments) : base(message)
        {
            EventId = eventId;
            MessageArguments = messageArguments ?? [];
        }
    }
}