// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.ServiceBusSessions
{
    public class SessionProvider
    {
        private readonly ConcurrentDictionary<string, IQueueClient> _queueClientCache = new ConcurrentDictionary<string, IQueueClient>();

        private readonly ServiceBusOptions _options;
        public virtual SessionProcessor CreateSessionProcessor(string entityPath, string connectionString)
        {
            if (string.IsNullOrEmpty(entityPath))
            {
                throw new ArgumentNullException("entityPath");
            }
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            return new SessionProcessor(GetOrAddQueueClient(entityPath, connectionString), _options.SessionHandlerOptions);
        }
        public SessionProvider(IOptions<ServiceBusOptions> serviceBusOptions)
        {
            _options = serviceBusOptions?.Value ?? throw new ArgumentNullException(nameof(serviceBusOptions));
        }
        /// <summary>
        /// Creates a <see cref="MessageReceiver"/> for the specified ServiceBus entity.
        /// </summary>
        /// <remarks>
        /// You can override this method to customize the <see cref="MessageReceiver"/>.
        /// </remarks>
        /// <param name="entityPath">The ServiceBus entity to create a <see cref="MessageReceiver"/> for.</param>
        /// <param name="connectionString">The ServiceBus connection string.</param>
        /// <returns></returns>
        public virtual IQueueClient CreateQueueClient(string entityPath, string connectionString)
        {
            if (string.IsNullOrEmpty(entityPath))
            {
                throw new ArgumentNullException("entityPath");
            }
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            return GetOrAddQueueClient(entityPath, connectionString);
        }
        private IQueueClient GetOrAddQueueClient(string entityPath, string connectionString)
        {
            string cacheKey = $"{entityPath}-{connectionString}";
            return _queueClientCache.GetOrAdd(cacheKey,
                new QueueClient(connectionString, entityPath)
                {

                    PrefetchCount = _options.PrefetchCount
                });
        }
    }
}
