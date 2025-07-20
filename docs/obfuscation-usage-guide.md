# 代码混淆使用指南
# Code Obfuscation Usage Guide

*最后更新时间 / Last updated: 2023-06-16*

## 概述 / Overview

本指南提供了如何使用MultiChainWallet项目中的代码混淆工具的详细说明。这些工具旨在保护应用程序的关键部分免受逆向工程和篡改。

This guide provides detailed instructions on how to use the code obfuscation tools in the MultiChainWallet project. These tools are designed to protect critical parts of the application from reverse engineering and tampering.

## 前提条件 / Prerequisites

- Windows操作系统 / Windows operating system
- PowerShell 5.0或更高版本 / PowerShell 5.0 or higher
- .NET SDK 8.0或更高版本 / .NET SDK 8.0 or higher
- 互联网连接（用于首次下载ConfuserEx工具） / Internet connection (for first-time download of ConfuserEx tool)

## 可用脚本 / Available Scripts

项目中包含以下混淆相关脚本，位于`tools`目录下：

The project includes the following obfuscation-related scripts, located in the `tools` directory:

1. **download-confuserex.ps1** - 下载ConfuserEx混淆工具 / Downloads the ConfuserEx obfuscation tool
2. **post-build-obfuscate.ps1** - 构建后混淆处理脚本 / Post-build obfuscation processing script
3. **build-with-obfuscation.ps1** - 完整的构建和混淆流程 / Complete build and obfuscation process
4. **test-obfuscation.ps1** - 测试混淆有效性 / Tests obfuscation effectiveness
5. **simple-obfuscation.ps1** - 简化的混淆流程 / Simplified obfuscation process

## 基本使用流程 / Basic Usage Process

### 方法1：使用简化混淆脚本（推荐） / Method 1: Using the Simplified Obfuscation Script (Recommended)

这是最简单的方法，适合大多数开发者使用：

This is the simplest method, suitable for most developers:

1. 打开PowerShell终端 / Open a PowerShell terminal
2. 导航到项目根目录 / Navigate to the project root directory
3. 执行以下命令 / Execute the following command:

```powershell
cd tools
.\simple-obfuscation.ps1
```

脚本将自动：
The script will automatically:

- 检查是否已构建项目，如果没有，提示你构建 / Check if the project has been built, and prompt you to build if not
- 下载ConfuserEx工具（如果需要） / Download the ConfuserEx tool (if needed)
- 执行混淆过程 / Perform the obfuscation process
- 显示混淆后的文件位置 / Display the location of obfuscated files

### 方法2：完整构建和混淆流程 / Method 2: Complete Build and Obfuscation Process

如果你需要更多控制，可以使用完整的构建和混淆脚本：

If you need more control, you can use the complete build and obfuscation script:

```powershell
cd tools
.\build-with-obfuscation.ps1 -Configuration Release
```

可选参数 / Optional parameters:
- `-SkipBuild` - 跳过构建步骤 / Skip the build step
- `-SkipTests` - 跳过测试步骤 / Skip the test step
- `-SkipObfuscation` - 跳过混淆步骤 / Skip the obfuscation step
- `-ProjectDir <path>` - 指定项目目录 / Specify the project directory

### 方法3：仅测试混淆有效性 / Method 3: Test Obfuscation Effectiveness Only

如果你只想测试混淆是否有效：

If you only want to test if the obfuscation is effective:

```powershell
cd tools
.\test-obfuscation.ps1 -BuildOutputPath "..\bin\Release\net8.0" -UseObfuscated:$true
```

## 混淆配置 / Obfuscation Configuration

混淆配置存储在项目根目录的`Confusion.crproj`文件中。如果需要修改混淆规则，请编辑此文件。

The obfuscation configuration is stored in the `Confusion.crproj` file in the project root directory. If you need to modify the obfuscation rules, please edit this file.

### 主要配置部分 / Main Configuration Sections

1. **全局默认保护** / **Global Default Protection**
   ```xml
   <rule pattern="true" preset="normal" inherit="false">
     <protection id="anti ildasm" />
     <protection id="anti tamper" />
     <protection id="constants" />
     <protection id="rename" />
     <protection id="ref proxy" />
   </rule>
   ```

2. **特定组件的增强保护** / **Enhanced Protection for Specific Components**
   ```xml
   <rule pattern="MultiChainWallet.Core.Services.Security.*" preset="maximum" inherit="false">
     <!-- 保护设置 / Protection settings -->
   </rule>
   ```

3. **排除规则** / **Exclusion Rules**
   ```xml
   <rule pattern="MultiChainWallet.Core.Interfaces.*" preset="none" inherit="false">
     <protection id="rename" action="remove" />
   </rule>
   ```

## 输出文件 / Output Files

混淆后的文件将位于项目根目录下的`Confused`文件夹中。主要输出包括：

Obfuscated files will be located in the `Confused` folder under the project root directory. Main outputs include:

- `MultiChainWallet.Core.dll` - 混淆后的核心库 / Obfuscated core library
- `MultiChainWallet.Infrastructure.dll` - 混淆后的基础设施库 / Obfuscated infrastructure library
- `MultiChainWallet.UI.dll` - 混淆后的UI库 / Obfuscated UI library
- `Confused.log` - 混淆过程日志 / Obfuscation process log

## 故障排除 / Troubleshooting

### 常见问题 / Common Issues

1. **"找不到ConfuserEx工具"错误** / **"ConfuserEx tool not found" error**
   - 解决方案：手动运行`download-confuserex.ps1`脚本 / Solution: Manually run the `download-confuserex.ps1` script
   - 检查网络连接 / Check network connection

2. **"混淆配置文件不存在"错误** / **"Obfuscation configuration file does not exist" error**
   - 解决方案：确保`Confusion.crproj`文件位于项目根目录 / Solution: Ensure the `Confusion.crproj` file is in the project root directory

3. **应用程序运行时错误** / **Application runtime errors**
   - 解决方案：检查排除规则，确保关键类型未被混淆 / Solution: Check exclusion rules to ensure critical types are not obfuscated
   - 尝试使用`test-obfuscation.ps1`脚本验证混淆 / Try using the `test-obfuscation.ps1` script to validate obfuscation

### 日志文件 / Log Files

混淆过程的详细日志可在以下位置找到：

Detailed logs of the obfuscation process can be found at:

```
<ProjectRoot>/Confused/Confused.log
```

## CI/CD集成 / CI/CD Integration

项目已配置GitHub Actions工作流，在每次推送到主分支或发布分支时自动执行混淆：

The project has configured GitHub Actions workflows to automatically perform obfuscation on each push to the main or release branches:

- 工作流文件：`.github/workflows/release-with-obfuscation.yml`
- Workflow file: `.github/workflows/release-with-obfuscation.yml`

如果你需要在本地CI/CD环境中集成混淆，可以参考此工作流文件的配置。

If you need to integrate obfuscation in a local CI/CD environment, you can refer to the configuration in this workflow file.

## 最佳实践 / Best Practices

1. **定期更新混淆工具** / **Regularly update the obfuscation tool**
   - 检查ConfuserEx的最新版本 / Check for the latest version of ConfuserEx
   - 更新`download-confuserex.ps1`中的下载URL / Update the download URL in `download-confuserex.ps1`

2. **测试混淆后的应用程序** / **Test the obfuscated application**
   - 在发布前全面测试混淆后的应用程序 / Thoroughly test the obfuscated application before release
   - 使用`test-obfuscation.ps1`验证混淆有效性 / Use `test-obfuscation.ps1` to verify obfuscation effectiveness

3. **保持混淆配置的安全** / **Keep the obfuscation configuration secure**
   - 限制对`Confusion.crproj`文件的访问 / Restrict access to the `Confusion.crproj` file
   - 不要在公共场所分享混淆配置详情 / Do not share obfuscation configuration details in public

4. **平衡保护和性能** / **Balance protection and performance**
   - 过度混淆可能影响性能 / Excessive obfuscation may affect performance
   - 只对关键安全组件应用最高级别的保护 / Apply the highest level of protection only to critical security components

## 性能优化 / Performance Optimization

混淆可能会对应用程序性能产生一定影响，特别是在涉及反射操作的场景中。我们提供了以下工具和技术来评估和优化混淆后代码的性能：

Obfuscation may have some impact on application performance, especially in scenarios involving reflection operations. We provide the following tools and techniques to evaluate and optimize the performance of obfuscated code:

### 性能测量工具 / Performance Measurement Tools

1. **measure-obfuscation-performance.ps1**
   - 位置：`tools/measure-obfuscation-performance.ps1`
   - 功能：比较原始和混淆后程序集的性能差异
   - 用法：
   ```powershell
   cd tools
   .\measure-obfuscation-performance.ps1 -OriginalPath "..\bin\Release\net8.0" -ObfuscatedPath "..\Confused"
   ```
   - 输出：详细的性能比较报告，包括启动时间、反射操作和关键功能执行时间

   - Location: `tools/measure-obfuscation-performance.ps1`
   - Function: Compare performance differences between original and obfuscated assemblies
   - Usage:
   ```powershell
   cd tools
   .\measure-obfuscation-performance.ps1 -OriginalPath "..\bin\Release\net8.0" -ObfuscatedPath "..\Confused"
   ```
   - Output: Detailed performance comparison report, including startup time, reflection operations, and execution time of key functions

### 配置优化工具 / Configuration Optimization Tools

