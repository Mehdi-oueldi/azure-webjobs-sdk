﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.ServiceBusSessions;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Hosting;

[assembly: WebJobsStartup(typeof(ServiceBusWebJobsStartup))]

namespace Microsoft.Azure.WebJobs.ServiceBusSessions
{
    public class ServiceBusWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddServiceBus();
        }
    }
}
