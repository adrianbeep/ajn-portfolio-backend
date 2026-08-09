using Api.Common;                                                                                                                                                                                                                    
using Application.Authentication.Commands.Register;                                                                                                                                                                                  
using Application.Authentication.Queries.Login;                                                                                                                                                                                      
using Application.Abstractions.Authentication; // Asumo que IBootstrapTokenValidator está por acá                                                                                                                                    
using MediatR;                                                                                                                                                                                                                       
using Microsoft.AspNetCore.Builder;                                                                                                                                                                                                  
using Microsoft.AspNetCore.Http;                                                                                                                                                                                                     
using Microsoft.AspNetCore.Mvc;                                                                                                                                                                                                      
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints;

public static class AuthEndpoint
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        // Agrupo todo bajo "/admin/auth" para no repetir la ruta en cada uno
        var group = routes.MapGroup("/admin/auth").WithTags("Auth");
        
        // Endpoint Register
        group.MapPost("/register", async (
            [FromBody] RegisterCommand command,
            [FromHeader(Name = "X-Bootstrap-Token")]
            string? bootstrapToken,
            IBootstrapTokenValidator validator,
            ISender sender,
            HttpContext context) =>
        {
            if (string.IsNullOrEmpty(bootstrapToken) || !await validator.IsValid(bootstrapToken))
            {
                return Results.Unauthorized();
            }

            var result = await sender.Send(command);

            if (result.IsSuccess)
            {
                context.SetAuthCookie(result.Value.Token, result.Value.ExpiresAt);
            }

            return result.ToHttpResult();
        });
        
        // Endpoint Login
        group.MapPost("/login", async (
            [FromBody] LoginQuery query,
            ISender sender,
            HttpContext context) =>
        {
            var result = await sender.Send(query);

            if (result.IsSuccess)
            {
                context.SetAuthCookie(result.Value.Token, result.Value.ExpiresAt);
            }
            
            return result.ToHttpResult();
        });
        
        // Endpoint de Logout
        group.MapPost("/logout", (HttpContext context) =>
        {
            context.ClearAuthCookie();
            return Results.Ok(new { message = "Logged out successfully" });
        });
    }
}
