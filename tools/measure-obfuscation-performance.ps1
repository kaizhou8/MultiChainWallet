# 混淆性能测量脚本
# Obfuscation Performance Measurement Script

param (
    [Parameter(Mandatory=$false)]
    [string]$ProjectDir = (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)),
    
    [Parameter(Mandatory=$false)]
    [int]$IterationCount = 10,
    
    [Parameter(Mandatory=$false)]
    [switch]$GenerateReport = $true
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
Write-Info "====== 混淆性能测量工具 ======"
Write-Info "====== Obfuscation Performance Measurement Tool ======"
Write-Info "项目目录 / Project directory: $ProjectDir"
Write-Info "迭代次数 / Iteration count: $IterationCount"
Write-Info "生成报告 / Generate report: $GenerateReport"

# 设置路径
# Set paths
$buildDir = Join-Path $ProjectDir "bin\Release\net8.0"
$confusedDir = Join-Path $ProjectDir "Confused"
$reportDir = Join-Path $ProjectDir "PerformanceReports"
$reportPath = Join-Path $reportDir "ObfuscationPerformanceReport_$(Get-Date -Format 'yyyyMMdd_HHmmss').md"

# 检查目录
# Check directories
if (-not (Test-Path $buildDir)) {
    Write-Error "构建目录不存在: $buildDir / Build directory does not exist: $buildDir"
    exit 1
}

if (-not (Test-Path $confusedDir)) {
    Write-Error "混淆目录不存在: $confusedDir / Confused directory does not exist: $confusedDir"
    exit 1
}

