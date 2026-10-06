using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target & Wall")]
    [Tooltip("Sleep hier je speler naartoe.")]
    [SerializeField] private Transform _target;
    [Tooltip("Sleep hier de Wall Transform naartoe.")]
    [SerializeField] private Transform _wall;

    [Header("Framing & Position Sliders")]
    [Tooltip("Verschoof de speler naar links/rechts op het scherm (negatief = naar links, zodat rechts vrij is voor obstakels).")]
    [Range(-10f, 10f)]
    [SerializeField] private float _horizontalOffset = -3f;

    [Tooltip("Hoogte van de camera ten opzichte van de speler.")]
    [Range(-5f, 10f)]
    [SerializeField] private float _verticalOffset = 1.5f;

    [Tooltip("Hoe ver de camera naar voren/achterren staat (zoom / diepte).")]
    [Range(-20f, 0f)]
    [SerializeField] private float _depthOffset = -10f;

    [Header("Smoothness")]
    [Tooltip("Hoe snel de camera volgt. Lager = strakker en sneller. Hoger = trager.")]
    [SerializeField] private float _smoothTime = 0.15f;

    private Vector3 _velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (_target == null) return;

        // Start bij de positie van de speler
        Vector3 targetPosition = _target.position;

        if (_wall != null)
        {
            // Bouw de offset op basis van de Inspector sliders in lokale muur-ruimte
            Vector3 customOffset = new Vector3(_horizontalOffset, _verticalOffset, _depthOffset);
            targetPosition += _wall.TransformDirection(customOffset);

            // Neem de rotatie van de muur over
            transform.rotation = _wall.rotation;
        }
        else
        {
            // Fallback als er geen muur is gekoppeld
            targetPosition += new Vector3(_horizontalOffset, _verticalOffset, _depthOffset);
        }

        // Beweeg de camera super soepel naar de doelpositie toe
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime);
    }
}