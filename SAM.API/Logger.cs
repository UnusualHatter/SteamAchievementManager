using System;

namespace SAM.API
{
    public static class Logger
    {
        public static void Wipe()
        {
        }

        public static void Info(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        public static void Error(string message)
        {
            Console.WriteLine($"[ERROR] {message}");
        }

        public static void Warn(string message)
        {
            Console.WriteLine($"[WARN] {message}");
        }
    }
}
