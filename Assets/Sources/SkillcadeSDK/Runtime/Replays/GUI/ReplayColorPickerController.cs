using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkillcadeSDK.Replays.GUI
{
    public class ReplayColorPickerController : MonoBehaviour
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private FlexibleColorPicker _picker;

        private Action<Color> _colorPickedCallback;

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            Close();
        }

        public void OpenColorPicker(Color currentColor, Action<Color> onColorPickedCallback)
        {
            gameObject.SetActive(true);
            _picker.color = currentColor;
            _colorPickedCallback = onColorPickedCallback;
        }

        private void Close()
        {
            gameObject.SetActive(false);
            _colorPickedCallback?.Invoke(_picker.color);
        }
    }
}