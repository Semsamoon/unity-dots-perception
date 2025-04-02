using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Contains entities' children for ray casting.
    /// </summary>
    public struct BufferSightChild : IBufferElementData
    {
        public Entity Value;
    }
}