using System;
using System.Threading.Tasks;

namespace Core.Patterns.Pipelines.Spiffy
{
    public interface ISpiffyWorkerPool<T, TMessage> : IDisposable
        where T : new()
    {
        SpiffyPoolOptions PoolOptions { get; }
        int ActiveWorkers { get; }
        int IdleWorkers { get; }
        ValueTask Post(TMessage item);
        void CancelAll();
    }
}
