#if SKILLCADE_DEBUG
using TMPro;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.StatsOverlay
{
    public class NetworkStatsOverlayView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _contentText;

        public void SetContent(string content)
        {
            _contentText.text = content;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
#endif
