using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class DialoguePaginationValidation
{
    public static string PlayResult { get; private set; } = "검사 전";
    public static void StartPlayChecks()
    {
        if(!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode에서 실행하세요.");
        PlayResult="진행 중";
        var host=new GameObject("DialoguePaginationPlayCheck",typeof(UIController));
        host.GetComponent<UIController>().StartCoroutine(PlayChecks(host));
    }
    private static IEnumerator PlayChecks(GameObject host)
    {
        var canvas=new GameObject("PaginationPlayCanvas",typeof(RectTransform),typeof(Canvas));
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var root=new GameObject("Body",typeof(RectTransform),typeof(TextMeshProUGUI)); root.transform.SetParent(canvas.transform,false);
        var text=root.GetComponent<TextMeshProUGUI>();
        text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/DungGeunMo SDF.asset");
        text.fontSize=32; text.enableAutoSizing=false; text.rectTransform.sizeDelta=new Vector2(320,80);
        string source="\"첫 문장입니다. 두 번째 문장입니다.\" [세 번째 문장입니다.] 마지막 문장입니다.";
        var pages=TypewriterEffect.BuildPages(text,TypewriterEffect.FormatSentences(source));
        var routine=TypewriterEffect.TypeTextCoroutine(text,source,.03f);
        float previousScale=Time.timeScale; Time.timeScale=1;
        int waits=0;
        string previousText=string.Empty;
        try
        {
            while(routine.MoveNext())
            {
                var wait=routine.Current as WaitForSeconds;
                bool pageWait=wait!=null && Mathf.Abs((float)typeof(WaitForSeconds).GetField("m_Seconds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(wait)-TypewriterEffect.PagePauseSeconds)<.0001f;
                if(!pageWait && text.text.Length!=1 && !(text.text.StartsWith(previousText,StringComparison.Ordinal) && text.text.Length==previousText.Length+1))
                { PlayResult="실패: 한 글자 타이핑"; yield break; }
                previousText=text.text;
                float before=Time.realtimeSinceStartup;
                yield return routine.Current;
                if(pageWait) {
                    if(Time.realtimeSinceStartup-before<.65f) {PlayResult="실패: 페이지 대기 시간";yield break;}
                    waits++;
                }
            }
            PlayResult=pages.Count>1 && waits==pages.Count-1 && text.text==pages.Last() ? "Play Mode 통과: 한 글자 타이핑/묶음 분리/자동 페이지 전환/실제0.7초 대기/최종 본문" : "실패: 자동 페이지 출력";
        }
        finally { (routine as IDisposable)?.Dispose(); Time.timeScale=previousScale; UnityEngine.Object.Destroy(canvas); UnityEngine.Object.Destroy(host); }
    }
    [MenuItem("Tools/Validation/Dialogue Pagination")]
    public static void RunMenu() => Debug.Log(RunChecks());
    public static string RunChecks()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode 검사");
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new InvalidOperationException(name);passed++;};
        check(TypewriterEffect.FormatSentences("첫 문장. 둘째 문장.")=="첫 문장.\n둘째 문장.","마침표 유지/문장 개행");
        check(TypewriterEffect.FormatSentences("첫.\n\n 둘째")=="첫.\n둘째","마침표 뒤 중복 개행 정리");
        check(TypewriterEffect.FormatSentences("말줄임... 다음")=="말줄임...\n다음","연속 마침표 유지");
        check(TypewriterEffect.FormatSentences(null)=="","null 본문");
        check(TypewriterEffect.FormatSentences("끝에 마침표 없음")=="끝에 마침표 없음","구분자 없는 본문");
        check(TypewriterEffect.FormatSentences("<link=\"a.b\">첫.</link> 다음.")=="<link=\"a.b\">첫.</link>\n다음.","태그 속성 구분자 보호");
        check(TypewriterEffect.FormatSentences("<b>끝.</b>")=="<b>끝.</b>","닫는 태그 뒤 빈 문장 방지");
        check(TypewriterEffect.FormatSentences("\"첫. 둘.\" 다음.")=="\"첫. 둘.\"\n다음.","따옴표 내부 마침표 보호");
        check(TypewriterEffect.FormatSentences("[첫. 둘.] 다음.")=="[첫. 둘.]\n다음.","대괄호 묶음 우선");
        check(TypewriterEffect.FormatSentences("서술 \"대사.\" 뒤.")=="서술\n\"대사.\"\n뒤.","묶음 앞뒤 분리");
        check(TypewriterEffect.FormatSentences("[첫 \"둘. 셋.\" 넷.] 끝.")=="[첫 \"둘. 셋.\" 넷.]\n끝.","중첩 묶음 유지");
        check(TypewriterEffect.FormatSentences("“첫. 둘.” (셋. 넷.)")=="“첫. 둘.”\n(셋. 넷.)","곡선 따옴표 및 소괄호");
        check(TypewriterEffect.FormatSentences("\"말\". 다음.")=="\"말\".\n다음.","닫는 기호 뒤 마침표 유지");
        check(TypewriterEffect.FormatSentences("don't split. 다음.")=="don't split.\n다음.","단어 안 작은따옴표 유지");
        check(TypewriterEffect.FormatSentences("[닫힘 없음. 계속.")=="[닫힘 없음. 계속.","닫히지 않은 묶음의 글자 보존");
        var canvas=new GameObject("DialoguePaginationCanvas",typeof(RectTransform),typeof(Canvas));
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var root=new GameObject("DialoguePaginationCheck",typeof(RectTransform),typeof(TextMeshProUGUI));
        root.transform.SetParent(canvas.transform,false);
        try
        {
            var text=root.GetComponent<TextMeshProUGUI>();
            text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/DungGeunMo SDF.asset");
            if(text.font==null) throw new InvalidOperationException("검사 한글 폰트 없음");
            text.fontSize=32; text.enableAutoSizing=false; text.rectTransform.sizeDelta=new Vector2(400,100); text.enableWordWrapping=true;
            text.margin=new Vector4(5,5,5,5); text.text="기존";
            int originalVisible=text.maxVisibleCharacters;
            var letters=new System.Collections.Generic.List<string>();
            var typing=TypewriterEffect.TypeTextCoroutine(text,"한글 출력");
            while(typing.MoveNext()) letters.Add(text.text);
            check(letters.SequenceEqual(new[]{"한","한글","한글 ","한글 출","한글 출력"}),"어절 아닌 한 글자씩 문자열 증가");
            letters.Clear();typing=TypewriterEffect.TypeTextCoroutine(text,"<b>한글</b>");
            while(typing.MoveNext()) letters.Add(text.text);
            check(letters.SequenceEqual(new[]{"<b>한</b>","<b>한글</b>"}),"리치 텍스트를 유지한 한 글자 타이핑");
            letters.Clear();typing=TypewriterEffect.TypeTextCoroutine(text,"가😄나");
            while(typing.MoveNext()) letters.Add(text.text);
            check(letters.SequenceEqual(new[]{"가","가😄","가😄나"}),"이모지 UTF16 분리 방지");
            text.text="기존";
            string longText=string.Join(" ",Enumerable.Repeat("첫 문장입니다. 두번째 문장입니다.",12));
            string formatted=TypewriterEffect.FormatSentences(longText);
            var pages=TypewriterEffect.BuildPages(text,formatted);
            check(pages.Count>1,"실제 영역의 다중 페이지");
            check(text.text=="기존" && text.overflowMode==TextOverflowModes.Overflow,"측정 후 기존 상태 복원");
            check(string.Concat(pages).Replace("\n","")==formatted.Replace("\n",""),"페이지 경계에서 글자 손실 없음");
            int large=pages.Count;
            text.rectTransform.sizeDelta=new Vector2(400,60);
            check(TypewriterEffect.BuildPages(text,formatted).Count>=large,"작은 영역에서 페이지 증가");
            text.rectTransform.sizeDelta=new Vector2(400,100);
            check(TypewriterEffect.BuildPages(text,new string('가',300)).Count>1,"한 문장도 영역을 넘으면 계속 출력");
            var richPages=TypewriterEffect.BuildPages(text,"<color=#ff0000>"+formatted+"</color>");
            check(richPages.Count>1 && richPages.All(p=>p.StartsWith("<color=#ff0000>") && p.EndsWith("</color>")),"페이지별 색상 태그 보존");
            int pauses=0;
            var routine=TypewriterEffect.TypeTextCoroutine(text,longText,0.03f);
            int guard=0;
            while(routine.MoveNext())
            {
                if(++guard>10000) throw new InvalidOperationException("코루틴 종료 실패");
                if(routine.Current is WaitForSeconds wait) {
                    float seconds=(float)typeof(WaitForSeconds).GetField("m_Seconds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(wait);
                    if(Mathf.Abs(seconds-TypewriterEffect.PagePauseSeconds)<.0001f) pauses++;
                }
            }
            check(pauses==pages.Count-1,"중간 페이지마다 0.7초 대기");
            check(text.text==pages.Last(),"영역 초기화 후 마지막 페이지만 유지");
            check(text.maxVisibleCharacters==originalVisible,"표시 문자 제한 복원");
            var stopped=TypewriterEffect.TypeTextCoroutine(text,longText);
            stopped.MoveNext(); ((IDisposable)stopped).Dispose();
            check(text.maxVisibleCharacters==originalVisible,"취소 시 표시 상태 복원");
            var empty=TypewriterEffect.TypeTextCoroutine(text,null);while(empty.MoveNext()){}
            check(text.text=="","빈 본문 초기화");
        }
        finally { UnityEngine.Object.DestroyImmediate(canvas); }
        return "Dialogue Pagination Edit Mode 검사 "+passed+"개 통과";
    }
}
