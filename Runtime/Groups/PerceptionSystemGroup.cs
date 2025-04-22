using Unity.Entities;

namespace Perception
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PerceptionSystemGroup : ComponentSystemGroup
    {
    }
}