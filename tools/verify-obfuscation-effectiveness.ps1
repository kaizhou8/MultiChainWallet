# 混淆有效性验证脚本
# Obfuscation Effectiveness Verification Script

# 参数 / Parameters
param (
    [Parameter(Mandatory=$true)]
    [string]$BuildOutputPath,
    
    [Parameter(Mandatory=$false)]
    [string]$DecompilerPath = $null, # 如果不提供，将尝试查找已安装的反编译工具 / If not provided, will try to find installed decompilation tools
    
    [Parameter(Mandatory=$false)]
    [switch]$InstallTools = $false, # 是否安装测试工具 / Whether to install testing tools
    
    [Parameter(Mandatory=$false)]
    [string]$LogFile = "obfuscation-validation.log" # 日志文件路径 / Log file path
)

# 设置颜色函数 / Set color functions
function Write-Success($message) { 
    Write-Host $message -ForegroundColor Green 
    Add-Content -Path $LogFile -Value "[SUCCESS] $message"
}

function Write-Info($message) { 
    Write-Host $message -ForegroundColor Cyan 
    Add-Content -Path $LogFile -Value "[INFO] $message"
}

function Write-Warning($message) { 
    Write-Host $message -ForegroundColor Yellow 
    Add-Content -Path $LogFile -Value "[WARNING] $message"
}

function Write-Error($message) { 
    Write-Host $message -ForegroundColor Red 
    Add-Content -Path $LogFile -Value "[ERROR] $message"
}

# 初始化日志文件 / Initialize log file
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
"[$timestamp] 开始混淆有效性验证 / Starting obfuscation effectiveness verification" | Out-File -FilePath $LogFile -Force

# 设置测试变量 / Set test variables
$obfuscatedDir = Join-Path $BuildOutputPath "Confused"
$originalDir = Join-Path $BuildOutputPath "Original"
$tempDir = Join-Path $BuildOutputPath "TempDecompile"

# 创建临时目录 / Create temporary directory
if (-not (Test-Path $tempDir)) {
    New-Item -Path $tempDir -ItemType Directory -Force | Out-Null
}

# 检查条件 / Check conditions
$hasOriginal = Test-Path $originalDir
$hasObfuscated = Test-Path $obfuscatedDir

if (-not $hasObfuscated) {
    Write-Error "混淆输出目录不存在: $obfuscatedDir / Obfuscated output directory does not exist: $obfuscatedDir"
    exit 1
}

if (-not $hasOriginal) {
    Write-Warning "原始目录不存在，无法进行比较: $originalDir / Original directory does not exist, cannot compare: $originalDir"
}

# 测试文件 / Test files
$testFiles = @(
    "MultiChainWallet.Infrastructure.dll",
    "MultiChainWallet.Core.dll",
    "MultiChainWallet.UI.dll"
)

# 1. 基本文件检查 / Basic file check
Write-Info "===== 基本文件检查 / Basic File Check ====="
foreach ($file in $testFiles) {
    $obfuscatedFile = Join-Path $obfuscatedDir $file
    if (Test-Path $obfuscatedFile) {
        Write-Success "混淆文件存在: $file / Obfuscated file exists: $file"
        
        # 如果有原始文件，比较大小 / If original file exists, compare sizes
        if ($hasOriginal) {
            $originalFile = Join-Path $originalDir $file
            if (Test-Path $originalFile) {
                $origSize = (Get-Item $originalFile).Length
                $obfuSize = (Get-Item $obfuscatedFile).Length
                
                Write-Info "原始大小: $origSize 字节, 混淆大小: $obfuSize 字节 / Original size: $origSize bytes, Obfuscated size: $obfuSize bytes"
                
                if ($origSize -eq $obfuSize) {
                    Write-Warning "文件大小未变化，混淆可能未应用 / File size unchanged, obfuscation might not be applied"
                } else {
                    Write-Success "文件大小已变化，混淆可能已应用 / File size changed, obfuscation likely applied"
                }
            }
        }
    } else {
        Write-Error "混淆文件不存在: $file / Obfuscated file does not exist: $file"
    }
}

# 2. 程序集反射检查 / Assembly reflection check
Write-Info "===== 程序集反射检查 / Assembly Reflection Check ====="

