# MultiChainWallet 安全实施计划 / Security Implementation Plan

*创建日期 / Creation Date: 2025-03-28*

## 概述 / Overview

本文档详细说明了 MultiChainWallet 应用的代码混淆和加密保护计划，采用混合方案，结合开源工具与自定义加密保护措施，为钱包应用提供多层次安全保障。

This document details the code obfuscation and encryption protection plan for the MultiChainWallet application, using a hybrid approach that combines open-source tools with custom encryption protection measures to provide multi-layered security for the wallet application.

## 安全目标 / Security Objectives

1. **防止静态分析 / Prevent Static Analysis**
   - 混淆代码结构和控制流程
   - 加密字符串和常量
   - 混淆类型和方法名称

2. **防止动态分析 / Prevent Dynamic Analysis**
   - 实现运行时完整性检查
   - 检测调试器和虚拟机环境
   - 实现内存保护机制

3. **保护敏感数据 / Protect Sensitive Data**
   - 多层加密处理私钥和种子
   - 安全的内存管理
   - 防止内存查看和内存转储攻击

4. **确保交易安全 / Ensure Transaction Security**
   - 保护交易签名流程
   - 验证交易数据完整性
   - 防止交易篡改

## 实施方案 / Implementation Approach

### 一、开源混淆工具 / Open Source Obfuscation Tool

选用 **ConfuserEx2** 作为基础混淆工具，它是 ConfuserEx 的现代分支，支持 .NET Core/.NET 5+。

#### 配置文件 / Configuration File

```xml
<!-- Confusion.crproj -->
<project baseDir="." outputDir="./Confused" debug="false">
  <rule pattern="true" preset="maximum" inherit="false">
    <protection id="anti debug" />
    <protection id="anti dump" />
    <protection id="anti ildasm" />
    <protection id="anti tamper" />
    <protection id="constants" />
    <protection id="ctrl flow" />
    <protection id="rename" />
    <protection id="resources" />
  </rule>
  <rule pattern="MultiChainWallet.Core.Models.Wallet" preset="none" inherit="false">
    <protection id="rename" action="remove" />
  </rule>
  <module path="MultiChainWallet.UI.dll" />
  <module path="MultiChainWallet.Core.dll" />
  <module path="MultiChainWallet.Infrastructure.dll" />
</project>
```

#### 构建集成 / Build Integration

将混淆步骤添加到后构建事件：

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="$(ProjectDir)tools\Confuser.CLI.exe $(ProjectDir)Confusion.crproj" />
</Target>
```

### 二、自定义安全层 / Custom Security Layer

#### 1. 自定义加密服务 / Custom Encryption Service

创建增强型加密服务，用于保护敏感数据：

```csharp
// CustomSecurityService.cs
public class CustomSecurityService
{
    // 使用多层加密处理敏感数据
    public static byte[] ProtectCriticalData(byte[] data)
    {
        // 第一层: 设备指纹相关加密
        byte[] layer1 = AesEncrypt(data, GetDeviceFingerprint());
        
        // 第二层: 时间和会话相关加密
        byte[] layer2 = AesEncrypt(layer1, GetSessionKey());
        
        // 第三层: 用户密码派生密钥加密
        return AesEncrypt(layer2, GetUserDerivedKey());
    }
    
    // 解密方法 (反向顺序)
    public static byte[] UnprotectCriticalData(byte[] protectedData)
    {
        byte[] layer2 = AesDecrypt(protectedData, GetUserDerivedKey());
        byte[] layer1 = AesDecrypt(layer2, GetSessionKey());
        return AesDecrypt(layer1, GetDeviceFingerprint());
    }
    
