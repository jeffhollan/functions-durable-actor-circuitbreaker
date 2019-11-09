using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Hollan.Function
{
    public class OpenCircuitOrchestrator
    {
        
        [FunctionName(nameof(OpenCircuit))]
        public async Task OpenCircuit(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            if(!context.IsReplaying) log.LogInformation("Disabling function app to open circuit");

            var resourceId = context.GetInput<string>();

             var stopFunctionRequest = new DurableHttpRequest(
                HttpMethod.Post, 
                new Uri($"https://management.azure.com{resourceId}/stop?api-version=2016-08-01"),
                tokenSource: new ManagedIdentityTokenSource("https://management.core.windows.net"));
            
             DurableHttpResponse restartResponse = await context.CallHttpAsync(stopFunctionRequest);
            
             if (restartResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ArgumentException($"Failed to stop Function App: {restartResponse.StatusCode}: {restartResponse.Content}");
            }

            if(!context.IsReplaying) log.LogInformation("Function disabled");
        }
    }
}