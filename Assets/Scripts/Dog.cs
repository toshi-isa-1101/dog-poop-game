using UnityEngine;

namespace PoopPanic
{
    public enum DogPersonality
    {
        Normal,   // 普通
        Lazy,     // のんびり犬：遅い・フェイント少なめ
        Mischief, // いたずら犬：フェイント多め
        Dash,     // ダッシュ犬：予兆前に別の場所へ走る
        Pack      // 群れ犬：他の犬につられてしゃがむ（簡易）
    }

    /// <summary>
    /// 状態機械で動く犬。Wander→Tell(予兆)→本物 or フェイント。
    /// 本物ならお尻の位置にウンチを生成する。性格でテンポと本物率が変わる。
    /// </summary>
    public class Dog : MonoBehaviour
    {
        private enum State { Wander, Tell, Cooldown }

        public DogPersonality Personality { get; private set; }

        private State _state;
        private Vector3 _wanderTarget;
        private float _speed;
        private float _stateTimer;
        private float _nextTellTimer;
        private bool _isReal;            // 今回の予兆が本物か
        private float _baseScaleY;
        private Transform _body;
        private Renderer _renderer;
        private Color _baseColor;
        private bool _frozen;

        // Pack 用：誰かが Tell に入ったらフラグを立てる（静的な簡易同期）。
        private static bool s_someoneTelling;

        public void Init(DogPersonality personality, Transform body, Renderer rend, Color color)
        {
            Personality = personality;
            _body = body;
            _renderer = rend;
            _baseColor = color;
            _baseScaleY = body.localScale.y;

            _speed = personality == DogPersonality.Lazy ? 1.6f
                   : personality == DogPersonality.Dash ? 4.2f
                   : 2.6f;

            ResetState();
            // 個体差で予兆タイミングをずらす（index ベースではなく初期化時に分散）。
            _nextTellTimer = Random.Range(0.6f, GameConfig.StartSpawnInterval);
        }

        public void ResetState()
        {
            _frozen = false;
            _state = State.Wander;
            PickWanderTarget();
            SetSquat(0f);
            if (_renderer != null) _renderer.material.color = _baseColor;
        }

        public void Freeze() => _frozen = true;

        private void Update()
        {
            var gm = GameManager.Instance;
            if (_frozen || (gm != null && gm.GameOver)) return;

            switch (_state)
            {
                case State.Wander:   TickWander(gm);   break;
                case State.Tell:     TickTell();       break;
                case State.Cooldown: TickCooldown();   break;
            }
        }

        // --- Wander --------------------------------------------------------

        private void TickWander(GameManager gm)
        {
            MoveTowards(_wanderTarget, _speed);

            Vector3 flat = transform.position; flat.y = 0;
            if ((flat - _wanderTarget).sqrMagnitude < 0.25f)
                PickWanderTarget();

            _nextTellTimer -= Time.deltaTime;

            // 群れ犬は、誰かが予兆を始めると自分も乗りやすい。
            bool packTrigger = Personality == DogPersonality.Pack && s_someoneTelling && Random.value < 0.03f;

            if (_nextTellTimer <= 0f || packTrigger)
                BeginTell(gm);
        }

        private void PickWanderTarget()
        {
            float e = GameConfig.FieldHalfExtent - 0.5f;
            _wanderTarget = new Vector3(Random.Range(-e, e), 0f, Random.Range(-e, e));
        }

        // --- Tell（予兆）---------------------------------------------------

        private void BeginTell(GameManager gm)
        {
            // ダッシュ犬は予兆の前に別の場所へ走ってからしゃがむ。
            if (Personality == DogPersonality.Dash)
                PickWanderTarget();

            // 本物率：難易度が上がるほどフェイントが増える。
            float difficulty = gm != null ? gm.Difficulty : 0f;
            float realChance = Personality switch
            {
                DogPersonality.Lazy => 0.8f,
                DogPersonality.Mischief => 0.35f,
                DogPersonality.Pack => 0.5f,
                _ => 0.6f
            };
            realChance -= difficulty * 0.2f; // 後半ほど騙してくる
            _isReal = Random.value < Mathf.Clamp01(realChance);

            _state = State.Tell;
            _stateTimer = Personality == DogPersonality.Lazy ? 0.5f : Random.Range(0.7f, 1.2f);
            s_someoneTelling = true;

            if (_renderer != null)
                _renderer.material.color = Color.Lerp(_baseColor, new Color(1f, 0.6f, 0.2f), 0.4f);
        }

        private void TickTell()
        {
            _stateTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(_stateTimer); // 0→1 でしゃがみ込む

            // プルプル揺れ＋しゃがみ。フレーム数で揺らす（Random/時刻に依存しすぎない）。
            float jitter = Mathf.Sin(Time.time * 40f) * 0.05f;
            SetSquat(Mathf.Min(0.5f, t * 0.5f) + Mathf.Abs(jitter));

            if (_stateTimer <= 0f)
            {
                s_someoneTelling = false;
                if (_renderer != null) _renderer.material.color = _baseColor;

                if (_isReal)
                {
                    SpawnPoop();
                    _state = State.Cooldown;
                    _stateTimer = Random.Range(1.2f, 2.0f);
                }
                else
                {
                    // フェイント：何食わぬ顔で歩き出す。
                    SetSquat(0f);
                    ScheduleNextTell();
                    _state = State.Wander;
                }
            }
        }

        private void TickCooldown()
        {
            SetSquat(Mathf.Lerp(GetSquat(), 0f, 10f * Time.deltaTime));
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
            {
                SetSquat(0f);
                ScheduleNextTell();
                _state = State.Wander;
            }
        }

        private void ScheduleNextTell()
        {
            float d = GameManager.Instance != null ? GameManager.Instance.Difficulty : 0f;
            float interval = Mathf.Lerp(GameConfig.StartSpawnInterval, GameConfig.MinSpawnInterval, d);
            if (Personality == DogPersonality.Mischief) interval *= 0.7f; // よく仕掛ける
            if (Personality == DogPersonality.Lazy) interval *= 1.5f;
            _nextTellTimer = interval * Random.Range(0.7f, 1.3f);
        }

        // --- 共通 ----------------------------------------------------------

        private void SpawnPoop()
        {
            // お尻側（進行方向の後ろ）に落とす。
            Vector3 behind = _body != null ? -_body.forward : Vector3.back;
            Vector3 pos = transform.position + behind * 0.6f;
            pos.y = 0f;
            Poop.Spawn(pos);
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            Vector3 flat = transform.position; flat.y = 0f;
            Vector3 to = target - flat;
            if (to.sqrMagnitude < 0.0004f) return;
            transform.position += to.normalized * speed * Time.deltaTime;
            if (_body != null)
                _body.forward = Vector3.Slerp(_body.forward, to.normalized, 8f * Time.deltaTime);
        }

        private void SetSquat(float amount)
        {
            if (_body == null) return;
            var s = _body.localScale;
            s.y = _baseScaleY * (1f - Mathf.Clamp(amount, 0f, 0.6f));
            _body.localScale = s;
        }

        private float GetSquat()
        {
            if (_body == null) return 0f;
            return 1f - (_body.localScale.y / _baseScaleY);
        }
    }
}
