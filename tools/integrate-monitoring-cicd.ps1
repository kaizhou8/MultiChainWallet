# 混淆性能监控CI/CD集成脚本
# Obfuscation Performance Monitoring CI/CD Integration Script

param (
    [Parameter(Mandatory=$false)]
    [string]$ProjectDir = (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)),
    
    [Parameter(Mandatory=$false)]
    [string]$BuildConfiguration = "Release",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$AutoOptimize = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$NotificationEmail = "",
    
    [Parameter(Mandatory=$false)]
    [int]$WarningThreshold = 10,
    
    [Parameter(Mandatory=$false)]
    [int]$CriticalThreshold = 20
)

# 设置颜色输出函数
# Set color output functions
function Write-ColorOutput($message, $color) {
    Write-Host $message -ForegroundColor $color
}

function Write-Success($message) { Write-ColorOutput $message "Green" }
function Write-Info($message) { Write-ColorOutput $message "Cyan" }
function Write-Warning($message) { Write-ColorOutput $message "Yellow" }
function Write-Error($message) { Write-ColorOutput $message "Red" }

# 显示脚本开始信息
# Display script start information
Write-Info "====== 混淆性能监控CI/CD集成工具 ======"
Write-Info "====== Obfuscation Performance Monitoring CI/CD Integration Tool ======"
Write-Info "项目目录 / Project directory: $ProjectDir"
Write-Info "构建配置 / Build configuration: $BuildConfiguration"
Write-Info "跳过构建 / Skip build: $SkipBuild"
Write-Info "跳过测试 / Skip tests: $SkipTests"
Write-Info "自动优化 / Auto optimize: $AutoOptimize"
Write-Info "警告阈值 / Warning threshold: $WarningThreshold%"
Write-Info "严重阈值 / Critical threshold: $CriticalThreshold%"

# 设置路径
# Set paths
$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildScript = Join-Path $toolsDir "build-with-obfuscation.ps1"
$monitorScript = Join-Path $toolsDir "continuous-obfuscation-monitor.ps1"
$historyDir = Join-Path $ProjectDir "PerformanceHistory"
$artifactsDir = if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) { $env:BUILD_ARTIFACTSTAGINGDIRECTORY } else { Join-Path $ProjectDir "artifacts" }

# 创建构件目录（如果不存在）
# Create artifacts directory if it doesn't exist
if (-not (Test-Path $artifactsDir)) {
    New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null
    Write-Info "创建构件目录: $artifactsDir / Created artifacts directory: $artifactsDir"
}

# 步骤1：构建和混淆
# Step 1: Build and obfuscate
if (-not $SkipBuild) {
    Write-Info "步骤1：构建和混淆 / Step 1: Build and obfuscate"
    
    $buildParams = @{
        ProjectDir = $ProjectDir
        Configuration = $BuildConfiguration
    }
    
    if ($SkipTests) {
        $buildParams.Add("SkipTests", $true)
    }
    
    & $buildScript @buildParams
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "构建失败，退出代码: $LASTEXITCODE / Build failed with exit code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    
    Write-Success "构建和混淆完成 / Build and obfuscation completed"
}
else {
    Write-Info "跳过构建步骤 / Skipping build step"
}

# 步骤2：运行性能监控
# Step 2: Run performance monitoring
Write-Info "步骤2：运行性能监控 / Step 2: Run performance monitoring"

$monitorParams = @{
    ProjectDir = $ProjectDir
    HistoryDir = $historyDir
    WarningThreshold = $WarningThreshold
    CriticalThreshold = $CriticalThreshold
    NotifyCritical = $true
}

if ($AutoOptimize) {
    $monitorParams.Add("AutoOptimize", $true)
}

if ($NotificationEmail) {
    $monitorParams.Add("NotificationEmail", $NotificationEmail)
}

& $monitorScript @monitorParams

