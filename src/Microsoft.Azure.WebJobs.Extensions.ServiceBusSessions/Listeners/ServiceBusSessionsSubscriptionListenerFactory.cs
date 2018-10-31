// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Azure.WebJobs.ServiceBus.Listeners;

namespace Microsoft.Azure.WebJobs.ServiceBusSessions.Listeners
{
    internal class ServiceBusSessionsSubscriptionListenerFactory : IListenerFactory
    {
        private readonly ServiceBusAccount _account;
        private readonly string _topicName;
        private readonly string _subscriptionName;
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly ServiceBusOptions _options;
        private readonly MessagingProvider _messagingProvider;
        private readonly SessionProvider _sessionProvider;

        public ServiceBusSessionsSubscriptionListenerFactory(ServiceBusAccount account, string topicName, string subscriptionName, ITriggeredFunctionExecutor executor, ServiceBusOptions options, MessagingProvider messagingProvider)
        {
            _account = account;
            _topicName = topicName;
            _subscriptionName = subscriptionName;
            _executor = executor;
            _options = options;
            _messagingProvider = messagingProvider;
        }
        public ServiceBusSessionsSubscriptionListenerFactory(ServiceBusAccount account, string topicName, string subscriptionName, ITriggeredFunctionExecutor executor, ServiceBusOptions options, SessionProvider sessionProvider)
        {
            _account = account;
            _topicName = topicName;
            _subscriptionName = subscriptionName;
            _executor = executor;
            _options = options;
            _sessionProvider = sessionProvider;
        }

        public Task<IListener> CreateAsync(CancellationToken cancellationToken)
        {
            string entityPath = EntityNameHelper.FormatSubscriptionPath(_topicName, _subscriptionName);

            ServiceBusTriggerExecutor triggerExecutor = new ServiceBusTriggerExecutor(_executor);
            
            var listener = (_sessionProvider == null) ?
             new ServiceBusListener(entityPath, triggerExecutor, _options, _account, _messagingProvider) as IListener :
             new ServiceBusSessionsListener(entityPath, triggerExecutor, _options, _account, _sessionProvider) as IListener;

            return Task.FromResult<IListener>(listener);
        }
    }
}
