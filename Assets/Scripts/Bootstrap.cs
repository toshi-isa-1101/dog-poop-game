using UnityEngine;

namespace PoopPanic
{
    /// <summary>
    /// 実行時にシーン一式（カメラ・ライト・地面・プレイヤー・犬・GameManager）を生成する。
    /// 使い方：空のシーンに空の GameObject を作り、このスクリプトを1つ付けて Play するだけ。
    /// プレハブや .meta/GUID を手書きしないので、Unity のバージョン差で壊れにくい。
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        private GameManager _gm;
        private Camera _cam;
        private float _dogCheckTimer;

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
            // 難易度上昇に合わせて犬を増やす。
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
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

            // 地面（公園の芝生）。Plane は10x10なので半径に合わせてスケール。
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            float scale = (GameConfig.FieldHalfExtent * 2f) / 10f + 0.2f;
            ground.transform.localScale = new Vector3(scale, 1f, scale);
            ground.GetComponent<Renderer>().material.color = new Color(0.42f, 0.65f, 0.32f);

            // ライト
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        }

        private Camera BuildCamera()
        {
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f); // 空色
            // 斜め見下ろし（約55度）で立体感を出す。
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

            // 向きが分かるよう、前にバイザー（小さい箱）を付ける。
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
            // 性格を巡回で割り当て（最初の3匹に必ず小・中・大の差を出す）。
            DogPersonality[] roster =
            {
                DogPersonality.Lazy, DogPersonality.Mischief, DogPersonality.Dash,
                DogPersonality.Pack, DogPersonality.Normal, DogPersonality.Mischief
            };
            DogPersonality personality = roster[index % roster.Length];

            // 小・中・大のサイズ差（設計書：小型犬は隠れ、大型犬は目立つ）。
            float[] sizes = { 0.7f, 1.0f, 1.4f };
            float size = sizes[index % sizes.Length];

            Color[] colors =
            {
                new Color(0.80f, 0.60f, 0.35f), // 茶
                new Color(0.25f, 0.22f, 0.20f), // 黒
                new Color(0.92f, 0.88f, 0.80f), // 白
                new Color(0.70f, 0.45f, 0.25f)  // こげ茶
            };
            Color color = colors[index % colors.Length];

            var root = new GameObject($"Dog_{personality}_{index}");
            float e = GameConfig.FieldHalfExtent - 1f;
            root.transform.position = new Vector3(Random.Range(-e, e), 0f, Random.Range(-e, e));

            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = Vector3.one * size;

            // 胴
            var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(torso.GetComponent<Collider>());
            torso.transform.SetParent(body.transform, false);
            torso.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // 横倒しで四足っぽく
            torso.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
            torso.transform.localPosition = new Vector3(0, 0.5f, 0);
            var rend = torso.GetComponent<Renderer>();
            rend.material.color = color;

            // 頭
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(body.transform, false);
            head.transform.localScale = Vector3.one * 0.5f;
            head.transform.localPosition = new Vector3(0, 0.6f, 0.6f);
            head.GetComponent<Renderer>().material.color = color;

            // 耳2つ
            for (int s = -1; s <= 1; s += 2)
            {
                var ear = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(ear.GetComponent<Collider>());
                ear.transform.SetParent(head.transform, false);
                ear.transform.localScale = new Vector3(0.3f, 0.5f, 0.15f);
                ear.transform.localPosition = new Vector3(0.35f * s, 0.5f, 0f);
                ear.GetComponent<Renderer>().material.color = color * 0.8f;
            }

            // しっぽ
            var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(tail.GetComponent<Collider>());
            tail.transform.SetParent(body.transform, false);
            tail.transform.localScale = new Vector3(0.15f, 0.15f, 0.5f);
            tail.transform.localPosition = new Vector3(0, 0.7f, -0.6f);
            tail.GetComponent<Renderer>().material.color = color * 0.8f;

            var dog = root.AddComponent<Dog>();
            dog.Init(personality, body.transform, rend, color);
            _gm.RegisterDog(dog);
        }
    }
}
