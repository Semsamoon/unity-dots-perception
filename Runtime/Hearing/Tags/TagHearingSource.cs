using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Marks entity to be a source.
    /// The source can be heard by receivers.
    /// </summary>
    public struct TagHearingSource : IComponentData
    {
    }
}