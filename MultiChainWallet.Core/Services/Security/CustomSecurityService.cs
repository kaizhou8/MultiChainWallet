using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Management;
using Microsoft.Extensions.Logging;

namespace MultiChainWallet.Core.Services.Security
{
    /// <summary>
    /// 自定义安全服务 - 提供多层加密和运行时安全验证功能
    /// Custom security service - Provides multi-layer encryption and runtime security verification
    /// </summary>
    public class CustomSecurityService
    {
        private readonly ILogger<CustomSecurityService> _logger;
        private static readonly byte[] _staticSalt = new byte[] { 0x43, 0x87, 0x23, 0x72, 0x45, 0x56, 0x68, 0x92, 0x54, 0x23, 0x89, 0x29, 0x76, 0x65, 0x43, 0x21 };
        private readonly string _sessionId;

        public CustomSecurityService(ILogger<CustomSecurityService> logger)
        {
            _logger = logger;
            // 每个会话生成唯一会话ID
            _sessionId = Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// 使用多层加密保护关键数据
        /// Protect critical data using multi-layer encryption
        /// </summary>
        public byte[] ProtectCriticalData(byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0)
                {
                    throw new ArgumentNullException(nameof(data));
                }

                // 第一层: 设备指纹相关加密
                byte[] layer1 = AesEncrypt(data, CombineKeys(GetDeviceFingerprint(), _staticSalt));
                
                // 第二层: 时间和会话相关加密
                byte[] layer2 = AesEncrypt(layer1, CombineKeys(GetSessionKey(), Encoding.UTF8.GetBytes(_sessionId)));
                
                // 第三层: 用户密码派生密钥加密 (如果可用)
                byte[] userKey = GetUserDerivedKey();
                if (userKey != null && userKey.Length > 0)
                {
                    return AesEncrypt(layer2, userKey);
                }
                
                return layer2;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保护关键数据失败 / Failed to protect critical data");
                throw new SecurityException("数据加密失败 / Data encryption failed", ex);
            }
        }

        /// <summary>
        /// 解密受保护的数据
        /// Decrypt protected data
        /// </summary>
        public byte[] UnprotectCriticalData(byte[] protectedData)
        {
            try
            {
                if (protectedData == null || protectedData.Length == 0)
                {
                    throw new ArgumentNullException(nameof(protectedData));
                }

                byte[] layer2 = protectedData;
                
                // 第三层解密: 用户密码派生密钥 (如果可用)
                byte[] userKey = GetUserDerivedKey();
                if (userKey != null && userKey.Length > 0)
                {
                    layer2 = AesDecrypt(protectedData, userKey);
                }
                
                // 第二层解密: 会话相关密钥
                byte[] layer1 = AesDecrypt(layer2, CombineKeys(GetSessionKey(), Encoding.UTF8.GetBytes(_sessionId)));
                
                // 第一层解密: 设备指纹
                return AesDecrypt(layer1, CombineKeys(GetDeviceFingerprint(), _staticSalt));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解密受保护数据失败 / Failed to unprotect critical data");
                throw new SecurityException("数据解密失败 / Data decryption failed", ex);
            }
        }

        /// <summary>
        /// 验证运行时环境安全性
        /// Verify runtime environment security
        /// </summary>
        public bool VerifyRuntimeSecurity()
        {
            try
            {
                bool isSecure = true;
                
                // 检测调试器
                if (IsDebugged())
                {
                    _logger.LogWarning("检测到调试器附加 / Debugger detected");
                    isSecure = false;
                }
                
                // 检测虚拟机环境
                if (IsRunningInVirtualMachine())
                {
                    _logger.LogWarning("检测到虚拟机环境 / Virtual machine environment detected");
                    isSecure = false;
                }
                
                // 检测内存分析工具
                if (IsMemoryAnalyzerDetected())
                {
                    _logger.LogWarning("检测到内存分析工具 / Memory analyzer detected");
                    isSecure = false;
                }
                
                return isSecure;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安全验证失败 / Security verification failed");
                // 出错时默认认为不安全
                return false;
            }
        }

        /// <summary>
        /// 混淆异常信息，防止泄露敏感信息
        /// Obfuscate exception information to prevent sensitive information leakage
        /// </summary>
        public Exception ObfuscateException(Exception ex)
        {
            if (ex == null) return null;
            
            // 创建新的安全异常，不包含原始异常的堆栈信息
            return new SecurityException("操作失败，请重试 / Operation failed, please try again");
        }