1. **optimize-obfuscation-config.ps1**
   - 位置：`tools/optimize-obfuscation-config.ps1`
   - 功能：根据性能测试结果自动调整混淆配置
   - 用法：
   ```powershell
   cd tools
   .\optimize-obfuscation-config.ps1 -ConfigFile "..\Confusion.crproj" -PerformanceReport "..\performance-report.json"
   ```
   - 输出：优化后的混淆配置文件

   - Location: `tools/optimize-obfuscation-config.ps1`
   - Function: Automatically adjust obfuscation configuration based on performance test results
   - Usage:
   ```powershell
   cd tools
   .\optimize-obfuscation-config.ps1 -ConfigFile "..\Confusion.crproj" -PerformanceReport "..\performance-report.json"
   ```
   - Output: Optimized obfuscation configuration file

2. **apply-optimized-config.ps1**
   - 位置：`tools/apply-optimized-config.ps1`
   - 功能：应用优化后的混淆配置并执行混淆
   - 用法：
   ```powershell
   cd tools
   .\apply-optimized-config.ps1 -OptimizedConfigFile "..\Confusion.optimized.crproj"
   ```

   - Location: `tools/apply-optimized-config.ps1`
   - Function: Apply optimized obfuscation configuration and perform obfuscation
   - Usage:
   ```powershell
   cd tools
   .\apply-optimized-config.ps1 -OptimizedConfigFile "..\Confusion.optimized.crproj"
   ```

### 反射性能优化 / Reflection Performance Optimization

为了减轻混淆对反射操作的性能影响，项目中包含了以下辅助类：

To mitigate the performance impact of obfuscation on reflection operations, the project includes the following helper classes:

1. **ReflectionHelper**
   - 位置：`MultiChainWallet.Core/Utilities/ReflectionHelper.cs`
   - 功能：通过缓存机制优化反射操作性能
   - 用法示例：
   ```csharp
   // 替代直接使用反射 / Instead of using reflection directly
   // var method = type.GetMethod("MethodName");
   
   // 使用ReflectionHelper / Use ReflectionHelper
   var method = ReflectionHelper.GetMethod(type, "MethodName");
   ```

   - Location: `MultiChainWallet.Core/Utilities/ReflectionHelper.cs`
   - Function: Optimize reflection operation performance through caching mechanisms
   - Usage example:
   ```csharp
   // Instead of using reflection directly
   // var method = type.GetMethod("MethodName");
   
   // Use ReflectionHelper
   var method = ReflectionHelper.GetMethod(type, "MethodName");
   ```

2. **AppStartupOptimizer**
   - 位置：`MultiChainWallet.Core/Utilities/AppStartupOptimizer.cs`
   - 功能：在应用程序启动时预热反射缓存
   - 用法：在应用程序启动时调用
   ```csharp
   // 在应用程序启动时 / During application startup
   AppStartupOptimizer.Initialize();
   ```

   - Location: `MultiChainWallet.Core/Utilities/AppStartupOptimizer.cs`
   - Function: Pre-warm reflection caches during application startup
   - Usage: Call during application startup
   ```csharp
   // During application startup
   AppStartupOptimizer.Initialize();
   ```

### 性能优化最佳实践 / Performance Optimization Best Practices

1. **使用反射辅助类** / **Use Reflection Helper Classes**
   - 在代码中使用`ReflectionHelper`代替直接的反射操作
   - Use `ReflectionHelper` instead of direct reflection operations in your code

2. **预热关键类型** / **Pre-warm Critical Types**
   - 在应用程序启动时调用`AppStartupOptimizer.Initialize()`
   - Call `AppStartupOptimizer.Initialize()` during application startup

3. **定期测量性能** / **Measure Performance Regularly**
   - 使用`measure-obfuscation-performance.ps1`定期评估混淆对性能的影响
   - Use `measure-obfuscation-performance.ps1` to regularly assess the impact of obfuscation on performance

4. **针对性优化** / **Targeted Optimization**
   - 对性能关键路径上的类型减少混淆强度
   - Reduce obfuscation intensity for types on performance-critical paths
   - 考虑将高频访问但安全性要求较低的类型添加到排除列表中
   - Consider adding high-frequency access but low-security requirement types to the exclusion list

5. **平衡安全与性能** / **Balance Security and Performance**
   - 使用`optimize-obfuscation-config.ps1`自动平衡安全性和性能
   - Use `optimize-obfuscation-config.ps1` to automatically balance security and performance
   - 对不同模块应用不同级别的混淆，根据其安全重要性和性能敏感性
   - Apply different levels of obfuscation to different modules based on their security importance and performance sensitivity

## 结论 / Conclusion

正确使用代码混淆工具可以显著提高MultiChainWallet应用程序的安全性，防止逆向工程和知识产权盗窃。按照本指南中的步骤，你可以轻松地将混淆集成到你的开发和发布流程中。

Properly using code obfuscation tools can significantly enhance the security of the MultiChainWallet application, preventing reverse engineering and intellectual property theft. By following the steps in this guide, you can easily integrate obfuscation into your development and release process.

通过利用本指南中描述的性能优化工具和技术，你可以在保持强大安全保护的同时，确保应用程序的性能不受显著影响。平衡安全性和性能是成功应用代码混淆的关键，我们提供的工具链使这一平衡变得更加容易实现。 