$infraDll = Join-Path $obfuscatedDir "MultiChainWallet.Infrastructure.dll"
try {
    $assembly = [System.Reflection.Assembly]::LoadFile($infraDll)
    Write-Success "混淆程序集可以加载 / Obfuscated assembly can be loaded"
    
    # 检查应该被混淆的类型 / Check types that should be obfuscated
    $typesToCheck = @(
        "MultiChainWallet.Infrastructure.Services.SecurityService",
        "MultiChainWallet.Infrastructure.Services.HardwareWalletManager",
        "MultiChainWallet.Infrastructure.Services.WalletService"
    )
    
    foreach ($typeName in $typesToCheck) {
        $type = $assembly.GetType($typeName)
        if ($type -eq $null) {
            Write-Success "类型已混淆: $typeName / Type is obfuscated: $typeName"
        } else {
            Write-Warning "类型未混淆: $typeName / Type is not obfuscated: $typeName"
        }
    }
    
    # 检查不应被混淆的类型 / Check types that should not be obfuscated
    $typesNotToObfuscate = @(
        "MultiChainWallet.Core.Interfaces.IHardwareWallet",
        "MultiChainWallet.Core.Models.Wallet"
    )
    
    foreach ($typeName in $typesNotToObfuscate) {
        $type = $assembly.GetType($typeName)
        if ($type -ne $null) {
            Write-Success "接口/模型正确保留: $typeName / Interface/model correctly preserved: $typeName"
        } else {
            Write-Warning "接口/模型可能被错误混淆: $typeName / Interface/model might be incorrectly obfuscated: $typeName"
        }
    }
} catch {
    Write-Error "程序集加载失败: $_ / Assembly loading failed: $_"
}

# 3. 字符串常量检查 / String constants check
# 这个测试检查敏感字符串是否已经加密 / This test checks if sensitive strings have been encrypted
Write-Info "===== 字符串常量检查 / String Constants Check ====="

function Find-StringsInFile {
    param (
        [string]$FilePath
    )
    
    # 使用strings命令如果可用，否则使用PowerShell / Use strings command if available, otherwise use PowerShell
    if (Get-Command "strings.exe" -ErrorAction SilentlyContinue) {
        $output = & strings.exe $FilePath
    } else {
        # PowerShell的备选方案 / PowerShell alternative
        $bytes = [System.IO.File]::ReadAllBytes($FilePath)
        $output = New-Object System.Collections.ArrayList
        
        $currentString = New-Object System.Text.StringBuilder
        
        for ($i = 0; $i -lt $bytes.Length; $i++) {
            $byte = $bytes[$i]
            # 检查可打印ASCII字符 / Check for printable ASCII characters
            if ($byte -ge 32 -and $byte -le 126) {
                [void]$currentString.Append([char]$byte)
            } else {
                if ($currentString.Length -ge 4) {
                    [void]$output.Add($currentString.ToString())
                }
                $currentString.Clear() | Out-Null
            }
        }
        
        $output
    }
    
    return $output
}

# 敏感字符串列表 / List of sensitive strings
$sensitiveStrings = @(
    "SecurityService",
    "HardwareWalletManager",
    "PrivateKey",
    "SignTransaction",
    "ConnectionSecret"
)

foreach ($file in $testFiles) {
    $obfuscatedFile = Join-Path $obfuscatedDir $file
    if (Test-Path $obfuscatedFile) {
        Write-Info "检查敏感字符串: $file / Checking sensitive strings: $file"
        
        $strings = Find-StringsInFile -FilePath $obfuscatedFile
        
        $foundSensitive = 0
        foreach ($sensitive in $sensitiveStrings) {
            if ($strings -contains $sensitive) {
                Write-Warning "发现未混淆的敏感字符串: $sensitive / Found unobfuscated sensitive string: $sensitive"
                $foundSensitive++
            }
        }
        
        if ($foundSensitive -eq 0) {
            Write-Success "未找到未混淆的敏感字符串 / No unobfuscated sensitive strings found"
        } else {
            Write-Warning "发现 $foundSensitive 个未混淆的敏感字符串 / Found $foundSensitive unobfuscated sensitive strings"
        }
    }
}

# 4. ILDASM测试 / ILDASM Test
Write-Info "===== ILDASM测试 / ILDASM Test ====="

# 查找ILDASM工具 / Find ILDASM tool
$ildasmPath = $null
$sdkPaths = Get-ChildItem "C:\Program Files\dotnet\sdk" -Directory | Sort-Object Name -Descending
foreach ($sdk in $sdkPaths) {
    $testPath = Join-Path $sdk.FullName "Ildasm.exe"
    if (Test-Path $testPath) {
        $ildasmPath = $testPath
        break
    }
}

