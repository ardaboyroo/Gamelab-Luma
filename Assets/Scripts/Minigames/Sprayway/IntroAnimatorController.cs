using System.Collections;
using UnityEngine;

namespace Minigames.Sprayway
{
    [RequireComponent(typeof(Animator))]
    public class IntroAnimatorController : MonoBehaviour
    {
        private Animator _animator;
        private RuntimeAnimatorController _controller;
        private string _stateName;
        private float _duration;

        public void Initialize(RuntimeAnimatorController controller, string stateName, float duration)
        {
            _controller = controller;
            _stateName = stateName;
            _duration = duration;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var sprayway = GetComponent<Minigames.Sprayway.PlayerMovement>();
            var cameraMovement = GetComponentInChildren<Player.Global.CameraMovement>();

            cameraMovement.Deactivate();

            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError("[IntroAnimatorController] Missing Animator!");
                Destroy(this);
                yield break;
            }

            var originalController = _animator.runtimeAnimatorController;
            _animator.runtimeAnimatorController = _controller;
            _animator.Play(_stateName, 0, 0f);

            yield return new WaitForSeconds(_duration);

            _animator.runtimeAnimatorController = originalController;

            cameraMovement.Activate();

            if (sprayway != null)
                sprayway.enabled = true;

            Destroy(this);
        }
    }
}