namespace TransmissionManager.BaseTests.HttpClient;

public sealed record TestRequest(
    HttpMethod Method,
    Uri? RequestUri,
    IReadOnlyDictionary<string, string>? Headers = null,
    string? Content = null)
{
    public bool Equals(TestRequest? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null || Method != other.Method || RequestUri != other.RequestUri || Content != other.Content)
            return false;

        if (Headers is null or { Count: 0 } || other.Headers is null or { Count: 0 })
            return Headers is null or { Count: 0 } && other.Headers is null or { Count: 0 };

        foreach (var (key, value) in Headers)
            if (!other.Headers.TryGetValue(key, out var otherValue) || value != otherValue)
                return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Method);
        hashCode.Add(RequestUri);
        hashCode.Add(Content);

        if (Headers is null or { Count: 0 })
            return hashCode.ToHashCode();

        foreach (var (key, value) in Headers)
        {
            hashCode.Add(key);
            hashCode.Add(value);
        }

        return hashCode.ToHashCode();
    }
}
