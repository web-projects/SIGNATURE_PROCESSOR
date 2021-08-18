using System.Threading.Tasks.Dataflow;

namespace Core.Patterns.Pipelines.Spiffy
{
    public sealed class SpiffyPoolOptions
    {
        /// <summary>
        /// This value specifies unbounded queue growth for posted messages.
        /// </summary>
        public const int DefaultBufferThreshold = DataflowBlockOptions.Unbounded;

        /// <summary>
        /// The default number of flyweight workers created by every Worker Pool.
        /// </summary>
        public const int DefaultWorkerCount = 20;

        public static readonly SpiffyPoolOptions Default = new SpiffyPoolOptions();

        /// <summary>
        /// The default of flyweight workers available for handling requests. 
        /// These are long lived and not subject to early collection by the
        /// Spiffy Pool of Workers.
        /// </summary>
        public int NumberOfWorkers { get; } = DefaultWorkerCount;

        /// <summary>
        /// The maximum number of messages that the queue is allowed to handle.
        /// </summary>
        public int MaxBufferThreshold { get; } = DefaultBufferThreshold;

        /// <summary>
        /// Initializes a new instance of pool options with default values set.
        /// </summary>
        public SpiffyPoolOptions() { }

        /// <summary>
        /// Initializes a new instance of pool options with specified default workers.
        /// </summary>
        /// <param name="numberOfWorkers">The number of flyweight workers available that are long lived.</param>
        public SpiffyPoolOptions(int numberOfWorkers) : this(numberOfWorkers, DefaultBufferThreshold) { }

        /// <summary>
        /// Initializes a new instance of pool options with the provided
        /// number of workers and max buffer size.
        /// </summary>
        /// <param name="numberOfWorkers">The number of flyweight workers available that are long lived.</param>
        /// <param name="maxBufferSize">The max amount of messages to queue before callers are blocked asynchronously.</param>
        public SpiffyPoolOptions(int numberOfWorkers, int maxBufferSize) =>
            (NumberOfWorkers, MaxBufferThreshold) = (
            numberOfWorkers <= 0 ? DefaultWorkerCount : numberOfWorkers,
            maxBufferSize <= 0 ? DefaultBufferThreshold : maxBufferSize
        );
    }
}
