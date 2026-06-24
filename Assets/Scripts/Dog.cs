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
    /// 見た目は ithappy の Dog_001 プレハブ（Animator付き）を使い、
    /// 移動は本スクリプトが transform で行い、歩行/待機は Animator の "Vert"
    /// （移動速度）で再生する。本物の予兆はしゃがみ＋プルプル＋色変化で表現。
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
        private bool _isReal;
        private float _baseScaleY;
        private Transform _body;          // 見た目モデル（向き回転・しゃがみ用）
        private Animator _anim;
        private Renderer _renderer;       // 予兆の色変化用（SkinnedMeshRenderer）
        private Color _baseColor;
        private bool _hasColor;
        private bool _frozen;
        private float _animVert;

        private static readonly int VertHash = Animator.StringToHash("Vert");
        private static bool s_someoneTelling;

        public void Init(DogPersonality personality, Transform body, Animator anim, Renderer rend)
        {
            Personality = personality;
            _body = body;
            _anim = anim;
            _renderer = rend;
            _baseScaleY = body.localScale.y;

            _hasColor = rend != null && rend.material.HasProperty("_Color");
            _baseColor = _hasColor ? _renderer.material.color : Color.white;

            _speed = personality == DogPersonality.Lazy ? 1.6f
                   : personality == DogPersonality.Dash ? 4.2f
                   : 2.6f;

            ResetState();
            _nextTellTimer = Random.Range(0.6f, GameConfig.StartSpawnInterval);
        }

        public void ResetState()
        {
            _frozen = false;
            _state = State.Wander;
            PickWanderTarget();
            SetSquat(0f);
            Tint(_baseColor);
            SetVert(0f, true);
        }

        public void Freeze() => _frozen = true;

        private void Update()
        {
            var gm = GameManager.Instance;
            if (_frozen || (gm != null && gm.GameOver))
            {
                SetVert(0f, false);
                return;
            }

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
            Vector3 flat = transform.position; flat.y = 0f;
            float dist = Vector3.Distance(flat, _wanderTarget);

            if (dist < 0.5f)
            {
                PickWanderTarget();
                SetVert(0f, false);
            }
            else
            {
                MoveTowards(_wanderTarget, _speed);
                SetVert(_speed, false);
            }

            _nextTellTimer -= Time.deltaTime;
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
            if (Personality == DogPersonality.Dash)
                PickWanderTarget();

            float difficulty = gm != null ? gm.Difficulty : 0f;
            float realChance = Personality switch
            {
                DogPersonality.Lazy => 0.8f,
                DogPersonality.Mischief => 0.35f,
                DogPersonality.Pack => 0.5f,
                _ => 0.6f
            };
            realChance -= difficulty * 0.2f;
            _isReal = Random.value < Mathf.Clamp01(realChance);

            _state = State.Tell;
            _stateTimer = Personality == DogPersonality.Lazy ? 0.5f : Random.Range(0.7f, 1.2f);
            s_someoneTelling = true;
            SetVert(0f, true);

            Tint(Color.Lerp(_baseColor, new Color(1f, 0.55f, 0.15f), 0.6f));
        }

        private void TickTell()
        {
            SetVert(0f, false);
            _stateTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(_stateTimer);

            float jitter = Mathf.Sin(Time.time * 40f) * 0.05f;
            SetSquat(Mathf.Min(0.5f, t * 0.5f) + Mathf.Abs(jitter));

            if (_stateTimer <= 0f)
            {
                s_someoneTelling = false;
                Tint(_baseColor);

                if (_isReal)
                {
                    SpawnPoop();
                    _state = State.Cooldown;
                    _stateTimer = Random.Range(1.2f, 2.0f);
                }
                else
                {
                    SetSquat(0f);
                    ScheduleNextTell();
                    _state = State.Wander;
                }
            }
        }

        private void TickCooldown()
        {
            SetVert(0f, false);
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
            if (Personality == DogPersonality.Mischief) interval *= 0.7f;
            if (Personality == DogPersonality.Lazy) interval *= 1.5f;
            _nextTellTimer = interval * Random.Range(0.7f, 1.3f);
        }

        // --- 共通 ----------------------------------------------------------

        private void SpawnPoop()
        {
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

        private void SetVert(float target, bool instant)
        {
            _animVert = instant ? target : Mathf.Lerp(_animVert, target, 10f * Time.deltaTime);
            if (_anim != null) _anim.SetFloat(VertHash, _animVert);
        }

        private void SetSquat(float amount)
        {
            if (_body == null) return;
            var s = _body.localScale;
            float baseX = _baseScaleY; // 等方スケール前提（Bootstrap で uniform 設定）
            s.y = baseX * (1f - Mathf.Clamp(amount, 0f, 0.6f));
            _body.localScale = s;
        }

        private float GetSquat()
        {
            if (_body == null) return 0f;
            return 1f - (_body.localScale.y / _baseScaleY);
        }

        private void Tint(Color c)
        {
            if (_hasColor && _renderer != null) _renderer.material.color = c;
        }
    }
}
