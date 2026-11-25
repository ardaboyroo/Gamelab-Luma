using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Luma.EmojiService;

namespace Luma.QuipService
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class QuipMenuUI : MonoBehaviour
    {
        [SerializeField] private QuipCatalog catalog;
        private UIDocument document;
        private Button backButton;
        private Button quipTab;
        private Button emojiTab;
        private EmojiMenuUI emojiMenu;
        private readonly Dictionary<Button, Action> callbacks = new();

        public event Action BackRequested;
        public bool IsVisible { get; private set; }

        // A separate document is created for each MenuUI, including menus in other scenes.
        public static QuipMenuUI Create(Transform parent, UIDocument menuDocument)
        {
            var layout = Resources.Load<VisualTreeAsset>("QuipService/QuipMenu");
            var catalog = Resources.Load<QuipCatalog>("QuipService/QuipCatalog");
            if (layout == null || catalog == null)
            {
                Debug.LogError("Quip menu layout or catalog is missing.");
                return null;
            }

            var go = new GameObject("QuipMenuUI");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = menuDocument.panelSettings;
            document.sortingOrder = menuDocument.sortingOrder + 1;
            document.visualTreeAsset = layout;
            var controller = go.AddComponent<QuipMenuUI>();
            controller.catalog = catalog;
            go.AddComponent<EmojiMenuUI>();
            go.SetActive(true);
            return controller;
        }

        private void OnEnable()
        {
            document = GetComponent<UIDocument>();
            backButton = document.rootVisualElement.Q<Button>("quip-back-btn");
            if (backButton != null) backButton.clicked += RequestBack;
            quipTab = document.rootVisualElement.Q<Button>("quips-tab");
            emojiTab = document.rootVisualElement.Q<Button>("emojis-tab");
            if (quipTab != null) quipTab.clicked += ShowQuips;
            if (emojiTab != null) emojiTab.clicked += ShowEmojis;
            emojiMenu = GetComponent<EmojiMenuUI>();
            if (emojiMenu != null) emojiMenu.Selected += CloseAfterEmoji;
            Hide();
        }

        private void OnDisable()
        {
            if (backButton != null) backButton.clicked -= RequestBack;
            if (quipTab != null) quipTab.clicked -= ShowQuips;
            if (emojiTab != null) emojiTab.clicked -= ShowEmojis;
            if (emojiMenu != null) emojiMenu.Selected -= CloseAfterEmoji;
            ClearButtons();
            IsVisible = false;
        }

        public void Show()
        {
            Refresh();
            ShowQuips();
            document.rootVisualElement.style.display = DisplayStyle.Flex;
            IsVisible = true;
        }

        public void Hide()
        {
            if (document != null) document.rootVisualElement.style.display = DisplayStyle.None;
            IsVisible = false;
        }

        public void Refresh()
        {
            ClearButtons();
            var list = document.rootVisualElement.Q<ScrollView>("quip-list");
            list.Clear();
            if (catalog != null)
            {
                foreach (var quip in catalog.Quips)
                {
                    if (quip == null) continue;
                    var id = quip.Id;
                    var button = new Button { text = quip.Text, name = $"quip-{id}" };
                    button.AddToClassList("quip-button");
                    button.SetEnabled(catalog.TryGetQuip(id, out _));
                    Action callback = () => SelectQuip(id);
                    button.clicked += callback;
                    callbacks.Add(button, callback);
                    list.Add(button);
                }
            }

            document.rootVisualElement.Q<Label>("quip-empty").style.display =
                list.childCount == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SelectQuip(int id)
        {
            if (catalog != null && catalog.TryGetQuip(id, out var quip))
            {
                Debug.Log(quip.Text);
                Hide();
                RequestBack();
            }
        }

        public void ShowQuips() => SelectTab(false);
        public void ShowEmojis() => SelectTab(true);

        private void SelectTab(bool emojis)
        {
            var root = document.rootVisualElement;
            root.Q<VisualElement>("quips-panel").style.display = emojis ? DisplayStyle.None : DisplayStyle.Flex;
            root.Q<VisualElement>("emoji-panel").style.display = emojis ? DisplayStyle.Flex : DisplayStyle.None;
            quipTab?.EnableInClassList("message-tab--selected", !emojis);
            emojiTab?.EnableInClassList("message-tab--selected", emojis);
            if (emojis && emojiMenu != null) emojiMenu.Refresh();
        }

        private void CloseAfterEmoji()
        {
            Hide();
            RequestBack();
        }

        private void ClearButtons()
        {
            foreach (var entry in callbacks) entry.Key.clicked -= entry.Value;
            callbacks.Clear();
        }

        private void RequestBack() => BackRequested?.Invoke();
    }
}
