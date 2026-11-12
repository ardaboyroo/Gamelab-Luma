using UnityEngine;

/// House tutorials: the buddy gets a '!' when the player enters the house,
/// and again after the player places a mirror for the first time.
/// Put ONE of these in the house scene. Text lives in the TutorialEntry assets.
public class HouseTutorialTriggers : MonoBehaviour
{
    [Tooltip("Leave empty to use the buddy in the scene.")]
    public BuddyTutorialMarker buddy;
    [Tooltip("Leave empty to use the PlacementController in the scene.")]
    public PlacementController placement;

    [Header("Tutorials")]
    public TutorialEntry enterHouseTutorial;
    public TutorialEntry mirrorPlacedTutorial;

    [Header("Progress")]
    [Tooltip("Off: tutorials show again every time you press Play (handy while testing). On: once per device.")]
    public bool rememberBetweenSessions = false;

    void Start()
    {
        TutorialProgress.SaveBetweenSessions = rememberBetweenSessions;

        if (buddy == null) buddy = FindFirstObjectByType<BuddyTutorialMarker>();
        if (placement == null) placement = FindFirstObjectByType<PlacementController>();

        if (buddy == null)
        {
            Debug.LogWarning("[HouseTutorialTriggers] No buddy (BuddyTutorialMarker) in the scene, tutorials can't show.", this);
            return;
        }

        if (placement != null) placement.Placed += OnFurniturePlaced;
        buddy.Queue(enterHouseTutorial);
    }

    void OnDestroy()
    {
        if (placement != null) placement.Placed -= OnFurniturePlaced;
    }

    void OnFurniturePlaced(FurnitureData data)
    {
        if (data != null && data.prefab != null && data.prefab.GetComponentInChildren<Mirror>(true) != null)
            buddy.Queue(mirrorPlacedTutorial);
    }

    [ContextMenu("Reset tutorial progress")]
    void ResetProgress()
    {
        TutorialProgress.ResetAll(new[] { enterHouseTutorial, mirrorPlacedTutorial });
        Debug.Log("[HouseTutorialTriggers] Tutorial progress reset.", this);
    }
}
