using Microsoft.Extensions.Logging;
using Polly;

namespace Hollan.Function
{
    public static class ContextExtensions
    {
        private static readonly string LoggerKey = "LoggerKey";

        public static Context WithLogger(this Context context, ILogger logger)
        {
            context[LoggerKey] = logger;
            return context;
        }

        public static ILogger GetLogger(this Context context)
        {
            if (context.TryGetValue(LoggerKey, out object logger))
            {
                return logger as ILogger;
            }
            return null;
        }
    }
}