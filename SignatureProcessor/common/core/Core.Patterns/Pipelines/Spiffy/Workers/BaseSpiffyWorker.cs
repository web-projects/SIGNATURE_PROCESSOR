using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static Core.Patterns.Pipelines.Spiffy.SpiffyLifetime;
using static Core.Patterns.Pipelines.Spiffy.SpiffyState;

namespace Core.Patterns.Pipelines.Spiffy.Workers
{
    internal abstract class BaseSpiffyWorker<T> : ISpiffyNotifiable, ISpiffyWorker<T> where T : new()
    {
        private static int nextWorkerId = 0;

        private bool disposed;

        public int WorkerId { get; protected set; }

        public virtual SpiffyLifetime Lifetime { get; protected set; } = Short;

        public virtual SpiffyState State { get; protected set; } = Idle;

        public T SpiffyableObject { get; }

        protected private ISpiffyContext<T> SpiffyContext { get; }

        protected private BaseSpiffyWorker(Func<T> activator, ISpiffyContext<T> spiffyContext)
        {
            if (activator is null)
            {
                throw new ArgumentNullException(nameof(activator), "A spiffy worker requires an activator.");
            }

            if (spiffyContext is null)
            {
                throw new ArgumentNullException(nameof(spiffyContext), "A spiffy worker requires a spiffy context.");
            }

            SpiffyableObject = activator();
            if (SpiffyableObject is null)
            {
                throw new NullReferenceException("Unable to construct a spiffyable object using the provided activation function.");
            }

            SpiffyContext = spiffyContext;
            AssignNewWorkerId();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssignNewWorkerId()
        {
            Interlocked.CompareExchange(ref nextWorkerId, 0, int.MaxValue);
            WorkerId = Interlocked.Increment(ref nextWorkerId);
        }

        public virtual void Dispose()
        {
            if (!disposed)
            {
                if (SpiffyableObject is IDisposable)
                {
                    (SpiffyableObject as IDisposable).Dispose();
                }
                disposed = true;
            }
        }

        public virtual ValueTask RunAsync<K>(K context = default)
        {
            State = Busy;
            return new ValueTask();
        }

        public virtual void AllSpiffy()
        {
            State = Idle;
            SpiffyContext.Reclaim(this);
        }
    }
}
