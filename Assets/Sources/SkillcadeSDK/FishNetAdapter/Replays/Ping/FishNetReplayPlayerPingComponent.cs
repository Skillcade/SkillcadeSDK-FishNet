using FishNet.Object;
using SkillcadeSDK.FishNetAdapter.PingService;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.Replays;
using SkillcadeSDK.Replays.Components;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    [ReplayDataObjectId(2)]
    public class FishNetReplayPlayerPingComponent : MonoBehaviour, IReplayComponent
    {
        public int Size => sizeof(int);
        public int Ping => _ping;

        [SerializeField] private FishNetReplayObjectHandler _handler;
        [SerializeField] private NetworkObject _networkObject;

        [Inject] private readonly IObjectResolver _objectResolver;
        
        private int _ping;
        private FishNetPingReader _reader;
        private FishNetPingWriter _writer;

        public void Read(ReplayReader reader)
        {
            if (_reader == null)
            {
                this.InjectToMe();
                _reader = new FishNetPingReader();
                _objectResolver.Inject(_reader);
            }
            
            _ping = reader.ReadUshort();
            _reader.SetPing(_handler.ObjectId, _ping);
        }

        public void Write(ReplayWriter writer)
        {
            if (_writer == null)
            {
                this.InjectToMe();
                _writer = new FishNetPingWriter();
                _objectResolver.Inject(_writer);
            }
            
            _writer.Write(writer, _networkObject.OwnerId);
        }
    }
}