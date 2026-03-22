using FishNet.Serializing;

namespace SkillcadeSDK.FishNetAdapter.Serialization
{
    /// <summary>
    /// FishNet custom serializer for Vector2Short.
    /// Discovered automatically by FishNet's IL codegen via the WriteT/ReadT naming convention.
    /// Writes exactly 4 bytes (2× Int16Unpacked) vs 8 bytes for a standard Vector2.
    /// </summary>
    public static class Vector2ShortSerializer
    {
        public static void WriteVector2Short(this Writer writer, Vector2Short value)
        {
            writer.WriteInt16Unpacked(value.X);
            writer.WriteInt16Unpacked(value.Y);
        }

        public static Vector2Short ReadVector2Short(this Reader reader) => new()
        {
            X = reader.ReadInt16Unpacked(),
            Y = reader.ReadInt16Unpacked()
        };
    }
}
