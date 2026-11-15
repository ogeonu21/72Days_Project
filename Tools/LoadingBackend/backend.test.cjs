// 실제 Google 서비스 호출 없이 두 배포 소스의 입력·상한·유한 중복 보관 정책을 검사한다.
const vm=require('node:vm'), fs=require('node:fs'), assert=require('node:assert/strict');
let passed=0;
for(const source of ['Code.gs','doPost-추가코드.txt']) {
  let rows,props,opens,day;
  const reset=()=>{rows=[['ErrorId']];props={};opens=0;day='2026-11-11';};
  reset();
  // 중복 판정에 누적 시트 검색이 재도입되면 즉시 실패한다.
  const reportSheet={appendRow:r=>rows.push(r)};
  const dataSheet={getLastRow:()=>3,getLastColumn:()=>2,getDataRange:()=>({getValues:()=>[[],['storyVersion','startNodeId'],['v1','Start']]})};
  const context={
    ContentService:{MimeType:{JSON:'json'},createTextOutput:s=>({setMimeType:()=>s})},
    SpreadsheetApp:{openById:()=>{opens++;return {getSheetByName:name=>name==='ErrorReports'?reportSheet:dataSheet};}},
    LockService:{getScriptLock:()=>({tryLock:()=>true,hasLock:()=>true,releaseLock:()=>{}})},
    PropertiesService:{getScriptProperties:()=>({getProperty:k=>props[k],setProperty:(k,v)=>props[k]=v})},
    Utilities:{formatDate:()=>day,newBlob:s=>({getBytes:()=>Array.from(Buffer.from(s,'utf8'))})}
  };
  vm.createContext(context);vm.runInContext(fs.readFileSync(__dirname+'/'+source,'utf8'),context);
  function test(name,fn){reset();fn();passed++;console.log('PASS '+source+' '+name);}
  function report(overrides={}){return {errorId:'a'.repeat(32),occurredAtUtc:'2026-11-11T00:00:00.000Z',message:'원격 데이터 조회 또는 버전 확인에 실패했습니다.',category:'RemoteData',storyVersion:'v1',appVersion:'1.0',...overrides};}
  const raw=body=>JSON.parse(context.doPost({postData:{contents:body}}));
  const post=r=>raw(JSON.stringify(r));
  const id=n=>n.toString(16).padStart(32,'0');
  test('정상 오류 수집',()=>{assert.equal(post(report()).ok,true);assert.equal(rows.length,2);});
  test('같은 ID 재전송은 시트 접근 없이 중복 응답',()=>{post(report());opens=0;assert.equal(post(report()).duplicate,true);assert.equal(rows.length,2);assert.equal(opens,0);});
  test('잘못된 ID 거부',()=>assert.equal(post(report({errorId:'bad'})).error,'INVALID_REPORT'));
  test('문자열로 강제변환 가능한 배열 ID도 거부',()=>assert.equal(post(report({errorId:['a'.repeat(32)]})).error,'INVALID_REPORT'));
  test('원시 비밀 메시지 거부',()=>assert.equal(post(report({message:'token=secret'})).error,'INVALID_REPORT'));
  test('미지원 분류 거부',()=>assert.equal(post(report({category:'Other'})).error,'INVALID_REPORT'));
  test('유효하지 않은 시각 거부',()=>assert.equal(post(report({occurredAtUtc:'yesterday'})).error,'INVALID_REPORT'));
  test('누락 필드 거부',()=>{const r=report();delete r.appVersion;assert.equal(post(r).error,'INVALID_REPORT');});
  test('미허용 필드 거부 및 시트 미접근',()=>{assert.equal(post(report({extra:'x'})).error,'INVALID_REPORT');assert.equal(opens,0);});
  test('null·배열·문자열 본문 거부',()=>{for(const r of [null,[], 'text'])assert.equal(post(r).error,'INVALID_REPORT');});
  test('요청 본문 누락 거부',()=>assert.equal(JSON.parse(context.doPost({})).error,'INVALID_REPORT'));
  test('UTF8 정확히4096바이트 허용',()=>{const body=JSON.stringify(report({storyVersion:'가'.repeat(80)}));const padded=body+' '.repeat(4096-Buffer.byteLength(body));assert.equal(Buffer.byteLength(padded),4096);assert.equal(raw(padded).ok,true);});
  test('UTF8 4097바이트 거부',()=>{const body=JSON.stringify(report({storyVersion:'가'.repeat(80)}));const padded=body+' '.repeat(4097-Buffer.byteLength(body));assert.ok(padded.length<4096);assert.equal(raw(padded).error,'INVALID_REPORT');assert.equal(opens,0);});
  test('ASCII 4097자 거부',()=>assert.equal(raw('x'.repeat(4097)).error,'INVALID_REPORT'));
  test('수식 문자열 무력화',()=>{assert.equal(post(report({storyVersion:'=1+1',appVersion:'@test'})).ok,true);assert.equal(rows.at(-1)[4],"'=1+1");assert.equal(rows.at(-1)[5],"'@test");});
  test('500번째 허용·501번째는 시트 접근 없이 차단',()=>{props.reportCounter=JSON.stringify({day,count:499});assert.equal(post(report()).ok,true);opens=0;assert.equal(post(report({errorId:id(501)})).error,'RATE_LIMIT');assert.equal(opens,0);assert.equal(rows.length,2);});
  test('상한 이후에도 최근 중복은 성공 응답·시트 미접근',()=>{post(report());props.reportCounter=JSON.stringify({day,count:500});opens=0;assert.equal(post(report()).duplicate,true);assert.equal(opens,0);});
  test('다음 UTC날짜에 상한 초기화',()=>{props.reportCounter=JSON.stringify({day,count:500});day='2026-11-12';assert.equal(post(report()).ok,true);assert.equal(JSON.parse(props.reportCounter).count,1);});
  test('중복 ID 보관은 최근100건·유한 크기',()=>{for(let n=0;n<101;n++) assert.equal(post(report({errorId:id(n)})).ok,true);const recent=JSON.parse(props.loadingReportRecentIds);assert.equal(recent.length,100);assert.equal(recent[0],id(1));assert.ok(Buffer.byteLength(props.loadingReportRecentIds)<4096);opens=0;assert.equal(post(report({errorId:id(1)})).duplicate,true);assert.equal(opens,0);});
  if(source==='Code.gs') {
    test('비허용 시트 거부',()=>{assert.equal(JSON.parse(context.doGet({parameter:{sheetName:'Private'}})).ok,false);assert.equal(opens,0);});
    test('두번째 행 헤더 버전 읽기',()=>{const r=JSON.parse(context.doGet({parameter:{sheetName:'StoryVersion'}}));assert.equal(r[0].storyVersion,'v1');assert.equal(r[0].startNodeId,'Start');});
  }
}
console.log('RESULT '+passed+' passed (Google 서버 실제 전송 아님)');
