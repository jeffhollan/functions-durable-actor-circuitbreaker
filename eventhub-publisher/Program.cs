using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;

namespace publisher_net
{
    class Program
    {
        private static EventHubClient eventHubClient;
        static async Task Main(string[] args)
        {
            
            string eventHubConnectionString = System.Environment.GetEnvironmentVariable("EventHubConnectionString");
            string eventHubName = System.Environment.GetEnvironmentVariable("EventHubName");
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(eventHubConnectionString) {
                EntityPath = eventHubName
            };

            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
            Console.WriteLine($"Sending {args[0]} messages to event hub stream....");
            
            var eventList = new List<EventData>();
            var uuid = Guid.NewGuid().ToString();
            for(int x = 0; x < int.Parse(args[0]); x++) {
                var message = new {
                    ActivityID = uuid,
                    MessageNumber = x,
                    PublishTime = ToRfc3339String(DateTime.UtcNow),
                    ThrowException = x % 10 == 0
                };
                eventList.Add(new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message))));
                if(eventList.Count >= 100) {
                    await SendEvents(eventList);
                    eventList.Clear();
                }
            }
            if(eventList.Count > 0)
            {
                await SendEvents(eventList);
            }
            
        }

        private static string ToRfc3339String(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", DateTimeFormatInfo.InvariantInfo);
        }

        private static async Task SendEvents(IList<EventData> eventList) {
            // Console.WriteLine($"Sending {eventList.Count} events...");
            await eventHubClient.SendAsync(eventList);
        }
    }
}