if ($GenerateReport -and -not (Test-Path $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    Write-Info "创建报告目录: $reportDir / Created report directory: $reportDir"
}

# 定义测试的程序集
# Define assemblies to test
$assemblies = @(
    "MultiChainWallet.Core.dll",
    "MultiChainWallet.Infrastructure.dll"
)

# 定义测试场景
# Define test scenarios
$testScenarios = @(
    @{
        Name = "加载程序集 / Load Assembly";
        Function = {
            param($assemblyPath)
            $assembly = [System.Reflection.Assembly]::LoadFile($assemblyPath)
            return $assembly
        }
    },
    @{
        Name = "枚举类型 / Enumerate Types";
        Function = {
            param($assemblyPath)
            $assembly = [System.Reflection.Assembly]::LoadFile($assemblyPath)
            $types = $assembly.GetTypes()
            return $types.Count
        }
    },
    @{
        Name = "反射方法调用 / Reflection Method Call";
        Function = {
            param($assemblyPath)
            $assembly = [System.Reflection.Assembly]::LoadFile($assemblyPath)
            $securityType = $assembly.GetTypes() | Where-Object { $_.Name -like "*Security*Service" } | Select-Object -First 1
            if ($securityType) {
                $method = $securityType.GetMethods() | Where-Object { $_.Name -like "*Generate*" -or $_.Name -like "*Create*" } | Select-Object -First 1
                if ($method -and $method.GetParameters().Count -eq 0) {
                    $instance = [Activator]::CreateInstance($securityType)
                    $result = $method.Invoke($instance, $null)
                    return $result
                }
            }
            return $null
        }
    }
)

# 创建结果存储
# Create result storage
$results = @{}

# 运行性能测试
# Run performance tests
foreach ($assembly in $assemblies) {
    $originalPath = Join-Path $buildDir $assembly
    $obfuscatedPath = Join-Path $confusedDir $assembly
    
    if (-not (Test-Path $originalPath)) {
        Write-Warning "原始程序集不存在: $originalPath / Original assembly does not exist: $originalPath"
        continue
    }
    
    if (-not (Test-Path $obfuscatedPath)) {
        Write-Warning "混淆程序集不存在: $obfuscatedPath / Obfuscated assembly does not exist: $obfuscatedPath"
        continue
    }
    
    Write-Info "===== 测试程序集: $assembly / Testing assembly: $assembly ====="
    $results[$assembly] = @{}
    
    foreach ($scenario in $testScenarios) {
        Write-Info "--- 场景: $($scenario.Name) / Scenario: $($scenario.Name) ---"
        $results[$assembly][$scenario.Name] = @{
            Original = @{
                Times = @()
                Average = 0
                Min = 0
                Max = 0
            }
            Obfuscated = @{
                Times = @()
                Average = 0
                Min = 0
                Max = 0
            }
            Difference = 0
            PercentageDifference = 0
        }
        
        # 测试原始程序集
        # Test original assembly
        Write-Info "测试原始程序集... / Testing original assembly..."
        for ($i = 1; $i -le $IterationCount; $i++) {
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $scenario.Function.Invoke($originalPath) | Out-Null
            $sw.Stop()
            $results[$assembly][$scenario.Name].Original.Times += $sw.ElapsedMilliseconds
            Write-Host "  迭代 $i / Iteration $i: $($sw.ElapsedMilliseconds) ms" -ForegroundColor Gray
        }
        
        # 计算原始程序集统计数据
        # Calculate original assembly statistics
        $originalTimes = $results[$assembly][$scenario.Name].Original.Times
        $results[$assembly][$scenario.Name].Original.Average = ($originalTimes | Measure-Object -Average).Average
        $results[$assembly][$scenario.Name].Original.Min = ($originalTimes | Measure-Object -Minimum).Minimum
        $results[$assembly][$scenario.Name].Original.Max = ($originalTimes | Measure-Object -Maximum).Maximum
        
        # 测试混淆程序集
        # Test obfuscated assembly
        Write-Info "测试混淆程序集... / Testing obfuscated assembly..."
        for ($i = 1; $i -le $IterationCount; $i++) {
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $scenario.Function.Invoke($obfuscatedPath) | Out-Null
            $sw.Stop()
            $results[$assembly][$scenario.Name].Obfuscated.Times += $sw.ElapsedMilliseconds
            Write-Host "  迭代 $i / Iteration $i: $($sw.ElapsedMilliseconds) ms" -ForegroundColor Gray
        }
        
        # 计算混淆程序集统计数据
        # Calculate obfuscated assembly statistics
        $obfuscatedTimes = $results[$assembly][$scenario.Name].Obfuscated.Times
        $results[$assembly][$scenario.Name].Obfuscated.Average = ($obfuscatedTimes | Measure-Object -Average).Average
        $results[$assembly][$scenario.Name].Obfuscated.Min = ($obfuscatedTimes | Measure-Object -Minimum).Minimum
        $results[$assembly][$scenario.Name].Obfuscated.Max = ($obfuscatedTimes | Measure-Object -Maximum).Maximum
        
        # 计算差异
        # Calculate difference
        $originalAvg = $results[$assembly][$scenario.Name].Original.Average
        $obfuscatedAvg = $results[$assembly][$scenario.Name].Obfuscated.Average
        $results[$assembly][$scenario.Name].Difference = $obfuscatedAvg - $originalAvg
        $results[$assembly][$scenario.Name].PercentageDifference = if ($originalAvg -ne 0) { ($obfuscatedAvg - $originalAvg) / $originalAvg * 100 } else { 0 }
        
        # 显示结果
        # Display results
        Write-Info "结果 / Results:"
        Write-Info "  原始平均时间 / Original average time: $($results[$assembly][$scenario.Name].Original.Average) ms"
        Write-Info "  混淆平均时间 / Obfuscated average time: $($results[$assembly][$scenario.Name].Obfuscated.Average) ms"
        Write-Info "  差异 / Difference: $($results[$assembly][$scenario.Name].Difference) ms"
        Write-Info "  百分比差异 / Percentage difference: $($results[$assembly][$scenario.Name].PercentageDifference)%"
        
        if ($results[$assembly][$scenario.Name].PercentageDifference -gt 20) {
            Write-Warning "  性能下降显著 (>20%) / Performance degradation is significant (>20%)"
        } elseif ($results[$assembly][$scenario.Name].PercentageDifference -gt 10) {
            Write-Warning "  性能下降中等 (>10%) / Performance degradation is moderate (>10%)"
        } elseif ($results[$assembly][$scenario.Name].PercentageDifference -gt 5) {
            Write-Info "  性能下降轻微 (>5%) / Performance degradation is minor (>5%)"
        } else {
            Write-Success "  性能影响可忽略 (<5%) / Performance impact is negligible (<5%)"
        }
    }
}

# 生成报告
# Generate report
if ($GenerateReport) {
    Write-Info "正在生成性能报告... / Generating performance report..."
    
    $reportContent = @"
# 混淆性能影响报告
# Obfuscation Performance Impact Report

**生成日期 / Generated on:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## 测试环境
## Test Environment

- **操作系统 / Operating System:** $(Get-CimInstance Win32_OperatingSystem | Select-Object -ExpandProperty Caption)
- **处理器 / Processor:** $(Get-CimInstance Win32_Processor | Select-Object -ExpandProperty Name)
- **内存 / Memory:** $([math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)) GB
- **迭代次数 / Iteration Count:** $IterationCount

## 测试结果摘要
## Test Results Summary

| 程序集 / Assembly | 场景 / Scenario | 原始 / Original (ms) | 混淆 / Obfuscated (ms) | 差异 / Difference (ms) | 差异百分比 / Difference (%) | 影响级别 / Impact Level |
|------------------|----------------|---------------------|----------------------|----------------------|---------------------------|------------------------|
"@
    
    foreach ($assembly in $results.Keys) {
        foreach ($scenario in $results[$assembly].Keys) {
            $originalAvg = [math]::Round($results[$assembly][$scenario].Original.Average, 2)
            $obfuscatedAvg = [math]::Round($results[$assembly][$scenario].Obfuscated.Average, 2)
            $difference = [math]::Round($results[$assembly][$scenario].Difference, 2)
            $percentageDifference = [math]::Round($results[$assembly][$scenario].PercentageDifference, 2)
            
            $impactLevel = "可忽略 / Negligible"
            if ($percentageDifference -gt 20) {
                $impactLevel = "显著 / Significant"
            } elseif ($percentageDifference -gt 10) {
                $impactLevel = "中等 / Moderate"
            } elseif ($percentageDifference -gt 5) {
                $impactLevel = "轻微 / Minor"
            }
            
            $reportContent += "`n| $assembly | $scenario | $originalAvg | $obfuscatedAvg | $difference | $percentageDifference% | $impactLevel |"
        }
    }
    
    $reportContent += @"

## 详细测试结果
## Detailed Test Results

"@
    
    foreach ($assembly in $results.Keys) {
        $reportContent += @"

### $assembly

"@
        
        foreach ($scenario in $results[$assembly].Keys) {
            $reportContent += @"

#### $scenario

- **原始版本 / Original Version:**
  - 平均时间 / Average time: $([math]::Round($results[$assembly][$scenario].Original.Average, 2)) ms
  - 最小时间 / Minimum time: $([math]::Round($results[$assembly][$scenario].Original.Min, 2)) ms
  - 最大时间 / Maximum time: $([math]::Round($results[$assembly][$scenario].Original.Max, 2)) ms

- **混淆版本 / Obfuscated Version:**
  - 平均时间 / Average time: $([math]::Round($results[$assembly][$scenario].Obfuscated.Average, 2)) ms
  - 最小时间 / Minimum time: $([math]::Round($results[$assembly][$scenario].Obfuscated.Min, 2)) ms
  - 最大时间 / Maximum time: $([math]::Round($results[$assembly][$scenario].Obfuscated.Max, 2)) ms

- **性能影响 / Performance Impact:**
  - 绝对差异 / Absolute difference: $([math]::Round($results[$assembly][$scenario].Difference, 2)) ms
  - 相对差异 / Relative difference: $([math]::Round($results[$assembly][$scenario].PercentageDifference, 2))%

"@
        }
    }
    
    $reportContent += @"

## 结论与建议
## Conclusions and Recommendations

根据上述测试结果，可以得出以下结论：
Based on the test results above, the following conclusions can be drawn:

"@
    
    # 添加自动生成的结论
    # Add automatically generated conclusions
    $significantImpacts = 0
    $moderateImpacts = 0
    $minorImpacts = 0
    $negligibleImpacts = 0
    
    foreach ($assembly in $results.Keys) {
        foreach ($scenario in $results[$assembly].Keys) {
            $percentageDifference = $results[$assembly][$scenario].PercentageDifference
            if ($percentageDifference -gt 20) {
                $significantImpacts++
            } elseif ($percentageDifference -gt 10) {
                $moderateImpacts++
            } elseif ($percentageDifference -gt 5) {
                $minorImpacts++
            } else {
                $negligibleImpacts++
            }
        }
    }
    
    $totalScenarios = $significantImpacts + $moderateImpacts + $minorImpacts + $negligibleImpacts
    
    $reportContent += @"

1. **总体性能影响 / Overall Performance Impact:**
   - 显著影响场景 / Scenarios with significant impact: $significantImpacts ($([math]::Round($significantImpacts / $totalScenarios * 100, 1))%)
   - 中等影响场景 / Scenarios with moderate impact: $moderateImpacts ($([math]::Round($moderateImpacts / $totalScenarios * 100, 1))%)
   - 轻微影响场景 / Scenarios with minor impact: $minorImpacts ($([math]::Round($minorImpacts / $totalScenarios * 100, 1))%)
   - 可忽略影响场景 / Scenarios with negligible impact: $negligibleImpacts ($([math]::Round($negligibleImpacts / $totalScenarios * 100, 1))%)

"@
    
    if ($significantImpacts -gt 0) {
        $reportContent += @"
2. **需要优化的关键领域 / Key Areas Requiring Optimization:**
   - 反射操作性能 / Reflection operation performance
   - 类型加载时间 / Type loading time
   - 方法调用开销 / Method invocation overhead

3. **建议的优化措施 / Recommended Optimization Measures:**
   - 调整混淆配置，减少对关键性能路径的控制流混淆 / Adjust obfuscation configuration to reduce control flow obfuscation on critical performance paths
   - 考虑对频繁访问的方法使用更轻量级的混淆 / Consider using lighter obfuscation for frequently accessed methods
   - 实现缓存机制减少反射操作 / Implement caching mechanisms to reduce reflection operations
   - 预加载关键类型减少运行时延迟 / Preload critical types to reduce runtime delays

"@
    } elseif ($moderateImpacts -gt 0) {
        $reportContent += @"
2. **建议的优化措施 / Recommended Optimization Measures:**
   - 监控生产环境中的应用性能 / Monitor application performance in production environment
   - 考虑对性能敏感的操作使用更轻量级的混淆 / Consider using lighter obfuscation for performance-sensitive operations
   - 实现选择性缓存以减少反射开销 / Implement selective caching to reduce reflection overhead

"@
    } else {
        $reportContent += @"
2. **结论 / Conclusion:**
   - 混淆对应用性能的影响在可接受范围内 / The impact of obfuscation on application performance is within acceptable range
   - 无需进行特定的性能优化 / No specific performance optimizations are required
   - 建议定期监控应用性能以确保长期稳定性 / Regular monitoring of application performance is recommended to ensure long-term stability

"@
    }
    
    # 写入报告文件
    # Write report file
    $reportContent | Out-File -FilePath $reportPath -Encoding utf8
    Write-Success "性能报告已生成: $reportPath / Performance report has been generated: $reportPath"
}

Write-Success "====== 性能测量完成 / Performance measurement completed ======" 