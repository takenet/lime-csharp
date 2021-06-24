using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Defines an envelope receiving service.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEnvelopeListener<in T> where T : Envelope, new()
    {
        /// <summary>
        /// Gets the filter for receiving envelopes.
        /// </summary>
        Predicate<T> Filter { get; }
        
        /// <summary>
        /// Receives an envelope instance.
        /// </summary>
        /// <param name="envelope"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task OnEnvelopeAsync(T envelope, CancellationToken cancellationToken);
    }
}