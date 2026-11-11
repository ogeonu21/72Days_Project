using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[Serializable] public sealed class LoadingErrorReport
{
    public string errorId, occurredAtUtc, message, category, storyVersion, appVersion;
    public static LoadingErrorReport Create(string category, string safeMessage) => new LoadingErrorReport {
        errorId=Guid.NewGuid().ToString("N"), occurredAtUtc=DateTime.UtcNow.ToString("O"),
        message=safeMessage, category=category, storyVersion=RuntimeStoryCatalog.Version, appVersion=Application.version
    };
}

public sealed class LoadingSceneController : MonoBehaviour
{
    public LoadingSettings settings;
    public UnityEngine.UI.Image phone;
    public Sprite[] phoneFrames;
    public AudioSource ring;
    public TextMeshProUGUI loadingText, messageText;
    public GameObject dialog;
    public UnityEngine.UI.Button confirmButton, retryButton, reportButton;
    private float clock;
    private Vector2 phoneOrigin;
    private bool busy, awaitingConfirm, sending, alive;
    private LoadingErrorReport report;
    private RuntimeStoryCatalog candidate;
    private string queuePath => Path.Combine(Application.persistentDataPath,"loading-error-pending.json");
    private void Awake() { alive=true; phoneOrigin=phone.rectTransform.anchoredPosition; }
    private void Start()
    {
        confirmButton.onClick.AddListener(Confirm); retryButton.onClick.AddListener(Begin);
        reportButton.onClick.AddListener(SendReport); dialog.SetActive(false); Begin();
    }
    private void Update()
    {
        clock+=Time.unscaledDeltaTime;
        loadingText.text="로딩중"+new string('.',(int)clock%4);
        if(phoneFrames!=null && phoneFrames.Length>0) {
            int frame=(int)(clock/.2f)%phoneFrames.Length; phone.sprite=phoneFrames[frame];
            phone.rectTransform.anchoredPosition=phoneOrigin+new Vector2(frame<14 ? Mathf.Sin(clock*65)*3 : 0,0);
            if(ring!=null && ring.clip!=null) { if(frame<10 && !ring.isPlaying) ring.Play(); else if(frame>=10 && ring.isPlaying) ring.Stop(); }
        }
    }
    public async void Begin()
    {
        if(busy || sending) return;
        busy=true; report=null; awaitingConfirm=false; dialog.SetActive(false);
        candidate?.Dispose(); candidate=null;
        float started=Time.realtimeSinceStartup;
        string category="RemoteData";
        try
        {
            if(settings==null) throw new InvalidOperationException("설정 누락");
            var before=ReadVersion(await Fetch("StoryVersion"));
            var sheets=await Task.WhenAll(Fetch("NodeData"),Fetch("EventData"),Fetch("EventChoiceData"),Fetch("RewardData"));
            var after=ReadVersion(await Fetch("StoryVersion"));
            if(before.version!=after.version || before.start!=after.start) throw new InvalidOperationException("조회 중 버전 변경");
            category="Validation";
            candidate=RuntimeStoryCatalog.Build(before.version,before.start,sheets[0],sheets[1],sheets[2],sheets[3]);
            category="SaveCompatibility";
            string path=Path.Combine(Application.persistentDataPath,"savedata.json");
            bool incompatible=false;
            if(File.Exists(path)) {
                var info=new FileInfo(path);
                if(info.Length>8000000) incompatible=true;
                else {
                    try { var header=JObject.Parse(File.ReadAllText(path)); incompatible=!RuntimeStoryCatalog.Compatible((string)header["storyVersion"],before.version); }
                    catch { incompatible=true; }
                }
            }
            while(alive && Time.realtimeSinceStartup-started<settings.minimumLoadingSeconds) await Task.Delay(50);
            if(!alive) return;
            if(incompatible) {
                awaitingConfirm=true;
                ShowDialog("스토리 데이터 버전이 달라 기존 저장 데이터를 사용할 수 없습니다.\n기존 파일은 보존됩니다. 새 게임을 시작해 주세요.",true,false);
            } else EnterMain();
        }
        catch(Exception)
        {
            candidate?.Dispose(); candidate=null;
            if(!alive) return;
            // 원시 응답·경로·토큰·세이브·스택은 외부 전송하지 않는다.
            report=LoadingErrorReport.Create(category,category=="Validation" ? "스토리 데이터의 ID 또는 참조 검증에 실패했습니다." : category=="SaveCompatibility" ? "저장 데이터 호환성 확인에 실패했습니다." : "원격 데이터 조회 또는 버전 확인에 실패했습니다.");
            try { File.WriteAllText(queuePath,JsonUtility.ToJson(report)); } catch { }
            ShowDialog("로딩중 치명적인 버그가 발생했습니다.\n오류 ID: "+report.errorId+"\n"+report.message,false,true);
        }
        finally { busy=false; }
    }
    private static (string version,string start) ReadVersion(string json)
    {
        var rows=JArray.Parse(json); if(rows.Count!=1) throw new FormatException("버전은 한 행이어야 합니다.");
        string version=(string)rows[0]["storyVersion"], start=(string)rows[0]["startNodeId"];
        if(string.IsNullOrWhiteSpace(version) || version.Length>80) throw new FormatException("storyVersion 오류");
        RuntimeStoryCatalog.CheckId(start); return (version,start);
    }
    public static bool AllowedEndpoint(string value)
    { return Uri.TryCreate(value,UriKind.Absolute,out Uri uri) && uri.Scheme=="https" && uri.Host=="script.google.com" && uri.AbsolutePath.StartsWith("/macros/s/",StringComparison.Ordinal) && uri.AbsolutePath.EndsWith("/exec",StringComparison.Ordinal); }
    private async Task<string> Fetch(string sheet)
    {
        if(!AllowedEndpoint(settings.dataEndpoint)) throw new InvalidOperationException("데이터 주소 오류");
        using(var request=UnityWebRequest.Get(settings.dataEndpoint+"?sheetName="+Uri.EscapeDataString(sheet))) {
            request.timeout=Mathf.Clamp(settings.timeoutSeconds,5,60);
            var operation=request.SendWebRequest();
            while(!operation.isDone) {
                if(!alive || request.downloadedBytes>(ulong)settings.maximumResponseBytes) { request.Abort(); throw new InvalidOperationException("조회 취소 또는 크기 초과"); }
                await Task.Delay(40);
            }
            if(request.result!=UnityWebRequest.Result.Success || request.downloadHandler.data.Length>settings.maximumResponseBytes) throw new InvalidOperationException("데이터 조회 실패");
            return request.downloadHandler.text;
        }
    }
    private void ShowDialog(string message,bool confirm,bool fatal)
    { dialog.SetActive(true); messageText.text=message; confirmButton.gameObject.SetActive(confirm); retryButton.gameObject.SetActive(fatal); reportButton.gameObject.SetActive(fatal); reportButton.interactable=report!=null; }
    private void Confirm() { if(awaitingConfirm && !busy) EnterMain(); }
    private void EnterMain()
    { RuntimeStoryCatalog.Publish(candidate); candidate=null; if(ring!=null) ring.Stop(); SceneManager.LoadScene(settings.mainScene); }
    public async void SendReport()
    {
        if(report==null || sending || busy) return;
        if(!AllowedEndpoint(settings.reportEndpoint)) { messageText.text="오류 전송 연결이 아직 설정되지 않았습니다.\n오류 ID: "+report.errorId+"\n재시도할 수 있습니다."; return; }
        sending=true; reportButton.interactable=false;
        try {
            using(var request=new UnityWebRequest(settings.reportEndpoint,"POST")) {
                request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(report)));
                request.downloadHandler=new DownloadHandlerBuffer(); request.SetRequestHeader("Content-Type","text/plain; charset=utf-8"); request.timeout=20;
                var op=request.SendWebRequest(); while(!op.isDone) { if(!alive){request.Abort();return;} await Task.Delay(50); }
                if(request.result!=UnityWebRequest.Result.Success || request.downloadHandler.data.Length>4096 || (bool?)JObject.Parse(request.downloadHandler.text)["ok"]!=true) throw new InvalidOperationException();
                if(!alive) return;
                try { if(File.Exists(queuePath)) File.Delete(queuePath); } catch { }
                messageText.text="오류 정보를 전송했습니다.\n오류 ID: "+report.errorId; report=null;
            }
        } catch { if(alive) messageText.text="오류 전송에 실패했습니다. 잠시 후 다시 시도해 주세요.\n오류 ID: "+report?.errorId; }
        finally { sending=false; if(alive) reportButton.interactable=report!=null; }
    }
    private void OnDestroy() { alive=false; candidate?.Dispose(); candidate=null; if(phone!=null) phone.rectTransform.anchoredPosition=phoneOrigin; }
}
