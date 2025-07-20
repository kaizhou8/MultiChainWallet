# CI/CD环境中的混淆验证脚本
# Obfuscation verification script for CI/CD environments

# 参数 / Parameters
param (
    [Parameter(Mandatory=$true)]
    [string]$ArtifactsPath
)

# 设置变量 / Set variables
$originalDllPath = Join-Path $ArtifactsPath "Original"
$obfuscatedDllPath = Join-Path $ArtifactsPath "MultiChainWallet.Infrastructure.dll"
$logFilePath = Join-Path $ArtifactsPath "obfuscation-verification.log"

# 初始化日志 / Initialize log
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
"[$timestamp] 开始验证混淆 / Starting obfuscation verification" | Out-File -FilePath $logFilePath

# 设置验证状态 / Set verification status
$verificationPassed = $true

# 函数：记录日志 / Function: Log message
function LogMessage {
    param (
        [string]$Message,
        [string]$Type = "INFO"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "[$timestamp] [$Type] $Message" | Out-File -FilePath $logFilePath -Append
    
    switch ($Type) {
        "ERROR" { Write-Host $Message -ForegroundColor Red }
        "WARNING" { Write-Host $Message -ForegroundColor Yellow }
        "SUCCESS" { Write-Host $Message -ForegroundColor Green }
        default { Write-Host $Message }
    }
}

# 验证1：检查混淆DLL是否存在 / Verification 1: Check if obfuscated DLL exists
if (-not (Test-Path $obfuscatedDllPath)) {
    LogMessage "混淆后的DLL文件不存在: $obfuscatedDllPath / Obfuscated DLL file does not exist: $obfuscatedDllPath" "ERROR"
    $verificationPassed = $false
}
else {
    LogMessage "混淆后的DLL文件存在 / Obfuscated DLL file exists" "SUCCESS"
    
    # 验证2：检查文件大小是否有变化 / Verification 2: Check if file size has changed
    $originalSize = (Get-Item (Join-Path $originalDllPath "MultiChainWallet.Infrastructure.dll")).Length
    $obfuscatedSize = (Get-Item $obfuscatedDllPath).Length
    
    LogMessage "原始DLL大小: $originalSize 字节 / Original DLL size: $originalSize bytes" "INFO"
    LogMessage "混淆DLL大小: $obfuscatedSize 字节 / Obfuscated DLL size: $obfuscatedSize bytes" "INFO"
    
    if ($originalSize -eq $obfuscatedSize) {
        LogMessage "警告：混淆前后文件大小相同，混淆可能未正确应用 / Warning: File size is the same before and after obfuscation, obfuscation may not be properly applied" "WARNING"
    }
    else {
        LogMessage "混淆前后文件大小不同，这是预期的 / File size is different before and after obfuscation, which is expected" "SUCCESS"
    }
    
    # 验证3：使用reflection加载DLL并检查类型 / Verification 3: Load DLL using reflection and check types
    try {
        $obfuscatedAssembly = [System.Reflection.Assembly]::LoadFile($obfuscatedDllPath)
        LogMessage "成功加载混淆后的程序集 / Successfully loaded obfuscated assembly" "SUCCESS"
        
        # 检查接口类型是否可访问 / Check if interface types are accessible
        $interfaceType = $obfuscatedAssembly.GetType("MultiChainWallet.Core.Interfaces.IHardwareWallet")
        if ($interfaceType -ne $null) {
            LogMessage "接口类型可以访问：$($interfaceType.FullName) / Interface type is accessible: $($interfaceType.FullName)" "SUCCESS"
        }
        else {
            LogMessage "警告：接口类型不可访问，可能导致运行时问题 / Warning: Interface type is not accessible, may cause runtime issues" "WARNING"
        }
        
        # 尝试访问应该被混淆的类型 / Try to access types that should be obfuscated
        $securityServiceType = $obfuscatedAssembly.GetType("MultiChainWallet.Infrastructure.Services.SecurityService")
        if ($securityServiceType -eq $null) {
            LogMessage "安全服务类型已成功混淆 / Security service type is successfully obfuscated" "SUCCESS"
        }
        else {
            LogMessage "警告：安全服务类型可以访问，混淆可能未正确应用 / Warning: Security service type is accessible, obfuscation may not be properly applied" "WARNING"
        }
    }
    catch {
        LogMessage "加载混淆后的程序集时出错: $_ / Error loading obfuscated assembly: $_" "ERROR"
        $verificationPassed = $false
    }
}

# 显示最终结果 / Display final result
if ($verificationPassed) {
    LogMessage "混淆验证通过 / Obfuscation verification passed" "SUCCESS"
    exit 0
}
else {
    LogMessage "混淆验证失败 / Obfuscation verification failed" "ERROR"
    exit 1
} 