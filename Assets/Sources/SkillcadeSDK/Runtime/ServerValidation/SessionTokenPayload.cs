#if UNITY_SERVER || UNITY_EDITOR
using System;
using Newtonsoft.Json;

namespace SkillcadeSDK.ServerValidation
{
    public class SessionTokenPayload
    {
        [JsonProperty("gameSessionId")]
        public string GameSessionId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("issuedAtUtc")]
        public DateTime IssuedAtUtc { get; set; }

        [JsonProperty("expiresAtUtc")]
        public DateTime ExpiresAtUtc { get; set; }

        [JsonProperty("nonce")]
        public string Nonce { get; set; }
    }
}
#endif