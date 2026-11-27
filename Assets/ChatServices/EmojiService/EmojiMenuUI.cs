using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Luma.EmojiService
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class EmojiMenuUI : MonoBehaviour
    {
        [SerializeField] private EmojiCatalog catalog;
        private VisualElement panel;
        private ScrollView list;
        private Label empty;
        private readonly Dictionary<Button, Action> callbacks = new();

        public event Action Selected;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            panel = root.Q<VisualElement>("emoji-panel");
            if (panel == null) return;
            var layout = Resources.Load<VisualTreeAsset>("EmojiService/EmojiPanel");
            if (layout != null) layout.CloneTree(panel);
            list = panel.Q<ScrollView>("emoji-list");
            empty = panel.Q<Label>("emoji-empty");
            if (catalog == null) catalog = Resources.Load<EmojiCatalog>("EmojiService/EmojiCatalog");
        }

        private void OnDisable()
        {
            ClearButtons();
            panel?.Clear();
        }

        public void Refresh()
        {
            ClearButtons();
            if (list == null) return;
            list.Clear();
            if (catalog != null)
            {
                foreach (var emoji in catalog.Emojis)
                {
                    if (emoji == null) continue;
                    var id = emoji.Id;
                    var button = new Button { name = $"emoji-{id}" };
                    button.AddToClassList("emoji-button");
                    if (emoji.Image != null)
                    {
                        var image = new Image { sprite = emoji.Image, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                        image.AddToClassList("emoji-image");
                        button.Add(image);
                    }
                    else button.text = emoji.DisplayName;
                    button.SetEnabled(catalog.TryGetEmoji(id, out _));
                    Action callback = () => SelectEmoji(id);
                    button.clicked += callback;
                    callbacks.Add(button, callback);
                    list.Add(button);
                }
            }
            if (empty != null) empty.style.display = list.childCount == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SelectEmoji(int id)
        {
            if (catalog == null || !catalog.TryGetEmoji(id, out var emoji)) return;
            Debug.Log(emoji.DisplayName);
            Luma.BubbleChatService.BubbleChatService.SendLocal(new Luma.BubbleChatService.ChatMessage(emoji.MessageType, emoji.Id));
            Selected?.Invoke();
        }

        private void ClearButtons()
        {
            foreach (var entry in callbacks) entry.Key.clicked -= entry.Value;
            callbacks.Clear();
        }
    }
}
