using System;

namespace SkillcadeSDK.Connection
{
    public interface ITransportAdapter
    {
        event Action OnConnected;
        event Action<DisconnectionReason> OnDisconnected;
        
        bool IsConnected { get; }
        bool IsServer { get; }
        bool IsClient { get; }
        
        void StartServer(ConnectionData config);
        void StartClient(ConnectionData config);
        void Disconnect();
    }
}