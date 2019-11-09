using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Hollan.Function.CircuitLibrary;
using Polly.Retry;

namespace Hollan.Function
{
    public class EventHubTrigger
    {
        private readonly AsyncRetryPolicy _policy;
        private readonly InstanceId _instance;
        private readonly HttpClient _client;

        private static readonly string circuitRequestUri = Environment.GetEnvironmentVariable("CircuitRequestUri");
        private static readonly string resourceId = Environment.GetEnvironmentVariable("ResourceId");
        private static readonly string appName = resourceId.Split('/').Last();
        private static readonly string circuitCode = Environment.GetEnvironmentVariable("CircuitCode");
        
        public EventHubTrigger(AsyncRetryPolicy policy, InstanceId instance, IHttpClientFactory factory)
        {
            _policy = policy;
            _instance = instance;
            _client = factory.CreateClient(); 
        }

        [FunctionName("EventHubTrigger")]
        public async Task Run([EventHubTrigger("events", Connection = "EventHubConnectionString")] EventData[] events, ILogger log)
        {
            Context context = new Context().WithLogger(log);

            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    await _policy.ExecuteAsync(
                        async ctx =>
                        {
                            string messageBody = Encoding.UTF8.GetString(
                                bytes: eventData.Body.Array,
                                index: eventData.Body.Offset,
                                count: eventData.Body.Count);

                            // Replace these two lines with your processing logic.
                            log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");
                            var message = JsonConvert.DeserializeObject<dynamic>(messageBody);

                            if ((bool) message["ThrowException"])
                            {
                                throw new Exception("I'm throwing a fit");
                            }

                            await Task.Yield();

                        },
                        context);

                }
                catch (Exception e)
                {
                    await _client.PostAsJsonAsync($"{circuitRequestUri}/{appName}?op=AddFailure&code={circuitCode}", new FailureRequest
                    {
                        RequestId = $"{eventData.SystemProperties.PartitionKey}:{eventData.SystemProperties.Offset}",
                        FailureTime = DateTime.UtcNow,
                        InstanceId = _instance.Guid,
                        ResourceId = resourceId
                    });
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
