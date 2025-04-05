using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Adds near clipping plane to the receiver's cone of vision.
    /// </summary>
    public struct ComponentSightClip : IComponentData
    {
        public float RadiusSquared;
    }
}