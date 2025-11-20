using UnityEngine;

public class CameraRotationLock : MonoBehaviour
{
    [SerializeField] private bool _rotationYLock = true;
    [SerializeField] private Transform _player;   

    private void LateUpdate()
    {
        if (!_rotationYLock || _player == null)
            return;

        Vector3 local = transform.localEulerAngles;
        float playerY = _player.eulerAngles.y;    

        local.y = -playerY;                       
        transform.localEulerAngles = local;
    }
}