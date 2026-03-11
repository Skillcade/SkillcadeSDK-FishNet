using UnityEngine;
using VContainer;

namespace SkillcadeSDK.Common
{
    public class PlayerLayerController : MonoBehaviour
    {
        [Inject] private readonly IObjectResolver _objectResolver;
        
        private int _defaultLayer;
        private int? _layer;

        private void Awake()
        {
            _defaultLayer = gameObject.layer;
        }

        private void OnEnable()
        {
            this.InjectToMe();
            if (!_objectResolver.TryResolve(out LayerProvider layerProvider))
                return;
            
            layerProvider.CollisionsStateChanged += OnCollisionStateChanged;
            if (layerProvider.CollisionsEnabled)
                GetLayer();
        }

        private void OnDisable()
        {
            if (!_objectResolver.TryResolve(out LayerProvider layerProvider))
                return;
            
            layerProvider.CollisionsStateChanged -= OnCollisionStateChanged;
            ReturnLayer();
        }

        private void OnCollisionStateChanged()
        {
            if (!_objectResolver.TryResolve(out LayerProvider layerProvider))
                return;
            
            ReturnLayer();
            if (layerProvider.CollisionsEnabled)
                GetLayer();
        }
        
        private void GetLayer()
        {
            if (!_objectResolver.TryResolve(out LayerProvider layerProvider))
                return;
            
            if (layerProvider.TryGetLayer(out int layer))
            {
                gameObject.SetLayerWithChildren(layer);
                _layer = layer;
            }
        }
        
        private void ReturnLayer()
        {
            if (!_objectResolver.TryResolve(out LayerProvider layerProvider))
                return;
            
            if (_layer.HasValue)
            {
                layerProvider.ReturnLayer(_layer.Value);
                _layer = null;
                gameObject.SetLayerWithChildren(_defaultLayer);
            }
        }
    }
}