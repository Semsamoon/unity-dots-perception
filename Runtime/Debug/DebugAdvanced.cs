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

        public static void DrawOctahedron(float3 center, float3 size, Color color)
        {
            var verticalPoint = new float3(0, size.y / 2, 0);

            var horizontalPoints = new float3x4(
                new float3(size.x / 2, 0, size.z / 2),
                new float3(-size.x / 2, 0, size.z / 2),
                new float3(-size.x / 2, 0, -size.z / 2),
                new float3(size.x / 2, 0, -size.z / 2));

            for (var i = 0; i < 4; i++)
            {
                Debug.DrawLine(center + horizontalPoints[i], center + verticalPoint, color);
                Debug.DrawLine(center + horizontalPoints[i], center - verticalPoint, color);
                Debug.DrawLine(center + horizontalPoints[i], center + horizontalPoints[(i + 1) % 4], color);
            }
        }
    }
}