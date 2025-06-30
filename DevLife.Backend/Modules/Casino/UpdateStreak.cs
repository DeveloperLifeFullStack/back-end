using DevLife.Backend.Domain;

namespace DevLife.Backend.Modules.Casino
{
    public static class UpdateStreak
    {
        public static void ApplyStreak(User user, bool isCorrect)
        {
            if (isCorrect)
            {
                user.Streak += 1;
                user.LastCorrectGuessDate = DateTime.UtcNow;
            }
            else
            {
                user.Streak = 0;
            }
        }
    }
}
