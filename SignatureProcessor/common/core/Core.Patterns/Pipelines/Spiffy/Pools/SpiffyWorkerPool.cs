using Core.Patterns.Pipelines.Spiffy.Workers;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static Core.Patterns.Pipelines.Spiffy.SpiffyLifetime;

namespace Core.Patterns.Pipelines.Spiffy.Pools
{
    internal sealed class SpiffyWorkerPool<T, TMessage> : BaseSpiffyWorkerPool<T, TMessage>
        where T : new()
    {
        private readonly Func<T> activator;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ConcurrentQueue<ISpiffyWorker<T>> idleWorkerQueue = new ConcurrentQueue<ISpiffyWorker<T>>();
        private readonly ConcurrentDictionary<int, ISpiffyWorker<T>> busyWorkerMap = new ConcurrentDictionary<int, ISpiffyWorker<T>>();

        private protected readonly object locker = new object();
        private readonly BufferBlock<TMessage> headNetworkBlock;
        private bool disposed;
        private bool pipelineActive;

        internal SpiffyWorkerPool()
            : this(default, default)
        { }

        internal SpiffyWorkerPool(Func<T> activator = default)
            : this(default, activator)
        { }

        internal SpiffyWorkerPool(SpiffyPoolOptions poolOptions = default)
            : this(poolOptions, default)
        { }

        internal SpiffyWorkerPool(SpiffyPoolOptions poolOptions = default, Func<T> activator = default)
            : base(poolOptions)
        {
            this.activator = activator ?? new Func<T>(() => new T());
            CreateDefaultWorkerObjects();
            headNetworkBlock ??= BuildDataFlowNetwork();
            _ = StartConsumerPipeline();
        }

        public override void CancelAll()
        {
            cancellationTokenSource?.Cancel();
            headNetworkBlock.Complete();
        }

        public override void Dispose()
        {
            if (!disposed)
            {
                pipelineActive = false;

                CancelAll();
                _ = ClearIdleWorkers();

                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;

                disposed = true;
            }
        }

        public override ValueTask Post(TMessage item)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Cannot use a worker pool once it has been disposed.");
            }

            if (pipelineActive)
            {
                _ = headNetworkBlock.SendAsync(item);
            }

            return new ValueTask();
        }

        public override async ValueTask Reclaim(ISpiffyWorker<T> spiffyWorker)
            => await MakeIdleOrCleanup(spiffyWorker);

        private BufferBlock<TMessage> BuildDataFlowNetwork()
        {
            BufferBlock<TMessage> bufferingNetworkBlock = new BufferBlock<TMessage>(
                    new DataflowBlockOptions
                    {
                        BoundedCapacity = PoolOptions.MaxBufferThreshold,
                        CancellationToken = cancellationTokenSource.Token
                    }
            );

            ActionBlock<TMessage> mitosisNetworkBlock = new ActionBlock<TMessage>((TMessage _) =>
            {
                /**
                 * Since there is a possibility two threads on separate cores could execute at the same
                 * time, we must do one extra check to allow the volatile variable to increment first.
                 * Without this check, its possible that you can generate hundreds of idle workers until
                 * the CPU finally is able to read the updated idleWorkers count.
                 */
                lock (locker)
                {
                    if (idleWorkers <= 2)
                    {
                        IncreaseWorkerCount(5);
                    }
                }
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount == 1 ? 1 : Environment.ProcessorCount >> 1,
                CancellationToken = cancellationTokenSource.Token
            });

            // Create the network and allow completion propagation to occur throughout the network.
            DataflowLinkOptions linkOptions = new DataflowLinkOptions
            {
                PropagateCompletion = true
            };

            bufferingNetworkBlock.LinkTo(mitosisNetworkBlock, linkOptions, p => idleWorkers <= 2);

            return bufferingNetworkBlock;
        }

        private ValueTask StartConsumerPipeline()
        {
            _ = Task.Run(async () =>
            {
                pipelineActive = true;

                while (await headNetworkBlock.OutputAvailableAsync(cancellationTokenSource.Token))
                {
                    TMessage msg = await headNetworkBlock.ReceiveAsync(cancellationTokenSource.Token);
                    ISpiffyWorker<T> worker = await GetAvailableWorker();
                    _ = worker.RunAsync(msg);
                }

                pipelineActive = false;
            });

            return new ValueTask();
        }

        private ValueTask<ISpiffyWorker<T>> GetAvailableWorker()
        {
            /**
             * At first glance it looks like a lot of ceremony is going on in order
             * to increment and decrement variables which could easily be read from
             * the .Count property on both the concurrent queue. 
             * 
             * The reason is because internal locks are used in concurrent collections
             * which can all be activated all at once when the Count is read. This can cause
             * up to a second in delay depending on the number of locks active in the collection.
             * 
             * To avoid this, we should avoid looking at the .Count property in high
             * performance code scenarios.
             */
            if (idleWorkerQueue.TryDequeue(out ISpiffyWorker<T> spiffyWorker))
            {
                Interlocked.Decrement(ref idleWorkers);
            }

            if (busyWorkerMap.TryAdd(spiffyWorker.WorkerId, spiffyWorker))
            {
                Interlocked.Increment(ref activeWorkers);
            }

            return new ValueTask<ISpiffyWorker<T>>(spiffyWorker);
        }

        private void CreateDefaultWorkerObjects()
            => Parallel.For(0, PoolOptions.NumberOfWorkers, async (int _) => await CreateIdleWorker(Long));

        private void IncreaseWorkerCount(int totalNewWorkers)
        {
            for (int i = 0; i < totalNewWorkers; i++)
            {
                _ = CreateIdleWorker(Short);
            }
        }

        private async ValueTask CreateIdleWorker(SpiffyLifetime lifetime)
            => await MakeIdle(new GenericSpiffyWorker<T>(activator, this, lifetime));

        private ValueTask MakeIdle(ISpiffyWorker<T> worker)
        {
            idleWorkerQueue.Enqueue(worker);
            Interlocked.Increment(ref idleWorkers);

            return new ValueTask();
        }

        private async ValueTask MakeIdleOrCleanup(ISpiffyWorker<T> worker)
        {
            if (busyWorkerMap.TryRemove(worker.WorkerId, out ISpiffyWorker<T> oldWorker))
            {
                Interlocked.Decrement(ref activeWorkers);
            }

            if (worker.Lifetime == Short)
            {
                oldWorker.Dispose();
            }
            else
            {
                await MakeIdle(worker);
            }
        }

        private ValueTask ClearIdleWorkers()
        {
            while (idleWorkerQueue.TryDequeue(out ISpiffyWorker<T> worker))
            {
                worker.Dispose();
                Interlocked.Decrement(ref idleWorkers);
            }

            return new ValueTask();
        }
    }
}
