#if SKILLCADE_DEBUG
using SkillcadeSDK.FishNetAdapter.DebugPanel.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.Views
{
    public class LatencySimulatorView : MonoBehaviour
    {
        [Header("Enable Toggle")]
        [SerializeField] private Toggle _enabledToggle;

        [Header("Latency")]
        [SerializeField] private Slider _latencySlider;
        [SerializeField] private TMP_Text _latencyValueText;
        [SerializeField] private TMP_InputField _latencyInputField;

        [Header("Packet Loss")]
        [SerializeField] private Slider _packetLossSlider;
        [SerializeField] private TMP_Text _packetLossValueText;
        [SerializeField] private TMP_InputField _packetLossInputField;

        [Header("Out of Order")]
        [SerializeField] private Slider _outOfOrderSlider;
        [SerializeField] private TMP_Text _outOfOrderValueText;
        [SerializeField] private TMP_InputField _outOfOrderInputField;

        [Header("Reset")]
        [SerializeField] private Button _resetButton;

        private LatencySimulatorControl _control;
        private bool _isUpdatingFromControl;

        private void Awake()
        {
            SetupSlider(_latencySlider, 0, 500, OnLatencySliderChanged);
            SetupSlider(_packetLossSlider, 0, 50, OnPacketLossSliderChanged);
            SetupSlider(_outOfOrderSlider, 0, 50, OnOutOfOrderSliderChanged);

            if (_enabledToggle != null)
            {
                _enabledToggle.onValueChanged.AddListener(OnEnabledToggleChanged);
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.AddListener(OnResetClicked);
            }

            SetupInputField(_latencyInputField, OnLatencyInputChanged);
            SetupInputField(_packetLossInputField, OnPacketLossInputChanged);
            SetupInputField(_outOfOrderInputField, OnOutOfOrderInputChanged);
        }

        private void SetupSlider(Slider slider, float min, float max, UnityEngine.Events.UnityAction<float> callback)
        {
            if (slider == null) return;

            slider.minValue = min;
            slider.maxValue = max;
            slider.onValueChanged.AddListener(callback);
        }

        private void SetupInputField(TMP_InputField inputField, UnityEngine.Events.UnityAction<string> callback)
        {
            if (inputField == null) return;

            inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            inputField.onEndEdit.AddListener(callback);
        }

        public void UpdateFromControl(LatencySimulatorControl control)
        {
            _control = control;

            if (control == null || !control.IsAvailable)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            _isUpdatingFromControl = true;

            if (_enabledToggle != null)
            {
                _enabledToggle.isOn = control.Enabled;
            }

            UpdateLatencyDisplay(control.Latency);
            UpdatePacketLossDisplay(control.PacketLoss * 100f);
            UpdateOutOfOrderDisplay(control.OutOfOrder * 100f);

            _isUpdatingFromControl = false;
        }

        private void UpdateLatencyDisplay(float value)
        {
            if (_latencySlider != null)
            {
                _latencySlider.value = value;
            }

            if (_latencyValueText != null)
            {
                _latencyValueText.text = $"{value:F0}ms";
            }

            if (_latencyInputField != null && !_latencyInputField.isFocused)
            {
                _latencyInputField.text = $"{value:F0}";
            }
        }

        private void UpdatePacketLossDisplay(float value)
        {
            if (_packetLossSlider != null)
            {
                _packetLossSlider.value = value;
            }

            if (_packetLossValueText != null)
            {
                _packetLossValueText.text = $"{value:F0}%";
            }

            if (_packetLossInputField != null && !_packetLossInputField.isFocused)
            {
                _packetLossInputField.text = $"{value:F0}";
            }
        }

        private void UpdateOutOfOrderDisplay(float value)
        {
            if (_outOfOrderSlider != null)
            {
                _outOfOrderSlider.value = value;
            }

            if (_outOfOrderValueText != null)
            {
                _outOfOrderValueText.text = $"{value:F0}%";
            }

            if (_outOfOrderInputField != null && !_outOfOrderInputField.isFocused)
            {
                _outOfOrderInputField.text = $"{value:F0}";
            }
        }

        private void OnEnabledToggleChanged(bool value)
        {
            if (_isUpdatingFromControl || _control == null) return;
            _control.Enabled = value;
        }

        private void OnLatencySliderChanged(float value)
        {
            if (_isUpdatingFromControl || _control == null) return;

            _control.Latency = (long)value;
            UpdateLatencyDisplay(value);
        }

        private void OnLatencyInputChanged(string value)
        {
            if (_isUpdatingFromControl || _control == null) return;

            if (float.TryParse(value, out float latency))
            {
                latency = Mathf.Clamp(latency, 0, 500);
                _control.Latency = (long)latency;
                UpdateLatencyDisplay(latency);
            }
        }

        private void OnPacketLossSliderChanged(float value)
        {
            if (_isUpdatingFromControl || _control == null) return;

            _control.PacketLoss = value / 100f;
            UpdatePacketLossDisplay(value);
        }

        private void OnPacketLossInputChanged(string value)
        {
            if (_isUpdatingFromControl || _control == null) return;

            if (float.TryParse(value, out float packetLoss))
            {
                packetLoss = Mathf.Clamp(packetLoss, 0, 50);
                _control.PacketLoss = packetLoss / 100f;
                UpdatePacketLossDisplay(packetLoss);
            }
        }

        private void OnOutOfOrderSliderChanged(float value)
        {
            if (_isUpdatingFromControl || _control == null) return;

            _control.OutOfOrder = value / 100f;
            UpdateOutOfOrderDisplay(value);
        }

        private void OnOutOfOrderInputChanged(string value)
        {
            if (_isUpdatingFromControl || _control == null) return;

            if (float.TryParse(value, out float outOfOrder))
            {
                outOfOrder = Mathf.Clamp(outOfOrder, 0, 50);
                _control.OutOfOrder = outOfOrder / 100f;
                UpdateOutOfOrderDisplay(outOfOrder);
            }
        }

        private void OnResetClicked()
        {
            if (_control == null) return;

            _control.Reset();
            UpdateFromControl(_control);
        }

        private void OnDestroy()
        {
            if (_latencySlider != null)
                _latencySlider.onValueChanged.RemoveListener(OnLatencySliderChanged);

            if (_packetLossSlider != null)
                _packetLossSlider.onValueChanged.RemoveListener(OnPacketLossSliderChanged);

            if (_outOfOrderSlider != null)
                _outOfOrderSlider.onValueChanged.RemoveListener(OnOutOfOrderSliderChanged);

            if (_enabledToggle != null)
                _enabledToggle.onValueChanged.RemoveListener(OnEnabledToggleChanged);

            if (_resetButton != null)
                _resetButton.onClick.RemoveListener(OnResetClicked);

            if (_latencyInputField != null)
                _latencyInputField.onEndEdit.RemoveListener(OnLatencyInputChanged);

            if (_packetLossInputField != null)
                _packetLossInputField.onEndEdit.RemoveListener(OnPacketLossInputChanged);

            if (_outOfOrderInputField != null)
                _outOfOrderInputField.onEndEdit.RemoveListener(OnOutOfOrderInputChanged);
        }
    }
}
#endif
