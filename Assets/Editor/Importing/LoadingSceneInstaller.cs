using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public static class LoadingSceneInstaller
{
    public const string ScenePath="Assets/Scenes/LoadingScene.unity";
    [MenuItem("Tools/Loading System/Create Loading Scene")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("편집 중인 씬을 먼저 저장하고 Edit Mode에서 실행하세요.");
        if(File.Exists(ScenePath)) throw new InvalidOperationException("기존 LoadingScene은 자동 덮어쓰지 않습니다.");
        string previous=EditorSceneManager.GetActiveScene().path;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera").AddComponent<Camera>(); camera.tag="MainCamera"; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
        new GameObject("Directional Light").AddComponent<Light>().type=LightType.Directional;
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        var canvas=new GameObject("LoadingCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster),typeof(CanvasSafeArea));
        var safeArea=new SerializedObject(canvas.GetComponent<CanvasSafeArea>());
        safeArea.FindProperty("fitReferenceResolution").boolValue=true; safeArea.ApplyModifiedPropertiesWithoutUndo();
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1080,1920); scaler.matchWidthOrHeight=.5f;
        // 어느 화면비에서도 고정 배치가 사용하는 참조 영역 전체를 유지한다.
        scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/DungGeunMo SDF.asset");
        if(font==null) throw new InvalidOperationException("로딩 한글 폰트 누락");
        // 배경은 안전 영역 밖까지 채우고, 중앙 원본 구도를 보존한 확장부만 화면비에 따라 자른다.
        var backgroundCanvas=new GameObject("LoadingBackgroundCanvas",typeof(RectTransform),typeof(Canvas));
        backgroundCanvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        backgroundCanvas.GetComponent<Canvas>().sortingOrder=-100;
        var bg=Image("Background",backgroundCanvas.transform,new Vector2(1080,2520),Vector2.zero);
        bg.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Loading/background-extended-1080x2520.png");
        bg.preserveAspect=true;
        var backgroundFit=bg.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
        backgroundFit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
        backgroundFit.aspectRatio=1080f/2520f;
        var phone=Image("Phone",canvas.transform,new Vector2(384,384),new Vector2(72,38));
        var status=Label("LoadingText",canvas.transform,"로딩중",font,63,new Vector2(950,120),new Vector2(0,-837));
        var dialog=Image("Dialog",canvas.transform,new Vector2(980,780),Vector2.zero); dialog.color=new Color(.025f,.03f,.05f,.97f); dialog.raycastTarget=true;
        var message=Label("Message",dialog.transform,"",font,38,new Vector2(890,470),new Vector2(0,95)); message.enableWordWrapping=true;
        var confirm=Button("Confirm","확인",dialog.transform,font,new Vector2(0,-250));
        var retry=Button("Retry","다시 시도",dialog.transform,font,new Vector2(-210,-250));
        var report=Button("SendReport","오류 전송",dialog.transform,font,new Vector2(210,-250));
        var root=new GameObject("LoadingSystem"); var controller=root.AddComponent<LoadingSceneController>();
        controller.settings=AssetDatabase.LoadAssetAtPath<LoadingSettings>("Assets/Resources/LoadingSettings.asset");
        controller.phone=phone; controller.phoneFrames=Enumerable.Range(1,18).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Loading/Phone/idle-"+i+".png")).ToArray();
        if(bg.sprite==null || controller.settings==null || controller.phoneFrames.Any(s=>s==null)) throw new InvalidOperationException("로딩 에셋 또는 설정 누락");
        phone.sprite=controller.phoneFrames[0]; controller.loadingText=status; controller.messageText=message; controller.dialog=dialog.gameObject;
        controller.confirmButton=confirm; controller.retryButton=retry; controller.reportButton=report;
        controller.ring=root.AddComponent<AudioSource>(); controller.ring.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Loading/ring.wav"); controller.ring.loop=true; controller.ring.volume=.25f; controller.ring.playOnAwake=true;
        dialog.gameObject.SetActive(false);
        EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath)).ToArray();
        if(!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
        Debug.Log("[Loading] LoadingScene 생성 및 첫 실행 씬 등록 완료");
    }
    private static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 size,Vector2 pos)
    { var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); go.transform.SetParent(parent,false); var image=go.GetComponent<UnityEngine.UI.Image>(); image.rectTransform.sizeDelta=size; image.rectTransform.anchoredPosition=pos; image.raycastTarget=false; return image; }
    private static TextMeshProUGUI Label(string name,Transform parent,string text,TMP_FontAsset font,float size,Vector2 bounds,Vector2 pos)
    { var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false); var label=go.GetComponent<TextMeshProUGUI>(); label.font=font; label.fontSize=size; label.text=text; label.alignment=TextAlignmentOptions.Center; label.raycastTarget=false; label.rectTransform.sizeDelta=bounds; label.rectTransform.anchoredPosition=pos; return label; }
    private static UnityEngine.UI.Button Button(string name,string text,Transform parent,TMP_FontAsset font,Vector2 pos)
    { var image=Image(name,parent,new Vector2(370,110),pos); image.color=new Color(.18f,.21f,.28f); image.raycastTarget=true; var button=image.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=image; Label("Text",image.transform,text,font,40,new Vector2(360,100),Vector2.zero); return button; }
}