    // 运行时安全验证
    public static bool VerifyRuntimeSecurity()
    {
        return !IsDebugged() && 
               !IsRunningInVirtualMachine() && 
               !IsMemoryAnalyzerDetected();
    }
}
```

#### 2. 内存保护 / Memory Protection

实现安全的内存管理机制：

```csharp
// MemoryProtection.cs
public static class MemoryProtection
{
    // 在操作完成后清除内存中的敏感数据
    public static void SecureWipe<T>(ref T[] data) where T : struct
    {
        if (data == null) return;
        
        // 采用多次覆写策略
        for (int i = 0; i < 3; i++)
        {
            // 第一遍: 使用0覆写
            for (int j = 0; j < data.Length; j++)
                data[j] = default;
                
            // 第二遍: 使用随机数据覆写
            var rnd = new Random();
            var buffer = new byte[data.Length * Marshal.SizeOf<T>()];
            rnd.NextBytes(buffer);
            Buffer.BlockCopy(buffer, 0, data, 0, buffer.Length);
            
            // 第三遍: 再次使用0覆写
            for (int j = 0; j < data.Length; j++)
                data[j] = default;
        }
        
        // 使GC尽快回收
        Array.Clear(data, 0, data.Length);
        data = null;
        GC.Collect();
    }
    
    // 使用安全分配的非托管内存
    public static IntPtr SecureAllocate(int size)
    {
        IntPtr ptr = Marshal.AllocHGlobal(size);
        
        // 使用随机数据初始化内存
        byte[] random = new byte[size];
        new Random().NextBytes(random);
        Marshal.Copy(random, 0, ptr, size);
        
        return ptr;
    }
    
    // 安全释放非托管内存
    public static void SecureFree(ref IntPtr ptr, int size)
    {
        if (ptr == IntPtr.Zero) return;
        
        // 多次覆写内存
        byte[] zeros = new byte[size];
        Marshal.Copy(zeros, 0, ptr, size);
        
        Marshal.FreeHGlobal(ptr);
        ptr = IntPtr.Zero;
    }
}
```

#### 3. 运行时完整性检查 / Runtime Integrity Check

添加防篡改和完整性检查机制：

```csharp
// IntegrityVerifier.cs
public static class IntegrityVerifier
{
    private static readonly byte[] _assemblyHash;
    
    static IntegrityVerifier()
    {
        // 在加载时计算并存储程序集哈希
        _assemblyHash = CalculateAssemblyHash(
            Assembly.GetExecutingAssembly());
    }
    
    public static bool VerifyAssemblyIntegrity()
    {
        // 重新计算哈希并比较
        byte[] currentHash = CalculateAssemblyHash(
            Assembly.GetExecutingAssembly());
            
        return CompareHashes(_assemblyHash, currentHash);
    }
    
    private static byte[] CalculateAssemblyHash(Assembly assembly)
    {
        try
        {
            using (var ms = new MemoryStream())
            {
                using (var moduleStream = assembly.GetManifestResourceStream(
                    assembly.GetName().Name + ".dll"))
                {
                    moduleStream?.CopyTo(ms);
                }
                
                using (SHA256 sha = SHA256.Create())
                {
                    return sha.ComputeHash(ms.ToArray());
                }
            }
        }
        catch
        {
            // 失败时返回预定义哈希，不抛出异常以避免信息泄露
            return GetBackupHash();
        }
    }
}
```

#### 4. 硬件钱包交互保护 / Hardware Wallet Interaction Protection

增强硬件钱包交互的安全性：

```csharp
// 修改 HardwareWalletManager.cs
public async Task<string> SignTransactionAsync(string coinType, string derivationPath, string unsignedTx)
{
    // 运行时安全验证
    if (!CustomSecurityService.VerifyRuntimeSecurity())
    {
        // 如检测到不安全环境，进行虚假操作而不显示错误
        _logger.LogWarning("检测到不安全环境，执行虚假签名");
        return GenerateFakeSignature(unsignedTx);
    }
    
    // 混淆签名流程的真实执行路径
    return await ExecuteWithProtection(() => _activeWallet.SignTransactionAsync(
        coinType, derivationPath, unsignedTx));
}

