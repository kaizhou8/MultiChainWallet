# 混淆构建脚本 - 主流程
# Obfuscation build script - Main process

# 参数 / Parameters
param (
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectDir = (Join-Path $PSScriptRoot ".."),
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipObfuscation = $false
)

# 设置变量 / Set variables
$scriptDir = $PSScriptRoot
$confuserToolsDir = Join-Path $scriptDir "ConfuserEx"
$confusionConfigPath = Join-Path $ProjectDir "Confusion.crproj"
$buildOutputDir = Join-Path $ProjectDir "bin" $Configuration "net8.0"
$startTime = Get-Date

# 设置输出颜色函数 / Set output color functions
function Write-Success($message) { Write-Host $message -ForegroundColor Green }
function Write-Info($message) { Write-Host $message -ForegroundColor Cyan }
function Write-Warning($message) { Write-Host $message -ForegroundColor Yellow }
function Write-Error($message) { Write-Host $message -ForegroundColor Red }

# 显示开始信息 / Display start information
Write-Info "====== 开始混淆构建流程 / Starting obfuscation build process ======"
Write-Info "配置: $Configuration / Configuration: $Configuration"
Write-Info "项目目录: $ProjectDir / Project directory: $ProjectDir"
Write-Info "跳过构建: $SkipBuild / Skip build: $SkipBuild"
Write-Info "跳过测试: $SkipTests / Skip tests: $SkipTests"
Write-Info "跳过混淆: $SkipObfuscation / Skip obfuscation: $SkipObfuscation"

# 步骤1: 下载ConfuserEx工具（如果需要）/ Step 1: Download ConfuserEx tool (if needed)
if (-not (Test-Path (Join-Path $confuserToolsDir "Confuser.CLI.exe"))) {
    Write-Info "正在下载ConfuserEx工具... / Downloading ConfuserEx tool..."
    & (Join-Path $scriptDir "download-confuserex.ps1")
    
    if (-not (Test-Path (Join-Path $confuserToolsDir "Confuser.CLI.exe"))) {
        Write-Error "无法下载ConfuserEx工具，构建过程停止 / Unable to download ConfuserEx tool, build process stopped"
        exit 1
    }
}

# 步骤2: 执行标准构建（如果不跳过）/ Step 2: Perform standard build (if not skipped)
if (-not $SkipBuild) {
    Write-Info "正在执行标准构建... / Performing standard build..."
    $buildStartTime = Get-Date
    
    # 使用dotnet命令构建解决方案 / Use dotnet command to build solution
    $slnPath = Join-Path $ProjectDir "MultiChainWallet.sln"
    $buildOutput = & dotnet build $slnPath -c $Configuration --no-incremental
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "标准构建失败，错误代码: $LASTEXITCODE / Standard build failed, error code: $LASTEXITCODE"
        Write-Error $buildOutput
        exit 1
    }
    
    $buildDuration = (Get-Date) - $buildStartTime
    Write-Success "标准构建完成 (耗时: $($buildDuration.TotalSeconds) 秒) / Standard build completed (duration: $($buildDuration.TotalSeconds) seconds)"
}
else {
    Write-Info "已跳过标准构建步骤 / Standard build step skipped"
}

# 步骤3: 执行混淆（如果不跳过）/ Step 3: Perform obfuscation (if not skipped)
if (-not $SkipObfuscation) {
    if (-not (Test-Path $buildOutputDir)) {
        Write-Error "构建输出目录不存在: $buildOutputDir / Build output directory does not exist: $buildOutputDir"
        Write-Error "请先执行构建或检查目录路径 / Please perform build first or check directory path"
        exit 1
    }
    
    Write-Info "正在执行混淆... / Performing obfuscation..."
    $obfuscationStartTime = Get-Date
    
    # 调用混淆后处理脚本 / Call post-build obfuscation script
    & (Join-Path $scriptDir "post-build-obfuscate.ps1") -ProjectDir $ProjectDir -TargetDir $buildOutputDir -TargetName "MultiChainWallet" -ConfigurationName $Configuration
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "混淆过程失败，错误代码: $LASTEXITCODE / Obfuscation process failed, error code: $LASTEXITCODE"
        exit 1
    }
    
    $obfuscationDuration = (Get-Date) - $obfuscationStartTime
    Write-Success "混淆完成 (耗时: $($obfuscationDuration.TotalSeconds) 秒) / Obfuscation completed (duration: $($obfuscationDuration.TotalSeconds) seconds)"
}
else {
    Write-Info "已跳过混淆步骤 / Obfuscation step skipped"
}

# 步骤4: 执行测试（如果不跳过）/ Step 4: Perform tests (if not skipped)
if (-not $SkipTests) {
    Write-Info "正在执行混淆测试... / Performing obfuscation tests..."
    $testStartTime = Get-Date
    
    # 测试原始程序集 / Test original assemblies
    Write-Info "测试原始程序集... / Testing original assemblies..."
    & (Join-Path $scriptDir "test-obfuscation.ps1") -BuildOutputPath $buildOutputDir -UseObfuscated:$false
    
    if (-not $SkipObfuscation) {
        # 测试混淆程序集 / Test obfuscated assemblies
        Write-Info "测试混淆程序集... / Testing obfuscated assemblies..."
        & (Join-Path $scriptDir "test-obfuscation.ps1") -BuildOutputPath $buildOutputDir -UseObfuscated:$true
    }
    
    $testDuration = (Get-Date) - $testStartTime
    Write-Success "测试完成 (耗时: $($testDuration.TotalSeconds) 秒) / Tests completed (duration: $($testDuration.TotalSeconds) seconds)"
}
else {
    Write-Info "已跳过测试步骤 / Test step skipped"
}

# 计算总时间 / Calculate total time
$totalDuration = (Get-Date) - $startTime
Write-Success "====== 混淆构建流程完成 (总耗时: $($totalDuration.TotalMinutes) 分钟) / Obfuscation build process completed (total duration: $($totalDuration.TotalMinutes) minutes) ======" 