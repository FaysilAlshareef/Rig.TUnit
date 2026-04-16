using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Rig.TUnit.WebAPI.Tests.Unit.TestInfrastructure;

/// <summary>
/// Test endpoints used by HttpClientHelper and Authentication tests — one of each common HTTP verb,
/// plus an authorized endpoint for exercising the auth pipeline.
/// </summary>
public static class TestEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/echo/{message}", (string message) => Results.Ok(new EchoResponse(message)));

        app.MapPost("/echo", (EchoRequest request) => Results.Ok(new EchoResponse(request.Message)));

        app.MapPut("/echo/{id:int}", (int id, EchoRequest request) =>
            Results.Ok(new EchoResponse($"{id}:{request.Message}")));

        app.MapDelete("/echo/{id:int}", (int id) => Results.Ok(new EchoResponse($"deleted:{id}")));

        // Authorized endpoint — returns the authenticated user's Name claim.
        app.MapGet("/secure/me", (HttpContext ctx) =>
            Results.Ok(new EchoResponse(ctx.User.Identity?.Name ?? string.Empty)))
            .RequireAuthorization();

        // Echoes the Authorization header so tests can verify WithBearerToken behavior end-to-end.
        app.MapGet("/headers/authorization", (HttpContext ctx) =>
            Results.Ok(new EchoResponse(ctx.Request.Headers.Authorization.ToString())));

        // Echoes an arbitrary header so tests can verify WithHeader behavior end-to-end.
        app.MapGet("/headers/{name}", (string name, HttpContext ctx) =>
            Results.Ok(new EchoResponse(ctx.Request.Headers[name].ToString())));
    }

    public sealed record EchoRequest(string Message);

    public sealed record EchoResponse(string Message);
}
