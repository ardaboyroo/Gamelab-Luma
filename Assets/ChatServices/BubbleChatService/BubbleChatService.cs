using System;
using UnityEngine;
using Luma.QuipService;
using Luma.EmojiService;

namespace Luma.BubbleChatService
{
    public static class BubbleChatService
    {
        // Subscribe to forward local messages. DisplayReceived never emits this event.
        public static event Action<Player, ChatMessage> MessageSent;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => MessageSent = null;

        public static bool SendLocal(ChatMessage message)
        {
            var player = PlayerService.Instance != null ? PlayerService.Instance.LocalPlayer : null;
            if (player == null || !DisplayReceived(player, message)) return false;
            MessageSent?.Invoke(player, message);
            return true;
        }

        public static bool DisplayReceived(Player player, ChatMessage message)
        {
            if (player == null) return false;
            string text;
            Sprite image = null;
            switch (message.Type)
            {
                case ChatMessageType.Quip:
                    var quips = Resources.Load<QuipCatalog>("QuipService/QuipCatalog");
                    if (quips == null || !quips.TryGetQuip(message.Id, out var quip)) return false;
                    text = quip.Text;
                    break;
                case ChatMessageType.Emoji:
                    var emojis = Resources.Load<EmojiCatalog>("EmojiService/EmojiCatalog");
                    if (emojis == null || !emojis.TryGetEmoji(message.Id, out var emoji)) return false;
                    text = emoji.DisplayName;
                    image = emoji.Image;
                    break;
                default: return false;
            }
            var bubbles = player.GetComponent<PlayerChatBubbles>();
            if (bubbles == null) bubbles = player.gameObject.AddComponent<PlayerChatBubbles>();
            return bubbles.Show(text, image);
        }
    }
}
