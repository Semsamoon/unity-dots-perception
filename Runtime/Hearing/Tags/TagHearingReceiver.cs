using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Marks entity to be a receiver.
    /// The receiver can hear sources.
    /// </summary>
    public struct TagHearingReceiver : IComponentData
    {
    }
}