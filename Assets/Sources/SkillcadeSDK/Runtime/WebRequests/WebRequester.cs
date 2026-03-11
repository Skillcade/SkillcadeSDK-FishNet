using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SkillcadeSDK.Connection;
using UnityEngine;
using VContainer;

#if UNITY_SERVER || UNITY_EDITOR
using SkillcadeSDK.ServerValidation;
#endif

namespace SkillcadeSDK.WebRequests
{
#if UNITY_SERVER || UNITY_EDITOR
    public class WebRequester
    {
        private const string BaseUri = "https://demo.skillcade.com";
        private const string MediaTypeJson = "application/json";
        private const string TokenHeaderKey = "X-Game-Server-Token";

        [Inject] private readonly IConnectionController _connectionController;
        [Inject] private readonly ServerPayloadController _serverPayloadController;

        public async Task SendWinner(string winnerId)
        {
            Debug.Log($"[WebRequester] Send winner {winnerId}");
            if (_serverPayloadController.Payload == null)
            {
                Debug.LogError("[WebRequester] Server payload is null");
                return;
            }

            if (string.IsNullOrEmpty(_serverPayloadController.Payload.MatchId))
            {
                Debug.LogError("[WebRequester] Match id is empty");
                return;
            }

            if (string.IsNullOrEmpty(_serverPayloadController.Payload.ServerAuthToken))
            {
                Debug.LogError("[WebRequester] Server auth token is empty");
                return;
            }
            
            if (string.IsNullOrEmpty(winnerId))
            {
                Debug.LogError("[WebRequester] Winner id are empty");
                return;
            }
            
            string url = GetRequestUrl(winnerId);
            Debug.Log($"[WebRequester] Create http client to {url}");
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(url),
                DefaultRequestHeaders = { { TokenHeaderKey, _serverPayloadController.Payload.ServerAuthToken } }
            };

            Debug.Log("[WebRequester] Create request");
            var request = new ChooseWinnerRequest
            {
                WinnerId = winnerId
            };

            string matchId = _serverPayloadController.Payload.MatchId;
            Debug.Log($"[WebRequester] Sending winner request, match id: {matchId}, winnerId: {winnerId}, token: {_serverPayloadController.Payload.ServerAuthToken}");
            
            try
            {
                using var jsonContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, MediaTypeJson);
                Debug.Log("[WebRequester] Send request");
                using var response = await httpClient.PostAsync("", jsonContent);

                Debug.Log($"[WebRequester] choose winner response status: {response.StatusCode} - {response.ReasonPhrase}");
                response.EnsureSuccessStatusCode();
                
                var responseString = await response.Content.ReadAsStringAsync();
                Debug.Log($"[WebRequester] choose winner response: {responseString}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebRequester] Error sending winner {e}");
            }
        }

        private string GetRequestUrl(string matchId)
        {
            if (_serverPayloadController.Payload != null && !string.IsNullOrEmpty(_serverPayloadController.Payload.ChooseWinnerUrl))
            {
                Debug.Log($"[WebRequester] Got choose winner url from payload: {_serverPayloadController.Payload.ChooseWinnerUrl}");
                return _serverPayloadController.Payload.ChooseWinnerUrl;
            }

            Debug.Log("[WebRequester] Combine choose winner url as cached");
            return BaseUri + $"/api/playing-game/{matchId}/choose-winner";
        }
    }
#endif
}