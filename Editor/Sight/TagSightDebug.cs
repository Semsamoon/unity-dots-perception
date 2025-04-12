using Unity.Entities;

namespace Perception.Editor
{
    /// <summary>
    /// Marks receiver to debug its cone of vision.
    /// </summary>
    public struct TagSightDebug : IComponentData, IEnableableComponent
    {
    }
}