using DevLife.Backend.Common.Extensions;
using DevLife.Backend.Domain;
using DevLife.Backend.Helpers;
using DevLife.Backend.Modules.Casino;
using DevLife.Backend.Persistence;
using DevLife.Backend.Services;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace DevLife.Backend.Features.CodeCasino;

public static class SnippetEndpoint
{
    public static RouteGroupBuilder MapSnippetEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/generate", GenerateSnippets);
        group.MapPost("/choose", ChooseSnippet);
        return group;
    }

    public record GenerateSnippetRequest(string Language, string Experience);
    public record ChooseSnippetRequest(string SnippetId, string SelectedOption, int PointsBet);

    private static async Task<IResult> GenerateSnippets(
    GenerateSnippetRequest request,
    AiSnippetService aiService,
    MongoDbContext mongo,
    AppDbContext db,
    HttpContext ctx)
    {
        int userId = ctx.User.GetUserId();
        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return Results.Unauthorized();

        var (correctSnippet, buggySnippet) = await aiService.GenerateSnippetsAsync(request.Language, request.Experience);

        var pair = new[] { ("A", correctSnippet), ("B", buggySnippet) }
                   .OrderBy(_ => Guid.NewGuid())
                   .ToArray();

        string correctOption = pair[0].Item2 == correctSnippet ? "A" : "B";

        var optionA = pair[0].Item2;
        var optionB = pair[1].Item2;

        var snippet = new CodeSnippet
        {
            UserId = userId,
            Language = request.Language,
            CorrectSnippet = correctSnippet,
            BuggySnippet = buggySnippet,
            CorrectOption = correctOption

        };

        await mongo.Snippets.InsertOneAsync(snippet);

        return Results.Ok(new
        {
            SnippetId = snippet.Id,
            OptionA = optionA,
            OptionB = optionB,
            CorrectOne = correctOption
        });
    }


    private static async Task<IResult> ChooseSnippet(
    ChooseSnippetRequest request,
    MongoDbContext mongo,
    AppDbContext db,
    HttpContext ctx)
    {
        int userId = ctx.User.GetUserId();
        var user = await db.Users.FindAsync(userId);
        if (user is null) return Results.Unauthorized();

        var snippet = await mongo.Snippets
            .Find(s => s.Id == request.SnippetId && s.UserId == userId)
            .FirstOrDefaultAsync();

        if (snippet is null)
            return Results.NotFound("Snippet not found or not yours.");

        if (request.PointsBet > user.Points)
            return Results.BadRequest("You cannot bet more points than you currently have.");

        if (snippet.HasGuessed)
            return Results.BadRequest("You already guessed.");

        var selectedOption = request.SelectedOption.ToUpperInvariant();

        if (selectedOption != "A" && selectedOption != "B")
            return Results.BadRequest("Invalid option selected. Choose 'A' or 'B'.");

        bool isCorrect = selectedOption == snippet.CorrectOption;

        var multiplier = ZodiacLuck.GetMultiplier(user.Zodiac);
        var reward = isCorrect ? request.PointsBet * 2 * multiplier : -request.PointsBet;

        user.Points += (int)reward;

        UpdateStreak.ApplyStreak(user, isCorrect);

        snippet.HasGuessed = true;

        db.Users.Update(user);
        await mongo.Snippets.ReplaceOneAsync(s => s.Id == snippet.Id, snippet);

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            IsCorrect = isCorrect,
            ChosenOption = selectedOption,
            PointsChanged = reward,
            NewTotal = user.Points,
            Streak = user.Streak
        });
    }

}