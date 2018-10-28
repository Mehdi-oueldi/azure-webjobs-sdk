// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.WebJobs.ServiceBus.Listeners
{

    internal sealed class ServiceBusSessionListener : IListener
    {
    
        private readonly SessionProvider _sessionProvider;
        private readonly string _entityPath;
        private readonly ServiceBusTriggerExecutor _triggerExecutor;
        private readonly CancellationTokenSource _cancellationTokenSource;      
        private readonly SessionProcessor _sessionProcessor;
        private readonly ServiceBusAccount _serviceBusAccount;
        private IQueueClient _sessionReceiver;
        private bool _disposed;
        private bool _started;

        public IMessageSession Session => _sessionProcessor?.Session;
        public ServiceBusSessionListener(string entityPath, ServiceBusTriggerExecutor triggerExecutor, ServiceBusOptions config, ServiceBusAccount serviceBusAccount, SessionProvider sessionProvider)
        {
            _entityPath = entityPath;
            _triggerExecutor = triggerExecutor;
            _cancellationTokenSource = new CancellationTokenSource();
            _sessionProvider = sessionProvider;
            _serviceBusAccount = serviceBusAccount;
            _sessionProcessor = sessionProvider.CreateSessionProcessor(entityPath, _serviceBusAccount.ConnectionString);
        }

       

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_started)
            {
                throw new InvalidOperationException("The listener has already been started.");
            }
            _sessionReceiver = _sessionProvider.CreateQueueClient(_entityPath, _serviceBusAccount.ConnectionString);
            _sessionReceiver.RegisterSessionHandler(ProcessSessionMessageAsync, _sessionProcessor.SessionOptions);
            _started = true;

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (!_started)
            {
                throw new InvalidOperationException("The listener has not yet been started or has already been stopped.");
            }

            // cancel our token source to signal any in progress
            // ProcessMessageAsync invocations to cancel
            _cancellationTokenSource.Cancel();

            await _sessionReceiver.CloseAsync();
            _sessionReceiver = null;
            _started = false;
        }

        public void Cancel()
        {
            ThrowIfDisposed();
            _cancellationTokenSource.Cancel();
        }

        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_cancellationTokenSource")]
        public void Dispose()
        {
            if (!_disposed)
            {
                // Running callers might still be using the cancellation token.
                // Mark it canceled but don't dispose of the source while the callers are running.
                // Otherwise, callers would receive ObjectDisposedException when calling token.Register.
                // For now, rely on finalization to clean up _cancellationTokenSource's wait handle (if allocated).
                _cancellationTokenSource.Cancel();

                if (_sessionReceiver != null)
                {
                    _sessionReceiver.CloseAsync().Wait();
                    _sessionReceiver = null;
                }

                _disposed = true;
            }
        }

     
        internal async Task ProcessSessionMessageAsync(IMessageSession session, Message message, CancellationToken cancellationToken)
        {
            if (!await _sessionProcessor.BeginProcessingMessageAsync(session, message, cancellationToken))
            {
                return;
            }

            FunctionResult result = await _triggerExecutor.ExecuteAsync(message, cancellationToken);

            await _sessionProcessor.CompleteProcessingMessageAsync(session, message, result, cancellationToken);
        }


        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }
        }
    }
}