        /// <summary>
        /// 计算运行时完整性哈希，用于检测代码是否被篡改
        /// Calculate runtime integrity hash for detecting code tampering
        /// </summary>
        public byte[] CalculateRuntimeIntegrityHash()
        {
            try
            {
                // 获取当前程序集
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                
                using (var ms = new MemoryStream())
                {
                    using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".dll"))
                    {
                        if (stream != null)
                        {
                            stream.CopyTo(ms);
                        }
                    }
                    
                    // 使用SHA256计算哈希
                    using (SHA256 sha = SHA256.Create())
                    {
                        return sha.ComputeHash(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算完整性哈希失败 / Failed to calculate integrity hash");
                // 失败时返回空哈希
                return new byte[32];
            }
        }

        #region Private Methods

        /// <summary>
        /// 使用AES加密数据
        /// Encrypt data using AES
        /// </summary>
        private byte[] AesEncrypt(byte[] data, byte[] key)
        {
            if (data == null || key == null)
                return null;

            using (var aes = Aes.Create())
            {
                aes.Key = DeriveKeyFromBytes(key, 32); // 使用密钥派生确保长度为32字节
                aes.GenerateIV(); // 生成随机IV

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    // 写入IV
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// 使用AES解密数据
        /// Decrypt data using AES
        /// </summary>
        private byte[] AesDecrypt(byte[] encryptedData, byte[] key)
        {
            if (encryptedData == null || key == null || encryptedData.Length <= 16)
                return null;

            using (var aes = Aes.Create())
            {
                aes.Key = DeriveKeyFromBytes(key, 32); // 使用密钥派生确保长度为32字节
                byte[] iv = new byte[16];
                
                // 读取IV (前16字节)
                Array.Copy(encryptedData, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedData, iv.Length, encryptedData.Length - iv.Length);
                        cs.FlushFinalBlock();
                    }

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// 从字节数组派生指定长度的密钥
        /// Derive a key of specified length from a byte array
        /// </summary>
        private byte[] DeriveKeyFromBytes(byte[] input, int length)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(
                input, 
                _staticSalt, 
                10000, 
                HashAlgorithmName.SHA256))
            {
                return deriveBytes.GetBytes(length);
            }
        }

        /// <summary>
        /// 组合多个密钥
        /// Combine multiple keys
        /// </summary>
        private byte[] CombineKeys(byte[] key1, byte[] key2)
        {
            if (key1 == null || key2 == null)
                return key1 ?? key2 ?? new byte[0];

            using (var hmac = new HMACSHA256(key1))
            {
                return hmac.ComputeHash(key2);
            }
        }

        /// <summary>
        /// 获取设备指纹
        /// Get device fingerprint
        /// </summary>
        private byte[] GetDeviceFingerprint()
        {
            StringBuilder sb = new StringBuilder();
            
            // 处理器ID
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (var mo in searcher.Get())
                    {
                        sb.Append(mo["ProcessorId"]?.ToString() ?? "");
                    }
                }
            }
            catch 
            {
                // 忽略异常
                sb.Append(Environment.ProcessorCount);
            }
            
            // 主板序列号
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (var mo in searcher.Get())
                    {
                        sb.Append(mo["SerialNumber"]?.ToString() ?? "");
                    }
                }
            }
            catch 
            {
                // 忽略异常
                sb.Append(Environment.MachineName);
            }
            
            // 操作系统信息
            sb.Append(Environment.OSVersion.ToString());
            
            // 机器名称
            sb.Append(Environment.MachineName);
            
            // 用户名
            sb.Append(Environment.UserName);
            
            // 转换为字节数组
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            }
        }

        /// <summary>
        /// 获取会话密钥（基于时间和随机因素）
        /// Get session key (based on time and random factors)
        /// </summary>
        private byte[] GetSessionKey()
        {
            using (SHA256 sha = SHA256.Create())
            {
                // 使用当前日期、会话ID和随机数生成会话密钥
                string sessionData = $"{DateTime.UtcNow.Date.Ticks}_{_sessionId}_{Guid.NewGuid()}";
                return sha.ComputeHash(Encoding.UTF8.GetBytes(sessionData));
            }
        }

        /// <summary>
        /// 获取用户派生密钥（如果可用）
        /// Get user derived key (if available)
        /// </summary>
        private byte[] GetUserDerivedKey()
        {
            // 在实际实现中，这将从安全的用户凭据存储中获取
            // TODO: 实现密码派生密钥的获取
            return null;
        }

        /// <summary>
        /// 检测是否有调试器附加
        /// Detect if a debugger is attached
        /// </summary>
        private bool IsDebugged()
        {
            // 检查系统调试器
            if (Debugger.IsAttached)
                return true;
            
            // 检查远程调试器
            if (IsRemoteDebuggerPresent())
                return true;
            
            return false;
        }

        /// <summary>
        /// 检测是否正在虚拟机中运行
        /// Detect if running in a virtual machine
        /// </summary>
        private bool IsRunningInVirtualMachine()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                {
                    foreach (var mo in searcher.Get())
                    {
                        string manufacturer = mo["Manufacturer"]?.ToString() ?? "";
                        string model = mo["Model"]?.ToString() ?? "";
                        
                        if (manufacturer.Contains("VMware") || 
                            manufacturer.Contains("Virtual") || 
                            model.Contains("Virtual") ||
                            model.Contains("VMware") ||
                            manufacturer.Contains("Xen") ||
                            manufacturer.Contains("innotek GmbH") || // VirtualBox
                            model.Contains("VirtualBox"))
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // 忽略异常
            }
            
            return false;
        }

        /// <summary>
        /// 检测是否存在内存分析工具
        /// Detect if memory analyzer tools are present
        /// </summary>
        private bool IsMemoryAnalyzerDetected()
        {
            try
            {
                // 检查常见内存分析工具的进程名称
                string[] suspiciousProcessNames = new string[] 
                {
                    "CheatEngine", "OllyDbg", "x64dbg", "x32dbg", "dnSpy", "ILSpy",
                    "Reflector", "de4dot", "Fiddler", "WireShark", "ProcessHacker"
                };
                
                Process[] runningProcesses = Process.GetProcesses();
                foreach (Process process in runningProcesses)
                {
                    try
                    {
                        if (suspiciousProcessNames.Any(name => 
                            process.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // 忽略单个进程检查异常
                    }
                }
            }
            catch
            {
                // 忽略异常
            }
            
            return false;
        }

        /// <summary>
        /// 检测是否有远程调试器附加
        /// Detect if a remote debugger is attached
        /// </summary>
        private bool IsRemoteDebuggerPresent()
        {
            // 这是一个占位符，在实际实现中应检查远程调试器
            return false;
        }

        #endregion
    }
} 