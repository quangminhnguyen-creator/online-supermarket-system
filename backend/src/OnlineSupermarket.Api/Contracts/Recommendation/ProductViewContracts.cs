using System.Text.Json.Serialization;

namespace OnlineSupermarket.Api.Contracts.Recommendation;

public sealed record RecordProductViewRequest(
    [property: JsonPropertyName("anonymousSessionId")] Guid AnonymousSessionId,
    [property: JsonPropertyName("branchId")] Guid? BranchId);

public sealed record MergeSessionRequest(
    [property: JsonPropertyName("anonymousSessionId")] Guid AnonymousSessionId);

public sealed record MergeSessionResponse(
    [property: JsonPropertyName("mergedCount")] int MergedCount);