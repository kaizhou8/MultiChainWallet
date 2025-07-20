# 简化混淆脚本
# Simplified Obfuscation Script

# 设置路径变量
# Set path variables
$ProjectDir = (Get-Location).Path
$buildOutputDir = Join-Path $ProjectDir "bin\Release\net8.0"
$confuserDir = Join-Path $ProjectDir "tools\ConfuserEx" 
$confuserCliPath = Join-Path $confuserDir "Confuser.CLI.exe"
$confusionConfigPath = Join-Path $ProjectDir "Confusion.crproj"

# 输出彩色信息函数
# Output colored information functions
function Write-ColorOutput($message, $color)
{
    Write-Host $message -ForegroundColor $color
}

function Write-Success($message) { Write-ColorOutput $message "Green" }
function Write-Info($message) { Write-ColorOutput $message "Cyan" }
function Write-Warning($message) { Write-ColorOutput $message "Yellow" }
function Write-Error($message) { Write-ColorOutput $message "Red" }

# 显示脚本开始信息
# Display script start information
Write-Info "====== 开始简化混淆流程 ======"
Write-Info "====== Starting simplified obfuscation process ======"
Write-Info "项目目录 / Project directory: $ProjectDir"
Write-Info "构建输出目录 / Build output directory: $buildOutputDir"
Write-Info "混淆工具路径 / Obfuscation tool path: $confuserCliPath"
Write-Info "混淆配置文件 / Obfuscation config file: $confusionConfigPath"

# 步骤1: 检查是否有构建输出
# Step 1: Check if build output exists
if (-not (Test-Path $buildOutputDir)) {
    Write-Error "构建输出目录不存在: $buildOutputDir"
    Write-Error "Build output directory does not exist: $buildOutputDir"
    Write-Info "正在创建目录..."
    Write-Info "Creating directory..."
    New-Item -ItemType Directory -Path $buildOutputDir -Force | Out-Null
    
    Write-Info "请先构建项目: dotnet build -c Release"
    Write-Info "Please build project first: dotnet build -c Release"
    exit 1
}

# 步骤2: 检查混淆工具和下载（如果需要）
# Step 2: Check obfuscation tool and download (if needed)
if (-not (Test-Path $confuserCliPath)) {
    Write-Warning "混淆工具不存在，需要下载"
    Write-Warning "Obfuscation tool does not exist, needs to be downloaded"
    
    # 检查下载脚本
    # Check download script
    $downloadScript = Join-Path $ProjectDir "tools\download-confuserex.ps1"
    if (Test-Path $downloadScript) {
        Write-Info "正在运行下载脚本..."
        Write-Info "Running download script..."
        & $downloadScript
    } else {
        Write-Error "下载脚本不存在: $downloadScript"
        Write-Error "Download script does not exist: $downloadScript"
        
        # 创建下载目录
        # Create download directory
        if (-not (Test-Path $confuserDir)) {
            New-Item -ItemType Directory -Path $confuserDir -Force | Out-Null
        }
        
        # 直接下载
        # Direct download
        $url = "https://github.com/mkaring/ConfuserEx/releases/latest/download/ConfuserEx.zip"
        $zipPath = Join-Path $ProjectDir "tools\ConfuserEx.zip"
        
        Write-Info "正在从 $url 下载 ConfuserEx..."
        Write-Info "Downloading ConfuserEx from $url..."
        
        try {
            Invoke-WebRequest -Uri $url -OutFile $zipPath
            
            Write-Info "正在解压 ConfuserEx..."
            Write-Info "Extracting ConfuserEx..."
            Expand-Archive -Path $zipPath -DestinationPath $confuserDir -Force
            
            Remove-Item $zipPath -Force
            
            if (Test-Path $confuserCliPath) {
                Write-Success "ConfuserEx 下载并解压成功"
                Write-Success "ConfuserEx downloaded and extracted successfully"
            } else {
                Write-Error "下载后未找到 ConfuserEx 工具"
                Write-Error "ConfuserEx tool not found after download"
                exit 1
            }
        } catch {
            Write-Error "下载 ConfuserEx 失败: $_"
            Write-Error "Failed to download ConfuserEx: $_"
            exit 1
        }
    }
}

# 步骤3: 检查混淆配置文件
# Step 3: Check obfuscation configuration file
if (-not (Test-Path $confusionConfigPath)) {
    Write-Error "混淆配置文件不存在: $confusionConfigPath"
    Write-Error "Obfuscation configuration file does not exist: $confusionConfigPath"
    exit 1
}

# 步骤4: 执行混淆
# Step 4: Execute obfuscation
$startTime = Get-Date
Write-Info "正在执行混淆..."
Write-Info "Executing obfuscation..."

try {
    & $confuserCliPath $confusionConfigPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "混淆过程失败，退出代码: $LASTEXITCODE"
        Write-Error "Obfuscation process failed, exit code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-Success "混淆成功完成，用时: $($duration.TotalSeconds) 秒"
    Write-Success "Obfuscation completed successfully, duration: $($duration.TotalSeconds) seconds"
    
    # 显示混淆后的文件
    # Display obfuscated files
    $obfuscatedOutputDir = Join-Path $ProjectDir "Confused"
    if (Test-Path $obfuscatedOutputDir) {
        Write-Info "混淆后的文件位于: $obfuscatedOutputDir"
        Write-Info "Obfuscated files located at: $obfuscatedOutputDir"
        Get-ChildItem $obfuscatedOutputDir | ForEach-Object {
            Write-Host "  - $($_.Name)" -ForegroundColor Gray
        }
    } else {
        Write-Warning "未找到混淆输出目录: $obfuscatedOutputDir"
        Write-Warning "Obfuscation output directory not found: $obfuscatedOutputDir"
    }
}
catch {
    Write-Error "混淆过程发生错误: $_"
    Write-Error "Error during obfuscation process: $_"
    exit 1
}

Write-Info "====== 简化混淆流程完成 ======"
Write-Info "====== Simplified obfuscation process completed ====== 