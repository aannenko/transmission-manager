namespace TransmissionManager.BaseTests.HttpClient;

public sealed class UnexpectedTestRequestException : Exception
{
    public UnexpectedTestRequestException()
    {
    }

    public UnexpectedTestRequestException(string message) : base(message)
    {
    }

    public UnexpectedTestRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public UnexpectedTestRequestException(TestRequest request)
    {
        Request = request;
    }

    public TestRequest? Request { get; }
}
