using Unity.Entities;

namespace Perception
{
    [UpdateInGroup(typeof(PerceptionSystemGroup))]
    public partial class SightSystemGroup : ComponentSystemGroup
    {
    }
}