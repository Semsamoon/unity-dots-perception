using Unity.Entities;

namespace Perception
{
    public struct ComponentSightLimit : IComponentData
    {
        public int ChunksAmountPosition;
        public int ChunksAmountMemory;
        public int ChunksAmountCone;
        public int ChunksAmountPerceiveSingle;
        public int ChunksAmountPerceiveOffset;
    }
}