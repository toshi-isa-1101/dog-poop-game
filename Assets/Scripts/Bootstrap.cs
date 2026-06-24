using UnityEngine;

namespace PoopPanic
{
    /// <summary>
    /// 実行時にシーン一式（カメラ・ライト・地面・プレイヤー・犬・GameManager）を生成する。
    /// 犬モデル・テクスチャ・地面/環境プレハブのアセット参照は BuildTool がシーン生成時に割り当てる。
    ///
    /// 注意: ithappy の犬マテリアルは URP 用シェーダーのため、Built-in パイプラインの本プロジェクト
    /// ではマゼンタになる。そこで犬は実行時に Standard マテリアル＋犬テクスチャへ差し替える。
    /// SimpleNaturePack のプロップ/地面は Standard なのでそのまま使える。
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [Header("Assets (BuildTool がシーン生成時に割り当て)")]
        public GameObject dogPrefab;        // ithappy Dog_001
        public Texture dogTexture;          // 犬の体テクスチャ（URPマテリアル回避用）
        public GameObject groundTilePrefab;  // SimpleNaturePack Ground_01
        public float groundTileSize = 4f;    // 上記プレハブの1枚あたりの footprint（BuildToolが測定）
        public GameObject[] envProps;        // 木・茂み・岩・草 など

        private GameManager _gm;
        private Camera _cam;
        private float _dogCheckTimer;
        private static Shader s_standard;

        private static Shader Standard => s_standard != null ? s_standard : (s_standard = Shader.Find("Standard"));

        private void Start()
        {
            BuildEnvironment();
            _cam = BuildCamera();
            _gm = gameObject.AddComponent<GameManager>();

            BuildPlayer();

            for (int i = 0; i < GameConfig.StartDogCount; i++)
                SpawnDog(i);
        }

        private void Update()
        {
            _dogCheckTimer -= Time.deltaTime;
            if (_dogCheckTimer <= 0f && _gm != null && !_gm.GameOver)
            {
                _dogCheckTimer = 3f;
                if (_gm.RegisteredDogCount < _gm.DesiredDogCount())
                    SpawnDog(_gm.RegisteredDogCount);
            }
        }

        // --- 環境 ----------------------------------------------------------

        private void BuildEnvironment()
        {
            RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.62f);

            BuildGround();

            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

