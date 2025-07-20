# 混淆配置优化脚本
# Obfuscation Configuration Optimization Script

param (
    [Parameter(Mandatory=$false)]
    [string]$ProjectDir = (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)),
    
    [Parameter(Mandatory=$false)]
    [string]$ConfigFile = "Confusion.crproj",
    
    [Parameter(Mandatory=$false)]
    [string]$OptimizedConfigFile = "Confusion.optimized.crproj"
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
Write-Info "====== 混淆配置优化工具 ======"
Write-Info "====== Obfuscation Configuration Optimization Tool ======"
Write-Info "项目目录 / Project directory: $ProjectDir"
Write-Info "配置文件 / Configuration file: $ConfigFile"
Write-Info "优化后配置文件 / Optimized configuration file: $OptimizedConfigFile"

# 设置路径
# Set paths
$configPath = Join-Path $ProjectDir $ConfigFile
$optimizedConfigPath = Join-Path $ProjectDir $OptimizedConfigFile

# 检查配置文件是否存在
# Check if configuration file exists
if (-not (Test-Path $configPath)) {
    Write-Error "混淆配置文件不存在: $configPath / Obfuscation configuration file does not exist: $configPath"
    exit 1
}

# 读取配置文件
# Read configuration file
try {
    Write-Info "正在读取混淆配置文件... / Reading obfuscation configuration file..."
    [xml]$confusionConfig = Get-Content $configPath -Encoding UTF8
    Write-Success "配置文件读取成功 / Configuration file read successfully"
} catch {
    Write-Error "读取配置文件时出错: $($_.Exception.Message) / Error reading configuration file: $($_.Exception.Message)"
    exit 1
}

# 定义性能敏感的类型和方法模式
# Define performance-sensitive type and method patterns
$performanceSensitivePatterns = @(
    # 核心服务接口
    # Core service interfaces
    "MultiChainWallet.Core.Interfaces.*",
    
    # 频繁使用的服务
    # Frequently used services
    "MultiChainWallet.Core.Services.Wallet*",
    "MultiChainWallet.Core.Services.Transaction*",
    "MultiChainWallet.Core.Services.Balance*",
    
    # 数据模型
    # Data models
    "MultiChainWallet.Core.Models.*",
    
    # UI绑定相关类型
    # UI binding related types
    "MultiChainWallet.UI.ViewModels.*",
    
    # 启动和初始化相关类型
    # Startup and initialization related types
    "MultiChainWallet.*.Startup",
    "MultiChainWallet.*.Bootstrap*",
    "MultiChainWallet.*.Initializer*"
)

# 定义需要保持强保护的安全敏感类型
# Define security-sensitive types that need to maintain strong protection
$securitySensitivePatterns = @(
    "MultiChainWallet.Core.Services.Security*",
    "MultiChainWallet.Core.Services.Crypto*",
    "MultiChainWallet.Core.Services.Authentication*",
    "MultiChainWallet.Infrastructure.Security*",
    "MultiChainWallet.*.PrivateKey*",
    "MultiChainWallet.*.Password*",
    "MultiChainWallet.*.Secret*"
)

# 优化配置
# Optimize configuration
Write-Info "正在优化混淆配置... / Optimizing obfuscation configuration..."

# 创建性能优化规则
# Create performance optimization rules
$performanceRules = @()

foreach ($pattern in $performanceSensitivePatterns) {
    # 检查是否已存在该模式的规则
    # Check if a rule for this pattern already exists
    $existingRule = $confusionConfig.project.rule | Where-Object { $_.pattern -eq $pattern }
    
    if ($existingRule) {
        Write-Info "更新现有规则: $pattern / Updating existing rule: $pattern"
        
        # 修改现有规则
        # Modify existing rule
        $existingRule.preset = "normal"
        
        # 移除控制流混淆和资源加密
        # Remove control flow obfuscation and resource encryption
        $controlFlowProtection = $existingRule.protection | Where-Object { $_.id -eq "ctrl flow" }
        if ($controlFlowProtection) {
            $existingRule.RemoveChild($controlFlowProtection)
        }
        
        $resourcesProtection = $existingRule.protection | Where-Object { $_.id -eq "resources" }
        if ($resourcesProtection) {
            $existingRule.RemoveChild($resourcesProtection)
        }
    } else {
        Write-Info "创建新的性能优化规则: $pattern / Creating new performance optimization rule: $pattern"
        
        # 创建新规则
        # Create new rule
        $newRule = $confusionConfig.CreateElement("rule")
        $newRule.SetAttribute("pattern", $pattern)
        $newRule.SetAttribute("preset", "normal")
        $newRule.SetAttribute("inherit", "false")
        
        # 添加基本保护但排除控制流混淆
        # Add basic protections but exclude control flow obfuscation
        $protections = @("anti ildasm", "constants", "rename", "ref proxy")
        foreach ($protectionId in $protections) {
            $protection = $confusionConfig.CreateElement("protection")
            $protection.SetAttribute("id", $protectionId)
            $newRule.AppendChild($protection) | Out-Null
        }
        
        # 添加到配置
        # Add to configuration
        $confusionConfig.project.AppendChild($newRule) | Out-Null
    }
}

