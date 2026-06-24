using UnityEngine;

namespace PoopPanic
{
    /// <summary>
    /// クリックした地面へ移動し、近くのウンチを自動回収するプレイヤー。
    /// 設計書の「プレイヤーが犬の近くにいるだけで自動回収（MVP）」に対応。
    /// </summary>
    public class Player : MonoBehaviour
    {
        private Vector3 _destination;
        private Camera _cam;
        private Transform _body;     // 見た目（向き回転用）
        private Plane _ground = new Plane(Vector3.up, Vector3.zero);

        public void Init(Camera cam, Transform body)
        {
            _cam = cam;
            _body = body;
            _destination = transform.position;
        }

        public void ResetState()
        {
            transform.position = Vector3.zero;
            _destination = Vector3.zero;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.GameOver) return;

            ReadClick();
            MoveTowardsDestination();
            AutoCollect();
        }

        private void ReadClick()
        {
            if (!Input.GetMouseButton(0)) return;
            if (_cam == null) return;

            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (_ground.Raycast(ray, out float enter))
            {
                Vector3 p = ray.GetPoint(enter);
                float e = GameConfig.FieldHalfExtent;
                p.x = Mathf.Clamp(p.x, -e, e);
                p.z = Mathf.Clamp(p.z, -e, e);
                p.y = 0f;
                _destination = p;
            }
        }

        private void MoveTowardsDestination()
        {
            Vector3 flat = transform.position;
            flat.y = 0f;
            Vector3 to = _destination - flat;
            if (to.sqrMagnitude < 0.0004f) return;

            Vector3 step = to.normalized * GameConfig.PlayerSpeed * Time.deltaTime;
            if (step.sqrMagnitude >= to.sqrMagnitude)
                transform.position = new Vector3(_destination.x, transform.position.y, _destination.z);
            else
                transform.position += step;

            if (_body != null)
                _body.forward = Vector3.Slerp(_body.forward, to.normalized, 12f * Time.deltaTime);
        }

        private void AutoCollect()
        {
            float r2 = GameConfig.CollectRadius * GameConfig.CollectRadius;
            // 数が少ないので全走査で十分（MVP）。
            foreach (var poop in Object.FindObjectsByType<Poop>(FindObjectsSortMode.None))
            {
                if (poop.Collected) continue;
                Vector3 d = poop.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude <= r2)
                    poop.Collect();
            }
        }
    }
}
