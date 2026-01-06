using System.Text;
using SkillcadeSDK.Replays;
using SkillcadeSDK.Replays.Events;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    [ReplayDataObjectId(1003)]
    public class FishNetPlayerDataEvent : ReplayEvent
    {
        public override int Size => sizeof(int) * 4 + _nicknameBytes?.Length ?? 0 + _playerIdBytes?.Length ?? 0;

        public int PlayerNetworkId;
        public int PlayerObjectId;
        public string Nickname;
        public string PlayerId;
        
        private byte[] _nicknameBytes;
        private byte[] _playerIdBytes;

        [Inject] private readonly FishNetReplayPlayerDataService _fishNetReplayPlayerDataService;

        public FishNetPlayerDataEvent() { }

        public FishNetPlayerDataEvent(int playerNetworkId, int playerObjectId, string nickname, string playerId)
        {
            PlayerNetworkId = playerNetworkId;
            PlayerObjectId = playerObjectId;
            Nickname = nickname;
            PlayerId = playerId;
            
            _nicknameBytes = Encoding.UTF8.GetBytes(nickname);
            _playerIdBytes = Encoding.UTF8.GetBytes(playerId);
        }

        public override void Read(ReplayReader reader)
        {
            PlayerNetworkId = reader.ReadUshort();
            PlayerObjectId = reader.ReadUshort();
            Nickname = reader.ReadString(out _nicknameBytes);
            PlayerId = reader.ReadString(out _playerIdBytes);
        }

        public override void Write(ReplayWriter writer)
        {
            writer.WriteUshort((ushort)PlayerNetworkId);
            writer.WriteUshort((ushort)PlayerObjectId);
            writer.WriteString(Nickname);
            writer.WriteString(PlayerId);
        }

        public override void Handle(int worldId)
        {
            Debug.Log($"[FishNetPlayerDataEvent] Handle player {PlayerId} - {PlayerObjectId} data event on world {worldId}");
            if (worldId == ReplayReadService.ServerWorldId) // Only set player data on server, we don't need to send this multiple times
                _fishNetReplayPlayerDataService.SetPlayerData(PlayerNetworkId, PlayerObjectId, Nickname, PlayerId);
        }

        public override void Undo(int clientId)
        {
            // Don't need to remove this data
        }
    }
}