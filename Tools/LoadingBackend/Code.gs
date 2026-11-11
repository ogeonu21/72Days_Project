// 새 독립 Apps Script 프로젝트에 배포한다. 기존 데이터 서비스는 덮어쓰지 않는다.
const DATA_ID = '1KIn585a8qHi8Jr7f1g4C7PrfJ8YO25Wd1qA-6irMKhs';
const REPORT_ID = '19X4WaUXcpJaBbIGDyJUN1-FsUMNCLoFv1bsMRx94oyI';
const HEADERS = {StoryVersion:'storyVersion',StatusData:'ID',ItemData:'ItemID',NodeData:'NodeID',EventData:'EventID',EventChoiceData:'ChoiceID',RewardData:'RewardID'};
function json_(value) { return ContentService.createTextOutput(JSON.stringify(value)).setMimeType(ContentService.MimeType.JSON); }
function doGet(e) {
  try {
    const name=String(e.parameter.sheetName || '');
    if(!Object.prototype.hasOwnProperty.call(HEADERS,name)) return json_({ok:false,error:'SHEET_NOT_ALLOWED'});
    const sheet=SpreadsheetApp.openById(DATA_ID).getSheetByName(name);
    if(!sheet || sheet.getLastRow()>6000 || sheet.getLastColumn()>100) return json_({ok:false,error:'SHEET_INVALID'});
    const values=sheet.getDataRange().getValues();
    const index=values.slice(0,5).findIndex(row=>row.includes(HEADERS[name]));
    if(index<0) return json_({ok:false,error:'HEADER_MISSING'});
    const headers=values[index].map(String);
    const rows=values.slice(index+1).filter(row=>String(row[headers.indexOf(HEADERS[name])]).trim()!=='');
    return json_(rows.map(row=>Object.fromEntries(headers.map((key,i)=>[key,row[i]]).filter(x=>x[0]))));
  } catch(_) { return json_({ok:false,error:'DATA_READ_FAILED'}); }
}
function doPost(e) {
  const lock=LockService.getScriptLock();
  try {
    if(!e || !e.postData || typeof e.postData.contents!=='string' || e.postData.contents.length>4096 || Utilities.newBlob(e.postData.contents).getBytes().length>4096) return json_({ok:false,error:'INVALID_REPORT'});
    const r=JSON.parse(e.postData.contents);
    const fields=['errorId','occurredAtUtc','message','category','storyVersion','appVersion'];
    if(!r || Array.isArray(r) || typeof r!=='object' || Object.keys(r).length!==fields.length || !fields.every(key=>Object.prototype.hasOwnProperty.call(r,key) && typeof r[key]==='string')) return json_({ok:false,error:'INVALID_REPORT'});
    const categories=['RemoteData','Validation','SaveCompatibility'];
    const messages=['원격 데이터 조회 또는 버전 확인에 실패했습니다.','스토리 데이터의 ID 또는 참조 검증에 실패했습니다.','저장 데이터 호환성 확인에 실패했습니다.'];
    if(!/^[a-f0-9]{32}$/.test(r.errorId) || !categories.includes(r.category) || !messages.includes(r.message) || !/^\d{4}-\d{2}-\d{2}T/.test(r.occurredAtUtc) || !Number.isFinite(Date.parse(r.occurredAtUtc))) return json_({ok:false,error:'INVALID_REPORT'});
    for(const key of ['storyVersion','appVersion']) if(typeof r[key]!=='string' || r[key].length>80 || /[\r\n]/.test(r[key])) return json_({ok:false,error:'INVALID_REPORT'});
    if(!lock.tryLock(5000)) return json_({ok:false,error:'BUSY'});
    // 최근100개 ID만 보관한다. 누적 시트 검색 없이 상한에서도 재전송을 확인한다.
    const properties=PropertiesService.getScriptProperties(), day=Utilities.formatDate(new Date(),'UTC','yyyy-MM-dd');
    const counter=JSON.parse(properties.getProperty('reportCounter') || '{}');
    const recent=JSON.parse(properties.getProperty('loadingReportRecentIds') || '[]');
    if(recent.includes(r.errorId)) return json_({ok:true,duplicate:true});
    // 공개 수신점의 서버 날짜당 최대500건. 상한 도달 시 시트에 접근하지 않는다.
    if(counter.day===day && counter.count>=500) return json_({ok:false,error:'RATE_LIMIT'});
    const sheet=SpreadsheetApp.openById(REPORT_ID).getSheetByName('ErrorReports');
    // 앱/버전 문자열도 수식으로 평가되지 않도록 보관한다.
    const safe=v=>/^[=+@-]/.test(String(v)) ? "'"+v : String(v);
    sheet.appendRow([r.errorId,r.occurredAtUtc,r.message,r.category,safe(r.storyVersion),safe(r.appVersion),new Date().toISOString()]);
    properties.setProperty('reportCounter',JSON.stringify({day:day,count:counter.day===day ? counter.count+1 : 1}));
    properties.setProperty('loadingReportRecentIds',JSON.stringify(recent.concat(r.errorId).slice(-100)));
    return json_({ok:true});
  } catch(_) { return json_({ok:false,error:'REPORT_FAILED'}); }
  finally { if(lock.hasLock()) lock.releaseLock(); }
}
