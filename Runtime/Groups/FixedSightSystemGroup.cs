using Unity.Entities;

namespace Perception
{
    [UpdateInGroup(typeof(FixedPerceptionSystemGroup))]
    public partial class FixedSightSystemGroup : ComponentSystemGroup
    {
    }
}