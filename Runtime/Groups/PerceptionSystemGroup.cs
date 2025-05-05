using Unity.Entities;
using Unity.Transforms;

namespace Perception
{
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(TransformSystemGroup))]
    public partial class PerceptionSystemGroup : ComponentSystemGroup
    {
    }
}