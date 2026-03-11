namespace SkillcadeSDK.Connection
{
    public class ConnectionData
    {
        public string ServerAddress;
        public ushort ServerListenPort;
        public ushort WssConnectPort;
        public string WssServerName;
        public bool UseEncryption;
        public bool SkillcadeHubIntegrated;

        public float ReconnectDelay;
        
        public int TargetPlayerCount;
    }
}