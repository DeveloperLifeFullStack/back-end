using DevLife.Backend.Common;
using DevLife.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevLife.Backend.Modules.Auth;

public static class LoginUserEndpoint
{
    public static IEndpointRouteBuilder MapLoginUser(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            AppDbContext db,
            JwtService jwt,
            LoginUserRequest request) =>
        {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

            if (user is null)
                return Results.NotFound(new { message = "User not found" });

            var token = jwt.GenerateToken(user.Id, user.Username);
            var (prediction, codingTip, luckyTech) = ZodiacCalculator.GetDailyInfo(user.Zodiac);
            var emoji = ZodiacCalculator.GetZodiacEmoji(user.Zodiac);

            return Results.Ok(new
            {
                message = $"Welcome back, {user.FirstName}!",
                token,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.FirstName,
                    user.LastName,
                    user.BirthDate,
                    user.Zodiac,
                    user.Stack,
                    Emoji = emoji,
                    user.Points
                },
                zodiacInfo = new
                {
                    Prediction = prediction,
                    CodingTip = codingTip,
                    LuckyTechnology = luckyTech
                }
            });
        });

        return app;
    }
}

public record LoginUserRequest(string Username);