if ($ildasmPath) {
    Write-Info "找到ILDASM工具: $ildasmPath / Found ILDASM tool: $ildasmPath"
    
    foreach ($file in $testFiles) {
        $obfuscatedFile = Join-Path $obfuscatedDir $file
        if (Test-Path $obfuscatedFile) {
            $ilOutput = Join-Path $tempDir "$($file).il"
            
            # 执行ILDASM / Run ILDASM
            & $ildasmPath $obfuscatedFile "/out=$ilOutput" "/text" | Out-Null
            
            if (Test-Path $ilOutput) {
                Write-Success "成功反汇编: $file / Successfully disassembled: $file"
                
                # 检查混淆特征 / Check obfuscation features
                $content = Get-Content $ilOutput -Raw
                
                $obfuscationFeatures = @{
                    "SupressIldasmAttribute" = "反ILDASM保护 / Anti-ILDASM protection"
                    "System.Reflection.Obfuscation" = "混淆属性 / Obfuscation attributes"
                    "[a-z0-9]{8}" = "随机命名 / Random naming"
                    "preservesig" = "方法签名保护 / Method signature protection"
                }
                
                foreach ($feature in $obfuscationFeatures.Keys) {
                    if ($content -match $feature) {
                        Write-Success "检测到混淆特征: $($obfuscationFeatures[$feature]) / Detected obfuscation feature: $($obfuscationFeatures[$feature])"
                    } else {
                        Write-Warning "未检测到混淆特征: $($obfuscationFeatures[$feature]) / Did not detect obfuscation feature: $($obfuscationFeatures[$feature])"
                    }
                }
                
                # 清理临时文件 / Clean up temporary file
                Remove-Item $ilOutput -Force
            } else {
                Write-Error "反汇编失败: $file / Disassembly failed: $file"
            }
        }
    }
} else {
    Write-Warning "找不到ILDASM工具，跳过IL分析 / ILDASM tool not found, skipping IL analysis"
}

# 5. DNSpy兼容性检查（如果可用）/ DNSpy compatibility check (if available)
Write-Info "===== DNSpy兼容性检查 / DNSpy Compatibility Check ====="

$dnspyPaths = @(
    "C:\Program Files\dnSpy\dnSpy.exe",
    "C:\Program Files (x86)\dnSpy\dnSpy.exe",
    "$env:LOCALAPPDATA\Programs\dnSpy\dnSpy.exe"
)

$dnspyPath = $null
foreach ($path in $dnspyPaths) {
    if (Test-Path $path) {
        $dnspyPath = $path
        break
    }
}

if ($dnspyPath) {
    Write-Info "找到DNSpy: $dnspyPath / Found DNSpy: $dnspyPath"
    Write-Info "您可以手动使用DNSpy工具打开混淆后的DLL并检查其保护 / You can manually use the DNSpy tool to open the obfuscated DLLs and inspect their protection"
    
    # 打印DNSpy命令示例 / Print DNSpy command examples
    foreach ($file in $testFiles) {
        $obfuscatedFile = Join-Path $obfuscatedDir $file
        if (Test-Path $obfuscatedFile) {
            Write-Info "DNSpy分析命令: `"$dnspyPath`" `"$obfuscatedFile`" / DNSpy analysis command: `"$dnspyPath`" `"$obfuscatedFile`""
        }
    }
} else {
    Write-Warning "找不到DNSpy工具，无法执行反编译测试 / DNSpy tool not found, cannot perform decompilation test"
    
    if ($InstallTools) {
        Write-Info "正在尝试下载DNSpy... / Attempting to download DNSpy..."
        $dnspyUrl = "https://github.com/dnSpy/dnSpy/releases/download/v6.1.8/dnSpy-net-win64.zip"
        $dnspyZip = Join-Path $tempDir "dnspy.zip"
        $dnspyExtract = Join-Path $tempDir "dnspy"
        
        try {
            Invoke-WebRequest -Uri $dnspyUrl -OutFile $dnspyZip
            Expand-Archive -Path $dnspyZip -DestinationPath $dnspyExtract -Force
            
            Write-Success "DNSpy已下载到: $dnspyExtract / DNSpy downloaded to: $dnspyExtract"
            Write-Info "您可以使用DNSpy手动检查混淆的程序集: $dnspyExtract\dnSpy.exe / You can use DNSpy to manually inspect obfuscated assemblies: $dnspyExtract\dnSpy.exe"
            
            # 打印DNSpy命令示例 / Print DNSpy command examples
            foreach ($file in $testFiles) {
                $obfuscatedFile = Join-Path $obfuscatedDir $file
                if (Test-Path $obfuscatedFile) {
                    Write-Info "DNSpy分析命令: `"$dnspyExtract\dnSpy.exe`" `"$obfuscatedFile`" / DNSpy analysis command: `"$dnspyExtract\dnSpy.exe`" `"$obfuscatedFile`""
                }
            }
        } catch {
            Write-Error "下载DNSpy失败: $_ / Failed to download DNSpy: $_"
        }
    }
}

# 总结验证结果 / Summarize verification results
Write-Info "===== 混淆验证总结 / Obfuscation Verification Summary ====="
Write-Info "验证日志保存在: $LogFile / Verification log saved in: $LogFile"
Write-Success "验证完成 / Verification completed" 