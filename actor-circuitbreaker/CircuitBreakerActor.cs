using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Converters;
using Microsoft.Extensions.Logging;
using Hollan.Function.CircuitLibrary;
using System.Net.Http;

namespace Hollan.Function
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Circuit
    {   
        private readonly ILogger _log;
        private readonly IDurableClient _durableClient;
        public Circuit(IDurableClient client, ILogger log)
        {
            _durableClient = client;
            _log = log;
        }

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public CircuitState state = CircuitState.Open;

        // Current rolling window of failures reported for this circuit
        [JsonProperty]
        public IDictionary<string, FailureRequest> FailureWindow = new Dictionary<string, FailureRequest>();

        // The TimeSpan difference from latest to keep failures in the window
        private static readonly TimeSpan windowSize = TimeSpan.Parse(Environment.GetEnvironmentVariable("WindowSize"));

        // The number of failures in the window until closing the circuit
        private static readonly int failureThreshold = int.Parse(Environment.GetEnvironmentVariable("FailureThreshold"));
        public void OpenCircuit() => state = CircuitState.Open;
        public async Task AddFailure(FailureRequest req)
        {
            if(state == CircuitState.Closed)
            {
                _log.LogInformation($"Tried to add additional failure to {Entity.Current.EntityKey} that is already closed");
                return;
            }

            FailureWindow.Add(req.RequestId, req);

            var cutoff = req.FailureTime.Subtract(windowSize);

            // Filter the window only to exceptions within the cutoff timespan
            FailureWindow = FailureWindow.Where(p => p.Value.FailureTime >= cutoff).ToDictionary( p => p.Key, p => p.Value);

            if(FailureWindow.Count >= failureThreshold)
            {
                _log.LogCritical($"Break this circuit for entity {Entity.Current.EntityKey}!");

                await _durableClient.StartNewAsync(nameof(CloseCircuitOrchestrator.CloseCircuit), req.ResourceId);

                // Mark the circuit as close
                state = CircuitState.Closed;
            }
            else 
            {
                _log.LogInformation($"The circuit {Entity.Current.EntityKey} currently has {FailureWindow.Count} exceptions in the window of {windowSize.ToString()}");
            }
        }

        [FunctionName(nameof(Circuit))]
        public static Task Run(
            [EntityTrigger] IDurableEntityContext ctx,
            [DurableClient] IDurableClient client,
            ILogger log) => ctx.DispatchAsync<Circuit>(client, log);
        
    }
}