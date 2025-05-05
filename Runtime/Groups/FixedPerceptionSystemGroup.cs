using Unity.Entities;
using Unity.Physics.Systems;

namespace Perception
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup)), UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial class FixedPerceptionSystemGroup : ComponentSystemGroup
    {
    }
}