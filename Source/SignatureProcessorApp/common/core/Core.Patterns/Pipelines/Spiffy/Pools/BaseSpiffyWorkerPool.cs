using System.Threading.Tasks;

namespace Core.Patterns.Pipelines.Spiffy.Pools
{
    internal abstract class BaseSpiffyWorkerPool<T, TMessage> : ISpiffyWorkerPool<T, TMessage>, ISpiffyContext<T>
        where T : new()
    {
        private protected volatile int activeWorkers;
        private protected volatile int idleWorkers;

        public SpiffyPoolOptions PoolOptions { get; }

        public int ActiveWorkers { get => activeWorkers; }
        public int IdleWorkers { get => idleWorkers; }

        private protected BaseSpiffyWorkerPool(SpiffyPoolOptions poolOptions = default)
            => (PoolOptions) = (poolOptions ?? SpiffyPoolOptions.Default);

        public abstract void CancelAll();

        public abstract void Dispose();

        public abstract ValueTask Post(TMessage item);

        public abstract ValueTask Reclaim(ISpiffyWorker<T> spiffyWorker);
    }
}
