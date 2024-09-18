namespace CSharpModBase
{
    public static class Log
    {
        private static string DateTimeString => DateTime.Now.ToString("MM-dd HH:mm:ss");  // .fff

        public static void Info(string message)
        {
            Console.WriteLine($"{DateTimeString} [I] {message}");
        }

        public static void Debug(string message)
        {
            Console.WriteLine($"{DateTimeString} [D] {message}");
        }

        public static void Warn(string message)
        {
            Console.WriteLine($"{DateTimeString} [W] {message}");
        }

        public static void WarnIf(bool condition, string message)
        {
            if (condition) {
                Warn(message);
            }
        }

        public static void Error(string message)
        {
            Console.Error.WriteLine($"{DateTimeString} [E] {message}");
        }

        public static void Error(Exception e)
        {
            Error(e.Message);
        }
    }
}
