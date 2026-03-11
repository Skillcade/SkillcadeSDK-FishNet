#if UNITY_SERVER || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SkillcadeSDK.ServerValidation
{
    public class ServerPayload
    {
        [ServerPayloadVariable("MATCH_ID")]
        public string MatchId;
        
        [ServerPayloadVariable("BACKEND_AUTH_TOKEN")]
        public string ServerAuthToken;

        [ServerPayloadVariable("CHOOSE_WINNER_URL")]
        public string ChooseWinnerUrl;
        
        [ServerPayloadVariable("SESSION_PUBLIC_KEY")]
        public string SessionPublicKey;

        [ServerPayloadVariable("REPLAY_UPLOAD_URL")]
        public string ReplayUploadUrl;
        
        [ServerPayloadVariable("SESSION_EXPIRES_AT", typeof(DateTimeVariableReader))]
        public DateTime SessionExpiresAt;
        
        [ServerPayloadVariable("CHARACTER_BY_PLAYER_IDS", typeof(JsonVariableReader<PlayerCharacterContainer[]>))]
        public PlayerCharacterContainer[] CharacterByPlayerIds;

        [JsonIgnore]
        public byte[] PublicKeyBytes;
    }
}
#endif