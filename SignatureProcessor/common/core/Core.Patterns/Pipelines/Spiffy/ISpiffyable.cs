using System.Threading.Tasks;

namespace Core.Patterns.Pipelines.Spiffy
{
    /// <summary>
    /// When specified on a class, it marks the class as capable of being leveraged
    /// as part of a spiffy workflow.
    /// </summary>
    /// <typeparam name="K"></typeparam>
    public interface ISpiffyable<K>
    {
        /// <summary>
        /// Forces a designated spiffy object to act with the given notifier and context object.
        /// </summary>
        /// <param name="notifier">The notifier to respond back to once processing is complete.</param>
        /// <param name="context">The context object representing the state object being expected.</param>
        Task Act(ISpiffyNotifiable notifier, K context);
    }
}
