using UnityEngine;

namespace SkillcadeSDK.Connection
{
    [CreateAssetMenu(fileName = "Connection Config", menuName = "Configs/Connection")]
    public class ConnectionConfig : ScriptableObject
    {
        [SerializeField] public string ServerAddress;
        [SerializeField] public ushort ServerListenPort;
        [SerializeField] public ushort WssConnectPort;
        [SerializeField] public string WssServerName;
        [SerializeField] public bool UseEncryption;
        [SerializeField] public bool SkillcadeHubIntegrated;

        [Header("Reconnect")]
        [SerializeField] public float ReconnectDelay;

        [Header("Game")]
        [SerializeField] public int TargetPlayerCount;

        [Header("Build Configuration")]
        [SerializeField] public string[] SceneNames;

        public ConnectionData GetData()
        {
            return new ConnectionData
            {
                ServerAddress = ServerAddress,
                ServerListenPort = ServerListenPort,
                WssServerName = WssServerName,
                WssConnectPort = WssConnectPort,
                UseEncryption = UseEncryption,
                ReconnectDelay = ReconnectDelay,
                TargetPlayerCount = TargetPlayerCount,
                SkillcadeHubIntegrated = SkillcadeHubIntegrated,
            };
        }
    }
}