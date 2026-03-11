using System.IO;
using UnityEngine;

namespace SkillcadeSDK.Replays
{
    public struct ReplayReader
    {
        private readonly BinaryReader _reader;

        public ReplayReader(BinaryReader reader)
        {
            _reader = reader;
        }

        public int ReadInt() => _reader.ReadInt32();
        public int ReadShort() => _reader.ReadInt16();
        public int ReadUshort() => _reader.ReadUInt16();
        public long ReadLong() => _reader.ReadInt64();
        public bool ReadBool() => _reader.ReadBoolean();
        public float ReadFloat() =>  _reader.ReadSingle();
        public double ReadDouble() =>  _reader.ReadDouble();
        public float ReadFloatHalfPresicion() => Mathf.HalfToFloat(_reader.ReadUInt16());
        public Vector2 ReadVector2() =>  new Vector2(ReadFloatHalfPresicion(), ReadFloatHalfPresicion());

        public string ReadString(out byte[] bytes)
        {
            int size = ReadInt();
            bytes = _reader.ReadBytes(size);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public void SkipBytes(int count)
        {
            _reader.BaseStream.Seek(count, SeekOrigin.Current);
        }
    }
}