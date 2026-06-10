using System;
using System.Threading.Tasks;
using SkillcadeSDK.Connection;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;

#if UNITY_SERVER || UNITY_EDITOR
using SkillcadeSDK.WebRequests;
#endif

namespace SkillcadeSDK.FishNetAdapter.Match
{
    /// <summary>
    /// Handles sending winner information to Skillcade backend.
    /// </summary>
    public class MatchService
    {
        [Inject] private readonly FishNetPlayersController _playersController;
        [Inject] private readonly ConnectionConfig _connectionConfig;

#if UNITY_SERVER || UNITY_EDITOR
        [Inject] private readonly WebRequester _webRequester;
#endif

        public async Task SendWinnerToBackend(int winnerClientId, string winnerPlayerId)
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (!_connectionConfig.SkillcadeHubIntegrated)
            {
                Debug.Log("[MatchService] Skillcade Hub not integrated, skipping winner send");
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(winnerPlayerId) || winnerClientId == 0)
                {
                    Debug.Log($"[MatchService] Sending winner {winnerClientId} with stable PlayerId {winnerPlayerId}");
                    await _webRequester.SendWinner(winnerPlayerId);
                    await _webRequester.StopDeployment();
                    return;
                }

                if (!_playersController.TryGetPlayerData(winnerClientId, out var playerData))
                {
                    Debug.Log($"[MatchService] Invalid winner ID: {winnerClientId}");
                    return;
                }

                if (!PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                {
                    Debug.LogError("[MatchService] Can't get winner player match data");
                    return;
                }

                Debug.Log($"[MatchService] Sending winner {winnerClientId} with PlayerId {matchData.PlayerId}");
                await _webRequester.SendWinner(matchData.PlayerId);
                await _webRequester.StopDeployment();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MatchService] Error sending winner: {e}");
            }
#else
            await Task.CompletedTask;
            Debug.Log("[MatchService] SendWinnerToBackend called on client, ignoring");
#endif
        }
    }
}
