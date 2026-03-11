using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SkillcadeSDK.Common;
using SkillcadeSDK.Connection;
using UnityEngine;
using VContainer;
using VContainer.Unity;

// ReSharper disable once CheckNamespace
public class WebBridge : MonoBehaviour, IInitializable, IDisposable
{
    public WebPayload Payload { get; private set; }
    
    [Inject] private readonly IConnectionController _connectionController;
    [Inject] private readonly ConnectionConfig _connectionConfig;
    
#if UNITY_WEBGL && !UNITY_EDITOR
    [UsedImplicitly]
    [DllImport("__Internal")]
    private static extern void GameLoaded();
    
    [UsedImplicitly]
    [DllImport("__Internal")]
    private static extern void ConnectedToServer();
    
    [UsedImplicitly]
    [DllImport("__Internal")]
    private static extern void QuitSinglePlayer();
#endif

    public void Initialize()
    {
        TriggerLoaded();
        _connectionController.OnStateChanged += OnConnectionStateChanged;
    }

    public void Dispose()
    {
        _connectionController.OnStateChanged -= OnConnectionStateChanged;
    }

    public void TriggerLoaded()
    {
        if (!_connectionConfig.SkillcadeHubIntegrated)
            return;
        
        Debug.Log("Trigger GameLoaded");
#if UNITY_WEBGL && !UNITY_EDITOR
        GameLoaded();
#endif
    }

    public void TriggerConnected()
    {
        if (!_connectionConfig.SkillcadeHubIntegrated)
            return;

        Debug.Log("Trigger ConnectedToServer");
#if UNITY_WEBGL && !UNITY_EDITOR
        ConnectedToServer();
#endif
    }

    public void TriggerQuitSinglePlayer()
    {
        Debug.Log("Trigger QuitSinglePlayer");
#if UNITY_WEBGL && !UNITY_EDITOR
        QuitSinglePlayer();
#endif
    }

    private void OnConnectionStateChanged(ConnectionState state)
    {
        if (state == ConnectionState.Connected)
            TriggerConnected();
    }

    [UsedImplicitly]
    private void Ping()
    {
        Debug.Log("Ping!");
    }

    [UsedImplicitly]
    private void SetPlayerId(int playerId)
    {
        Debug.Log($"Player id: {playerId}");
    }

    [UsedImplicitly]
    private void SetPlayerState(string json)
    {
        Debug.Log($"Player state: {json}");
        try
        {
            Payload = JsonConvert.DeserializeObject<WebPayload>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebBridge] Error on parsing payload: {e}");
        }
    }
}