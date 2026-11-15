using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LoadingSystemValidation
{
    [MenuItem("Tools/Validation/Loading System")]
    public static void RunMenu() => Debug.Log(RunChecks());
    public static string RunChecks()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode 전용 검사");
        int count=0;
        Action<bool,string> check=(value,name)=>{if(!value) throw new InvalidOperationException(name);count++;};
        Action<Action,string> reject=(action,name)=>{bool failed=false;try{action();}catch{failed=true;}check(failed,name);};
        check(RuntimeStoryCatalog.Compatible("v1","v1"),"일치 버전");
        check(!RuntimeStoryCatalog.Compatible(null,"v1"),"구 세이브 버전 없음 거부");
        check(!RuntimeStoryCatalog.Compatible("v1","v2"),"다른 버전 거부");
        check(LoadingSceneController.AllowedEndpoint("https://script.google.com/macros/s/TEST/exec"),"정상 배포 주소");
        check(!LoadingSceneController.AllowedEndpoint("http://script.google.com/macros/s/TEST/exec"),"HTTP 거부");
        check(!LoadingSceneController.AllowedEndpoint("https://script.google.com.evil.test/macros/s/TEST/exec"),"위조 호스트 거부");
        reject(()=>RuntimeStoryCatalog.CheckId("../save"),"경로 ID 거부");
        var rows=new[]{new NodeDataRaw{NodeType="MainStoryNode",NodeID="Start",WorldLocation="서울",NextNode="End",NodeMessage="원격 본문"},new NodeDataRaw{NodeType="EndingNode",NodeID="End",WorldLocation="서울",EndingName="끝"}};
        Func<RuntimeStoryCatalog> build=()=>RuntimeStoryCatalog.Build("v1","Start",JsonConvert.SerializeObject(rows),"[]","[]","[]");
        using(var catalog=build()) check(catalog.NodeCount==2,"메모리 정의 생성");
        rows[0].NextNode="Missing"; reject(()=>build(),"외부 노드 참조 차단"); rows[0].NextNode="End";
        rows[1].NodeID="Start"; reject(()=>build(),"중복 노드 거부");rows[1].NodeID="End";
        rows[0].NodeType="Unknown";reject(()=>build(),"미지원 종류 거부");rows[0].NodeType="MainStoryNode";
        reject(()=>RuntimeStoryCatalog.Build("","Start","[]","[]","[]","[]"),"빈 버전 거부");
        var report=LoadingErrorReport.Create("Validation","스토리 데이터의 ID 또는 참조 검증에 실패했습니다.");
        check(report.errorId.Length==32 && DateTime.TryParse(report.occurredAtUtc,out _),"오류 ID/시각");
        check(!JsonUtility.ToJson(report).Contains("player"),"플레이어 저장 데이터 제외");
        var settings=AssetDatabase.LoadAssetAtPath<LoadingSettings>("Assets/Resources/LoadingSettings.asset");
        check(settings!=null && settings.mainScene=="MainWindow","메인 씬 설정");
        check(EditorBuildSettings.scenes[0].path==LoadingSceneInstaller.ScenePath,"첫 씬 등록");
        var scene=EditorSceneManager.OpenScene(LoadingSceneInstaller.ScenePath,OpenSceneMode.Additive);
        try {
            var controller=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LoadingSceneController>(true)).Single();
            check(controller.phoneFrames.Length==18 && controller.phoneFrames.All(s=>s!=null),"18프레임 참조");
            check(controller.loadingText.fontSize==63,"하단 글꼴 크기");
            check(controller.messageText!=null && controller.confirmButton!=null && controller.retryButton!=null && controller.reportButton!=null,"모달 참조");
            check(controller.ring.clip!=null,"전화벨 참조");
        } finally { EditorSceneManager.CloseScene(scene,true); }
        return "Loading System Edit Mode 검사 "+count+"개 통과 (Play Mode/실제 세이브 미실행)";
    }
}
