using System.IO;
using UnityEngine;

namespace SkillcadeSDK.Replays
{
    public struct ReplayWriter
    {
        private readonly BinaryWriter _writer;
        public ReplayWriter(BinaryWriter writer) => _writer = writer;
        
        public void Write(IReplayDataObject dataObject)
        {
            var id = ReplayDataObjectsRegistry.TypeToId[dataObject.GetType()];
            WriteUshort((ushort)id);
            WriteUshort((ushort)dataObject.Size);
            dataObject.Write(this);
        }
        
        public void WriteInt(int value) => _writer.Write(value);
        public void WriteShort(short value) => _writer.Write(value);
        public void WriteUshort(ushort value) => _writer.Write(value);
        public void WriteLong(long value) => _writer.Write(value);
        public void WriteBool(bool value) => _writer.Write(value);
        public void WriteFloat(float value) =>  _writer.Write(value);
        public void WriteDouble(double value) =>  _writer.Write(value);
        public void WriteFloatHalfPresicion(float value) => _writer.Write(Mathf.FloatToHalf(value));

        public void WriteVector2(Vector2 value)
        {
            WriteFloatHalfPresicion(value.x);
            WriteFloatHalfPresicion(value.y);
        }

        public void WriteString(string value)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            WriteInt(bytes.Length);
            _writer.Write(bytes);
        }
    }
}