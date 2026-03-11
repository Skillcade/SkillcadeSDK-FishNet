using UnityEngine;
using VContainer;

namespace SkillcadeSDK.Replays.Events
{
    [ReplayDataObjectId(1002)]
    public class ObjectDestroyedEvent : ReplayEvent
    {
        public override int Size => sizeof(int) * 2 + sizeof(float) * 2;

        public int ObjectId;
        public int PrefabId;
        public Vector2 Position;
        
        [Inject] private readonly ReplayReadService _replayReadService;
        [Inject] private readonly ReplayPrefabRegistry _replayPrefabRegistry;
        
        public ObjectDestroyedEvent() { }
        
        public ObjectDestroyedEvent(int objectId, int prefabId, Vector2 position)
        {
            ObjectId = objectId;
            PrefabId = prefabId;
            Position = position;
        }

        public override void Read(ReplayReader reader)
        {
            ObjectId = reader.ReadUshort();
            PrefabId = reader.ReadUshort();
            Position = reader.ReadVector2();
        }

        public override void Write(ReplayWriter writer)
        {
            writer.WriteUshort((ushort)ObjectId);
            writer.WriteUshort((ushort)PrefabId);
            writer.WriteVector2(Position);
        }

        public override void Handle(int worldId)
        {
            if (!_replayReadService.ClientWorlds.TryGetValue(worldId, out var replayClientWorld))
            {
                Debug.LogError($"[ObjectDestroyedEvent] Can't get client world for client {worldId}");
                return;
            }
            
            Debug.Log($"[ObjectDestroyedEvent] Handle event with object {ObjectId} and prefab {PrefabId}");
            
            replayClientWorld.DeleteObject(ObjectId, out var handler);
            handler.DestroyGameObject();
        }

        public override void Undo(int clientId)
        {
            if (!_replayPrefabRegistry.TryGetPrefab(PrefabId, out var prefab))
            {
                Debug.LogError($"[ObjectDestroyedEvent] Prefab {PrefabId} not found");
                return;
            }
            
            if (!_replayReadService.ClientWorlds.TryGetValue(clientId, out var replayClientWorld))
            {
                Debug.LogError($"[ObjectDestroyedEvent] Can't get client world for client {clientId}");
                return;
            }

            var instance = prefab.Instantiate();
            instance.InitializeReplay(ObjectId, clientId);
            instance.transform.position = Position;
            replayClientWorld.RegisterObject(instance);

            Debug.Log($"[ObjectDestroyedEvent] Undo event with object {ObjectId} and prefab {PrefabId}");
        }
    }
}