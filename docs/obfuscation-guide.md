# 代码混淆指南
# Code Obfuscation Guide

> 本文档提供了关于MultiChainWallet应用程序代码混淆实现的完整指南。
> This document provides a complete guide to code obfuscation implementation for the MultiChainWallet application.

## 概述 / Overview

代码混淆是一种软件保护技术，通过重命名变量和方法名、修改控制流程和添加反调试措施等方式使软件难以被逆向工程。本项目使用ConfuserEx2进行.NET程序集混淆，实现了多层次安全策略。

Code obfuscation is a software protection technique that makes software difficult to reverse engineer by renaming variables and method names, modifying control flow, and adding anti-debugging measures. This project uses ConfuserEx2 for .NET assembly obfuscation, implementing a multi-level security strategy.

## 混淆策略 / Obfuscation Strategy

我们为MultiChainWallet应用程序实现了三层混淆保护策略：

We have implemented a three-layer obfuscation protection strategy for the MultiChainWallet application:

### 1. 标准保护（默认） / Standard Protection (Default)
- 适用于：一般代码 / Applied to: General code
- 预设：normal / Preset: normal
- 保护措施：
  - 控制流保护（部分） / Control flow protection (partial)
  - 名称混淆 / Name obfuscation
  - 常量保护 / Constants protection

### 2. 增强保护 / Enhanced Protection
- 适用于：关键业务逻辑 / Applied to: Critical business logic
- 预设：aggressive / Preset: aggressive
- 保护措施：
  - 控制流保护（增强） / Control flow protection (enhanced)
  - 名称混淆 / Name obfuscation
  - 常量保护 / Constants protection
  - 引用代理 / Reference proxies

### 3. 最大保护 / Maximum Protection
- 适用于：安全核心组件 / Applied to: Security core components
- 预设：maximum / Preset: maximum
- 保护措施：
  - 所有保护措施 / All protection measures
  - 反调试 / Anti-debugging
  - 反内存转储 / Anti-memory dumping
  - 反IL反汇编 / Anti-IL disassembly
  - 反篡改 / Anti-tampering

## 排除规则 / Exclusion Rules

为确保应用程序正常功能，以下类型被排除在混淆之外：

To ensure normal application functionality, the following types are excluded from obfuscation:

1. **公共接口和模型** / **Public interfaces and models**
   - 所有接口（I开头的类型） / All interfaces (types starting with I)
   - 模型类（Models命名空间中的类） / Model classes (classes in the Models namespace)
   - 原因：保持序列化/反序列化和反射功能 / Reason: Maintain serialization/deserialization and reflection functionality

2. **UI组件** / **UI Components**
   - ViewModel类 / ViewModel classes
   - 页面类 / Page classes
   - 控件 / Controls
   - 原因：保持XAML绑定功能 / Reason: Maintain XAML binding functionality

## 工具说明 / Tools Description

项目中提供了以下脚本工具来支持混淆流程：

The following script tools are provided in the project to support the obfuscation process:

### 1. download-confuserex.ps1
下载并设置ConfuserEx2工具。
Downloads and sets up the ConfuserEx2 tool.

**用法 / Usage:**
```powershell
./tools/download-confuserex.ps1
```

### 2. post-build-obfuscate.ps1
在构建后执行混淆，通常在发布构建中使用。
Performs obfuscation after build, typically used in release builds.

**用法 / Usage:**
```powershell
./tools/post-build-obfuscate.ps1 -ProjectDir <ProjectDir> -TargetDir <TargetDir> -TargetName <TargetName> -ConfigurationName <ConfigurationName>
```

### 3. test-obfuscation.ps1
测试混淆是否成功应用，并验证程序集功能。
Tests whether obfuscation has been successfully applied and verifies assembly functionality.

**用法 / Usage:**
```powershell
./tools/test-obfuscation.ps1 -BuildOutputPath <BuildOutputPath> -UseObfuscated:<$true|$false>
```

### 4. build-with-obfuscation.ps1
主构建脚本，集成了构建、混淆和测试流程。
Main build script that integrates build, obfuscation, and test processes.

**用法 / Usage:**
```powershell
./tools/build-with-obfuscation.ps1 [-Configuration <Configuration>] [-ProjectDir <ProjectDir>] [-SkipBuild] [-SkipTests] [-SkipObfuscation]
```

