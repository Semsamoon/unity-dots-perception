using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Marks entity to be a source.
    /// The source can be seen by receivers.
    /// </summary>
    public struct TagSightSource : IComponentData
    {
    }
}