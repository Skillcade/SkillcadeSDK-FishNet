using UnityEngine;

namespace SkillcadeSDK.Replays
{
    [CreateAssetMenu(fileName = "GameVersionConfig", menuName = "Configs/Game Version")]
    public class GameVersionConfig : ScriptableObject
    {
        [SerializeField] public string GameName;
        [SerializeField] public string GameVersion;
        [SerializeField] public string UnityVersion;
    }
}