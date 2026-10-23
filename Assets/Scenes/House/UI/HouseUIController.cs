using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HouseUIController : MonoBehaviour
{
    public EditModeController edit;
    public PlacementController placement;
    public RoomSurfaces surfaces;
    public StyleSheet styleSheet;

    [Header("Meubel Categorieën")]
    public List<FurnitureData> bedInventory = new List<FurnitureData>();
    public List<FurnitureData> deskInventory = new List<FurnitureData>();
    public List<FurnitureData> nightstandInventory = new List<FurnitureData>();
    public List<FurnitureData> chairInventory = new List<FurnitureData>();
    public List<FurnitureData> mirrorInventory = new List<FurnitureData>();

    [Header("Wanden en vloeren")]
    public List<SurfaceData> wallpaperInventory = new List<SurfaceData>();
    public List<SurfaceData> floorInventory = new List<SurfaceData>();

    VisualElement root, container, touchArea, inventoryPanel, placementBar, selectionBar, categoryPanel;
    Button toggleBtn, categoriesBtn, turnLeft, turnRight, rotateBtn, confirmBtn, cancelBtn;
    Button moveBtn, removeBtn, deselectBtn;
    Button catBeds, catDesks, catNightstands, catChairs, catMirrors, catWallpaper, catFloors, catBack;
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
        selectionBar = root.Q<VisualElement>("selection-bar");
        list = root.Q<ScrollView>("inventory-list");

        turnLeft = root.Q<Button>("turn-left");
        turnRight = root.Q<Button>("turn-right");
        rotateBtn = root.Q<Button>("btn-rotate");
        confirmBtn = root.Q<Button>("btn-confirm");
        cancelBtn = root.Q<Button>("btn-cancel");
        moveBtn = root.Q<Button>("btn-move");
        removeBtn = root.Q<Button>("btn-remove");
        deselectBtn = root.Q<Button>("btn-deselect");

        catBeds = root.Q<Button>("cat-beds");
        catDesks = root.Q<Button>("cat-desks");
        catNightstands = root.Q<Button>("cat-nightstands");
        catChairs = root.Q<Button>("cat-chairs");
        catMirrors = root.Q<Button>("cat-mirrors");
        catWallpaper = root.Q<Button>("cat-wallpaper");
        catFloors = root.Q<Button>("cat-floors");
        catBack = root.Q<Button>("cat-back");

        toggleBtn.clicked += ToggleMainUI;
        categoriesBtn.clicked += ToggleCategoryPanel;

        catBeds.clicked += () => LoadCategory(bedInventory);
        catDesks.clicked += () => LoadCategory(deskInventory);
        catNightstands.clicked += () => LoadCategory(nightstandInventory);
        catChairs.clicked += () => LoadCategory(chairInventory);
        catMirrors.clicked += () => LoadCategory(mirrorInventory);
        catWallpaper.clicked += () => LoadSurfaces(wallpaperInventory);
        catFloors.clicked += () => LoadSurfaces(floorInventory);
        catBack.clicked += () => Show(categoryPanel, false);

        turnLeft.clicked += edit.TurnLeft;
        turnRight.clicked += edit.TurnRight;
        rotateBtn.clicked += placement.Rotate;
        confirmBtn.clicked += () => placement.Confirm();
        cancelBtn.clicked += placement.Cancel;

        moveBtn.clicked += placement.MoveSelected;
        removeBtn.clicked += placement.RemoveSelected;
        deselectBtn.clicked += placement.Deselect;

        touchArea.RegisterCallback<PointerDownEvent>(OnPointerDown);
        touchArea.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        touchArea.RegisterCallback<PointerUpEvent>(OnPointerUp);

        placement.PlacementChanged += OnPlacementChanged;
        placement.ValidityChanged += OnValidityChanged;
        placement.SelectionChanged += OnSelectionChanged;

        SetUIOpen(false);
    }

    void OnDisable()
    {
        placement.PlacementChanged -= OnPlacementChanged;
        placement.ValidityChanged -= OnValidityChanged;
        placement.SelectionChanged -= OnSelectionChanged;
    }

    void ToggleMainUI() => SetUIOpen(!isOpen);

    void SetUIOpen(bool open)
    {
        isOpen = open;
        toggleBtn.EnableInClassList("btn-active", isOpen);   // house button lights up while editing

        // The container holds the inventory button, panels and bars,
        // so none of it is visible outside edit mode.
        Show(container, isOpen);
        Show(categoryPanel, false);
        Show(inventoryPanel, false);
        Show(placementBar, false);
        Show(selectionBar, false);
        Show(categoriesBtn, true);   // may have been hidden when closing during placement

        // In edit mode the whole screen listens for taps: drag while placing, select otherwise.
        touchArea.pickingMode = isOpen ? PickingMode.Position : PickingMode.Ignore;

        if (isOpen)
        {
            if (!edit.IsEditing) edit.Toggle();
        }
        else
        {
            placement.Deselect();
            if (placement.IsPlacing) placement.Cancel();   // a piece being moved goes back to where it was
            if (edit.IsEditing) edit.Toggle();
        }
    }

    void ToggleCategoryPanel()
    {
        bool currentlyShow = categoryPanel.style.display == DisplayStyle.Flex;
        Show(categoryPanel, !currentlyShow);
        Show(inventoryPanel, false);
    }

    // ---------- inventory ----------
    void LoadCategory(List<FurnitureData> dataList)
    {
        Show(categoryPanel, false);
        Show(inventoryPanel, true);

        list.Clear();
        foreach (var data in dataList)
        {
            if (data == null) continue;
            var item = MakeItem(data.icon, string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName, null);
            var captured = data;
            item.clicked += () => {
                Show(inventoryPanel, false);
                placement.Begin(captured);
            };
            list.Add(item);
        }
    }

    // Wallpaper and floors apply instantly and the bar stays open so you can try several.
    void LoadSurfaces(List<SurfaceData> dataList)
    {
        Show(categoryPanel, false);
        Show(inventoryPanel, true);

        list.Clear();
        foreach (var data in dataList)
        {
            if (data == null) continue;
            var item = MakeItem(data.icon, string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName, data.material);
            var captured = data;
            item.clicked += () => { if (surfaces != null) surfaces.Apply(captured); };
            list.Add(item);
        }
    }

    Button MakeItem(Sprite iconSprite, string text, Material swatch)
    {
        var item = new Button();
        item.AddToClassList("btn");
        item.AddToClassList("inv-item");

        var icon = new VisualElement();
        icon.AddToClassList("inv-icon");
        icon.pickingMode = PickingMode.Ignore;
        if (iconSprite != null)
            icon.style.backgroundImage = new StyleBackground(iconSprite);
        else if (swatch != null && (swatch.HasProperty("_Color") || swatch.HasProperty("_BaseColor")))
            icon.style.backgroundColor = swatch.color;   // no icon? show the material colour

        var label = new Label(text);
        label.AddToClassList("inv-label");
        label.pickingMode = PickingMode.Ignore;

        item.Add(icon);
        item.Add(label);
        return item;
    }

    static void Show(VisualElement e, bool on) => e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;

    // ---------- state callbacks ----------
    void OnPlacementChanged(bool placing)
    {
        Show(inventoryPanel, false);
        Show(categoryPanel, false);
        Show(selectionBar, false);
        Show(placementBar, isOpen && placing);
        Show(categoriesBtn, isOpen && !placing && !placement.HasSelection);
    }

    void OnSelectionChanged(bool hasSelection)
    {
        Show(selectionBar, isOpen && hasSelection);
        if (hasSelection)
        {
            Show(inventoryPanel, false);
            Show(categoryPanel, false);
        }
        Show(categoriesBtn, isOpen && !hasSelection && !placement.IsPlacing);
    }

    void OnValidityChanged(bool valid) => confirmBtn.SetEnabled(valid);

    // ---------- touch ----------
    void OnPointerDown(PointerDownEvent e)
    {
        if (!isOpen) return;
        if (placement.IsPlacing)
        {
            touchArea.CapturePointer(e.pointerId);
            placement.MoveToScreenPoint(ToScreen(e.position));
        }
        else
        {
            placement.TapSelect(ToScreen(e.position));
        }
    }

    void OnPointerMove(PointerMoveEvent e)
    {
        if (isOpen && placement.IsPlacing && touchArea.HasPointerCapture(e.pointerId))
            placement.MoveToScreenPoint(ToScreen(e.position));
    }

    void OnPointerUp(PointerUpEvent e)
    {
        if (touchArea.HasPointerCapture(e.pointerId)) touchArea.ReleasePointer(e.pointerId);
    }

    Vector2 ToScreen(Vector2 panelPos)
    {
        Vector2 panelSize = root.panel.visualTree.layout.size;
        return new Vector2(panelPos.x / panelSize.x * Screen.width,
                           Screen.height - panelPos.y / panelSize.y * Screen.height);
    }
}
