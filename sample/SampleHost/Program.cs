﻿// Copyright (c) .NET Foundation. All rights reserved.
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
using SampleHost.Models;

namespace SampleHost
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
                    .AddAzureStorage()
                    .AddServiceBus();
                    //.AddEventHubs();
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
                })                
                .UseConsoleLifetime();

            var host = builder.Build();
            using (host)
            {
                
                var sb = new ServiceBusConnectionStringBuilder(host.Services.GetService<IConfiguration>()?.GetConnectionString("ServiceBus"));
                sb.EntityPath = "test-session-queue";
              
                var queueClient = new QueueClient(sb);
           
                var state = new SessionState();
                //Send to bus 10 messages for 10 sessions (per user)
                for (var usr = 0; usr < 10; usr++)
                {
                    for (var i = 0; i < 10; i++)
                    {
                        var content = new WorkItem() { Category = 123, ID = $"{usr}-{i}", Description = $"session:{usr}", Priority = 1, Region = "IDF", SessionId= $"{usr}" };
                        var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content)));
                        
                        message.ContentType = "application/json";
                        message.SessionId = content.SessionId;
                        state.AddOrUpdate(message.SessionId, i);
                        // Send the message to the queue
                        await queueClient.SendAsync(message);
                    }
                }
                await host.RunAsync();
                foreach (var usr in state.Content)
                {
                    Console.WriteLine($"sessionId: {usr.Key},count: {usr.Value}");
                }
                Console.ReadLine();
                await queueClient.CloseAsync();                
            }
        }
    }
}
