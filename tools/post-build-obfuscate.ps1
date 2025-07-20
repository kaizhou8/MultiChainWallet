# 构建后混淆脚本
# Post-build obfuscation script

# 参数 / Parameters
param (
    [Parameter(Mandatory=$true)]
    [string]$ProjectDir,
    
    [Parameter(Mandatory=$true)]
    [string]$TargetDir,
    
    [Parameter(Mandatory=$true)]
    [string]$TargetName,
    
    [Parameter(Mandatory=$true)]
    [string]$ConfigurationName
)

# 设置路径 / Set paths
$toolsDir = Join-Path $ProjectDir "tools"
$confuserCliPath = Join-Path $toolsDir "ConfuserEx\Confuser.CLI.exe"
$confusionConfigPath = Join-Path $ProjectDir "Confusion.crproj"

# 记录信息 / Log information
Write-Host "====== 开始混淆过程 / Starting obfuscation process ======" -ForegroundColor Cyan
Write-Host "项目目录 / Project directory: $ProjectDir" -ForegroundColor Gray
Write-Host "目标目录 / Target directory: $TargetDir" -ForegroundColor Gray
Write-Host "目标名称 / Target name: $TargetName" -ForegroundColor Gray
Write-Host "配置名称 / Configuration name: $ConfigurationName" -ForegroundColor Gray

# 检查是否为Release配置 / Check if it's Release configuration
if ($ConfigurationName -ne "Release") {
    Write-Host "非Release构建，跳过混淆 / Not a Release build, skipping obfuscation" -ForegroundColor Yellow
    exit 0
}

# 检查混淆工具是否存在 / Check if obfuscation tool exists
if (-not (Test-Path $confuserCliPath)) {
    Write-Host "找不到Confuser.CLI.exe，尝试下载... / Confuser.CLI.exe not found, trying to download..." -ForegroundColor Yellow
    
    # 运行下载脚本 / Run download script
    $downloadScript = Join-Path $toolsDir "download-confuserex.ps1"
    if (Test-Path $downloadScript) {
        & $downloadScript
        
        # 再次检查是否存在 / Check again if it exists
        if (-not (Test-Path $confuserCliPath)) {
            Write-Host "混淆工具下载失败，跳过混淆 / Obfuscation tool download failed, skipping obfuscation" -ForegroundColor Red
            exit 1
        }
    }
    else {
        Write-Host "找不到下载脚本，跳过混淆 / Download script not found, skipping obfuscation" -ForegroundColor Red
        exit 1
    }
}

# 检查混淆配置文件是否存在 / Check if obfuscation configuration file exists
if (-not (Test-Path $confusionConfigPath)) {
    Write-Host "找不到混淆配置文件: $confusionConfigPath / Obfuscation configuration file not found: $confusionConfigPath" -ForegroundColor Red
    exit 1
}

# 创建混淆输出目录 / Create obfuscation output directory
$obfuscatedOutputDir = Join-Path $TargetDir "Confused"
if (-not (Test-Path $obfuscatedOutputDir)) {
    New-Item -ItemType Directory -Path $obfuscatedOutputDir -Force | Out-Null
}

# 执行混淆 / Execute obfuscation
Write-Host "正在执行混淆... / Executing obfuscation..." -ForegroundColor Cyan
$startTime = Get-Date

try {
    & $confuserCliPath $confusionConfigPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "混淆过程失败，退出代码: $LASTEXITCODE / Obfuscation process failed, exit code: $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-Host "混淆成功完成，用时: $($duration.TotalSeconds) 秒 / Obfuscation completed successfully, duration: $($duration.TotalSeconds) seconds" -ForegroundColor Green
    
    # 将混淆后的文件复制回目标目录 / Copy obfuscated files back to target directory
    Write-Host "正在将混淆后的文件复制回目标目录... / Copying obfuscated files back to target directory..." -ForegroundColor Cyan
    
    # 备份原始文件 / Backup original files
    $backupDir = Join-Path $TargetDir "Original"
    if (-not (Test-Path $backupDir)) {
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    }
    
    # 获取要备份的DLL / Get DLLs to backup
    $dlls = @(
        "MultiChainWallet.UI.dll",
        "MultiChainWallet.Core.dll",
        "MultiChainWallet.Infrastructure.dll"
    )
    
    foreach ($dll in $dlls) {
        $sourcePath = Join-Path $TargetDir $dll
        $backupPath = Join-Path $backupDir $dll
        if (Test-Path $sourcePath) {
            Copy-Item -Path $sourcePath -Destination $backupPath -Force
        }
    }
    
    # 复制混淆后的文件 / Copy obfuscated files
    foreach ($dll in $dlls) {
        $obfuscatedPath = Join-Path $obfuscatedOutputDir $dll
        $targetPath = Join-Path $TargetDir $dll
        if (Test-Path $obfuscatedPath) {
            Copy-Item -Path $obfuscatedPath -Destination $targetPath -Force
            Write-Host "已复制混淆后的文件: $dll / Copied obfuscated file: $dll" -ForegroundColor Green
        } else {
            Write-Host "警告: 找不到混淆后的文件: $dll / Warning: Obfuscated file not found: $dll" -ForegroundColor Yellow
        }
    }
    
    Write-Host "====== 混淆过程完成 / Obfuscation process completed ======" -ForegroundColor Cyan
}
catch {
    Write-Host "混淆过程发生错误: $_ / Error during obfuscation process: $_" -ForegroundColor Red
    exit 1
} 