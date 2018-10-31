// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Azure.WebJobs.ServiceBusSessions;
using Microsoft.Azure.WebJobs.ServiceBusSessions.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Hosting
{
    public static class ServiceBusSessionHostBuilderExtensions
    {
        public static IWebJobsBuilder AddServiceBusSession(this IWebJobsBuilder builder)
        {
          
            builder.AddServiceBusSession((c)=> { });
            return builder;
        }

        public static IWebJobsBuilder AddServiceBusSession(this IWebJobsBuilder builder, Action<ServiceBusSessionsOptions> configure)
        {
           // builder.AddServiceBus(configure);
            //Add new extention to service bus
            builder.AddExtension<ServiceBusSessionExtensionConfigProvider>()
             .ConfigureOptions<ServiceBusSessionsOptions>((config, path, options) =>
              {
                  options.ConnectionString = config.GetConnectionString(Constants.DefaultConnectionStringName);
                  IConfigurationSection section = config.GetSection(path);
                  section.Bind(options);

                  configure(options);
              });
            builder.Services.TryAddSingleton<SessionProvider>();
            return builder;
        }              
    }
}
