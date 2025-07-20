# ConfuserEx 下载脚本
# ConfuserEx Download Script

# 设置路径 / Set paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$toolsDir = Join-Path $scriptDir "ConfuserEx"
$zipPath = Join-Path $scriptDir "ConfuserEx.zip"

# 彩色输出函数 / Colored output functions
function Write-ColorOutput($message, $color) {
    Write-Host $message -ForegroundColor $color
}

function Write-Success($message) { Write-ColorOutput $message "Green" }
function Write-Info($message) { Write-ColorOutput $message "Cyan" }
function Write-Warning($message) { Write-ColorOutput $message "Yellow" }
function Write-Error($message) { Write-ColorOutput $message "Red" }

# 开始下载过程 / Start download process
Write-Info "====== 开始下载 ConfuserEx ======"
Write-Info "====== Starting ConfuserEx download ======"

# 检查目标目录 / Check target directory
if (-not (Test-Path $toolsDir)) {
    Write-Info "创建 ConfuserEx 目录... / Creating ConfuserEx directory..."
    New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null
}

# 定义下载 URL / Define download URL
$url = "https://github.com/mkaring/ConfuserEx/releases/latest/download/ConfuserEx.zip"

# 下载文件 / Download file
Write-Info "从 $url 下载 ConfuserEx... / Downloading ConfuserEx from $url..."
try {
    Invoke-WebRequest -Uri $url -OutFile $zipPath
    Write-Success "下载完成 / Download completed"
} catch {
    Write-Error "下载失败: $_ / Download failed: $_"
    exit 1
}

# 解压文件 / Extract files
Write-Info "解压 ConfuserEx... / Extracting ConfuserEx..."
try {
    # 清理目标目录 / Clean target directory
    if (Test-Path $toolsDir) {
        Get-ChildItem -Path $toolsDir -Force | Remove-Item -Recurse -Force
    }
    
    # 解压 / Extract
    Expand-Archive -Path $zipPath -DestinationPath $toolsDir -Force
    Write-Success "解压完成 / Extraction completed"
} catch {
    Write-Error "解压失败: $_ / Extraction failed: $_"
    exit 1
}

# 清理 ZIP 文件 / Clean up ZIP file
if (Test-Path $zipPath) {
    Write-Info "删除 ZIP 文件... / Removing ZIP file..."
    Remove-Item $zipPath -Force
}

# 验证下载 / Verify download
$cliPath = Join-Path $toolsDir "Confuser.CLI.exe"
if (Test-Path $cliPath) {
    Write-Success "====== ConfuserEx 下载成功 ======"
    Write-Success "====== ConfuserEx downloaded successfully ======"
    Write-Info "CLI 工具位置: $cliPath / CLI tool location: $cliPath"
} else {
    Write-Error "下载后未找到 ConfuserEx 可执行文件 / ConfuserEx executable not found after download"
    exit 1
} 