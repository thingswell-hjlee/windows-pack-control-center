# launcher.ps1 - Control Center Launcher
# Checks port availability, starts the gateway, and opens the dashboard in the browser.

Add-Type -AssemblyName System.Windows.Forms

$Port = 8088
$ExePath = Join-Path $PSScriptRoot "..\ControlCenter.Gateway.exe"
$DashboardUrl = "http://localhost:$Port"

# Check if port is available
$connection = $null
try {
    $connection = New-Object System.Net.Sockets.TcpClient
    $connection.Connect("127.0.0.1", $Port)
    $connection.Close()
    
    # Port is in use - ask user
    $result = [System.Windows.Forms.MessageBox]::Show(
        "포트 $Port 가 이미 사용 중입니다. 이미 실행 중인 인스턴스가 있을 수 있습니다.`n대시보드를 열겠습니까?",
        "Windows Pack Control Center",
        [System.Windows.Forms.MessageBoxButtons]::YesNo)
    
    if ($result -eq [System.Windows.Forms.DialogResult]::Yes) {
        Start-Process $DashboardUrl
    }
} catch {
    # Port is available - start the application
    Start-Process -FilePath $ExePath -WindowStyle Hidden
    Start-Sleep -Seconds 3
    Start-Process $DashboardUrl
}
