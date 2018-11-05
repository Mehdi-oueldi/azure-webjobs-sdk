// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Azure.WebJobs.ServiceBus.Listeners;

namespace Microsoft.Azure.WebJobs.ServiceBusSessions.Listeners
{
    internal class ServiceBusSessionsQueueListenerFactory : IListenerFactory
    {
        private readonly ServiceBusAccount _account;
        private readonly string _queueName;
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly ServiceBusOptions _options;     
        private readonly SessionProvider _sessionProvider;
       
        public ServiceBusSessionsQueueListenerFactory(ServiceBusAccount account, string queueName, ITriggeredFunctionExecutor executor, ServiceBusOptions options, SessionProvider sessionProvider)
        {
            _account = account;
            _queueName = queueName;
            _executor = executor;
            _options = options;
            _sessionProvider = sessionProvider;
        }

        public Task<IListener> CreateAsync(CancellationToken cancellationToken)
        {
            var triggerExecutor = new ServiceBusTriggerExecutor(_executor);
            var listener = new ServiceBusSessionsListener(_queueName, triggerExecutor, _options, _account, _sessionProvider) as IListener;

            return Task.FromResult<IListener>(listener);
        }
    }
}
