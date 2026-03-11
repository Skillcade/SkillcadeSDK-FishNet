using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SkillcadeSDK.Replays.GUI
{
    public class ReplayControlPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _currentTimeText;
        [SerializeField] private TMP_Text _totalTimeText;
        
        [Header("Play/Pause")]
        [SerializeField] private Button _playPauseButton;
        [SerializeField] private GameObject _pauseState;
        [SerializeField] private GameObject _playState;

        [Header("Jump forward/back")]
        [SerializeField] private Button _forward1FrameButton;
        [SerializeField] private Button _back1FrameButton;
        [SerializeField] private Button _forward5SecondsButton;
        [SerializeField] private Button _back5SecondsButton;
        [SerializeField] private Button _forward10SecondsButton;
        [SerializeField] private Button _back10SecondsButton;

        [Header("Timeline")]
        [SerializeField] private Slider _timelineSlider;

        [Inject] private readonly ReplayReadService _replayReadService;
        
        private void Awake()
        {
            _playPauseButton.onClick.AddListener(TogglePlayPause);
            _forward1FrameButton.onClick.AddListener(JumpForward1Frame);
            _back1FrameButton.onClick.AddListener(JumpBack1Frame);
            _forward5SecondsButton.onClick.AddListener(JumpForward5Seconds);
            _back5SecondsButton.onClick.AddListener(JumpBack5Seconds);
            _forward10SecondsButton.onClick.AddListener(JumpForward10Seconds);
            _back10SecondsButton.onClick.AddListener(JumpBack10Seconds);
            _timelineSlider.onValueChanged.AddListener(SetNormalizedTime);
            
            _pauseState.SetActive(true);
            _playState.SetActive(false);
        }

        private void Update()
        {
            if (_replayReadService == null || !_replayReadService.IsReplayReady)
                return;
            
            _currentTimeText.text = _replayReadService.CurrentTime.SecondsToTimeString();
            _totalTimeText.text = _replayReadService.TotalTime.SecondsToTimeString();
            
            _timelineSlider.SetValueWithoutNotify(_replayReadService.CurrentTime / _replayReadService.TotalTime);
        }

        private void TogglePlayPause()
        {
            if (_replayReadService == null || !_replayReadService.IsReplayReady)
                return;
            
            _replayReadService.IsPlaying = !_replayReadService.IsPlaying;
            _pauseState.SetActive(!_replayReadService.IsPlaying);
            _playState.SetActive(_replayReadService.IsPlaying);
        }

        private void JumpForward1Frame() => AddTimeAndJump(_replayReadService.TickInterval);
        private void JumpBack1Frame() =>  AddTimeAndJump(-_replayReadService.TickInterval);
        private void JumpForward5Seconds() => AddTimeAndJump(5);
        private void JumpBack5Seconds() => AddTimeAndJump(-5);
        private void JumpForward10Seconds() => AddTimeAndJump(10);
        private void JumpBack10Seconds() => AddTimeAndJump(-10);

        private void AddTimeAndJump(float value)
        {
            if (_replayReadService == null || !_replayReadService.IsReplayReady)
                return;
            
            float newTime = _replayReadService.CurrentTime + value;
            float normalizedTime = newTime / _replayReadService.TotalTime;
            SetNormalizedTime(normalizedTime);
        }

        private void SetNormalizedTime(float value)
        {
            if (_replayReadService == null || !_replayReadService.IsReplayReady)
                return;
            
            _replayReadService.SetNormalizedTime(value);
        }
    }
}