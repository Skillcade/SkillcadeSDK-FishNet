using UnityEngine;
using UnityEngine.UI;

namespace SkillcadeSDK.Common.GUI
{
    public class ExpandablePanel : MonoBehaviour
    {
        [SerializeField] private bool _defaultExpanded;
        [SerializeField] private Button _expandButton;
        [SerializeField] private Button _collapseButton;
        [SerializeField] private GameObject[] _expandedStateObjects;
        [SerializeField] private GameObject[] _collapsedStateObjects;

        private void Awake()
        {
            _expandButton.onClick.AddListener(Expand);
            _collapseButton.onClick.AddListener(Collapse);
            
            _expandedStateObjects.SetActive(_defaultExpanded);
            _collapsedStateObjects.SetActive(!_defaultExpanded);
        }

        private void Expand()
        {
            _expandedStateObjects.SetActive(true);
            _collapsedStateObjects.SetActive(false);
        }

        private void Collapse()
        {
            _expandedStateObjects.SetActive(false);
            _collapsedStateObjects.SetActive(true);
        }
    }
}