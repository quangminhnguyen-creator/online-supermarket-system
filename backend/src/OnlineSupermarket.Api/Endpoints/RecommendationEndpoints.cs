using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Recommendation;
using OnlineSupermarket.Domain.Recommendations;
using OnlineSupermarket.Infrastructure.Persistence;
using OnlineSupermarket.Infrastructure.Recommendations;

namespace OnlineSupermarket.Api.Endpoints;

public static class RecommendationEndpoints
{
    public static IEndpointRouteBuilder MapRecommendationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api").WithTags("Recommendations");

        group.MapPost("/products/{productId:guid}/view-events", RecordProductViewAsync)
            .WithName("RecordProductViewEvent")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/recommendations/session/merge", MergeSessionAsync)
            .WithName("MergeRecommendationSession")
            .RequireAuthorization()
            .Produces<MergeSessionResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return routes;
    }

    private static async Task<IResult> RecordProductViewAsync(
        [FromRoute] Guid productId,
        [FromBody] RecordProductViewRequest request,
        [FromServices] AppDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (request.AnonymousSessionId == Guid.Empty)
        {
            return Results.BadRequest(new { message = "Anonymous session id is required." });
        }

        var productExists = await dbContext.Products
            .AnyAsync(p => p.Id == productId, cancellationToken);

        if (!productExists)
        {
            return Results.NotFound(new { message = "Product not found." });
        }

        var userId = TryGetUserId(httpContext.User);
        var view = userId.HasValue
            ? ProductViewEvent.Create(productId, userId, null, request.BranchId, DateTime.UtcNow)
            : ProductViewEvent.Create(productId, null, request.AnonymousSessionId, request.BranchId, DateTime.UtcNow);

        dbContext.ProductViewEvents.Add(view);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Accepted();
    }

    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }

    private static async Task<IResult> MergeSessionAsync(
        [FromBody] MergeSessionRequest request,
        [FromServices] IProductViewEventStore viewEventStore,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (request.AnonymousSessionId == Guid.Empty)
        {
            return Results.BadRequest(new { message = "Anonymous session id is required." });
        }

        var userId = TryGetUserId(user);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var mergedCount = await viewEventStore.MergeAnonymousSessionAsync(
            request.AnonymousSessionId, userId.Value, cancellationToken);

        return Results.Ok(new MergeSessionResponse(mergedCount));
    }
}