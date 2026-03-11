using SkillcadeSDK.DI;
using SkillcadeSDK.Replays.GUI;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.Replays
{
    public class ReplaysInstaller : MonoInstaller
    {
        [SerializeField] private ReplayPrefabRegistry _replayPrefabRegistry;
        [SerializeField] private ReplayReadService _replayReadService;
        [SerializeField] private ReplayFilePicker _replayFilePicker;
        [SerializeField] private ReplayInfoPanel _replayInfoPanel;
        [SerializeField] private ReplayColorPickerController _colorPickerController;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_replayPrefabRegistry);
            builder.RegisterInstance(_replayReadService);
            builder.RegisterInstance(_replayFilePicker);
            builder.RegisterInstance(_replayInfoPanel);
            builder.RegisterInstance(_colorPickerController);
        }
    }
}