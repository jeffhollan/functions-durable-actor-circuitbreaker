using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

[assembly: FunctionsStartup(typeof(Hollan.Function.Startup))]

namespace Hollan.Function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton(sp => {
                return Policy
                    .Handle<Exception>()
                    .RetryAsync(3, onRetry: (ex, retryCount, context) => {
                        var log = context.GetLogger();
                        log?.LogInformation($"Retrying message attempt {retryCount} for exception {ex}");
                    });
            });
            builder.Services.AddSingleton(sp => new InstanceId{Guid = Guid.NewGuid().ToString()});
            builder.Services.AddHttpClient();
        }
    }

    public class InstanceId
    {
        public string Guid;
    }
}