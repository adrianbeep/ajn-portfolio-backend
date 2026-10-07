using Application.Contributions.Commands.CreateContribution;
using Application.Contributions.Commands.DeleteContribution;
using Application.Contributions.Commands.RestoreContribution;
using Application.Contributions.Commands.UpdateContribution;
using Application.Contributions.Dtos;
using Application.Contributions.Queries.GetAllContributionsForAdmin;
using Application.Contributions.Queries.GetContributionBySlug;
using Api.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints;

public static class AdminContributionEndpoints
{
    public static void MapAdminContributionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/admin/contributions")
            .WithTags("Contributions (admin)")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (ISender sender) =>
        {
            var query = new GetAllContributionsForAdminQuery();
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

        group.MapPost("/", async (
            [FromBody] CreateContributionRequest request,
            ISender sender) =>
        {
            var command = new CreateContributionCommand(
                request.Title,
                request.Slug,
                request.UrlThumbnail,
                request.UrlPrContribution,
                request.Description,
                request.Content);
            var result = await sender.Send(command);

            if (!result.IsSuccess)
                return result.ToHttpResult();

            return Results.Created(
                $"/contributions/{request.Slug}",
                new { id = result.Value });
        });

        group.MapPut("/{slug}", async (
            [FromRoute] string slug,
            [FromBody] UpdateContributionRequest request,
            ISender sender) =>
        {
            var command = new UpdateContributionCommand(
                slug,
                request.Title,
                request.Description,
                request.UrlThumbnail,
                request.UrlPrContribution,
                request.Content);
            var result = await sender.Send(command);
            return result.ToHttpResult();
        });

        group.MapDelete("/{slug}", async (
            [FromRoute] string slug,
            ISender sender) =>
        {
            var command = new DeleteContributionCommand(slug);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : result.ToHttpResult();
        });

        group.MapPost("/{slug}/restore", async (
            [FromRoute] string slug,
            ISender sender) =>
        {
            var command = new RestoreContributionCommand(slug);
            var result = await sender.Send(command);
            return result.ToHttpResult();
        });
    }
}