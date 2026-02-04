#if SKILLCADE_DEBUG
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.Views
{
    public class DebugSectionView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _contentText;
        [SerializeField] private TMP_Text _collapsedStateText;
        [SerializeField] private Button _collapseButton;
        [SerializeField] private GameObject _contentContainer;

        private bool _isCollapsed;

        private void Awake()
        {
            _collapseButton.onClick.AddListener(ToggleCollapse);
            _isCollapsed = false;
            UpdateCollapseState();
        }

        public void SetContent(string content)
        {
            if (_contentText != null)
                _contentText.text = content;
        }

        private void ToggleCollapse()
        {
            _isCollapsed = !_isCollapsed;
            UpdateCollapseState();
        }

        private void UpdateCollapseState()
        {
            _collapsedStateText.text = _isCollapsed ? "Expand" : "Collapse";
            _contentContainer.SetActive(!_isCollapsed);
        }
    }
}
#endif
