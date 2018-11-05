// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SampleHostServiceBus.Models;

namespace SampleHostServiceBus
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .UseEnvironment("Development")
                .ConfigureWebJobs(b =>
                {
                b
                .AddAzureStorageCoreServices()              
                .AddServiceBus()
                .AddServiceBusSession(c => c.SessionHandlerOptions.MaxConcurrentSessions = 2);
                    
                })
                .ConfigureAppConfiguration(b =>
                {
                    b.AddJsonFile("appsettings.development.json");
                    // Adding command line as a configuration source
                    b.AddCommandLine(args);
                })
                .ConfigureLogging((context, b) =>
                {
                    b.SetMinimumLevel(LogLevel.Debug);
                    b.AddConsole();

                    // If this key exists in any config, use it to enable App Insights
                    string appInsightsKey = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                    if (!string.IsNullOrEmpty(appInsightsKey))
                    {
                        b.AddApplicationInsights(o => o.InstrumentationKey = appInsightsKey);
                    }
                })
                .ConfigureServices(services =>
                {
                    // add some sample services to demonstrate job class DI
                    services.AddSingleton<ISampleServiceA, SampleServiceA>();
                    services.AddSingleton<ISampleServiceB, SampleServiceB>();
                    services.AddSingleton<SessionState>();

                })                
                .UseConsoleLifetime();

            var host = builder.Build();
            using (host)
            {
                var sessionState = host.Services.GetService<SessionState>().Content;
                var classicalClientConnection = new ServiceBusConnectionStringBuilder(host.Services.GetService<IConfiguration>()?.GetConnectionString("ServiceBus"));
                classicalClientConnection.EntityPath = "test-classical-queue";
                var classicalQueueClient = new QueueClient(classicalClientConnection);
                var sessionClientConnection = new ServiceBusConnectionStringBuilder(host.Services.GetService<IConfiguration>()?.GetConnectionString("ServiceBus"));
                sessionClientConnection.EntityPath = "test-session-queue";              
                var sessionQueueClient = new QueueClient(sessionClientConnection);
              

                //Send to bus 10 messages for 10 sessions (per user)
                Console.WriteLine("Sending 5 message for 5 sessions");
                for (var usr = 0; usr < 5; usr++)
                {
                    for (var i = 0; i < 5; i++)
                    {
                        var content = new WorkItem() { Category = 123, ID = $"{usr}-{i}", Description = $"session:{usr}", Priority = 1, Region = "IDF", SessionId= $"{usr}", Step=i };
                        var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content)));
                        
                        message.ContentType = "application/json";
                        message.SessionId = content.SessionId;
                        await classicalQueueClient.SendAsync(message);
                        // Send the message to the queue
                        await sessionQueueClient.SendAsync(message);
                    }
                }
                await host.RunAsync().ContinueWith(t=> {
                    Console.WriteLine($"messages handled:");
                    Console.WriteLine($"====================================");
                    foreach (var usr in sessionState)
                    {
                        Console.WriteLine($"sessionId: {usr.Key} \t|\tcount: {usr.Value}");
                    }
                    Console.WriteLine($"====================================");
                });
               
                Console.ReadLine();



                await sessionQueueClient.CloseAsync();                
            }
        }
    }
}
