using UnityEngine;

namespace Luma.QuipService
{
    [CreateAssetMenu(fileName = "Quip", menuName = "Luma/Quips/Quip")]
    public sealed class Quip : ScriptableObject
    {
        [Tooltip("Keep this ID unique and identical in every client's catalog.")]
        [SerializeField, Min(0)] private int id;
        [SerializeField, TextArea(2, 5)] private string text;

        public int Id => id;
        public string Text => text;

        public QuipMessage CreateMessage() => new QuipMessage(id);
    }
}