if ($LASTEXITCODE -ne 0) {
    Write-Error "性能监控失败，退出代码: $LASTEXITCODE / Performance monitoring failed with exit code: $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Success "性能监控完成 / Performance monitoring completed"

# 步骤3：收集性能报告作为构件
# Step 3: Collect performance reports as artifacts
Write-Info "步骤3：收集性能报告作为构件 / Step 3: Collect performance reports as artifacts"

$reportsDir = Join-Path $ProjectDir "PerformanceReports"
$monitoringReportsDir = $historyDir

# 创建报告目录（如果不存在）
# Create reports directory if it doesn't exist
$artifactReportsDir = Join-Path $artifactsDir "PerformanceReports"
if (-not (Test-Path $artifactReportsDir)) {
    New-Item -ItemType Directory -Path $artifactReportsDir -Force | Out-Null
}

# 复制最新的性能报告
# Copy the latest performance reports
Get-ChildItem -Path $reportsDir -Filter "ObfuscationPerformanceReport_*.md" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1 | 
    ForEach-Object {
        $destPath = Join-Path $artifactReportsDir $_.Name
        Copy-Item -Path $_.FullName -Destination $destPath -Force
        Write-Info "复制性能报告: $($_.Name) / Copied performance report: $($_.Name)"
    }

# 复制最新的监控报告
# Copy the latest monitoring reports
Get-ChildItem -Path $monitoringReportsDir -Filter "MonitoringReport_*.md" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1 | 
    ForEach-Object {
        $destPath = Join-Path $artifactReportsDir $_.Name
        Copy-Item -Path $_.FullName -Destination $destPath -Force
        Write-Info "复制监控报告: $($_.Name) / Copied monitoring report: $($_.Name)"
    }

# 复制监控日志
# Copy monitoring log
$logFile = Join-Path $monitoringReportsDir "monitoring-log.md"
if (Test-Path $logFile) {
    $destPath = Join-Path $artifactReportsDir "monitoring-log.md"
    Copy-Item -Path $logFile -Destination $destPath -Force
    Write-Info "复制监控日志 / Copied monitoring log"
}

Write-Success "性能报告已收集为构件 / Performance reports collected as artifacts"

# 步骤4：在CI/CD环境中发布构件
# Step 4: Publish artifacts in CI/CD environment
if ($env:TF_BUILD -eq "True") {
    Write-Info "步骤4：在Azure DevOps中发布构件 / Step 4: Publishing artifacts in Azure DevOps"
    
    # Azure DevOps特定命令
    # Azure DevOps specific commands
    Write-Host "##vso[artifact.upload containerfolder=PerformanceReports;artifactname=PerformanceReports]$artifactReportsDir"
    
    # 如果有性能警告或严重问题，添加构建警告
    # Add build warning if there are performance warnings or critical issues
    $latestMonitoringReport = Get-ChildItem -Path $monitoringReportsDir -Filter "MonitoringReport_*.md" | 
                             Sort-Object LastWriteTime -Descending | 
                             Select-Object -First 1
    
    if ($latestMonitoringReport) {
        $reportContent = Get-Content -Path $latestMonitoringReport.FullName -Raw
        
        if ($reportContent -match "状态 / Status:\s*❌ 严重 / Critical") {
            Write-Host "##vso[task.logissue type=warning;]检测到严重性能问题，请查看性能报告 / Critical performance issues detected, please check performance reports"
        }
        elseif ($reportContent -match "状态 / Status:\s*⚠️ 警告 / Warning") {
            Write-Host "##vso[task.logissue type=warning;]检测到性能警告，请查看性能报告 / Performance warnings detected, please check performance reports"
        }
    }
}
elseif ($env:GITHUB_ACTIONS -eq "true") {
    Write-Info "步骤4：在GitHub Actions中发布构件 / Step 4: Publishing artifacts in GitHub Actions"
    
    # GitHub Actions特定命令
    # GitHub Actions specific commands
    Write-Host "::set-output name=performance_reports_path::$artifactReportsDir"
    
    # 如果有性能警告或严重问题，添加工作流警告
    # Add workflow warning if there are performance warnings or critical issues
    $latestMonitoringReport = Get-ChildItem -Path $monitoringReportsDir -Filter "MonitoringReport_*.md" | 
                             Sort-Object LastWriteTime -Descending | 
                             Select-Object -First 1
    
    if ($latestMonitoringReport) {
        $reportContent = Get-Content -Path $latestMonitoringReport.FullName -Raw
        
        if ($reportContent -match "状态 / Status:\s*❌ 严重 / Critical") {
            Write-Host "::warning::检测到严重性能问题，请查看性能报告 / Critical performance issues detected, please check performance reports"
        }
        elseif ($reportContent -match "状态 / Status:\s*⚠️ 警告 / Warning") {
            Write-Host "::warning::检测到性能警告，请查看性能报告 / Performance warnings detected, please check performance reports"
        }
    }
}
else {
    Write-Info "步骤4：在本地环境中收集构件 / Step 4: Collecting artifacts in local environment"
    Write-Info "构件已保存到: $artifactReportsDir / Artifacts saved to: $artifactReportsDir"
}

Write-Success "====== 混淆性能监控CI/CD集成完成 ======"
Write-Success "====== Obfuscation Performance Monitoring CI/CD Integration Completed ======" 