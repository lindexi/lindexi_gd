using System;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    internal static class LoggerExtension
    {
        public static void Debug(this ILogger logger, string message)
        {
            Console.WriteLine(message);
        }

        public static void Trace(this ILogger logger, string message)
        {
            Console.WriteLine(message);
        }

        public static void Error(this ILogger logger, string message)
        {
            Console.WriteLine(message);
        }

        public static void Error(this ILogger logger, Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}
