using Unity.Entities;

namespace Perception
{
    [UpdateInGroup(typeof(PerceptionSystemGroup))]
    public partial class HearingSystemGroup : ComponentSystemGroup
    {
    }
}