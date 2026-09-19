using UnityEngine;

namespace Hollow
{
    /// <summary>Third-person orbit camera with wall collision and a small screen-shake.</summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform Target;
        public Enemy LockTarget;
        public float Distance = 5.5f;
        public float Sensitivity = 2.2f;
        public float PivotHeight = 1.7f;

        public float Yaw { get; private set; }
        float _pitch = 18f;
        float _current = 5.5f;
        float _shake;

        void Start()
        {
            SetCursorLocked(true);
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void Kick(float amount) { _shake = Mathf.Max(_shake, amount); }

        void LateUpdate()
        {
            if (Target == null) return;

            bool paused = Game.Instance != null && Game.Instance.Paused;
            bool toggled = Game.Instance != null && Game.Instance.InventoryToggledThisFrame;
            if (!paused && !toggled && GameInput.EscapePressed) SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            if (!paused && !locked && GameInput.AttackPressed) SetCursorLocked(true);

            bool hasTarget = LockTarget != null && !LockTarget.IsDead;
            if (locked && !paused)
            {
                Vector2 look = GameInput.Look * Sensitivity;
                if (!hasTarget) Yaw += look.x;                       // yaw is automatic while locked on
                _pitch = Mathf.Clamp(_pitch - look.y, -8f, 45f);
            }

            if (hasTarget)
            {
                Vector3 d = LockTarget.transform.position - Target.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.01f)
                {
                    float desired = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                    Yaw = Mathf.LerpAngle(Yaw, desired, 1f - Mathf.Exp(-9f * Time.deltaTime));
                }
            }

            Vector3 pivot = Target.position + Vector3.up * PivotHeight;
            Quaternion rot = Quaternion.Euler(_pitch, Yaw, 0f);
            Vector3 back = rot * Vector3.back;

            float dist = Distance;
            RaycastHit hit;
            if (Physics.SphereCast(pivot, 0.25f, back, out hit, Distance, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(0.7f, hit.distance);

            if (dist < _current) _current = dist;
            else _current = Mathf.MoveTowards(_current, dist, 10f * Time.deltaTime);

            Vector3 shake = Vector3.zero;
            if (paused) _shake = 0f;
            if (_shake > 0.001f)
            {
                shake = Random.insideUnitSphere * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, 1.6f * Time.deltaTime);
            }

            transform.position = pivot + back * _current + shake;
            transform.rotation = Quaternion.LookRotation(pivot - transform.position + Vector3.up * 0.2f);
        }
    }
}
