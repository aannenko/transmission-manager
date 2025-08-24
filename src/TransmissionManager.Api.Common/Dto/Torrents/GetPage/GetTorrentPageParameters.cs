using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Direction = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageDirection;
using Order = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageOrder;

namespace TransmissionManager.Api.Common.Dto.Torrents;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
public readonly partial record struct GetTorrentPageParameters(
    [property: EnumDataType(typeof(Order))] Order OrderBy = Order.Id,
    long? AnchorId = null,
    string? AnchorValue = null,
    [property: Range(1, GetTorrentPageParameters._maxTake)] int Take = 20,
    [property: EnumDataType(typeof(Direction))] Direction Direction = Direction.Forward,
    [property: MinLength(1)] string? PropertyStartsWith = null,
    bool? CronExists = null) : IValidatableObject
{
    private const int _maxTake = 1000;
    private const string _iso8601DateRegexPattern =
        @"^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])T([01]\d|2[0-3]):[0-5]\d:[0-5]\d(\.\d{1,7})?Z$";

    public static int MaxTake => _maxTake;

    public static string Iso8601DateRegexPattern => _iso8601DateRegexPattern;

    private static readonly CompositeFormat _orderByAndAnchorValueErrorFormat = CompositeFormat.Parse(
        $"When {nameof(OrderBy)} is '{{0}}', {nameof(AnchorValue)} must be '{{1}}'.");

    private static readonly CompositeFormat _dateTimeAnchorValueErrorFormat = CompositeFormat.Parse(
        $"When {nameof(OrderBy)} is '{{0}}', {nameof(AnchorValue)} must match '{{1}}'.");

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AnchorValue is null)
            yield break;

        if (OrderBy is Order.Id or Order.IdDesc)
        {
            yield return new ValidationResult(
                string.Format(null, _orderByAndAnchorValueErrorFormat, OrderBy, "null"),
                [nameof(AnchorValue)]);
        }
        else if (OrderBy is Order.RefreshDate or Order.RefreshDateDesc && !Iso8601DateRegex().IsMatch(AnchorValue))
        {
            yield return new ValidationResult(
                string.Format(null, _dateTimeAnchorValueErrorFormat, OrderBy, _iso8601DateRegexPattern),
                [nameof(AnchorValue)]);
        }
    }

    [GeneratedRegex(_iso8601DateRegexPattern, RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture, 50)]
    private static partial Regex Iso8601DateRegex();
}
