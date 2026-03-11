using System;
using Newtonsoft.Json;

namespace SkillcadeSDK.Replays
{
    [JsonObject]
    public class ReplayInfo
    {
        [JsonProperty("gn")]
        public string GameName { get; set; }
        
        [JsonProperty("gv")]
        public string GameVersion { get; set; }
        
        [JsonProperty("uv")]
        public string UnityVersion { get; set; }
        
        [JsonProperty("mid")]
        public string MatchId { get; set; }
        
        [JsonProperty("st")]
        public long StartTimestamp { get; set; }

        [JsonProperty("et")]
        public long EndTimestamp { get; set; }
        
        [JsonIgnore]
        public DateTime StartDateTime => new DateTime(StartTimestamp);
        
        [JsonIgnore]
        public DateTime EndDateTime => new DateTime(EndTimestamp);
    }
}