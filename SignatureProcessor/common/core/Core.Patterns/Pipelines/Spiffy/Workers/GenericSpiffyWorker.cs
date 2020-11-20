using System;
using System.Threading.Tasks;

namespace Core.Patterns.Pipelines.Spiffy.Workers
{
    internal sealed class GenericSpiffyWorker<T> : BaseSpiffyWorker<T>
        where T : new()
    {
        internal GenericSpiffyWorker(Func<T> activator, ISpiffyContext<T> spiffyContext, SpiffyLifetime lifetime)
            : base(activator, spiffyContext)
        {
            Lifetime = lifetime;
        }

        public override ValueTask RunAsync<K>(K context = default)
        {
            ISpiffyable<K> spiffyable = SpiffyableObject as ISpiffyable<K>;

            if (spiffyable is null)
            {
                throw new NullReferenceException($"Type ${typeof(T)} is not a valid Spiffyable<${typeof(K)}> object.");
            }

            _ = Task.Run(() => spiffyable.Act(this, context));

            return base.RunAsync(context);
        }
    }
}
