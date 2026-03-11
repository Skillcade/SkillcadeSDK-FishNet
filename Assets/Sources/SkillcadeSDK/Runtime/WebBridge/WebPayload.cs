using Newtonsoft.Json;

namespace SkillcadeSDK.Common
{
    [JsonObject]
    public class WebPayload
    {
        [JsonProperty("joinToken")]
        public string JoinToken;
        
        [JsonProperty("nickname")]
        public string Nickname;
        
        [JsonProperty("playerId")]
        public string PlayerId;
        
        [JsonProperty("connectIp")]
        public string ConnectIp;
        
        [JsonProperty("port")]
        public ushort Port;
        
        [JsonProperty("serverName")]
        public string ServerName;
    }
}