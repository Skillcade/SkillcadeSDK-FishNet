using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SkillcadeSDK.Replays.GUI
{
    public class ReplaySelectPanel : MonoBehaviour
    {
        [SerializeField] private GameObject[] _replayActiveObjects;
        [SerializeField] private Button _selectReplayButton;
        
        [Inject] private readonly ReplayReadService _replayReadService;
        [Inject] private readonly ReplayFilePicker _replayFilePicker;

        private void Awake()
        {
            _selectReplayButton.onClick.AddListener(OpenFileDialog);
            _replayActiveObjects.SetActive(false);
        }

        private async void OpenFileDialog()
        {
            try
            {
                var fileResult = await _replayFilePicker.OpenFileDialogAsync();
                Debug.Log($"[ReplaySelectPanel] File result: {fileResult.ToString()}");
                if (!fileResult.IsSuccess)
                    return;
                
                _replayReadService.ReadReplay(fileResult);
                _replayActiveObjects.SetActive(true);
                gameObject.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReplaySelectPanel] Error on selecting file: {e}");
            }
            finally
            {
                GC.Collect();
            }
        }
    }
}