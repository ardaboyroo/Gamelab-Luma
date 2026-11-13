using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HouseUIController : MonoBehaviour
{
    [Tooltip("The state this script is active on")]
	[SerializeField] private PlayerStates _workingState = PlayerStates.HouseEditState;

    public EditModeController edit;
    public PlacementController placement;
    public RoomSurfaces surfaces;
    public StyleSheet styleSheet;

    [Header("Huis verlaten")]
    [Tooltip("Only used when there is no SwitchScene (e.g. when the house is played on its own).")]
    public string exitSceneFallback = "V1 Build";

    [Header("Meubel Categorieën")]
    public List<FurnitureData> bedInventory = new List<FurnitureData>();
    public List<FurnitureData> deskInventory = new List<FurnitureData>();
    public List<FurnitureData> nightstandInventory = new List<FurnitureData>();
    public List<FurnitureData> chairInventory = new List<FurnitureData>();
    public List<FurnitureData> mirrorInventory = new List<FurnitureData>();

    [Header("Wanden en vloeren")]
    public List<SurfaceData> wallpaperInventory = new List<SurfaceData>();
    public List<SurfaceData> floorInventory = new List<SurfaceData>();

    [Header("Categorie-iconen (onderste balk)")]
    public Sprite bedIcon;
    public Sprite deskIcon;
    public Sprite nightstandIcon;
    public Sprite chairIcon;
    public Sprite mirrorIcon;
    public Sprite wallpaperIcon;
    public Sprite floorIcon;
    [Tooltip("Icon of the 'Terug' tile that goes from the furniture back to the categories.")]
    public Sprite backIcon;

    VisualElement root, container, touchArea, inventoryPanel, placementBar, selectionBar, categoryPanel;
    Button toggleBtn, exitBtn, categoriesBtn, turnLeft, turnRight, rotateBtn, confirmBtn, cancelBtn;
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
        exitBtn = root.Q<Button>("btn-exit-house");
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
        if (exitBtn != null) exitBtn.clicked += LeaveHouse;
        categoriesBtn.clicked += ToggleCategoryPanel;

        catBeds.clicked += () => LoadCategory(bedInventory);
        catDesks.clicked += () => LoadCategory(deskInventory);
        catNightstands.clicked += () => LoadCategory(nightstandInventory);
        catChairs.clicked += () => LoadCategory(chairInventory);
        catMirrors.clicked += () => LoadCategory(mirrorInventory);
        catWallpaper.clicked += () => LoadSurfaces(wallpaperInventory);
        catFloors.clicked += () => LoadSurfaces(floorInventory);
        catBack.clicked += () => Show(categoryPanel, false);
        Show(categoryPanel, false);

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

    // The door button: leaves the house the same way the menu's 'Huis' button does
    // (via the SwitchScene that comes along from the world scene).
    void LeaveHouse()
    {
        if (isOpen) return;   // not while decorating
        var switcher = FindFirstObjectByType<SwitchScene>();
        if (switcher != null) switcher.DoSwitchScene();
        else if (!string.IsNullOrEmpty(exitSceneFallback)) SceneManager.LoadScene(exitSceneFallback);
    }

    void ToggleMainUI()
    {
        if (isOpen)
        {
            PlayerService.Instance.LocalPlayer.StateManager.ChangePlayerState(PlayerStates.StandardState);
        }
        else
        {
            PlayerService.Instance.LocalPlayer.StateManager.ChangePlayerState(PlayerStates.HouseEditState);
        }
        SetUIOpen(!isOpen);
    }

    void SetUIOpen(bool open)
    {
        isOpen = open;
        toggleBtn.EnableInClassList("btn-active", isOpen);   // house button lights up while editing

        // The container holds the inventory button, panels and bars,
        // so none of it is visible outside edit mode.
        Show(container, isOpen);
        Show(categoryPanel, false);
        SetInventoryOpen(false);
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

    // The 'Inventaris' button opens/closes the bottom bar, starting with the categories.
    void ToggleCategoryPanel()
    {
        Show(categoryPanel, false);   // old pop-up, no longer used
        if (inventoryPanel.style.display == DisplayStyle.Flex) { SetInventoryOpen(false); return; }
        ShowCategories();
    }

    void ShowCategories()
    {
        SetInventoryOpen(true);
        list.Clear();
        AddCategory(bedIcon ? bedIcon : PickIcon(bedInventory), "Bedden", () => LoadCategory(bedInventory));
        AddCategory(deskIcon ? deskIcon : PickIcon(deskInventory), "Bureaus", () => LoadCategory(deskInventory));
        AddCategory(nightstandIcon ? nightstandIcon : PickIcon(nightstandInventory), "Nachtkastjes", () => LoadCategory(nightstandInventory));
        AddCategory(chairIcon ? chairIcon : PickIcon(chairInventory), "Stoelen", () => LoadCategory(chairInventory));
        AddCategory(mirrorIcon ? mirrorIcon : PickIcon(mirrorInventory), "Spiegels", () => LoadCategory(mirrorInventory));
        AddCategory(wallpaperIcon ? wallpaperIcon : PickIcon(wallpaperInventory), "Behang", () => LoadSurfaces(wallpaperInventory));
        AddCategory(floorIcon ? floorIcon : PickIcon(floorInventory), "Vloeren", () => LoadSurfaces(floorInventory));
        list.scrollOffset = Vector2.zero;
    }

    // No category icon set: use the modern piece's icon, otherwise the first icon in the list.
    static Sprite PickIcon(List<FurnitureData> items)
    {
        Sprite first = null;
        foreach (var d in items)
        {
            if (d == null || d.icon == null) continue;
            if (d.icon.name.Contains("Modern")) return d.icon;
            if (first == null) first = d.icon;
        }
        return first;
    }

    static Sprite PickIcon(List<SurfaceData> items)
    {
        foreach (var d in items) if (d != null && d.icon != null) return d.icon;
        return null;
    }

    void AddCategory(Sprite icon, string text, System.Action open)
    {
        var item = MakeItem(icon, text, null);
        item.AddToClassList("inv-category");
        item.clicked += open;
        list.Add(item);
    }

    // First tile in a category: back to the categories.
    void AddBackItem()
    {
        var back = MakeItem(backIcon, "Terug", null);
        back.AddToClassList("inv-back");
        back.clicked += ShowCategories;
        list.Add(back);
    }

    // Opening the bottom bar moves the Inventaris and house buttons up above it.
    void SetInventoryOpen(bool open)
    {
        Show(inventoryPanel, open);
        bool up = open && isOpen;
        toggleBtn.EnableInClassList("inv-open", up);
        categoriesBtn.EnableInClassList("inv-open", up);
    }

    // ---------- inventory ----------
    void LoadCategory(List<FurnitureData> dataList)
    {
        Show(categoryPanel, false);
        SetInventoryOpen(true);

        list.Clear();
        AddBackItem();
        foreach (var data in dataList)
        {
            if (data == null) continue;
            var item = MakeItem(data.icon, string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName, null);
            var captured = data;
            item.clicked += () => {
                SetInventoryOpen(false);
                placement.Begin(captured);
            };
            list.Add(item);
        }
    }

    // Wallpaper and floors apply instantly and the bar stays open so you can try several.
    void LoadSurfaces(List<SurfaceData> dataList)
    {
        Show(categoryPanel, false);
        SetInventoryOpen(true);

        list.Clear();
        AddBackItem();
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
        SetInventoryOpen(false);
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
            SetInventoryOpen(false);
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
