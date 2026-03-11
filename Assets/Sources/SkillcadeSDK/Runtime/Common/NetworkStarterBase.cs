using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using SkillcadeSDK.Connection;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.Common
{
    public enum ConnectionMode
    {
        None,
        Client,
        Server,
        SinglePlayer,
    }
    
    public class NetworkStarterBase : MonoBehaviour, IInitializable
    {
        [Header("Platform auto connection settings")]
        [SerializeField] private ConnectionMode _dedicatedServerMode;
        [SerializeField] private ConnectionMode _webGlMode;
        
        [Header("Default connection settings")]
        [SerializeField] private ConnectionMode _connectionMode;

        [Inject] private readonly IConnectionController _connectionController;
        [Inject] protected readonly WebBridge _webBridge;
        [Inject] private readonly ConnectionConfig _connectionConfig;

        private ConnectionData _data;

        public virtual void Initialize()
        {
            if (_connectionConfig == null)
            {
                Debug.LogError("[NetworkStarterBase] Config is null");
                return;
            }

            _data = _connectionConfig.GetData();
            
#if !UNITY_EDITOR && UNITY_SERVER
            _connectionMode = _dedicatedServerMode;
#elif !UNITY_EDITOR && UNITY_WEBGL
            _connectionMode = _webGlMode;
#endif

            if (_connectionMode == ConnectionMode.Server)
            {
                StartCoroutine(WaitAndStart(_connectionMode));
            }
            else if (_connectionMode == ConnectionMode.Client)
            {
                if (_connectionConfig.SkillcadeHubIntegrated)
                    WaitForPayloadAndConnect(destroyCancellationToken).DoNotAwait();
                else
                    StartCoroutine(WaitAndStart(_connectionMode));
            }
            else if (_connectionMode == ConnectionMode.SinglePlayer)
            {
                StartCoroutine(WaitAndStart(_connectionMode));
            }
            else
            {
                InitManualConnection();
            }
        }

        protected virtual void InitManualConnection() { }
        protected virtual void OnConnectionStarted(ConnectionMode mode) { }

        private async Task WaitForPayloadAndConnect(CancellationToken cancellationToken)
        {
            while (_webBridge.Payload == null)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
            
            SetupPayloadAndConnectToServer(_webBridge.Payload);
        }

        private void SetupPayloadAndConnectToServer(WebPayload payload)
        {
            Debug.Log("[NetworkStarterBase] Setup payload");
            if (string.IsNullOrEmpty(payload.ConnectIp))
            {
                Debug.LogError("[NetworkStarterBase] Payload connect ip is null");
                return;
            }

            if (string.IsNullOrEmpty(payload.ServerName))
            {
                Debug.LogError("[NetworkStarterBase] Payload server name is null");
                return;
            }

            if (payload.Port == 0)
            {
                Debug.LogError("[NetworkStarterBase] Payload port is 0");
                return;
            }
            
            _data.ServerAddress = payload.ConnectIp;
            _data.WssConnectPort = payload.Port;
            _data.WssServerName = payload.ServerName;
            _data.UseEncryption = true;

            Debug.Log($"[NetworkStarterBase] Start connection with payload, ip: {payload.ConnectIp}, port: {payload.Port}, server name: {payload.ServerName}");
            StartCoroutine(WaitAndStart(ConnectionMode.Client));
        }

        private IEnumerator WaitAndStart(ConnectionMode connectionMode)
        {
            yield return null;

            if (connectionMode == ConnectionMode.Server)
                StartServer();
            else if (connectionMode == ConnectionMode.Client)
                StartClient();
            else if (connectionMode == ConnectionMode.SinglePlayer)
                StartSinglePlayer();
        }

        protected void StartServer()
        {
            _connectionController.StartServer(_data);
            OnConnectionStarted(ConnectionMode.Server);
        }

        protected void StartClient()
        {
            _connectionController.StartClient(_data);
            OnConnectionStarted(ConnectionMode.Client);
        }

        protected void StartSinglePlayer()
        {
            _connectionController.StartSinglePlayer(_data);
            OnConnectionStarted(ConnectionMode.SinglePlayer);
        }
    }
}