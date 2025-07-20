# 混淆代码持续监控与优化脚本
# Continuous Monitoring and Optimization Script for Obfuscated Code

param (
    [Parameter(Mandatory=$false)]
    [string]$ProjectDir = (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)),
    
    [Parameter(Mandatory=$false)]
    [string]$HistoryDir = (Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "PerformanceHistory"),
    
    [Parameter(Mandatory=$false)]
    [int]$WarningThreshold = 10,
    
    [Parameter(Mandatory=$false)]
    [int]$CriticalThreshold = 20,
    
    [Parameter(Mandatory=$false)]
    [switch]$AutoOptimize = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$NotifyCritical = $true,
    
    [Parameter(Mandatory=$false)]
    [string]$NotificationEmail = ""
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
Write-Info "====== 混淆代码持续监控与优化工具 ======"
Write-Info "====== Continuous Monitoring and Optimization Tool for Obfuscated Code ======"
Write-Info "项目目录 / Project directory: $ProjectDir"
Write-Info "历史数据目录 / History directory: $HistoryDir"
Write-Info "警告阈值 / Warning threshold: $WarningThreshold%"
Write-Info "严重阈值 / Critical threshold: $CriticalThreshold%"
Write-Info "自动优化 / Auto optimize: $AutoOptimize"
Write-Info "严重问题通知 / Critical notification: $NotifyCritical"

# 创建历史数据目录（如果不存在）
# Create history directory if it doesn't exist
if (-not (Test-Path $HistoryDir)) {
    New-Item -ItemType Directory -Path $HistoryDir -Force | Out-Null
    Write-Info "创建历史数据目录: $HistoryDir / Created history directory: $HistoryDir"
}

# 设置日志文件路径
# Set log file path
$logFile = Join-Path $HistoryDir "monitoring-log.md"
if (-not (Test-Path $logFile)) {
    @"
# 混淆性能监控日志
# Obfuscation Performance Monitoring Log

| 日期 / Date | 构建ID / Build ID | 状态 / Status | 详情 / Details |
|------------|-----------------|--------------|---------------|
"@ | Out-File -FilePath $logFile -Encoding utf8
}

# 获取当前构建ID（CI/CD环境或本地时间戳）
# Get current build ID (CI/CD environment or local timestamp)
$buildId = if ($env:BUILD_BUILDID) { $env:BUILD_BUILDID } else { Get-Date -Format "yyyyMMdd_HHmmss" }

# 运行性能测量脚本并获取结果
# Run performance measurement script and get results
Write-Info "运行性能测量... / Running performance measurement..."
$measurementScript = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "measure-obfuscation-performance.ps1"
$reportDir = Join-Path $ProjectDir "PerformanceReports"
$currentReportPath = Join-Path $reportDir "ObfuscationPerformanceReport_$(Get-Date -Format 'yyyyMMdd_HHmmss').md"

# 执行性能测量脚本
# Execute performance measurement script
& $measurementScript -ProjectDir $ProjectDir -IterationCount 5 -GenerateReport

# 检查最新的报告文件
# Check the latest report file
$latestReport = Get-ChildItem -Path $reportDir -Filter "ObfuscationPerformanceReport_*.md" | 
                Sort-Object LastWriteTime -Descending | 
                Select-Object -First 1

if (-not $latestReport) {
    Write-Error "未找到性能报告 / Performance report not found"
    exit 1
}

# 复制最新报告到历史目录
# Copy latest report to history directory
$historicalReportPath = Join-Path $HistoryDir "ObfuscationPerformanceReport_$buildId.md"
Copy-Item -Path $latestReport.FullName -Destination $historicalReportPath
Write-Info "性能报告已保存到历史目录: $historicalReportPath / Performance report saved to history directory: $historicalReportPath"

# 解析报告以获取性能数据
# Parse report to get performance data
$reportContent = Get-Content -Path $latestReport.FullName -Raw
$performanceData = @{}

