﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Microsoft.Azure.WebJobs.ServiceBus
{
    /// <summary>
    /// This class defines a strategy used for processing ServiceBus session enabled messages
    /// </summary>
    /// <remarks>
    /// Custom <see cref="SessionProcessor"/> implementations can be specified by implementing
    /// a custom <see cref="SessionProvider"/> and setting it via <see cref="ServiceBusOptions.SessionHandlerOptions"/>.
    /// </remarks>
    public class SessionProcessor
    {
        public IQueueClient SessionReceiver { get; }       
        public SessionHandlerOptions SessionOptions { get; }
        public SessionProcessor(IQueueClient sessionReceiver, SessionHandlerOptions sessionOptions)
        {
            SessionReceiver = sessionReceiver ?? throw new ArgumentNullException(nameof(sessionReceiver));
            SessionOptions = sessionOptions ?? throw new ArgumentNullException(nameof(sessionOptions));
        }

        /// <summary>
        /// This method is called when there is a new message to process, before the job function is invoked.
        /// This allows any preprocessing to take place on the message before processing begins.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
        /// <returns>A <see cref="Task"/> that returns true if the message processing should continue, false otherwise.</returns>
        public virtual async Task<bool> BeginProcessingMessageAsync(Message message, CancellationToken cancellationToken)
        {
          
            return await Task.FromResult<bool>(true);
        }

        /// <summary>
        /// This method completes processing of the specified message, after the job function has been invoked.
        /// session lifetime should be handled manualy inside callbak 
        /// </summary>
        /// <remarks>
        /// The message is completed by the ServiceBus SDK based on how the <see cref="SessionHandlerOptions.AutoComplete"/> option
        /// is configured. E.g. if <see cref="SessionHandlerOptions.AutoComplete"/> is false, it is up to the job function to complete
        /// the message.
        /// SessionHandlerOptions.AutoComplete should be false by default to accomplish session close manually .
        /// </remarks>
        /// <param name="message">The message to complete processing for.</param>
        /// <param name="result">The <see cref="FunctionResult"/> from the job invocation.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use</param>
        /// <returns>A <see cref="Task"/> that will complete the message processing.</returns>
        public virtual Task CompleteProcessingMessageAsync(Message message, FunctionResult result, CancellationToken cancellationToken)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!result.Succeeded)
            {
                // if the invocation failed, we must propagate the
                // exception back to SB so it can handle message state
                // correctly
                throw result.Exception;
            }

            return Task.CompletedTask;
        }
    }
}
