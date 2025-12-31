using System;

namespace SAM.Game
{
    [Serializable]
    public class ScheduledAchievement
    {
        public uint GameId { get; set; }
        public string AchievementId { get; set; }
        public string AchievementName { get; set; }
        public DateTime UnlockTime { get; set; }
        public bool IsUnlocked { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Newtonsoft.Json.JsonIgnore]
        public TimeSpan TimeRemaining => UnlockTime > DateTime.Now ? UnlockTime - DateTime.Now : TimeSpan.Zero;

        public override string ToString()
        {
            return $"{GameId}: {AchievementId} @ {UnlockTime}";
        }
    }
}
