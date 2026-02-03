#if SKILLCADE_DEBUG
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.Views
{
    public class DebugSectionView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _contentText;
        [SerializeField] private Button _collapseButton;
        [SerializeField] private GameObject _contentContainer;

        private bool _isCollapsed;

        private void Awake()
        {
            _collapseButton.onClick.AddListener(ToggleCollapse);
        }

        public void SetContent(string content)
        {
            if (_contentText != null)
                _contentText.text = content;
        }

        public void SetCollapsed(bool collapsed)
        {
            _isCollapsed = collapsed;
            UpdateCollapseState();
        }

        private void ToggleCollapse()
        {
            _isCollapsed = !_isCollapsed;
            UpdateCollapseState();
        }

        private void UpdateCollapseState()
        {
            _contentContainer.SetActive(!_isCollapsed);
        }

        private void OnDestroy()
        {
            _collapseButton.onClick.RemoveListener(ToggleCollapse);
        }
    }
}
#endif
