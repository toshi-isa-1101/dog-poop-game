using System.Collections.Generic;
using UnityEngine;

namespace PoopPanic
{
    /// <summary>
    /// スコア・ミス・難易度・ゲームオーバーを統括するシングルトン。
    /// 犬とウンチはここに結果を報告する。HUD は OnGUI で描画（フォントアセット不要で堅牢）。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int Misses { get; private set; }
        public bool GameOver { get; private set; }
        public float Elapsed { get; private set; }

        private readonly List<Dog> _dogs = new List<Dog>();
        private Player _player;
        public Transform PlayerTransform => _player != null ? _player.transform : null;

        // 0(開始)→1(最難度) に近づく難易度係数。
        public float Difficulty => Mathf.Clamp01(Elapsed / GameConfig.DifficultyRampSeconds);

        private GUIStyle _hud;
        private GUIStyle _big;
        private GUIStyle _btn;

        private void Awake()
        {
            Instance = this;
        }

        public void RegisterPlayer(Player player) => _player = player;
        public void RegisterDog(Dog dog) => _dogs.Add(dog);

        private void Update()
        {
            if (GameOver) return;
            Elapsed += Time.deltaTime;
        }

        /// <summary>難易度に応じて目標の犬の数を返す（Bootstrap が追加生成に使う）。</summary>
        public int DesiredDogCount()
        {
            int extra = Mathf.RoundToInt(Difficulty * (GameConfig.MaxDogCount - GameConfig.StartDogCount));
            return Mathf.Clamp(GameConfig.StartDogCount + extra, GameConfig.StartDogCount, GameConfig.MaxDogCount);
        }

        public int RegisteredDogCount => _dogs.Count;

        // --- ウンチからの報告 ----------------------------------------------

        public void OnPoopCollected(bool freshCatch)
        {
            if (GameOver) return;
            Combo++;
            int baseScore = freshCatch ? GameConfig.ScoreFresh : GameConfig.ScoreLanded;
            int comboBonus = (Combo - 1) * GameConfig.ComboStep;
            Score += baseScore + comboBonus;
        }

        public void OnPoopMissed()
        {
            if (GameOver) return;
            Combo = 0;
            Misses++;
            if (Misses >= GameConfig.MaxMisses)
            {
                GameOver = true;
                foreach (var dog in _dogs)
                    if (dog != null) dog.Freeze();
            }
        }

        // --- HUD -----------------------------------------------------------

        private void EnsureStyles()
        {
            if (_hud != null) return;
            _hud = new GUIStyle { fontSize = 22, fontStyle = FontStyle.Bold };
            _hud.normal.textColor = Color.white;
            _big = new GUIStyle { fontSize = 52, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _big.normal.textColor = Color.white;
            _btn = new GUIStyle(GUI.skin != null ? GUI.skin.button : new GUIStyle()) { fontSize = 24 };
        }

        private void OnGUI()
        {
            EnsureStyles();

            // 影付き風に2回描いて視認性を上げる。
            DrawLabel(new Rect(20, 16, 600, 30), $"Clean Score : {Score}");
            DrawLabel(new Rect(20, 48, 600, 30), $"Combo : {Combo}");

            string lives = "";
            for (int i = 0; i < GameConfig.MaxMisses; i++)
                lives += i < (GameConfig.MaxMisses - Misses) ? "★ " : "☆ ";
            DrawLabel(new Rect(20, 80, 600, 30), $"Miss   : {lives}");

            DrawLabel(new Rect(Screen.width - 220, 16, 200, 30), $"Time : {Elapsed:0.0}s");

            if (GameOver)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;

                GUI.Label(new Rect(0, Screen.height * 0.32f, Screen.width, 70), "GAME OVER", _big);
                _big.fontSize = 30;
                GUI.Label(new Rect(0, Screen.height * 0.32f + 80, Screen.width, 50),
                    $"Clean Score : {Score}", _big);
                _big.fontSize = 52;

                var r = new Rect(Screen.width / 2f - 110, Screen.height * 0.6f, 220, 56);
                if (GUI.Button(r, "もう一度あそぶ", _btn))
                    Restart();
            }
        }

        private void DrawLabel(Rect r, string text)
        {
            var shadow = r;
            shadow.x += 2; shadow.y += 2;
            Color c = _hud.normal.textColor;
            _hud.normal.textColor = new Color(0, 0, 0, 0.5f);
            GUI.Label(shadow, text, _hud);
            _hud.normal.textColor = c;
            GUI.Label(r, text, _hud);
        }

        private void Restart()
        {
            Score = 0; Combo = 0; Misses = 0; Elapsed = 0; GameOver = false;

            foreach (var poop in Object.FindObjectsByType<Poop>(FindObjectsSortMode.None))
                Destroy(poop.gameObject);

            foreach (var dog in _dogs)
                if (dog != null) dog.ResetState();

            if (_player != null)
                _player.ResetState();
        }
    }
}
