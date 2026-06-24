using UnityEngine;

namespace PoopPanic
{
    /// <summary>
    /// 1か所にまとめたゲーム全体のチューニング値。
    /// MVP仕様（公園1ステージ / 犬3匹 / 3ミスで終了）の数値はここを触れば調整できる。
    /// </summary>
    public static class GameConfig
    {
        // フィールド（地面）の半径。プレイヤーも犬もこの範囲内で動く。
        public const float FieldHalfExtent = 8f;

        // ゲームルール
        public const int MaxMisses = 3;

        // スコア
        public const int ScoreLanded = 100;   // 着地後に回収
        public const int ScoreFresh = 200;    // 落ちる前（空中）にキャッチ
        public const int ComboStep = 50;      // 連続成功ボーナスの増分

        // プレイヤー
        public const float PlayerSpeed = 7.5f;
        public const float CollectRadius = 1.6f;

        // ウンチ
        public const float PoopFallTime = 0.85f; // 生成位置から地面まで落ちる時間
        public const float PoopLifetime = 4.0f;   // 着地後、放置でミスになるまでの猶予

        // 難易度カーブ（時間で上がる）
        public const float StartSpawnInterval = 2.6f; // 犬が「予兆」を始める平均間隔
        public const float MinSpawnInterval = 0.9f;
        public const float DifficultyRampSeconds = 90f; // この秒数で最難度へ近づく
        public const int StartDogCount = 3;
        public const int MaxDogCount = 6;
    }
}
