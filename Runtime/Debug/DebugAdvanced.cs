using Unity.Mathematics;
using UnityEngine;

namespace Perception
{
    public static class DebugAdvanced
    {
        public static void DrawCurve(float3 center, quaternion rotation, float radius, float angleRadians, Color color, float sparsity = 1000)
        {
            var halfAngle = angleRadians / 2;
            var segments = (int)(halfAngle * radius / sparsity) + 32;

            for (var i = 0; i < segments; i++)
            {
                var startAngle = math.lerp(-halfAngle, halfAngle, (float)i / segments);
                var endAngle = math.lerp(-halfAngle, halfAngle, (float)(i + 1) / segments);

                var start = center + math.rotate(rotation, new float3(math.sin(startAngle), 0, math.cos(startAngle))) * radius;
                var end = center + math.rotate(rotation, new float3(math.sin(endAngle), 0, math.cos(endAngle))) * radius;

                Debug.DrawLine(start, end, color);
            }
        }
    }
}