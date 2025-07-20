# 混淆测试运行脚本
# Obfuscation Test Runner Script

# 设置颜色输出函数
# Set color output functions
function Write-ColorOutput($message, $color) {
    Write-Host $message -ForegroundColor $color
}

function Write-Success($message) { Write-ColorOutput $message "Green" }
function Write-Info($message) { Write-ColorOutput $message "Cyan" }
function Write-Warning($message) { Write-ColorOutput $message "Yellow" }
function Write-Error($message) { Write-ColorOutput $message "Red" }

# 显示欢迎信息
# Display welcome message
Write-Info "====== 多链钱包混淆测试工具 ======"
Write-Info "====== MultiChainWallet Obfuscation Test Tool ======"
Write-Info ""

# 设置路径
# Set paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$buildDir = Join-Path $projectDir "bin\Release\net8.0"
$confusedDir = Join-Path $projectDir "Confused"

# 检查构建目录
# Check build directory
if (-not (Test-Path $buildDir)) {
    Write-Warning "构建目录不存在: $buildDir"
    Write-Warning "Build directory does not exist: $buildDir"
    
    $buildProject = Read-Host "是否构建项目? (Y/N) / Build the project? (Y/N)"
    if ($buildProject -eq "Y" -or $buildProject -eq "y") {
        Write-Info "正在构建项目..."
        Write-Info "Building project..."
        
        Push-Location $projectDir
        dotnet build -c Release
        Pop-Location
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "构建失败，退出代码: $LASTEXITCODE"
            Write-Error "Build failed, exit code: $LASTEXITCODE"
            exit 1
        }
        
        Write-Success "构建完成"
        Write-Success "Build completed"
    } else {
        Write-Error "无法继续，需要构建目录"
        Write-Error "Cannot continue, build directory required"
        exit 1
    }
}

# 检查混淆目录
# Check confused directory
if (-not (Test-Path $confusedDir)) {
    Write-Warning "混淆目录不存在: $confusedDir"
    Write-Warning "Confused directory does not exist: $confusedDir"
    
    $runObfuscation = Read-Host "是否运行混淆? (Y/N) / Run obfuscation? (Y/N)"
    if ($runObfuscation -eq "Y" -or $runObfuscation -eq "y") {
        Write-Info "正在运行混淆..."
        Write-Info "Running obfuscation..."
        
        $simpleObfuscationScript = Join-Path $scriptDir "simple-obfuscation.ps1"
        if (Test-Path $simpleObfuscationScript) {
            & $simpleObfuscationScript
            
            if ($LASTEXITCODE -ne 0) {
                Write-Error "混淆失败，退出代码: $LASTEXITCODE"
                Write-Error "Obfuscation failed, exit code: $LASTEXITCODE"
                exit 1
            }
            
            Write-Success "混淆完成"
            Write-Success "Obfuscation completed"
        } else {
            Write-Error "找不到混淆脚本: $simpleObfuscationScript"
            Write-Error "Obfuscation script not found: $simpleObfuscationScript"
            exit 1
        }
    } else {
        Write-Error "无法继续，需要混淆目录"
        Write-Error "Cannot continue, confused directory required"
        exit 1
    }
}

# 显示测试菜单
# Display test menu
Write-Info ""
Write-Info "请选择测试类型 / Please select test type:"
Write-Info "1. 测试原始程序集 / Test original assemblies"
Write-Info "2. 测试混淆程序集 / Test obfuscated assemblies"
Write-Info "3. 比较原始和混淆程序集 / Compare original and obfuscated assemblies"
Write-Info "4. 退出 / Exit"
Write-Info ""

$choice = Read-Host "请输入选择 (1-4) / Enter your choice (1-4)"

switch ($choice) {
    "1" {
        Write-Info "正在测试原始程序集..."
        Write-Info "Testing original assemblies..."
        & (Join-Path $scriptDir "test-obfuscation.ps1") -BuildOutputPath $buildDir -UseObfuscated:$false
    }
    "2" {
        Write-Info "正在测试混淆程序集..."
        Write-Info "Testing obfuscated assemblies..."
        & (Join-Path $scriptDir "test-obfuscation.ps1") -BuildOutputPath $buildDir -UseObfuscated:$true
    }
    "3" {
        Write-Info "正在比较原始和混淆程序集..."
        Write-Info "Comparing original and obfuscated assemblies..."
        
        # 测试原始程序集
        # Test original assemblies
        Write-Info ""
        Write-Info "=== 原始程序集测试 / Original Assemblies Test ==="
        & (Join-Path $scriptDir "test-obfuscation.ps1") -BuildOutputPath $buildDir -UseObfuscated:$false
        
        # 测试混淆程序集
        # Test obfuscated assemblies
        Write-Info ""
        Write-Info "=== 混淆程序集测试 / Obfuscated Assemblies Test ==="
        & (Join-Path $scriptDir "test-obfuscation.ps1") -BuildOutputPath $buildDir -UseObfuscated:$true
        
        # 比较文件大小
        # Compare file sizes
        Write-Info ""
        Write-Info "=== 文件大小比较 / File Size Comparison ==="
        
        $testFiles = @(
            "MultiChainWallet.Core.dll",
            "MultiChainWallet.Infrastructure.dll",
            "MultiChainWallet.UI.dll"
        )
        
        foreach ($file in $testFiles) {
            $origPath = Join-Path $buildDir $file
            $obfPath = Join-Path $confusedDir $file
            
            if ((Test-Path $origPath) -and (Test-Path $obfPath)) {
                $origSize = (Get-Item $origPath).Length
                $obfSize = (Get-Item $obfPath).Length
                $diff = $obfSize - $origSize
                $percentChange = [math]::Round(($diff / $origSize) * 100, 2)
                
                Write-Info "文件 / File: $file"
                Write-Info "  原始大小 / Original size: $origSize 字节 / bytes"
                Write-Info "  混淆大小 / Obfuscated size: $obfSize 字节 / bytes"
                Write-Info "  差异 / Difference: $diff 字节 / bytes ($percentChange%)"
                
                if ($obfSize -eq $origSize) {
                    Write-Warning "  文件大小相同，混淆可能未生效 / File sizes are the same, obfuscation might not be effective"
                } elseif ($obfSize -gt $origSize) {
                    Write-Success "  混淆后文件变大，混淆可能已应用 / File is larger after obfuscation, suggesting obfuscation was applied"
                } else {
                    Write-Warning "  混淆后文件变小，这很不寻常 / File is smaller after obfuscation, which is unusual"
                }
            }
        }
    }
    "4" {
        Write-Info "退出程序 / Exiting program"
        exit 0
    }
    default {
        Write-Error "无效选择，请输入1-4 / Invalid choice, please enter 1-4"
        exit 1
    }
}

Write-Info ""
Write-Success "测试完成 / Testing completed" 