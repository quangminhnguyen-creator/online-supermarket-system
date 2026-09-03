using System.Text.Json.Serialization;

namespace OnlineSupermarket.Api.Contracts.Inventory;

public sealed record InventoryTransactionDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("branchInventoryId")] Guid BranchInventoryId,
    [property: JsonPropertyName("transactionType")] string TransactionType,
    [property: JsonPropertyName("quantityOnHandDelta")] int QuantityOnHandDelta,
    [property: JsonPropertyName("reservedQuantityDelta")] int ReservedQuantityDelta,
    [property: JsonPropertyName("quantityOnHandAfter")] int QuantityOnHandAfter,
    [property: JsonPropertyName("reservedQuantityAfter")] int ReservedQuantityAfter,
    [property: JsonPropertyName("referenceType")] string ReferenceType,
    [property: JsonPropertyName("referenceId")] Guid? ReferenceId,
    [property: JsonPropertyName("actorUserId")] Guid? ActorUserId,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc);

public sealed record PaginatedInventoryTransactionsDto(
    [property: JsonPropertyName("data")] IReadOnlyList<InventoryTransactionDto> Data,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("pageSize")] int PageSize);