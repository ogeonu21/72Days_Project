using System;
using UnityEditor;
using UnityEngine;

/// <summary>실제 저장 파일과 씬 에셋을 쓰지 않는 캐릭터 회귀 검사.</summary>
public static class CharacterRefactorValidation
{
    [MenuItem("Tools/Validation/Character and Save Refactor")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        int passed = 0;
        Action<bool, string> check = (condition, label) =>
        {
            if (!condition) throw new InvalidOperationException("검증 실패: " + label);
            passed++;
        };
        var randomState = UnityEngine.Random.state;
        var playerObject = new GameObject("CharacterValidation_Player") { hideFlags = HideFlags.HideAndDontSave };
        var enemyObject = new GameObject("CharacterValidation_Enemy") { hideFlags = HideFlags.HideAndDontSave };
        var definition = ScriptableObject.CreateInstance<EnemyData>();
        var potion = ScriptableObject.CreateInstance<PotionItem>();
        try
        {
            var player = playerObject.AddComponent<Player>();
            player.InitializeFromData(new PlayerData { tendency = -4 });
            check(player.CurrentHP == player.MaxHP && player.tendency == -4, "새 플레이어 초기화");
            player.ChangeTendency(7);
            check(player.tendency == 3 && player.GetCurrentData().tendency == 3, "Player 성향 소유");
            player.TakeDamage(-5);
            check(player.CurrentHP == player.MaxHP, "음수 피해 거부");
            player.TakeDamage(9);
            int savedHP = player.CurrentHP;
            var snapshot = new SaveGameData { player = PlayerSaveData.FromPlayerData(player.GetCurrentData()) };
            string json = JsonUtility.ToJson(snapshot);
            check(SaveGameData.TryDeserialize(json, out var restored, out _), "현재 저장 버전 파싱");
            player.ResetTendency();
            player.LoadFromData(restored.player.ToPlayerData());
            check(player.tendency == 3 && player.CurrentHP == savedHP, "성향/HP 왕복");
            player.RestoreEquipment(new EquipmentData());
            check(player.CurrentHP == savedHP, "장비 복원 시 임의 회복 없음");
            check(SaveGameData.TryDeserialize("{\"version\":1,\"goodAndEvil\":-8,\"player\":{\"id\":\"Player\"}}", out restored, out _) && restored.player.tendency == -8 && restored.version == SaveGameData.CurrentVersion, "구 goodAndEvil 이전");
            check(SaveGameData.TryDeserialize("{\"version\":1.0,\"tendency\":5,\"player\":{\"id\":\"Player\"}}", out restored, out _) && restored.player.tendency == 5, "구 tendency 이전");
            check(!SaveGameData.TryDeserialize("{\"version\":9,\"player\":{}}", out _, out _), "미지원 버전 거부");
            check(!SaveGameData.TryDeserialize("{}", out _, out _), "버전 누락 거부");
            check(!SaveGameData.TryDeserialize("not json", out _, out _), "손상 JSON 거부");
            check(!SaveGameData.TryDeserialize("{\"version\":2,\"player\":null}", out _, out _), "플레이어 누락 거부");
            int deaths = 0;
            player.onDied += () => deaths++;
            player.TakeDamage(int.MaxValue);
            player.TakeDamage(1);
            check(player.CurrentHP == 0 && deaths == 1, "사망 통지 1회");
            player.InitializeFromData(new PlayerData());
            player.tuningStats.hpBonus = 100;
            player.UpdateStats();
            player.tuningStats.hpBonus = 0;
            player.UpdateStats();
            check(player.CurrentHP <= player.MaxHP, "최대 체력 감소 상한");

            var enemy = enemyObject.AddComponent<Enemy>();
            definition.id = "Validation";
            definition.tuningStats = new TuningStats(12, 20, .2f, 1);
            definition.dropItem = potion;
            definition.itemDropRate = .5f;
            UnityEngine.Random.InitState(42);
            enemy.InitializeFromData(definition);
            float initialHitRate = enemy.areaDataDB[0].hitRate;
            enemy.TakeEffect(new AreaData("팔", 1, 1, 1));
            check(enemy.tuningStats.attackBonus == 7, "적 보정치에 효과 가산");
            enemy.EffectReset();
            check(enemy.tuningStats.attackBonus == 12 && Mathf.Approximately(enemy.tuningStats.dodgeBonus, .2f), "적 원래 보정 복원");
            enemy.TakeEffect(new AreaData("팔", 1, 1, 1));
            definition.dropItem = null;
            UnityEngine.Random.InitState(42);
            enemy.InitializeFromData(definition);
            check(enemy.dropItem == null && enemy.itemDropRate == 0 && enemy.tuningStats.attackBonus == 12, "적 재사용 상태 초기화");
            check(Mathf.Approximately(initialHitRate, enemy.areaDataDB[0].hitRate), "부위 확률 누적 없음");
            string path = AssetDatabase.GUIDToAssetPath("de7a43d0e487ed54e8427f341b11fc4c");
            check(path.EndsWith("EnemyData.cs"), "스크립트 GUID 보존");
            string[] enemyGuids = AssetDatabase.FindAssets("t:EnemyData", new[] { "Assets/Resources/Characters" });
            check(enemyGuids.Length > 0, "적 에셋 발견");
            foreach (string guid in enemyGuids)
                check(AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(guid)) != null, "적 에셋 로드");
            return $"Character/Save 검사 {passed}개 통과";
        }
        finally
        {
            UnityEngine.Random.state = randomState;
            UnityEngine.Object.DestroyImmediate(playerObject);
            UnityEngine.Object.DestroyImmediate(enemyObject);
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(potion);
        }
    }
}