# 提取性能数据
# Extract performance data
$tablePattern = '(?s)\| 程序集 \/ Assembly \| 场景 \/ Scenario \| 原始 \/ Original \(ms\) \| 混淆 \/ Obfuscated \(ms\) \| 差异 \/ Difference \(ms\) \| 差异百分比 \/ Difference \(%\) \| 影响级别 \/ Impact Level \|(.*?)##'
$tableMatch = [regex]::Match($reportContent, $tablePattern)

if ($tableMatch.Success) {
    $tableContent = $tableMatch.Groups[1].Value
    $rows = $tableContent -split '\r?\n' | Where-Object { $_ -match '\|' -and $_ -notmatch '\|---' }
    
    foreach ($row in $rows) {
        $columns = $row -split '\|' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
        if ($columns.Count -ge 7) {
            $assembly = $columns[0]
            $scenario = $columns[1]
            $original = [double]($columns[2] -replace ' ms', '')
            $obfuscated = [double]($columns[3] -replace ' ms', '')
            $difference = [double]($columns[4] -replace ' ms', '')
            $percentageDifference = [double]($columns[5] -replace '%', '')
            $impactLevel = $columns[6]
            
            $key = "$assembly|$scenario"
            $performanceData[$key] = @{
                Assembly = $assembly
                Scenario = $scenario
                Original = $original
                Obfuscated = $obfuscated
                Difference = $difference
                PercentageDifference = $percentageDifference
                ImpactLevel = $impactLevel
            }
        }
    }
}

# 获取历史性能数据
# Get historical performance data
$historicalData = @{}
$historicalReports = Get-ChildItem -Path $HistoryDir -Filter "ObfuscationPerformanceReport_*.md" | 
                     Sort-Object LastWriteTime | 
                     Select-Object -Last 10 -SkipLast 1  # 最近10个报告，不包括当前报告

foreach ($report in $historicalReports) {
    $reportId = ($report.Name -replace 'ObfuscationPerformanceReport_', '' -replace '\.md', '')
    $reportContent = Get-Content -Path $report.FullName -Raw
    
    $tableMatch = [regex]::Match($reportContent, $tablePattern)
    if ($tableMatch.Success) {
        $tableContent = $tableMatch.Groups[1].Value
        $rows = $tableContent -split '\r?\n' | Where-Object { $_ -match '\|' -and $_ -notmatch '\|---' }
        
        foreach ($row in $rows) {
            $columns = $row -split '\|' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
            if ($columns.Count -ge 7) {
                $assembly = $columns[0]
                $scenario = $columns[1]
                $percentageDifference = [double]($columns[5] -replace '%', '')
                
                $key = "$assembly|$scenario"
                if (-not $historicalData.ContainsKey($key)) {
                    $historicalData[$key] = @()
                }
                
                $historicalData[$key] += $percentageDifference
            }
        }
    }
}

# 分析性能趋势
# Analyze performance trends
$performanceTrends = @{}
$criticalScenarios = @()
$warningScenarios = @()

foreach ($key in $performanceData.Keys) {
    $currentPerformance = $performanceData[$key]
    $trend = "稳定 / Stable"
    $trendValue = 0
    
    if ($historicalData.ContainsKey($key) -and $historicalData[$key].Count -gt 0) {
        $historicalAvg = ($historicalData[$key] | Measure-Object -Average).Average
        $trendValue = $currentPerformance.PercentageDifference - $historicalAvg
        
        if ($trendValue -gt 5) {
            $trend = "恶化 / Degrading"
        } elseif ($trendValue -lt -5) {
            $trend = "改善 / Improving"
        }
    }
    
    $performanceTrends[$key] = @{
        Assembly = $currentPerformance.Assembly
        Scenario = $currentPerformance.Scenario
        CurrentPerformance = $currentPerformance.PercentageDifference
        HistoricalAverage = if ($historicalData.ContainsKey($key)) { ($historicalData[$key] | Measure-Object -Average).Average } else { $null }
        Trend = $trend
        TrendValue = $trendValue
    }
    
    # 检查是否超过阈值
    # Check if thresholds are exceeded
    if ($currentPerformance.PercentageDifference -ge $CriticalThreshold) {
        $criticalScenarios += $key
    } elseif ($currentPerformance.PercentageDifference -ge $WarningThreshold) {
        $warningScenarios += $key
    }
}

