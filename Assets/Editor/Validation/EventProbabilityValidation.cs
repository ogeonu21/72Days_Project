using System;
using UnityEditor;
using UnityEngine;

public static class EventProbabilityValidation
{
    [MenuItem("Tools/Validation/Event Probability")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        int count = 0, rolls = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); count++; };
        var option = new EventOption { id = "test", text = "시도" };
        Func<float> roll = () => { rolls++; return .5f; };
        check(option.RollSuccess(roll) && rolls == 0, "100% 추첨 생략");
        option.successProbability = 0;
        check(!option.RollSuccess(roll) && rolls == 0, "0% 추첨 생략");
        option.successProbability = .5f;
        check(!option.RollSuccess(roll) && rolls == 1, "경계값 실패와 단일 추첨");
        check(option.RollSuccess(() => .499f), "중간 확률 성공");
        var definition = ScriptableObject.CreateInstance<EventDefinition>();
        var exit = ScriptableObject.CreateInstance<StoryNode>();
        definition.exitNode = exit; definition.options.Add(option);
        try
        {
            check(EventDefinitionValidator.Validate(definition) != null, "실패 문구 필수");
            option.failureText = "실패하였다.";
            check(EventDefinitionValidator.Validate(definition) == null, "정상 확률 설정");
            option.successProbability = 1; option.failureText = null;
            check(EventDefinitionValidator.Validate(definition) == null, "100% 실패 문구 불필요");
            foreach (float invalid in new[] { -1f, 1.1f, float.NaN, float.PositiveInfinity })
            {
                option.successProbability = invalid;
                check(EventDefinitionValidator.Validate(definition) != null, "잘못된 확률 거부");
            }
            var parse = typeof(DataImporter).GetMethod("ParseEventProbability", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            check((float)parse.Invoke(null, new object[] { "", "test" }) == 1, "빈 셀 기본 1");
            check((float)parse.Invoke(null, new object[] { "0", "test" }) == 0, "0 보존");
            check((float)parse.Invoke(null, new object[] { "0.5", "test" }) == .5f, "소수 확률 파싱");
            return "Event probability: " + count + " passed";
        }
        finally { UnityEngine.Object.DestroyImmediate(definition); UnityEngine.Object.DestroyImmediate(exit); }
    }
}