            ScatterProps();
        }

        private void BuildGround()
        {
            float cover = GameConfig.FieldHalfExtent + 2f;

            // 下地の緑プレーン（タイルの隙間から空が見えないように）。
            var backing = GameObject.CreatePrimitive(PrimitiveType.Plane);
            backing.name = "GroundBacking";
            Destroy(backing.GetComponent<Collider>());
            float bscale = (cover * 2f) / 10f;
            backing.transform.localScale = new Vector3(bscale, 1f, bscale);
            backing.transform.position = new Vector3(0, -0.02f, 0);
            backing.GetComponent<Renderer>().material.color = new Color(0.40f, 0.60f, 0.30f);

            if (groundTilePrefab == null || groundTileSize < 0.1f) return;

            // Ground プレハブ（Standard・正しいUV）を敷き詰める。タイル数は上限を設けて
            // 過剰生成を防ぎつつ、必要ならタイルを拡大してフィールド全体を覆う。
            int n = Mathf.Clamp(Mathf.RoundToInt((cover * 2f) / groundTileSize), 1, 12);
            float step = (cover * 2f) / n;
            float scale = step / groundTileSize;
            float start = -cover + step * 0.5f;

            var parent = new GameObject("GroundTiles").transform;
            for (int ix = 0; ix < n; ix++)
            {
                for (int iz = 0; iz < n; iz++)
                {
                    var pos = new Vector3(start + ix * step, 0f, start + iz * step);
                    var tile = Instantiate(groundTilePrefab, pos, Quaternion.identity, parent);
                    tile.transform.localScale *= scale;
                    StripColliders(tile);
                }
            }
        }

        private void ScatterProps()
        {
            if (envProps == null || envProps.Length == 0) return;

            int count = 22;
            for (int i = 0; i < count; i++)
            {
                var prefab = envProps[Random.Range(0, envProps.Length)];
                if (prefab == null) continue;

                float ang = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
                float radius = GameConfig.FieldHalfExtent + Random.Range(0.5f, 4.5f);
                Vector3 pos = new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);

                var inst = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                inst.transform.localScale *= Random.Range(0.8f, 1.4f);
                StripColliders(inst);
            }
        }

        private static void StripColliders(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                Destroy(c);
        }

        private Camera BuildCamera()
        {
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f);
            camGo.transform.position = new Vector3(0f, 15f, -10f);
            camGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            cam.fieldOfView = 55f;
            return cam;
        }

        // --- プレイヤー ----------------------------------------------------

        private void BuildPlayer()
        {
            var root = new GameObject("Player");
            root.transform.position = Vector3.zero;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            body.transform.localPosition = new Vector3(0, 0.8f, 0);
            body.GetComponent<Renderer>().material.color = new Color(0.2f, 0.5f, 0.95f);

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(nose.GetComponent<Collider>());
            nose.transform.SetParent(body.transform, false);
            nose.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            nose.transform.localPosition = new Vector3(0, 0.2f, 0.6f);
            nose.GetComponent<Renderer>().material.color = new Color(0.95f, 0.9f, 0.3f);

            var player = root.AddComponent<Player>();
            player.Init(_cam, body.transform);
            _gm.RegisterPlayer(player);
        }

        // --- 犬 ------------------------------------------------------------

        private void SpawnDog(int index)
        {
            DogPersonality[] roster =
            {
                DogPersonality.Lazy, DogPersonality.Mischief, DogPersonality.Dash,
                DogPersonality.Pack, DogPersonality.Normal, DogPersonality.Mischief
            };
            DogPersonality personality = roster[index % roster.Length];

            float[] sizes = { 0.7f, 1.0f, 1.4f };
            float size = sizes[index % sizes.Length];

            var root = new GameObject($"Dog_{personality}_{index}");
            float e = GameConfig.FieldHalfExtent - 1f;
            root.transform.position = new Vector3(Random.Range(-e, e), 0f, Random.Range(-e, e));

            Transform visual;
            Animator anim = null;
            Renderer rend;

            if (dogPrefab != null)
            {
                var model = Instantiate(dogPrefab);
                model.name = "DogModel";

                // 付属のサンプル AI/入力スクリプトと CharacterController を除去（Animator は残す）。
                foreach (var mb in model.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    mb.enabled = false;
                    Destroy(mb);
                }
                var cc = model.GetComponent<CharacterController>();
                if (cc != null) Destroy(cc);

                anim = model.GetComponentInChildren<Animator>();
                rend = model.GetComponentInChildren<SkinnedMeshRenderer>();

                // URPマテリアル→Standardへ差し替え（マゼンタ回避）。
                FixDogMaterials(model);

                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = Vector3.one * size;
                visual = model.transform;
            }
            else
            {
                rend = BuildPrimitiveDog(root.transform, size, out visual);
            }

            var dog = root.AddComponent<Dog>();
            dog.Init(personality, visual, anim, rend);
            _gm.RegisterDog(dog);
        }

        /// <summary>犬モデルの全レンダラーを Standard マテリアル＋犬テクスチャに置き換える。</summary>
        private void FixDogMaterials(GameObject model)
        {
            if (Standard == null) return;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = new Material(Standard) { color = Color.white };
                    if (dogTexture != null) m.mainTexture = dogTexture;
                    mats[i] = m;
                }
                r.materials = mats;
            }
        }

        private Renderer BuildPrimitiveDog(Transform parent, float size, out Transform visual)
        {
            var body = new GameObject("Body");
            body.transform.SetParent(parent, false);
            body.transform.localScale = Vector3.one * size;
            visual = body.transform;

            var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(torso.GetComponent<Collider>());
            torso.transform.SetParent(body.transform, false);
            torso.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            torso.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
            torso.transform.localPosition = new Vector3(0, 0.5f, 0);
            var rend = torso.GetComponent<Renderer>();
            rend.material.color = new Color(0.80f, 0.60f, 0.35f);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(body.transform, false);
            head.transform.localScale = Vector3.one * 0.5f;
            head.transform.localPosition = new Vector3(0, 0.6f, 0.6f);
            head.GetComponent<Renderer>().material.color = rend.material.color;

            return rend;
        }
    }
}
