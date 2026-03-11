#if UNITY_SERVER || UNITY_EDITOR
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SkillcadeSDK.ServerValidation;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.Replays
{
    public class ReplaySendService
    {
        private const string TokenHeaderKey = "X-Game-Server-Token";
        
        [Inject] private readonly ServerPayloadController _serverPayloadController;

        public async Task SendReplayFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[ReplaySendService] File not found: {filePath}");
                return;
            }

            if (_serverPayloadController.Payload == null)
            {
                Debug.LogError("[ReplaySendService] Server payload not initialized");
                return;
            }

            if (string.IsNullOrEmpty(_serverPayloadController.Payload.ReplayUploadUrl))
            {
                Debug.LogError("[ReplaySendService] Replay upload url is not found in server envs");
                return;
            }

            Debug.Log($"[ReplaySendService] Send replay file at {filePath} to {_serverPayloadController.Payload.ReplayUploadUrl}");
            Debug.Log("[ReplaySendService] Open file");
            await using var fileStream = File.OpenRead(filePath);
            using var content = new StreamContent(fileStream);

            Debug.Log("[ReplaySendService] File opened, sending");
            
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.PutAsync(_serverPayloadController.Payload.ReplayUploadUrl, content);
                Debug.Log($"[ReplaySendService] replay file send, success: {response.IsSuccessStatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"[ReplaySendService] Failed to upload replay to S3. Status: {response.StatusCode}, Details: {error}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReplaySendService] Error on sending replay file: {e}");
            }
        }
    }
}
#endif