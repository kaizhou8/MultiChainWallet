# 简化版混淆构建脚本
# Simplified obfuscation build script

param (
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectDir = (Join-Path $PSScriptRoot ".."),
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild = $false
)

# 设置变量
$scriptDir = $PSScriptRoot
$confuserToolsDir = Join-Path $scriptDir "ConfuserEx"
$confusionConfigPath = Join-Path $ProjectDir "Confusion.crproj"
$buildOutputDir = Join-Path $ProjectDir "bin" $Configuration "net8.0"
$startTime = Get-Date

# 显示开始信息
Write-Host "====== 开始混淆构建流程 / Starting obfuscation build process ======" -ForegroundColor Cyan
Write-Host "配置: $Configuration / Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "项目目录: $ProjectDir / Project directory: $ProjectDir" -ForegroundColor Cyan

# 步骤1: 下载ConfuserEx工具（如果需要）
if (-not (Test-Path (Join-Path $confuserToolsDir "Confuser.CLI.exe"))) {
    Write-Host "正在下载ConfuserEx工具... / Downloading ConfuserEx tool..." -ForegroundColor Cyan
    & (Join-Path $scriptDir "download-confuserex.ps1")
}

# 步骤2: 执行标准构建（如果不跳过）
if (-not $SkipBuild) {
    Write-Host "正在执行标准构建... / Performing standard build..." -ForegroundColor Cyan
    $buildStartTime = Get-Date
    
    # 使用dotnet命令构建解决方案
    $slnPath = Join-Path $ProjectDir "MultiChainWallet.sln"
    
    Write-Host "解决方案路径: $slnPath / Solution path: $slnPath" -ForegroundColor Yellow
    
    if (Test-Path $slnPath) {
        Write-Host "解决方案文件存在 / Solution file exists" -ForegroundColor Green
        $buildOutput = & dotnet build $slnPath -c $Configuration
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "标准构建失败，错误代码: $LASTEXITCODE / Standard build failed, error code: $LASTEXITCODE" -ForegroundColor Red
            Write-Host $buildOutput -ForegroundColor Red
        } else {
            $buildDuration = (Get-Date) - $buildStartTime
            Write-Host "标准构建完成 (耗时: $($buildDuration.TotalSeconds) 秒) / Standard build completed (duration: $($buildDuration.TotalSeconds) seconds)" -ForegroundColor Green
        }
    } else {
        Write-Host "解决方案文件不存在: $slnPath / Solution file does not exist: $slnPath" -ForegroundColor Red
    }
}

# 输出构建目录状态
if (Test-Path $buildOutputDir) {
    Write-Host "构建输出目录存在: $buildOutputDir / Build output directory exists: $buildOutputDir" -ForegroundColor Green
    Write-Host "目录内容 / Directory contents:" -ForegroundColor Yellow
    Get-ChildItem -Path $buildOutputDir | Format-Table Name, Length, LastWriteTime
} else {
    Write-Host "构建输出目录不存在: $buildOutputDir / Build output directory does not exist: $buildOutputDir" -ForegroundColor Red
}

# 计算总时间
$totalDuration = (Get-Date) - $startTime
Write-Host "====== 混淆构建流程完成 (总耗时: $($totalDuration.TotalSeconds) 秒) / Obfuscation build process completed (total duration: $($totalDuration.TotalSeconds) seconds) ======" -ForegroundColor Green 