# 确保安全敏感类型保持强保护
# Ensure security-sensitive types maintain strong protection
foreach ($pattern in $securitySensitivePatterns) {
    # 检查是否已存在该模式的规则
    # Check if a rule for this pattern already exists
    $existingRule = $confusionConfig.project.rule | Where-Object { $_.pattern -eq $pattern }
    
    if (-not $existingRule) {
        Write-Info "创建安全敏感类型的强保护规则: $pattern / Creating strong protection rule for security-sensitive type: $pattern"
        
        # 创建新规则
        # Create new rule
        $newRule = $confusionConfig.CreateElement("rule")
        $newRule.SetAttribute("pattern", $pattern)
        $newRule.SetAttribute("preset", "maximum")
        $newRule.SetAttribute("inherit", "false")
        
        # 添加所有保护
        # Add all protections
        $protections = @("anti ildasm", "anti tamper", "constants", "ctrl flow", "rename", "ref proxy", "resources")
        foreach ($protectionId in $protections) {
            $protection = $confusionConfig.CreateElement("protection")
            $protection.SetAttribute("id", $protectionId)
            $newRule.AppendChild($protection) | Out-Null
        }
        
        # 添加到配置
        # Add to configuration
        $confusionConfig.project.AppendChild($newRule) | Out-Null
    }
}

# 添加性能优化注释
# Add performance optimization comment
$comment = $confusionConfig.CreateComment("Performance optimized configuration - Reduced obfuscation for performance-critical components")
$confusionConfig.project.PrependChild($comment) | Out-Null

# 保存优化后的配置
# Save optimized configuration
try {
    Write-Info "正在保存优化后的配置文件... / Saving optimized configuration file..."
    $confusionConfig.Save($optimizedConfigPath)
    Write-Success "优化后的配置文件已保存: $optimizedConfigPath / Optimized configuration file has been saved: $optimizedConfigPath"
} catch {
    Write-Error "保存配置文件时出错: $($_.Exception.Message) / Error saving configuration file: $($_.Exception.Message)"
    exit 1
}

# 创建应用优化配置的脚本
# Create script to apply optimized configuration
$applyScriptPath = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "apply-optimized-config.ps1"
$applyScriptContent = @"
# 应用优化混淆配置脚本
# Apply Optimized Obfuscation Configuration Script

# 设置路径
# Set paths
`$projectDir = "$ProjectDir"
`$originalConfig = Join-Path `$projectDir "$ConfigFile"
`$optimizedConfig = Join-Path `$projectDir "$OptimizedConfigFile"
`$backupConfig = Join-Path `$projectDir "$ConfigFile.backup"

# 备份原始配置
# Backup original configuration
if (Test-Path `$originalConfig) {
    Copy-Item `$originalConfig `$backupConfig -Force
    Write-Host "原始配置已备份: `$backupConfig" -ForegroundColor Cyan
    Write-Host "Original configuration has been backed up: `$backupConfig" -ForegroundColor Cyan
}

# 应用优化配置
# Apply optimized configuration
if (Test-Path `$optimizedConfig) {
    Copy-Item `$optimizedConfig `$originalConfig -Force
    Write-Host "优化配置已应用: `$originalConfig" -ForegroundColor Green
    Write-Host "Optimized configuration has been applied: `$originalConfig" -ForegroundColor Green
} else {
    Write-Host "找不到优化配置文件: `$optimizedConfig" -ForegroundColor Red
    Write-Host "Optimized configuration file not found: `$optimizedConfig" -ForegroundColor Red
    exit 1
}

Write-Host "完成! 现在可以使用优化后的混淆配置进行构建。" -ForegroundColor Green
Write-Host "Done! You can now build with the optimized obfuscation configuration." -ForegroundColor Green
"@

try {
    Write-Info "正在创建应用脚本... / Creating application script..."
    $applyScriptContent | Out-File -FilePath $applyScriptPath -Encoding utf8
    Write-Success "应用脚本已创建: $applyScriptPath / Application script has been created: $applyScriptPath"
} catch {
    Write-Error "创建应用脚本时出错: $($_.Exception.Message) / Error creating application script: $($_.Exception.Message)"
    exit 1
}

# 显示使用说明
# Display usage instructions
Write-Info ""
Write-Info "====== 使用说明 / Usage Instructions ======"
Write-Info "1. 查看优化后的配置文件: $optimizedConfigPath"
Write-Info "   Review the optimized configuration file: $optimizedConfigPath"
Write-Info ""
Write-Info "2. 应用优化后的配置:"
Write-Info "   Apply the optimized configuration:"
Write-Info "   .\apply-optimized-config.ps1"
Write-Info ""
Write-Info "3. 使用优化后的配置构建项目:"
Write-Info "   Build the project with the optimized configuration:"
Write-Info "   .\build-with-obfuscation.ps1"
Write-Info ""
Write-Info "4. 测量性能改进:"
Write-Info "   Measure performance improvements:"
Write-Info "   .\measure-obfuscation-performance.ps1"
Write-Info ""

Write-Success "====== 混淆配置优化完成 / Obfuscation configuration optimization completed ======" 