# 生成监控报告
# Generate monitoring report
$monitoringReportPath = Join-Path $HistoryDir "MonitoringReport_$buildId.md"
$status = "✅ 正常 / Normal"

if ($criticalScenarios.Count -gt 0) {
    $status = "❌ 严重 / Critical"
} elseif ($warningScenarios.Count -gt 0) {
    $status = "⚠️ 警告 / Warning"
}

$monitoringReport = @"
# 混淆性能监控报告
# Obfuscation Performance Monitoring Report

**构建ID / Build ID:** $buildId
**日期 / Date:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**状态 / Status:** $status

## 性能趋势摘要
## Performance Trend Summary

| 程序集 / Assembly | 场景 / Scenario | 当前性能差异 / Current Performance Difference | 历史平均差异 / Historical Average Difference | 趋势 / Trend | 趋势值 / Trend Value |
|------------------|----------------|-------------------------------------------|---------------------------------------------|-------------|-------------------|
"@

foreach ($key in $performanceTrends.Keys) {
    $trend = $performanceTrends[$key]
    $historicalAvg = if ($trend.HistoricalAverage -ne $null) { [math]::Round($trend.HistoricalAverage, 2) } else { "N/A" }
    $trendValue = if ($trend.TrendValue -ne $null) { [math]::Round($trend.TrendValue, 2) } else { "N/A" }
    
    $monitoringReport += "`n| $($trend.Assembly) | $($trend.Scenario) | $([math]::Round($trend.CurrentPerformance, 2))% | $historicalAvg% | $($trend.Trend) | $trendValue% |"
}

if ($criticalScenarios.Count -gt 0) {
    $monitoringReport += @"

## 严重性能问题
## Critical Performance Issues

以下场景的性能差异超过了严重阈值 ($CriticalThreshold%):
The following scenarios have performance differences exceeding the critical threshold ($CriticalThreshold%):

"@

    foreach ($key in $criticalScenarios) {
        $scenario = $performanceData[$key]
        $monitoringReport += "`n- **$($scenario.Assembly)** - $($scenario.Scenario): $([math]::Round($scenario.PercentageDifference, 2))%"
    }
    
    $monitoringReport += @"

### 建议的优化措施
### Recommended Optimization Measures

1. 检查最近的代码更改，特别是与上述场景相关的更改
   Check recent code changes, especially those related to the scenarios above
2. 考虑调整混淆配置，减少对这些场景的混淆强度
   Consider adjusting obfuscation configuration to reduce obfuscation intensity for these scenarios
3. 实现或改进缓存机制，特别是对于反射操作
   Implement or improve caching mechanisms, especially for reflection operations
4. 使用 `ReflectionHelper` 类优化反射操作
   Use the `ReflectionHelper` class to optimize reflection operations
5. 考虑将这些场景添加到混淆排除列表中
   Consider adding these scenarios to the obfuscation exclusion list
"@
}

if ($warningScenarios.Count -gt 0 -and $criticalScenarios.Count -eq 0) {
    $monitoringReport += @"

## 警告性能问题
## Warning Performance Issues

以下场景的性能差异超过了警告阈值 ($WarningThreshold%):
The following scenarios have performance differences exceeding the warning threshold ($WarningThreshold%):

"@

    foreach ($key in $warningScenarios) {
        $scenario = $performanceData[$key]
        $monitoringReport += "`n- **$($scenario.Assembly)** - $($scenario.Scenario): $([math]::Round($scenario.PercentageDifference, 2))%"
    }
    
    $monitoringReport += @"

### 建议的监控措施
### Recommended Monitoring Measures

1. 持续监控这些场景的性能趋势
   Continuously monitor performance trends for these scenarios
2. 如果趋势恶化，考虑实施优化措施
   If the trend worsens, consider implementing optimization measures
3. 检查是否有用户报告与这些场景相关的性能问题
   Check if users report performance issues related to these scenarios
"@
}

