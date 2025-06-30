using DevLife.Backend.Common;
using DevLife.Backend.Domain;
using DevLife.Backend.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace DevLife.Backend.Modules.Auth;

public static class RegisterUserEndpoint
{
    public static IEndpointRouteBuilder MapRegisterUser(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (
            AppDbContext db,
            RegisterUserRequest request,
            IValidator<RegisterUserRequest> validator) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Results.BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));

            var allowedStacks = new[]
            {
                "C# / .NET", "Node.js", "Python", "Java",
                "Go", "Rust", "PHP", "Ruby", "Angular"
            };

            if (!allowedStacks.Contains(request.Stack))
                return Results.BadRequest($"Invalid stack. Allowed options: {string.Join(", ", allowedStacks)}");

            var existingUser = await db.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

            if (existingUser is not null)
                return Results.BadRequest("Username already taken");

            var tzId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "Georgian Standard Time"
                : "Asia/Tbilisi";

            var georgiaZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);

            var georgiaDateTime = DateTime.SpecifyKind(request.BirthDate, DateTimeKind.Unspecified);
            var utcBirthDate = TimeZoneInfo.ConvertTimeToUtc(georgiaDateTime, georgiaZone);

            var zodiac = ZodiacCalculator.CalculateZodiac(utcBirthDate);

            var user = new User
            {
                Username = request.Username,
                FirstName = request.FirstName,
                LastName = request.LastName,
                BirthDate = utcBirthDate,
                Zodiac = zodiac,
                Stack = request.Stack,
                Experience = request.Experience
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "User registered successfully",
                zodiac
            });
        });

        return app;
    }
}

public record RegisterUserRequest(
    string Username,
    string FirstName,
    string LastName,
    DateTime BirthDate,
    string Stack,
    string Experience
);
