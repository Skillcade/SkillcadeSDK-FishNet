using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Managing.Server;
using SkillcadeSDK.Connection;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter
{
    public class FishNetConnectionController : ConnectionControllerBase
    {
        public int LocalClientId => _networkManager.IsClientStarted ? _networkManager.ClientManager.Connection.ClientId : -1;

        public override ITransportAdapter Transport => _transportAdapter;
        
        [Inject] private readonly NetworkManager _networkManager;
        [Inject] private readonly ITransportAdapter _transportAdapter;

        public void KickClient(int clientId, KickReason reason, string log = "")
        {
            if (!_networkManager.IsServerStarted)
                return;

            if (!_networkManager.ServerManager.Clients.TryGetValue(clientId, out var connection))
                return;
            
            connection.Kick(reason, LoggingType.Common, log);
        }
    }
}