using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HouseUIController : MonoBehaviour
{
    public EditModeController edit;
    public PlacementController placement;
    public StyleSheet styleSheet;

    [Header("Meubel Categorieën")]
    public List<FurnitureData> bedInventory = new List<FurnitureData>();
    public List<FurnitureData> deskInventory = new List<FurnitureData>();
    public List<FurnitureData> nightstandInventory = new List<FurnitureData>();

    VisualElement root, container, touchArea, inventoryPanel, placementBar, categoryPanel;
    Button toggleBtn, categoriesBtn, turnLeft, turnRight, rotateBtn, confirmBtn, cancelBtn;
    Button catBeds, catDesks, catNightstands, catBack;
    ScrollView list;

    bool isOpen = false;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        if (styleSheet != null) root.styleSheets.Add(styleSheet);
        root.pickingMode = PickingMode.Ignore;

        container = root.Q<VisualElement>("house-ui-container");
        toggleBtn = root.Q<Button>("btn-toggle-ui");
        categoriesBtn = root.Q<Button>("btn-categories");
        categoryPanel = root.Q<VisualElement>("category-panel");
        touchArea = root.Q<VisualElement>("touch-area");
        inventoryPanel = root.Q<VisualElement>("inventory");
        placementBar = root.Q<VisualElement>("placement-bar");
        list = root.Q<ScrollView>("inventory-list");

        turnLeft = root.Q<Button>("turn-left");
        turnRight = root.Q<Button>("turn-right");
        rotateBtn = root.Q<Button>("btn-rotate");
        confirmBtn = root.Q<Button>("btn-confirm");
        cancelBtn = root.Q<Button>("btn-cancel");

        catBeds = root.Q<Button>("cat-beds");
        catDesks = root.Q<Button>("cat-desks");
        catNightstands = root.Q<Button>("cat-nightstands");
        catBack = root.Q<Button>("cat-back");

        toggleBtn.clicked += ToggleMainUI;
        categoriesBtn.clicked += ToggleCategoryPanel;

        catBeds.clicked += () => LoadCategory(bedInventory);
        catDesks.clicked += () => LoadCategory(deskInventory);
        catNightstands.clicked += () => LoadCategory(nightstandInventory);
        catBack.clicked += () => Show(categoryPanel, false);

        turnLeft.clicked += edit.TurnLeft;
        turnRight.clicked += edit.TurnRight;
        rotateBtn.clicked += placement.Rotate;
        confirmBtn.clicked += () => placement.Confirm();
        cancelBtn.clicked += placement.Cancel;

        touchArea.RegisterCallback<PointerDownEvent>(OnPointerDown);
        touchArea.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        touchArea.RegisterCallback<PointerUpEvent>(OnPointerUp);

        placement.PlacementChanged += OnPlacementChanged;
        placement.ValidityChanged += OnValidityChanged;

        SetUIOpen(false);
    }

    void OnDisable()
    {
        placement.PlacementChanged -= OnPlacementChanged;
        placement.ValidityChanged -= OnValidityChanged;
    }

    void ToggleMainUI() => SetUIOpen(!isOpen);

    void SetUIOpen(bool open)
    {
        isOpen = open;
        Show(container, isOpen);
        Show(categoryPanel, false);
        Show(inventoryPanel, false);
        Show(placementBar, false);

        if (isOpen)
        {
            if (!edit.IsEditing) edit.Toggle();
        }
        else
        {
            if (placement.IsPlacing) placement.Cancel();
            if (edit.IsEditing) edit.Toggle();
        }
    }

    void ToggleCategoryPanel()
    {
        bool currentlyShow = categoryPanel.style.display == DisplayStyle.Flex;
        Show(categoryPanel, !currentlyShow);
        Show(inventoryPanel, false);
    }

    void LoadCategory(List<FurnitureData> dataList)
    {
        Show(categoryPanel, false);
        Show(inventoryPanel, true);

        list.Clear();
        foreach (var data in dataList)
        {
            if (data == null) continue;
            var item = new Button();
            item.AddToClassList("inv-item");

            var icon = new VisualElement();
            icon.AddToClassList("inv-icon");
            if (data.icon != null) icon.style.backgroundImage = new StyleBackground(data.icon);

            var label = new Label(string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName);
            label.AddToClassList("inv-label");

            item.Add(icon);
            item.Add(label);
            var captured = data;
            item.clicked += () => {
                Show(inventoryPanel, false);
                placement.Begin(captured);
            };
            list.Add(item);
        }
    }

    static void Show(VisualElement e, bool on) => e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;

    void OnPlacementChanged(bool placing)
    {
        Show(inventoryPanel, false);
        Show(categoryPanel, false);
        Show(placementBar, isOpen && placing);
        Show(categoriesBtn, isOpen && !placing);
        touchArea.pickingMode = placing && isOpen ? PickingMode.Position : PickingMode.Ignore;
    }

    void OnValidityChanged(bool valid) => confirmBtn.SetEnabled(valid);

    void OnPointerDown(PointerDownEvent e) { if (isOpen) { touchArea.CapturePointer(e.pointerId); Drag(e.position); } }
    void OnPointerMove(PointerMoveEvent e) { if (isOpen && touchArea.HasPointerCapture(e.pointerId)) Drag(e.position); }
    void OnPointerUp(PointerUpEvent e) { if (touchArea.HasPointerCapture(e.pointerId)) touchArea.ReleasePointer(e.pointerId); }

    void Drag(Vector2 panelPos)
    {
        Vector2 panelSize = root.panel.visualTree.layout.size;
        Vector2 screen = new Vector2(panelPos.x / panelSize.x * Screen.width,
                                     Screen.height - panelPos.y / panelSize.y * Screen.height);
        placement.MoveToScreenPoint(screen);
    }
}