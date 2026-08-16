using Application.Contributions.Queries.GetContributionBySlug;
using Application.Contributions.Queries.GetContributions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Api.Common;

namespace Api.Endpoints;

public static class PublicContributionEndpoints
{
    public static void MapPublicContributionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/contributions").WithTags("Contributions (public)");

        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender) =>
        {
            var query = new GetContributionsQuery(page ?? 1, pageSize ?? 20);
            var result = await sender.Send(query);
            return result.ToHttpResult();
        });

        group.MapGet("/{slug}", async (
            [FromRoute] string slug,
            ISender sender) =>
        {
            var query = new GetContributionBySlugQuery(slug);
            var result = await sender.Send(query);
            return result.ToHttpResult();
        });
    }
}
