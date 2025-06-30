using DevLife.Backend.Modules.BugChase;

namespace DevLife.Backend.Domain;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime BirthDate { get; set; }

    public string Zodiac { get; set; } = string.Empty;

    public string Stack { get; set; } = string.Empty;

    public string Experience { get; set; } = string.Empty;

    public int Points { get; set; } = 100;

    public int Streak { get; set; } = 0;

    public DateTime? LastCorrectGuessDate { get; set; }

    public Score? Score { get; set; }
}
