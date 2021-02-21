namespace Core.Patterns.Pipelines.Spiffy
{
    /// <summary>
    /// A Spiffy notifiable object which contains an instance of the current class.
    /// </summary>
    public interface ISpiffyNotifiable
    {
        /// <summary>
        /// Notifies the container that you're complete and the worker can be recycled if necessary.
        /// Only call this when you're ready for disposal and cleanup.
        /// </summary>
        void AllSpiffy();
    }
}
