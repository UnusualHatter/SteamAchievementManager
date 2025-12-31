using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SAM.Game
{
    public static class SchedulerStorage
    {
        private static readonly string _FilePath;

        static SchedulerStorage()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var samDir = Path.Combine(appData, "SAM");
            if (!Directory.Exists(samDir))
            {
                Directory.CreateDirectory(samDir);
            }
            _FilePath = Path.Combine(samDir, "ScheduledAchievements.json");
        }

        public static List<ScheduledAchievement> Load()
        {
            if (!File.Exists(_FilePath))
            {
                return new List<ScheduledAchievement>();
            }

            try
            {
                var json = File.ReadAllText(_FilePath);
                return JsonConvert.DeserializeObject<List<ScheduledAchievement>>(json) ?? new List<ScheduledAchievement>();
            }
            catch (Exception)
            {
                return new List<ScheduledAchievement>();
            }
        }

        public static void Save(List<ScheduledAchievement> schedules)
        {
            try
            {
                var json = JsonConvert.SerializeObject(schedules, Formatting.Indented);
                File.WriteAllText(_FilePath, json);
            }
            catch (Exception)
            {
            }
        }
    }
}
