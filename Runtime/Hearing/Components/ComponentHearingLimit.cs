using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Specifies limits for calculations per frame.
    /// Must be a singleton.
    /// </summary>
    public struct ComponentHearingLimit : IComponentData
    {
        public int ChunksAmountPosition;
        public int ChunksAmountMemory;
        public int ChunksAmountSphere;
        public int ChunksAmountPerceive;
    }
}