using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Promotion;
using OnlineSupermarket.Domain.Promotions;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class PromotionEndpoints
{
    public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder routes)
    {
        var adminGroup = routes.MapGroup("/api/admin/promotions")
            .WithTags("Admin-Promotions")
            .RequireAuthorization("AdminOnly");

        adminGroup.MapGet(string.Empty, ListPromotionsAsync)
            .WithName("ListPromotions")
            .Produces<PaginatedPromotionsDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        adminGroup.MapPost(string.Empty, CreatePromotionAsync)
            .WithName("CreatePromotion")
            .Produces<PromotionDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        adminGroup.MapPut("/{id:guid}", UpdatePromotionAsync)
            .WithName("UpdatePromotion")
            .Produces<PromotionDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static PromotionDto ToDto(Promotion p) => new(
        p.Id, p.Code, p.DiscountType.ToString(), p.DiscountValue, p.MinOrderAmount,
        p.UsageLimit, p.UsageCount, p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc);

    private static async Task<IResult> ListPromotionsAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var totalCount = await dbContext.Promotions.CountAsync(cancellationToken);
        var promotions = await dbContext.Promotions
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Results.Ok(new PaginatedPromotionsDto(
            page, pageSize, totalCount, promotions.Select(ToDto).ToList()));
    }

    private static async Task<IResult> CreatePromotionAsync(
        [FromBody] CreatePromotionRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DiscountType>(request.DiscountType, ignoreCase: true, out var discountType)
            || !Enum.IsDefined(typeof(DiscountType), discountType))
        {
            return Results.BadRequest(new { message = "Invalid discount type. Valid values: Percentage, FixedAmount." });
        }

        var normalizedCode = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(normalizedCode)
            && await dbContext.Promotions.AnyAsync(p => p.Code == normalizedCode, cancellationToken))
        {
            return Results.Conflict(new { message = "A promotion with this code already exists." });
        }

        Promotion promotion;
        try
        {
            promotion = Promotion.Create(
                normalizedCode, discountType, request.DiscountValue, request.MinOrderAmount, request.UsageLimit);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }

        dbContext.Promotions.Add(promotion);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/admin/promotions/{promotion.Id}", ToDto(promotion));
    }

    private static async Task<IResult> UpdatePromotionAsync(
        [FromRoute] Guid id,
        [FromBody] UpdatePromotionRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var promotion = await dbContext.Promotions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (promotion is null)
        {
            return Results.NotFound(new { message = "Promotion not found." });
        }

        try
        {
            promotion.Update(request.DiscountValue, request.MinOrderAmount, request.UsageLimit);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }

        if (request.IsActive) promotion.Activate();
        else promotion.Deactivate();

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToDto(promotion));
    }
}
