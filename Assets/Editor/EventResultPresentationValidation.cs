using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class EventResultPresentationValidation
{
    [MenuItem("Tools/Validation/Event Result Presentation")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        int passed = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); passed++; };
        var root = new GameObject("ResultLogTest", typeof(RectTransform), typeof(EventResultLog));
        var template = new GameObject("Template", typeof(RectTransform), typeof(TextMeshProUGUI));
        template.transform.SetParent(root.transform, false);
        template.SetActive(false);
        var exit = ScriptableObject.CreateInstance<StoryNode>();
        try
        {
            var log = root.GetComponent<EventResultLog>();
            var serialized = new SerializedObject(log);
            serialized.FindProperty("lineTemplate").objectReferenceValue = template.GetComponent<TMP_Text>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Func<TMP_Text[]> lines = () => root.GetComponentsInChildren<TMP_Text>(true).Where(t => t.gameObject != template).ToArray();
            log.Enqueue("골드 +10\r\n\n체력 +5");
            log.Tick(0); check(lines().Length == 1, "첫 줄 즉시 출력");
            check(lines()[0].text == "골드 +10" && lines()[0].fontSize == 36 && lines()[0].color == Color.white, "로그 내용과 스타일");
            check(!lines()[0].raycastTarget, "로그가 클릭을 차단하지 않음");
            log.Tick(.19f); check(lines().Length == 1, "0.2초 이전 추가 출력 없음");
            log.Tick(.2f); check(lines().Length == 2 && lines()[1].text == "체력 +5", "0.2초 간격과 빈 줄 제거");
            check(lines()[0].rectTransform.anchoredPosition.y > lines()[1].rectTransform.anchoredPosition.y, "기존 로그 위로 배치");
            log.Tick(1.5f); check(Mathf.Approximately(lines()[0].alpha, .5f), "점진적 투명도");
            log.Tick(3f); check(lines().Length == 1, "첫 줄 3초 후 제거");
            log.Tick(3.21f); check(lines().Length == 0, "줄마다 개별 수명");
            log.Enqueue("미출력 로그");
            // Edit Mode에서는 일반 MonoBehaviour의 disable 콜백을 직접 호출한다.
            typeof(EventResultLog).GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(log, null);
            log.Tick(4);
            check(lines().Length == 0, "비활성화 시 대기열 정리");
            var definitions = DataImporter.BuildEventDefinitions(
                Newtonsoft.Json.JsonConvert.SerializeObject(new[] { new EventDataRaw { EventID = "ResultTest", Kind = "Reward", Title = "검사", ExitNodeID = "exit" } }),
                Newtonsoft.Json.JsonConvert.SerializeObject(new[] { new EventChoiceDataRaw { EventID = "ResultTest", ChoiceID = "test", Text = "선택", Action = "None", ResultText = @"도왔다.\n그는 고마워했다." } }),
                "[]", id => null, id => exit);
            try { check(definitions["ResultTest"].options[0].resultText == "도왔다.\n그는 고마워했다.", "ResultText 매핑과 줄바꿈"); }
            finally { foreach (var definition in definitions.Values) UnityEngine.Object.DestroyImmediate(definition); }
            check(SheetJson.Multiline(null) == "", "기존 빈 결과 데이터 호환");
            return "Event result presentation: " + passed + " passed";
        }
        finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(exit); }
    }
}
