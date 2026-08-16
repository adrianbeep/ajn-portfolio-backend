using Application.Projects.Queries.GetProjectBySlug;
using Application.Projects.Queries.GetProjects;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Api.Common;

namespace Api.Endpoints;

public static class PublicProjectEndpoints
{
    public static void MapPublicProjectEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/projects").WithTags("Projects (public)");

        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender) =>
        {
            var query = new GetProjectsQuery(page ?? 1, pageSize ?? 20);
            var result = await sender.Send(query);
            return result.ToHttpResult();
        });

        group.MapGet("/{slug}", async (
            [FromRoute] string slug,
            ISender sender) =>
        {
            var query = new GetProjectBySlugQuery(slug);
            var result = await sender.Send(query);
            return result.ToHttpResult();
        });
    }
}
