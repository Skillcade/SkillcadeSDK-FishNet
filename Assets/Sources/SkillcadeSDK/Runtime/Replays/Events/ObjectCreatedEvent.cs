using UnityEngine;
using VContainer;

namespace SkillcadeSDK.Replays.Events
{
    [ReplayDataObjectId(1001)]
    public class ObjectCreatedEvent : ReplayEvent
    {
        public override int Size => sizeof(int) * 2 + sizeof(float) * 2;

        public int ObjectId;
        public int PrefabId;
        public Vector2 Position;

        [Inject] private readonly ReplayReadService _replayReadService;
        [Inject] private readonly ReplayPrefabRegistry _replayPrefabRegistry;
        
        public ObjectCreatedEvent() { }

        public ObjectCreatedEvent(int objectId, int prefabId, Vector2 position)
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
            Debug.Log($"[ObjectCreatedEvent] Handle event with object {ObjectId} and prefab {PrefabId}");
            if (!_replayPrefabRegistry.TryGetPrefab(PrefabId, out var prefab))
            {
                Debug.LogError($"[ObjectCreatedEvent] Prefab {PrefabId} not found");
                return;
            }

            if (!_replayReadService.ClientWorlds.TryGetValue(worldId, out var replayClientWorld))
            {
                Debug.LogError($"[ObjectCreatedEvent] Can't get client world for client {worldId}");
                return;
            }

            var instance = prefab.Instantiate();
            instance.InitializeReplay(ObjectId, worldId);
            instance.transform.position = Position;
            replayClientWorld.RegisterObject(instance);
        }

        public override void Undo(int clientId)
        {
            if (!_replayReadService.ClientWorlds.TryGetValue(clientId, out var replayClientWorld))
            {
                Debug.LogError($"[ObjectCreatedEvent] Can't get client world for client {clientId}");
                return;
            }
            
            replayClientWorld.DeleteObject(ObjectId, out var handler);
            if (handler != null)
                handler.DestroyGameObject();

            Debug.Log($"[ObjectCreatedEvent] Undo event with object {ObjectId} and prefab {PrefabId}, found object: {handler != null}");
        }
    }
}