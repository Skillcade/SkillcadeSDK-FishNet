using System.Runtime.InteropServices;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.Serialization
{
    /// <summary>
    /// Half-precision Vector2 for bandwidth-efficient FishNet RPCs and SyncVars.
    /// Uses Unity's IEEE 754 half-float conversion — 4 bytes on the wire vs 8 for Vector2.
    /// Range: ±65504. Precision: ~0.001 at typical game velocities.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2Short
    {
        // Stored as signed short; the bit pattern is identical to ushort for IEEE 754 half-float.
        public short X;
        public short Y;

        public Vector2 ToVector2() => new(
            Mathf.HalfToFloat((ushort)X),
            Mathf.HalfToFloat((ushort)Y)
        );

        public static implicit operator Vector2Short(Vector2 v) => new()
        {
            X = (short)Mathf.FloatToHalf(v.x),
            Y = (short)Mathf.FloatToHalf(v.y)
        };

        public static implicit operator Vector2(Vector2Short v) => new(
            Mathf.HalfToFloat((ushort)v.X),
            Mathf.HalfToFloat((ushort)v.Y));

        public static implicit operator Vector2Short(Vector3 v) => new()
        {
            X = (short)Mathf.FloatToHalf(v.x),
            Y = (short)Mathf.FloatToHalf(v.y)
        };

        public static implicit operator Vector3(Vector2Short v) => new(
            Mathf.HalfToFloat((ushort)v.X),
            Mathf.HalfToFloat((ushort)v.Y));
    }
}
