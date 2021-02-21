using System;
using System.Threading.Tasks;

namespace Core.Patterns.Pipelines.Spiffy
{
    internal interface ISpiffyWorker<T> : IDisposable
        where T : new()
    {
        int WorkerId { get; }
        SpiffyLifetime Lifetime { get; }
        SpiffyState State { get; }
        T SpiffyableObject { get; }
        ValueTask RunAsync<K>(K context = default);
    }
}
