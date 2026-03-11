using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.Replays.GUI
{
    public class ReplayWorldControlPanel : MonoBehaviour
    {
        [SerializeField] private ReplayWorldControlItem _itemPrefab;
        [SerializeField] private Transform _itemsParent;

        [Inject] private readonly ReplayReadService _replayReadService;
        [Inject] private readonly ReplayColorPickerController _replayColorPickerController;
        
        private List<ReplayWorldControlItem> _items;

        private void OnEnable()
        {
            if (_replayReadService == null || !_replayReadService.IsReplayReady)
                return;
            
            if (_items != null)
                return;

            _items = new List<ReplayWorldControlItem>();
            var orderedWorlds = _replayReadService.ClientWorlds.OrderBy(x => x.Key);
            foreach (var clientWorld in orderedWorlds)
            {
                var item = Instantiate(_itemPrefab, _itemsParent);
                InitializeItem(item, clientWorld.Value);
                _items.Add(item);
            }
        }

        private void Update()
        {
            if (_items == null)
                return;
            
            foreach (var item in _items)
            {
                if (!_replayReadService.ClientWorlds.TryGetValue(item.WorldId, out var clientWorld))
                    continue;
                
                if (clientWorld.Tick == item.Tick)
                    continue;
                
                item.Tick = clientWorld.Tick;
                item.WorldTickText.text = clientWorld.Tick.ToString();
            }
        }

        private void InitializeItem(ReplayWorldControlItem item, ReplayClientWorld clientWorld)
        {
            int worldId = clientWorld.WorldId;
            item.WorldId = worldId;
            item.ActiveState.SetActive(worldId == _replayReadService.CurrentActiveWorldId);
            item.WorldNameText.text = worldId == ReplayReadService.ServerWorldId
                ? "Server View"
                : $"Player_{worldId} View";

            item.Tick = clientWorld.Tick;
            item.WorldTickText.text = clientWorld.Tick.ToString();
            
            item.SelectButton.onClick.AddListener(() => SetActiveWorld(worldId));
            
            item.TransparencySlider.value = clientWorld.Transparency;
            item.TransparencySlider.onValueChanged.AddListener(value => UpdateWorldTransparency(worldId, value));

            item.WorldColorImage.color = clientWorld.Color;
            item.PickColorButton.onClick.AddListener(() =>
            {
                _replayColorPickerController.OpenColorPicker(clientWorld.Color, color =>
                {
                    item.WorldColorImage.color = color;
                    _replayReadService.SetWorldColor(worldId, color);
                });
            });
        }

        private void SetActiveWorld(int worldId)
        {
            _replayReadService.SetActiveWorld(worldId);
            foreach (var item in _items)
            {
                item.ActiveState.SetActive(item.WorldId == worldId);
            }
        }

        private void UpdateWorldTransparency(int worldId, float value)
        {
            _replayReadService.SetWorldTransparency(worldId, value);
        }
    }
}