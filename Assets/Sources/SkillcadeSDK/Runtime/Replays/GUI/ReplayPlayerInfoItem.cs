using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillcadeSDK.Replays.GUI
{
    public class ReplayPlayerInfoItem : MonoBehaviour
    {
        public int PlayerId { get; set; }
        
        [SerializeField] public TMP_Text NicknameText;
        [SerializeField] public TMP_Text PingText;
        [SerializeField] public Button FollowButton;
    }
}