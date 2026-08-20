using Application.Projects.Commands.CreateProject;
using Application.Projects.Commands.DeleteProject;
using Application.Projects.Commands.RestoreProject;
using Application.Projects.Commands.UpdateProject;
using Application.Projects.Dtos;
using Application.Projects.Queries.GetAllProjectsForAdmin;
using Api.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints;

public static class AdminProjectEndpoints
{
    public static void MapAdminProjectEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/admin/projects")
            .WithTags("Projects (admin)")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (ISender sender) =>
        {
            var query = new GetAllProjectsForAdminQuery();
            var result = await sender.Send(query);
            return result.ToHttpResult();
        });

        group.MapPost("/", async (
            [FromBody] CreateProjectRequest request,
            ISender sender) =>
        {
            var command = new CreateProjectCommand(
                request.Title,
                request.Slug,
                request.UrlThumbnail,
                request.Description,
                request.Content);
            var result = await sender.Send(command);

            if (!result.IsSuccess)
                return result.ToHttpResult();

            return Results.Created(
                $"/projects/{request.Slug}",
                new { id = result.Value });
        });

        group.MapPut("/{slug}", async (
            [FromRoute] string slug,
            [FromBody] UpdateProjectRequest request,
            ISender sender) =>
        {
            var command = new UpdateProjectCommand(
                slug,
                request.Title,
                request.Description,
                request.UrlThumbnail,
                request.Content);
            var result = await sender.Send(command);
            return result.ToHttpResult();
        });

        group.MapDelete("/{slug}", async (
            [FromRoute] string slug,
            ISender sender) =>
        {
            var command = new DeleteProjectCommand(slug);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : result.ToHttpResult();
        });

        group.MapPost("/{slug}/restore", async (
            [FromRoute] string slug,
            ISender sender) =>
        {
            var command = new RestoreProjectCommand(slug);
            var result = await sender.Send(command);
            return result.ToHttpResult();
        });
    }
}