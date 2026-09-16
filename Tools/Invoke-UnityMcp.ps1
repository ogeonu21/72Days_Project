param(
    [Parameter(Mandatory=$true)][string]$Method,
    [string]$ParamsJson = '{}'
)
$ErrorActionPreference = 'Stop'
$taskUri = 'http://127.0.0.1:8080/mcp'
$taskHeaders = @{ Accept = 'application/json, text/event-stream' }
function Send-Rpc($taskBody) {
    Invoke-WebRequest -Uri $taskUri -Method Post -ContentType 'application/json; charset=utf-8' -Headers $taskHeaders -Body ([Text.Encoding]::UTF8.GetBytes(($taskBody | ConvertTo-Json -Depth 30 -Compress))) -TimeoutSec 60
}
$taskInit = Send-Rpc @{jsonrpc='2.0';id=1;method='initialize';params=@{protocolVersion='2024-11-05';capabilities=@{};clientInfo=@{name='project-validation';version='1.0'}}}
if ($taskInit.Headers['Mcp-Session-Id']) { $taskHeaders['Mcp-Session-Id'] = $taskInit.Headers['Mcp-Session-Id'][0] }
$null = Send-Rpc @{jsonrpc='2.0';method='notifications/initialized'}
$taskReply = Send-Rpc @{jsonrpc='2.0';id=2;method=$Method;params=($ParamsJson | ConvertFrom-Json)}
foreach ($taskLine in ($taskReply.Content -split "`n")) {
    if ($taskLine.StartsWith('data: ')) { $taskLine.Substring(6) }
}
if ($taskReply.Content.TrimStart().StartsWith('{')) { $taskReply.Content }
