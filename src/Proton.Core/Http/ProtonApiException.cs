using System.Net;

namespace Proton.Core.Http;

public sealed class ProtonApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public int ApiCode { get; }

    public ProtonApiException(HttpStatusCode statusCode, int apiCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ApiCode = apiCode;
    }
}
