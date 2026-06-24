using UnityEngine;

namespace PoopPanic
{
    /// <summary>
    /// 犬が生成するウンチ。生成位置から地面へ落下し、
    /// 空中キャッチ＝高得点 / 着地後の回収＝通常得点 / 放置＝ミス。
    /// </summary>
    public class Poop : MonoBehaviour
    {
        public bool Collected { get; private set; }

        private float _fallT;       // 0→1 で着地
        private bool _landed;
        private float _ageAfterLand;
        private float _spawnHeight;
        private Renderer _renderer;

        private static readonly Color Fresh = new Color(0.45f, 0.30f, 0.12f); // できたて（茶）
        private static readonly Color Warn = new Color(0.85f, 0.55f, 0.10f);  // そろそろ危ない
        private static readonly Color Danger = new Color(0.85f, 0.15f, 0.10f); // 限界（赤）

        /// <summary>プリミティブでコミカルなウンチを組み立てて配置する。</summary>
        public static Poop Spawn(Vector3 groundPos)
        {
            var root = new GameObject("Poop");
            root.transform.position = groundPos + Vector3.up * 1.4f;

            // とぐろ：球を3段重ねて簡易的に表現。
            float[] scales = { 0.55f, 0.42f, 0.28f };
            float y = 0.18f;
            Renderer firstRend = null;
            for (int i = 0; i < scales.Length; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(s.GetComponent<Collider>());
                s.transform.SetParent(root.transform, false);
                s.transform.localScale = Vector3.one * scales[i];
                s.transform.localPosition = new Vector3(0, y, 0);
                y += scales[i] * 0.55f;
                if (firstRend == null) firstRend = s.GetComponent<Renderer>();
            }

            var poop = root.AddComponent<Poop>();
            poop._renderer = firstRend;
            poop._spawnHeight = root.transform.position.y;
            poop.ApplyColorToAll(Fresh);
            return poop;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.GameOver) return;
            if (Collected) return;

            if (!_landed)
            {
                _fallT += Time.deltaTime / GameConfig.PoopFallTime;
                float h = Mathf.Lerp(_spawnHeight, 0f, _fallT);
                var p = transform.position; p.y = h; transform.position = p;

                if (_fallT >= 1f)
                {
                    _landed = true;
                    var lp = transform.position; lp.y = 0f; transform.position = lp;
                    transform.localScale = new Vector3(1.1f, 0.85f, 1.1f); // 着地でぺちゃっと
                }
            }
            else
            {
                _ageAfterLand += Time.deltaTime;
                float a = _ageAfterLand / GameConfig.PoopLifetime;
                ApplyColorToAll(a < 0.6f ? Color.Lerp(Fresh, Warn, a / 0.6f)
                                         : Color.Lerp(Warn, Danger, (a - 0.6f) / 0.4f));

                // 限界が近いと点滅して警告。
                if (a > 0.75f && Mathf.Sin(Time.time * 18f) > 0f)
                    ApplyColorToAll(Danger);

                if (_ageAfterLand >= GameConfig.PoopLifetime)
                {
                    Collected = true; // 二重判定防止
                    if (gm != null) gm.OnPoopMissed();
                    Destroy(gameObject);
                }
            }
        }

        public void Collect()
        {
            if (Collected) return;
            Collected = true;
            bool freshCatch = !_landed; // 着地前ならボーナス
            if (GameManager.Instance != null)
                GameManager.Instance.OnPoopCollected(freshCatch);
            Destroy(gameObject);
        }

        private void ApplyColorToAll(Color c)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.material.color = c;
        }
    }
}