**参数 / Parameters:**
- Configuration: 构建配置，默认为"Release" / Build configuration, defaults to "Release"
- ProjectDir: 项目目录，默认为当前脚本的上一级目录 / Project directory, defaults to the parent directory of the current script
- SkipBuild: 跳过构建步骤 / Skip the build step
- SkipTests: 跳过测试步骤 / Skip the test step
- SkipObfuscation: 跳过混淆步骤 / Skip the obfuscation step

## 集成到开发流程 / Integration into Development Process

### 本地开发 / Local Development

1. **开发期间** / **During Development**
   - 使用Debug配置，不应用混淆 / Use Debug configuration, obfuscation is not applied
   - 调试和测试更容易 / Debugging and testing are easier

2. **测试混淆效果** / **Testing Obfuscation Effect**
   - 在本地执行带混淆的构建 / Run a build with obfuscation locally
   ```powershell
   ./tools/build-with-obfuscation.ps1
   ```

### CI/CD集成 / CI/CD Integration

在CI/CD管道中，您可以将混淆过程集成到发布构建中：

In the CI/CD pipeline, you can integrate the obfuscation process into release builds:

```yaml
# Azure DevOps示例 / Azure DevOps example
steps:
- task: PowerShell@2
  displayName: 'Build with Obfuscation'
  inputs:
    filePath: './tools/build-with-obfuscation.ps1'
    arguments: '-Configuration Release'
  condition: eq(variables['BuildConfiguration'], 'Release')
```

## 混淆配置文件 / Obfuscation Configuration File

混淆配置存储在项目根目录的`Confusion.crproj`文件中。该文件指定了混淆规则、保护级别和排除项。

Obfuscation configuration is stored in the `Confusion.crproj` file in the project root directory. This file specifies obfuscation rules, protection levels, and exclusions.

如需修改混淆策略，请编辑此文件并调整规则和保护级别。

To modify the obfuscation strategy, edit this file and adjust the rules and protection levels.

## 故障排除 / Troubleshooting

### 常见问题 / Common Issues

1. **XAML绑定错误** / **XAML Binding Errors**
   - 症状：UI元素未显示或绑定失败 / Symptom: UI elements not displayed or bindings fail
   - 解决方案：检查排除规则，确保ViewModel和UI类未被混淆 / Solution: Check exclusion rules, ensure ViewModels and UI classes are not obfuscated

2. **序列化/反序列化失败** / **Serialization/Deserialization Failures**
   - 症状：无法加载或保存数据 / Symptom: Unable to load or save data
   - 解决方案：确保模型类已排除在混淆之外 / Solution: Ensure model classes are excluded from obfuscation

3. **反射调用失败** / **Reflection Calls Failing**
   - 症状：使用反射的功能无法工作 / Symptom: Features using reflection do not work
   - 解决方案：添加相关类型到排除规则 / Solution: Add relevant types to exclusion rules

### 日志和诊断 / Logging and Diagnostics

混淆过程日志位于：
Obfuscation process logs are located at:

```
<ProjectDir>/bin/<Configuration>/Confused/Confused.log
```

使用此日志定位混淆过程中的问题。
Use this log to locate issues in the obfuscation process.

## 安全注意事项 / Security Considerations

1. **保护混淆配置文件** / **Protect Obfuscation Configuration**
   - 混淆配置包含有关保护策略的信息 / Obfuscation configuration contains information about protection strategies
   - 考虑在源代码管理中限制访问 / Consider restricting access in source control

2. **定期更新混淆工具** / **Regularly Update Obfuscation Tools**
   - 检查ConfuserEx2更新以获取最新保护 / Check for ConfuserEx2 updates to get the latest protections

3. **测试反混淆尝试** / **Test De-obfuscation Attempts**
   - 定期测试混淆的有效性 / Regularly test the effectiveness of obfuscation
   - 使用反混淆工具进行评估 / Use de-obfuscation tools for assessment

## 结论 / Conclusion

代码混淆是保护MultiChainWallet应用程序知识产权和敏感安全逻辑的重要部分。通过实施分层混淆策略并仔细排除关键组件，我们在维护应用程序功能的同时提供了强大的保护措施。

Code obfuscation is an important part of protecting the intellectual property and sensitive security logic of the MultiChainWallet application. By implementing a layered obfuscation strategy and carefully excluding critical components, we provide strong protection while maintaining application functionality. 