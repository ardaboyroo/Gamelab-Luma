using UnityEngine;
using Luma.BubbleChatService;

namespace Luma.EmojiService
{
    [CreateAssetMenu(fileName = "Emoji", menuName = "Luma/Emojis/Emoji")]
    public sealed class EmojiOption : ScriptableObject
    {
        [SerializeField] private ChatMessageType chatMessageType = ChatMessageType.Emoji;
        [Tooltip("Keep this ID unique and identical in every client's emoji catalog.")]
        [SerializeField, Min(0)] private int id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite image;

        public int Id => id;
        public ChatMessageType MessageType => chatMessageType;
        public string DisplayName => displayName;
        public Sprite Image => image;
        public EmojiMessage CreateMessage() => new EmojiMessage(id);
    }
}
