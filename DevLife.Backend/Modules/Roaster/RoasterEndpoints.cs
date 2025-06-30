using DevLife.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace DevLife.Backend.Modules.Roaster;

public static class RoasterEndpoints
{
    public static IEndpointRouteBuilder MapRoasterEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/get-task", async (
            [FromServices] AiRoaster ai,
            [FromQuery] string language,
            [FromQuery] string difficulty) =>
        {
            var task = await ai.GenerateTaskAsync(language, difficulty);
            return Results.Ok(task);
        });

        app.MapPost("/submit-code", async (
            [FromServices] AiRoaster ai,
            [FromBody] RoastRequest req) =>
        {
            var (review, rating) = await ai.EvaluateCodeAsync(req.Code);
            return Results.Ok(new
            {
                message = review,
                rating = rating
            });
        });

        return app;
    }

    public record RoastRequest(string Code);
}