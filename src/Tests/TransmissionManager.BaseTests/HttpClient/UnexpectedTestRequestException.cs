namespace TransmissionManager.BaseTests.HttpClient;

public sealed class UnexpectedTestRequestException(TestRequest request) : Exception
{
    public TestRequest Request { get; } = request;
}
