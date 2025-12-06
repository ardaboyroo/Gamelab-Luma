using UnityEngine;

public class GraphittiProgress : MonoBehaviour
{
    public float maxScale = 7;
    public float startHeight = 0;


    public void SetMaskFromValue(float value)
    {
        var end = startHeight - maxScale * 0.5f;
        transform.position = new Vector3(transform.position.x, Mathf.Lerp(maxScale * 0.5f + startHeight, startHeight, value), transform.position.z);
        transform.localScale = new Vector3(transform.localScale.x, Mathf.Lerp(0, maxScale, value), transform.localScale.z);
    }
}
