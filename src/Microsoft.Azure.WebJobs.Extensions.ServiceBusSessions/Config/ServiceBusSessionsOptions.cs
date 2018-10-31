// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs.ServiceBus;

namespace Microsoft.Azure.WebJobs.ServiceBusSessions
{
    /// <summary>
    /// Configuration options for the ServiceBus extension.
    /// </summary>
    public class ServiceBusSessionsOptions : ServiceBusOptions
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public ServiceBusSessionsOptions() : base()
        {
            
            SessionHandlerOptions = new SessionHandlerOptions(ExceptionReceivedHandler)
            {
                // SessionHandlerOptions.AutoComplete should be false by default to accomplish session close manually (when any business condition is verified)
                AutoComplete = false,
                 MaxConcurrentSessions = 16
            };
        }

      

        /// <summary>
        /// Gets or sets the default <see cref="Azure.ServiceBus.SessionHandlerOptions"/> that will be used by
        /// <see cref="IQueueClient"/>s.
        /// </summary>
        public SessionHandlerOptions SessionHandlerOptions;

         

       

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs args)
        {
            ExceptionHandler?.Invoke(args);

            return Task.CompletedTask;
        }
    }
}
