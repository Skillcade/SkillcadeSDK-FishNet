using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace SkillcadeSDK.Common
{
    public class LayerProvider : IInitializable
    {
        private const int LayerCount = 4;

        public event Action CollisionsStateChanged;
        
        public bool CollisionsEnabled { get; private set; }
        
        private readonly Stack<int> _availableLayerMasks = new Stack<int>();
        
        public void Initialize()
        {
            for (int i = 0; i < LayerCount; i++)
            {
                int layerId = i + 1;
                string layerName = $"Player{layerId}";
                int layer = LayerMask.NameToLayer(layerName);
                _availableLayerMasks.Push(layer);
            }

            CollisionsEnabled = true;
        }

        public bool TryGetLayer(out int layer) => _availableLayerMasks.TryPop(out layer);
        public void ReturnLayer(int layer) => _availableLayerMasks.Push(layer);

        public void SetCollisionsState(bool collisionsEnabled)
        {
            if (CollisionsEnabled == collisionsEnabled)
                return;
            
            CollisionsEnabled = collisionsEnabled;
            CollisionsStateChanged?.Invoke();
        }
    }
}