using Unity.Entities;

namespace Perception
{
    public struct ComponentSightConeClip : IComponentData
    {
        public float RadiusSquared;
    }
}