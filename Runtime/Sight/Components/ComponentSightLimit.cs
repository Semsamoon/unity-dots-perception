using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Specifies limits for calculations per frame.
    /// Must be a singleton.
    /// </summary>
    public struct ComponentSightLimit : IComponentData
    {
        public int ChunksAmountPosition;
        public int ChunksAmountMemory;
        public int ChunksAmountCone;
        public int ChunksAmountPerceiveSingle;
        public int ChunksAmountPerceiveOffset;
    }
}