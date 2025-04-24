namespace ai_agents_hack_tariffed.ApiService.Tools
{
   public static class Utils
    {
        public static void LogGreen(string message) => Console.WriteLine($"\u001b[32m{message}\u001b[0m");
        public static void LogPurple(string message) => Console.WriteLine($"\u001b[35m{message}\u001b[0m");
        public static void LogBlue(string message) => Console.WriteLine($"\u001b[34m{message}\u001b[0m");
    }
}
