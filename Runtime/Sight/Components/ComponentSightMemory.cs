using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Adds memory to the receiver.
    /// </summary>
    public struct ComponentSightMemory : IComponentData
    {
        public float Time;
    }
}