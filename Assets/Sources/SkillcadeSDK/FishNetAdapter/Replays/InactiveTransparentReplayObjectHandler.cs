using System.Collections.Generic;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class InactiveTransparentReplayObjectHandler : FishNetReplayObjectHandler
    {
        private struct SpriteRendererContainer
        {
            public SpriteRenderer Renderer;
            public float DefaultAlpha;
        }
        
        private struct CanvasGroupContainer
        {
            public CanvasGroup CanvasGroup;
            public float DefaultAlpha;
        }

        [SerializeField] private SpriteRenderer[] _targetRenderers;
        [SerializeField] private CanvasGroup[] _targetGroups;

        private List<SpriteRendererContainer> _rendererContainers;
        private List<CanvasGroupContainer> _canvasGroupContainers;

        protected override void Awake()
        {
            base.Awake();
            _rendererContainers = new List<SpriteRendererContainer>();
            _canvasGroupContainers = new List<CanvasGroupContainer>();

            foreach (var targetRenderer in _targetRenderers)
            {
                _rendererContainers.Add(new SpriteRendererContainer
                {
                    Renderer = targetRenderer,
                    DefaultAlpha = targetRenderer.color.a
                });
            }
            
            foreach (var targetGroup in _targetGroups)
            {
                _canvasGroupContainers.Add(new CanvasGroupContainer
                {
                    CanvasGroup = targetGroup,
                    DefaultAlpha = targetGroup.alpha
                });
            }
        }

        public override void SetVisible(float transparency)
        {
            foreach (var container in _rendererContainers)
            {
                var color = container.Renderer.color;
                color.a = container.DefaultAlpha * transparency;
                container.Renderer.color = color;
            }

            foreach (var container in _canvasGroupContainers)
            {
                container.CanvasGroup.alpha = container.DefaultAlpha * transparency;
            }
        }
    }
}