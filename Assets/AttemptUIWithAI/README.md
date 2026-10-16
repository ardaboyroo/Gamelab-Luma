# Avatar Creation

Open AvatarCreation.uxml in UI Builder. AvatarCreation.prefab is also placed in the Main scene as Avatar Creation (AttemptUIWithAI).

PanelSettings uses Scale With Screen Size, a 1080 x 1920 reference resolution matching the current Game view, and height matching. The document fills the panel. The phone frame and camera cutout in the reference are device hardware and are omitted.

This is an editable UI recreation with a functional HSV color picker. The remaining buttons are not wired to avatar customization or navigation. AvatarPreview.png is a static preview, not a live model.

## Switching selection panels

`optionsPanel` is the shared container. Its Style and Color tabs remain visible while its two separate content panels are switched. `colorPanel` is visible initially; `stylePanel` starts with the `is-hidden` class. The empty `styleOptions` ScrollView is ready for category-specific style buttons or thumbnails, and wraps its children into rows.

Use `using UnityEngine.UIElements;` and query the UIDocument root. For example, to show styles:

```csharp
root.Q<VisualElement>("stylePanel").RemoveFromClassList("is-hidden");
root.Q<VisualElement>("colorPanel").AddToClassList("is-hidden");
root.Q<Button>("styleTab").AddToClassList("selected-option");
root.Q<Button>("colorTab").RemoveFromClassList("selected-option");
```

Reverse those class changes to show colors. Populate `root.Q<ScrollView>("styleOptions")` with the selected category's styles. AvatarCreationPanelTabs now connects the Style and Color buttons and updates the selected tab highlight. You can also call ShowStyles() or ShowColors() on this component. Switching preserves the color picker state.

The layout was rendered and visually inspected in Unity at 1080 x 1920. No UI import errors or warnings were reported.

## Avatar asset provenance

Textures/AvatarPreview.png was created using the built-in image generation tool with Avatar_creation_.png as reference. The prompt was:

Extract only the gray 3D cartoon avatar from the attached UI reference into a transparent PNG game UI asset. Same black short wavy center parted hair, little horns, large cartoon eyes with lashes, smiling mouth, pale gray toy body and round hands. Front view, arms angled down. Show head through upper thighs; reconstruct area hidden by orange bar. No UI, no text, no phone, no decoration. Faithfully preserve reference character appearance. Portrait asset tightly framed.

The wave, trails, hue wheel and gradient textures were generated mathematically in the Unity Editor.

## Functional HSV color picker

AvatarCreationColorPicker is attached to the document prefab and scene instance. Drag the hue ring, left saturation track, or right brightness track. The center swatch is drawn as a true circle and displays the resulting opaque RGB color. Track gradients and handles follow the current HSV values. Top is 1 and bottom is 0 for both tracks. Pointer capture supports mouse and touch dragging outside a track.

```csharp
using Luma.AttemptUIWithAI;
using UnityEngine;

// Get the component from the same GameObject as the UIDocument.
var picker = GetComponent<AvatarCreationColorPicker>();
Color current = picker.SelectedColor;
picker.ColorChanged += color => Debug.Log(color);
picker.SetColor(Color.cyan);
picker.SetHSV(0.5f, 0.8f, 1f);
```

Unsubscribe named event handlers when your subscriber is disabled or destroyed. Hue, Saturation and Brightness are also available as normalized read-only properties. Switching between the separate style/color panels preserves the selected color. The picker does not change the avatar model automatically.

