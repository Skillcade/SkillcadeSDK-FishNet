#if SKILLCADE_DEBUG
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel
{
    [CreateAssetMenu(fileName = "NetworkDebugConfig", menuName = "Configs/Network Debug Config")]
    public class NetworkDebugConfig : ScriptableObject
    {
        [Header("Display Settings")]
        [Tooltip("Default panel visibility on startup")]
        public bool ShowPanelOnStart = false;

        [Tooltip("Toggle key for showing/hiding the panel")]
        public KeyCode ToggleKey = KeyCode.F3;

        [Tooltip("Update interval for statistics in seconds")]
        [Range(0.1f, 2f)]
        public float UpdateInterval = 0.5f;

        [Header("Section Toggles")]
        public bool ShowConnectionSection = true;
        public bool ShowTimingSection = true;
        public bool ShowPingSection = true;
        public bool ShowBandwidthSection = true;
        public bool ShowPacketStatsSection = true;
        public bool ShowLatencySimulatorSection = true;
    }
}
#endif
