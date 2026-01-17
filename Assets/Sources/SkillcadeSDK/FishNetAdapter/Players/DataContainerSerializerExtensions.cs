using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Serializing;
using JetBrains.Annotations;
using SkillcadeSDK.FishNetAdapter.Replays;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    public static class DataContainerSerializerExtensions
    {
        private static Dictionary<int, Type> _idToType;
        private static Dictionary<Type, int> _typeToId;
        
        [RuntimeInitializeOnLoadMethod]
        public static void InitializeContainerTypes()
        {
            _idToType = new Dictionary<int, Type>();
            _typeToId = new Dictionary<Type, int>();
            
            int id = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var containerType in assembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IDataContainer))))
                {
                    _idToType.Add(id, containerType);
                    _typeToId.Add(containerType, id);
                    id++;
                }
            }
        }
        
        [UsedImplicitly]
        public static void WriteDataContainer(this Writer writer, IDataContainer dataContainer)
        {
            if (dataContainer == null)
            {
                Debug.LogError("Trying to write null data container");
                return;
            }

            if (!_typeToId.TryGetValue(dataContainer.GetType(), out int typeId))
            {
                Debug.LogError("Trying to write not registered data container");
                return;
            }
            
            writer.WriteInt32(typeId);
            dataContainer.Write(writer);
        }
        
        [UsedImplicitly]
        public static IDataContainer ReadDataContainer(this Reader reader)
        {
            var typeId = reader.ReadInt32();
            if (!_idToType.TryGetValue(typeId, out Type type))
                throw new KeyNotFoundException($"Trying to read not registered data container {typeId}");

            var dataContainer = (IDataContainer)Activator.CreateInstance(type);
            dataContainer.Read(reader);
            return dataContainer;
        }

        [UsedImplicitly]
        public static void WriteFishNetFrameDataBroadcast(this Writer writer, FishNetFrameDataBroadcast data)
        {
            writer.WriteInt32(data.FrameId);
            writer.WriteInt32(data.FrameData.Length);
            writer.WriteUInt8Array(data.FrameData, 0, data.FrameData.Length);
        }

        [UsedImplicitly]
        public static FishNetFrameDataBroadcast ReadFishNetFrameDataBroadcast(this Reader reader)
        {
            int frameId = reader.ReadInt32();
            int length = reader.ReadInt32();
            var data = new byte[length];
            reader.ReadUInt8Array(ref data, length);
            return new FishNetFrameDataBroadcast(frameId, data);
        }
    }
}