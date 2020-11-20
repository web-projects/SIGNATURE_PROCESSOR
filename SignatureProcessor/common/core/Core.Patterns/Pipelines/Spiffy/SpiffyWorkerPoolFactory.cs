using Core.Patterns.Pipelines.Spiffy.Pools;
using System;

namespace Core.Patterns.Pipelines.Spiffy
{
    public static class SpiffyWorkerPoolFactory
    {
        public static ISpiffyWorkerPool<T, TMessage> CreatePool<T, TMessage>(SpiffyPoolOptions poolOptions = default, Func<T> activator = default)
            where T : new()
            => new SpiffyWorkerPool<T, TMessage>(poolOptions, activator);
    }
}
