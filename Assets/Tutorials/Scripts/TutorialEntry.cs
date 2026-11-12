using UnityEngine;

/// One tutorial the buddy can explain. Create via: Assets > Create > Tutorials > Tutorial Entry.
/// Edit the name, portrait and text right here in the Inspector. Every page is one tap.
[CreateAssetMenu(menuName = "Tutorials/Tutorial Entry", fileName = "NewTutorial")]
public class TutorialEntry : ScriptableObject
{
    [Tooltip("Shown on the name plate above the text box. Leave empty to hide the name plate.")]
    public string speakerName = "Buddy";

    [Tooltip("Big picture of the buddy shown above the text box. Leave empty to hide it.")]
    public Sprite portrait;

    [Tooltip("Each entry is one page of text. The player taps to go to the next page.")]
    [TextArea(3, 8)]
    public string[] pages = { "Schrijf hier de tekst van de uitleg." };
}
