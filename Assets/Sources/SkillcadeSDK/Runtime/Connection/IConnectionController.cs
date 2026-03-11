using System;

namespace SkillcadeSDK.Connection
{
    public interface IConnectionController
    {
        event Action<ConnectionState> OnStateChanged;
        event Action<DisconnectionReason> OnDisconnected;
        
        bool IsServer => ConnectionState is ConnectionState.Hosting or ConnectionState.SinglePlayer;
        bool IsClient => ConnectionState is ConnectionState.Connected or ConnectionState.SinglePlayer;
        
        ConnectionState ConnectionState { get; }
        ConnectionData ActiveConfig { get; }

        void StartServer(ConnectionData config);
        void StartClient(ConnectionData config);
        void StartSinglePlayer(ConnectionData config);
        void Disconnect();
    }
}