$monitoringReport += @"

## 历史性能趋势
## Historical Performance Trends

"@

# 添加趋势图数据（用于后续可视化）
# Add trend chart data (for later visualization)
foreach ($key in $performanceTrends.Keys) {
    $trend = $performanceTrends[$key]
    if ($historicalData.ContainsKey($key) -and $historicalData[$key].Count -gt 0) {
        $monitoringReport += @"

### $($trend.Assembly) - $($trend.Scenario)

历史性能差异数据点:
Historical performance difference data points:
```
$($historicalData[$key] -join ", "), $([math]::Round($trend.CurrentPerformance, 2))
```

"@
    }
}

# 保存监控报告
# Save monitoring report
$monitoringReport | Out-File -FilePath $monitoringReportPath -Encoding utf8
Write-Info "监控报告已保存: $monitoringReportPath / Monitoring report saved: $monitoringReportPath"

# 更新日志
# Update log
$logEntry = "| $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') | $buildId | $status | [详情 / Details]($monitoringReportPath) |"
$logEntry | Out-File -FilePath $logFile -Encoding utf8 -Append
Write-Info "日志已更新 / Log updated"

# 如果有严重问题并启用了自动优化，则运行优化脚本
# If there are critical issues and auto-optimize is enabled, run optimization script
if ($criticalScenarios.Count -gt 0 -and $AutoOptimize) {
    Write-Warning "检测到严重性能问题，正在运行自动优化... / Critical performance issues detected, running automatic optimization..."
    
    # 创建临时优化配置
    # Create temporary optimization configuration
    $optimizationConfig = @{
        CriticalScenarios = $criticalScenarios | ForEach-Object { 
            $scenario = $performanceData[$_]
            @{
                Assembly = $scenario.Assembly
                Scenario = $scenario.Scenario
                PercentageDifference = $scenario.PercentageDifference
            }
        }
    }
    
    $optimizationConfigPath = Join-Path $HistoryDir "OptimizationConfig_$buildId.json"
    $optimizationConfig | ConvertTo-Json -Depth 5 | Out-File -FilePath $optimizationConfigPath -Encoding utf8
    
    # 运行优化脚本
    # Run optimization script
    $optimizeScript = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "optimize-obfuscation-config.ps1"
    & $optimizeScript -ProjectDir $ProjectDir -ConfigFile "Confusion.crproj" -OptimizedConfigFile "Confusion.optimized.crproj"
    
    Write-Success "自动优化完成 / Automatic optimization completed"
}

# 如果有严重问题并启用了通知，则发送通知
# If there are critical issues and notification is enabled, send notification
if ($criticalScenarios.Count -gt 0 -and $NotifyCritical -and $NotificationEmail) {
    Write-Warning "检测到严重性能问题，正在发送通知... / Critical performance issues detected, sending notification..."
    
    # 在实际环境中，这里可以集成邮件发送或其他通知机制
    # In a real environment, email sending or other notification mechanisms can be integrated here
    Write-Warning "通知功能需要在实际环境中配置 / Notification feature needs to be configured in the actual environment"
    
    # 示例：如果在Azure DevOps中，可以创建工作项
    # Example: If in Azure DevOps, create a work item
    if ($env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI -and $env:SYSTEM_TEAMPROJECT) {
        Write-Warning "可以集成Azure DevOps工作项创建API / Can integrate Azure DevOps work item creation API"
    }
}

# 显示总结
# Display summary
Write-Info ""
Write-Info "====== 监控总结 / Monitoring Summary ======"
Write-Info "状态 / Status: $status"
Write-Info "严重问题数量 / Critical issues count: $($criticalScenarios.Count)"
Write-Info "警告问题数量 / Warning issues count: $($warningScenarios.Count)"
Write-Info "监控报告 / Monitoring report: $monitoringReportPath"
Write-Info "历史数据目录 / History directory: $HistoryDir"

Write-Success "====== 混淆代码持续监控完成 / Continuous monitoring of obfuscated code completed ======" 