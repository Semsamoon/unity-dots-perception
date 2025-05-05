using System.Diagnostics;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Perception
{
    [BurstCompile]
    public static class DebugAdvanced
    {
        [BurstCompile]
        public static void DrawCurve(in float3 center, in quaternion rotation, float radius, float angleRadians, in Color color, float sparsity = 1000)
        {
            if (radius < Constants.Epsilon || angleRadians < Constants.Epsilon || color.a == 0)
            {
                return;
            }

            var halfAngle = angleRadians / 2;
            var segments = (int)(halfAngle * radius / sparsity) + 32;

            for (var i = 0; i < segments; i++)
            {
                var startAngle = math.lerp(-halfAngle, halfAngle, (float)i / segments);
                var endAngle = math.lerp(-halfAngle, halfAngle, (float)(i + 1) / segments);

                var start = center + math.rotate(rotation, new float3(math.sin(startAngle), 0, math.cos(startAngle))) * radius;
                var end = center + math.rotate(rotation, new float3(math.sin(endAngle), 0, math.cos(endAngle))) * radius;

                UnityEngine.Debug.DrawLine(start, end, color);
            }
        }

        [BurstCompile]
        public static void DrawSphere(in float3 center, in quaternion rotation, float radius, in Color color, float sparsity = 1000)
        {
            DrawCurve(in center, in rotation, radius, math.PI2, in color, sparsity);
            DrawCurve(in center, math.mul(rotation, quaternion.RotateX(math.PIHALF)), radius, math.PI2, in color, sparsity);
            DrawCurve(in center, math.mul(rotation, quaternion.RotateZ(math.PIHALF)), radius, math.PI2, in color, sparsity);
        }

        [BurstCompile]
        public static void DrawOctahedron(in float3 center, in float3 size, in Color color)
        {
            if (size.x + size.y + size.z < Constants.Epsilon || color.a == 0)
            {
                return;
            }

            var verticalPoint = new float3(0, size.y / 2, 0);

            var horizontalPoints = new float3x4(
                new float3(size.x / 2, 0, size.z / 2),
                new float3(-size.x / 2, 0, size.z / 2),
                new float3(-size.x / 2, 0, -size.z / 2),
                new float3(size.x / 2, 0, -size.z / 2));

            for (var i = 0; i < 4; i++)
            {
                UnityEngine.Debug.DrawLine(center + horizontalPoints[i], center + verticalPoint, color);
                UnityEngine.Debug.DrawLine(center + horizontalPoints[i], center - verticalPoint, color);
                UnityEngine.Debug.DrawLine(center + horizontalPoints[i], center + horizontalPoints[(i + 1) % 4], color);
            }
        }
    }
}