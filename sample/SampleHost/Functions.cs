// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.InteropExtensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus.Triggers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SampleHost.Filters;
using SampleHost.Models;

namespace SampleHost
{
    [ErrorHandler]
    public class Functions
    {


        private readonly ISampleServiceA _sampleServiceA;
        private readonly ISampleServiceB _sampleServiceB;
        private readonly SessionState _sessionState;

        public Functions(ISampleServiceA sampleServiceA, ISampleServiceB sampleServiceB, SessionState state)
        {
            _sampleServiceA = sampleServiceA;
            _sampleServiceB = sampleServiceB;
            _sessionState = state;
        }

        public async Task ProcessWorkItem_ServiceBus(
           [ServiceBusTrigger("test-classical-queue")] WorkItem message,
           string messageId,
           int deliveryCount,         
           ILogger log)

        {
            log.LogInformation($"Processing ServiceBus message (Id={messageId}, DeliveryCount={deliveryCount})");
            _sessionState.AddOrUpdate("-", message.Step);
            await Task.Delay(100);

            log.LogInformation($"Message complete (Id={messageId})");
        }

        public async Task ProcessWorkItem_Session_ServiceBus(
            [ServiceBusSessionTrigger("test-session-queue")] WorkItem message, string messageId, int deliveryCount, IMessageSession messageSession, ILogger log)

        {
            try
            {
                //check session instance correlation 
                if (messageSession.SessionId != message.SessionId)
                {
                    throw new System.Exception("Session Id conflict");
                }

                log.LogInformation($"Processing ServiceBus message (Id={messageId}, DeliveryCount={deliveryCount})");
                _sessionState.AddOrUpdate(message.SessionId, message.Step);
                await Task.Delay(100);
                log.LogInformation($"Message complete (Id={messageId})");

                // SessionHandlerOptions.AutoComplete should be false by default to accomplish session close manually (when any business condition is verified)
                if (message.Step == 4)
                {
                    await messageSession.CloseAsync();
                }
            }
            catch (Exception ex) {

                log.LogError($"Message exception (Id={messageId} {ex.Message})");
            }
        }    
    }
}
