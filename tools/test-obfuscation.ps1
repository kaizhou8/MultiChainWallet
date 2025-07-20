# 混淆测试脚本
# Obfuscation Test Script

# 参数 / Parameters
param (
    [Parameter(Mandatory=$false)]
    [string]$BuildOutputPath = (Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "bin\Release\net8.0"),
    
    [Parameter(Mandatory=$false)]
    [switch]$UseObfuscated = $true
)

# 彩色输出函数 / Colored output functions
function Write-ColorOutput($message, $color) {
    Write-Host $message -ForegroundColor $color
}

function Write-Success($message) { Write-ColorOutput $message "Green" }
function Write-Info($message) { Write-ColorOutput $message "Cyan" }
function Write-Warning($message) { Write-ColorOutput $message "Yellow" }
function Write-Error($message) { Write-ColorOutput $message "Red" }

# 设置路径 / Set paths
$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$confusedDir = Join-Path $projectDir "Confused"
$originalDir = Join-Path $BuildOutputPath ""
$testDir = Join-Path $BuildOutputPath "TestResults"

# 检查目标文件 / Check target files
$testFiles = @(
    "MultiChainWallet.Core.dll",
    "MultiChainWallet.Infrastructure.dll",
    "MultiChainWallet.UI.dll"
)

# 开始测试过程 / Start testing process
Write-Info "====== 开始混淆测试 ======"
Write-Info "====== Starting Obfuscation Test ======"
Write-Info "构建输出路径 / Build output path: $BuildOutputPath"
Write-Info "使用混淆文件 / Using obfuscated files: $UseObfuscated"

# 选择要测试的目录 / Choose directory to test
$testPath = if ($UseObfuscated) { $confusedDir } else { $originalDir }
Write-Info "测试路径 / Test path: $testPath"

# 检查测试目录存在 / Check if test directory exists
if (-not (Test-Path $testPath)) {
    Write-Error "测试目录不存在 / Test directory does not exist: $testPath"
    exit 1
}

# 创建测试结果目录 / Create test results directory
if (-not (Test-Path $testDir)) {
    New-Item -ItemType Directory -Path $testDir -Force | Out-Null
}

# 测试 1: 基本文件检查 / Test 1: Basic file check
Write-Info "===== 测试 1: 基本文件检查 / Test 1: Basic file check ====="
foreach ($file in $testFiles) {
    $filePath = Join-Path $testPath $file
    if (Test-Path $filePath) {
        $fileSize = (Get-Item $filePath).Length
        Write-Success "文件存在 / File exists: $file (大小 / Size: $fileSize 字节 / bytes)"
    } else {
        Write-Error "文件不存在 / File does not exist: $file"
    }
}

# 测试 2: 反射加载 / Test 2: Reflection loading
Write-Info "===== 测试 2: 反射加载测试 / Test 2: Reflection loading test ====="
foreach ($file in $testFiles) {
    $filePath = Join-Path $testPath $file
    if (Test-Path $filePath) {
        try {
            $assembly = [System.Reflection.Assembly]::LoadFile($filePath)
            $types = $assembly.GetTypes()
            Write-Success "程序集加载成功 / Assembly loaded successfully: $file"
            Write-Info "类型数量 / Type count: $($types.Count)"
            
            # 获取一些类型名称 / Get some type names
            $sampleTypes = $types | Select-Object -First 5
            foreach ($type in $sampleTypes) {
                Write-Info "  - $($type.FullName)"
            }
        } catch {
            Write-Error "程序集加载失败 / Assembly loading failed: $file"
            Write-Error "错误 / Error: $_"
        }
    }
}

# 测试 3: 字符串分析 / Test 3: String analysis
Write-Info "===== 测试 3: 字符串分析 / Test 3: String analysis ====="
foreach ($file in $testFiles) {
    $filePath = Join-Path $testPath $file
    if (Test-Path $filePath) {
        Write-Info "检查文件的字符串 / Checking strings in file: $file"
        
        # 读取文件的二进制数据 / Read file binary data
        $bytes = [System.IO.File]::ReadAllBytes($filePath)
        $stringList = New-Object System.Collections.Generic.List[string]
        $currentString = New-Object System.Text.StringBuilder
        
        # 扫描可打印ASCII字符串 / Scan for printable ASCII strings
        for ($i = 0; $i -lt $bytes.Length; $i++) {
            $byte = $bytes[$i]
            if ($byte -ge 32 -and $byte -le 126) {
                [void]$currentString.Append([char]$byte)
            } else {
                if ($currentString.Length -ge 5) {
                    [void]$stringList.Add($currentString.ToString())
                }
                $currentString.Clear() | Out-Null
            }
        }
        
        # 检查敏感字符串 / Check sensitive strings
        $sensitiveStrings = @(
            "PrivateKey",
            "SecretKey",
            "Password",
            "Credential",
            "SecurityService"
        )
        
        $foundSensitive = $false
        foreach ($sensitive in $sensitiveStrings) {
            $matches = $stringList | Where-Object { $_ -like "*$sensitive*" }
            if ($matches -and $matches.Count -gt 0) {
                $foundSensitive = $true
                Write-Warning "发现敏感字符串 / Found sensitive string: $sensitive ($($matches.Count) 匹配 / matches)"
            }
        }
        
        if (-not $foundSensitive) {
            Write-Success "未发现敏感字符串 / No sensitive strings found"
        }
        
        # 输出字符串统计 / Output string statistics
        Write-Info "共提取了 $($stringList.Count) 个字符串 / Extracted $($stringList.Count) strings"
    }
}

# 测试 4: 比较文件大小 / Test 4: Compare file sizes
if (Test-Path $originalDir -and Test-Path $confusedDir) {
    Write-Info "===== 测试 4: 文件大小比较 / Test 4: File size comparison ====="
    foreach ($file in $testFiles) {
        $origPath = Join-Path $originalDir $file
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
} else {
    Write-Warning "原始目录或混淆目录不存在，无法比较文件大小 / Original or obfuscated directory does not exist, cannot compare file sizes"
}

# 完成测试 / Complete testing
Write-Info "====== 混淆测试完成 ======"
Write-Info "====== Obfuscation test completed ======" 