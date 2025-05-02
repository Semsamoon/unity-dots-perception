using Unity.Entities;

namespace Perception
{
    public struct ComponentHearingLimit : IComponentData
    {
        public int ChunksAmountPosition;
        public int ChunksAmountMemory;
        public int ChunksAmountSphere;
        public int ChunksAmountPerceive;
    }
}