// 添加执行保护包装方法
private async Task<T> ExecuteWithProtection<T>(Func<Task<T>> operation)
{
    // 运行时二次验证与杂凑计算
    byte[] integrityHash = CalculateRuntimeIntegrityHash();
    
    // 添加时间延迟与随机性，防止分析
    await Task.Delay(new Random().Next(50, 200));
    
    try 
    {
        return await operation();
    }
    catch (Exception ex)
    {
        // 安全异常处理，避免泄露详细信息
        throw new SecurityException("Operation failed", 
            CustomSecurityService.ObfuscateException(ex));
    }
}
```

## 安全层次结构 / Security Layer Structure

```
┌─────────────────────────────────────────┐
│         应用功能层 / Application Layer   │
├─────────────────────────────────────────┤
│       自定义安全层 / Custom Security     │
│  ┌───────────────┐  ┌───────────────┐   │
│  │ 内存保护      │  │ 运行时检查    │   │
│  │ Memory        │  │ Runtime       │   │
│  │ Protection    │  │ Checks        │   │
│  └───────────────┘  └───────────────┘   │
│  ┌───────────────┐  ┌───────────────┐   │
│  │ 多层加密      │  │ 防篡改验证    │   │
│  │ Multi-layer   │  │ Anti-tampering│   │
│  │ Encryption    │  │ Verification  │   │
│  └───────────────┘  └───────────────┘   │
├─────────────────────────────────────────┤
│       开源混淆层 / Open Source Layer     │
│  ┌───────────────┐  ┌───────────────┐   │
│  │ 名称混淆      │  │ 控制流混淆    │   │
│  │ Name          │  │ Control Flow  │   │
│  │ Obfuscation   │  │ Obfuscation   │   │
│  └───────────────┘  └───────────────┘   │
│  ┌───────────────┐  ┌───────────────┐   │
│  │ 字符串加密    │  │ 资源保护      │   │
│  │ String        │  │ Resource      │   │
│  │ Encryption    │  │ Protection    │   │
│  └───────────────┘  └───────────────┘   │
└─────────────────────────────────────────┘
```

## 实施步骤 / Implementation Steps

1. **准备工作 / Preparation**
   - 下载并设置ConfuserEx2
   - 创建混淆配置文件
   - 准备构建脚本

2. **基础混淆实现 / Basic Obfuscation Implementation**
   - 配置项目后构建事件
   - 设置混淆规则和例外项
   - 测试基础混淆功能

3. **自定义安全服务实现 / Custom Security Service Implementation**
   - 创建CustomSecurityService类
   - 实现多层加密功能
   - 实现运行时安全验证

4. **内存保护实现 / Memory Protection Implementation**
   - 创建MemoryProtection类
   - 实现SecureWipe方法
   - 实现安全内存分配和释放功能

5. **完整性检查实现 / Integrity Check Implementation**
   - 创建IntegrityVerifier类
   - 实现程序集哈希计算
   - 实现运行时完整性验证

6. **应用集成 / Application Integration**
   - 在App启动时添加完整性检查
   - 在关键操作前添加运行时安全验证
   - 在敏感数据处理过程中使用内存保护

7. **测试和验证 / Testing and Verification**
   - 测试混淆效果
   - 验证完整性检查功能
   - 测试内存保护机制
   - 验证应用功能的正常运行

## 时间表 / Timeline

1. **阶段一：基础工作 (2025-03-28 - 2025-03-30)**
   - 设置开源混淆工具
   - 创建配置文件
   - 集成到构建过程

2. **阶段二：自定义安全实现 (2025-03-31 - 2025-04-05)**
   - 实现自定义加密服务
   - 实现内存保护
   - 实现完整性检查

3. **阶段三：集成和测试 (2025-04-06 - 2025-04-10)**
   - 将安全功能集成到应用
   - 测试和验证
   - 解决问题和优化

## 维护计划 / Maintenance Plan

1. **定期更新混淆工具 / Regular Tool Updates**
   - 跟踪ConfuserEx2的更新
   - 更新混淆规则和配置

2. **安全审计 / Security Audit**
   - 定期审查安全机制有效性
   - 测试抵御新型攻击的能力

3. **文档更新 / Documentation Update**
   - 保持安全实施文档的最新
   - 记录所有安全相关的变更 