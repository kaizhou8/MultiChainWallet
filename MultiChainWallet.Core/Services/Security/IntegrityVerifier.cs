using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace MultiChainWallet.Core.Services.Security
{
    /// <summary>
    /// 完整性验证器 - 验证程序集和运行时环境的完整性
    /// Integrity verifier - Verify the integrity of assemblies and runtime environment
    /// </summary>
    public class IntegrityVerifier
    {
        private readonly ILogger<IntegrityVerifier> _logger;
        private readonly Dictionary<string, byte[]> _assemblyHashes;
        private readonly ReaderWriterLockSlim _lock;
        private byte[] _backupHash; // 作为备用的预定义哈希
        private bool _isInitialized;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public IntegrityVerifier(ILogger<IntegrityVerifier> logger)
        {
            _logger = logger;
            _assemblyHashes = new Dictionary<string, byte[]>();
            _lock = new ReaderWriterLockSlim();
            _isInitialized = false;
            
            // 初始化备用哈希
            using (var sha = SHA256.Create())
            {
                _backupHash = sha.ComputeHash(Encoding.UTF8.GetBytes("MultiChainWallet_Integrity_Verification"));
            }
        }

        /// <summary>
        /// 初始化完整性验证器，计算并存储程序集哈希
        /// Initialize integrity verifier, calculate and store assembly hashes
        /// </summary>
        public void Initialize()
        {
            try
            {
                _lock.EnterWriteLock();
                
                if (_isInitialized)
                    return;
                
                // 计算主程序集的哈希
                CalculateAndStoreAssemblyHash(Assembly.GetExecutingAssembly());
                
                // 计算其他关键程序集的哈希
                CalculateAndStoreAssemblyHash(typeof(MultiChainWallet.Core.Models.Wallet).Assembly);
                
                _isInitialized = true;
                _logger.LogInformation("完整性验证器初始化完成 / Integrity verifier initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化完整性验证器失败 / Failed to initialize integrity verifier");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 验证程序集的完整性
        /// Verify the integrity of an assembly
        /// </summary>
        /// <param name="assembly">要验证的程序集 / Assembly to verify</param>
        /// <returns>验证结果 / Verification result</returns>
        public bool VerifyAssemblyIntegrity(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            
            try
            {
                _lock.EnterReadLock();
                
                if (!_isInitialized)
                {
                    _logger.LogWarning("完整性验证器未初始化 / Integrity verifier not initialized");
                    return false;
                }
                
                string assemblyName = assembly.GetName().Name;
                
                // 如果程序集之前未被记录，先记录它的哈希
                if (!_assemblyHashes.ContainsKey(assemblyName))
                {
                    _lock.ExitReadLock();
                    try
                    {
                        _lock.EnterWriteLock();
                        if (!_assemblyHashes.ContainsKey(assemblyName))
                        {
                            CalculateAndStoreAssemblyHash(assembly);
                            _logger.LogInformation($"已将程序集 {assemblyName} 添加到完整性验证 / Added assembly {assemblyName} to integrity verification");
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                        _lock.EnterReadLock();
                    }
                }
                
                // 计算当前程序集哈希
                byte[] currentHash = CalculateAssemblyHash(assembly);
                
                // 比较与存储的哈希
                byte[] storedHash = _assemblyHashes[assemblyName];
                bool result = CompareHashes(storedHash, currentHash);
                
                if (!result)
                {
                    _logger.LogWarning($"程序集 {assemblyName} 完整性验证失败 / Assembly {assemblyName} integrity verification failed");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"验证程序集 {assembly.GetName().Name} 完整性时出错 / Error verifying assembly {assembly.GetName().Name} integrity");
                return false;
            }
            finally
            {
                if (_lock.IsReadLockHeld)
                    _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 验证当前运行的程序集的完整性
        /// Verify the integrity of currently running assemblies
        /// </summary>
        /// <returns>验证结果 / Verification result</returns>
        public bool VerifyApplicationIntegrity()
        {
            try
            {
                if (!_isInitialized)
                {
                    Initialize();
                }
                
                // 验证主程序集
                if (!VerifyAssemblyIntegrity(Assembly.GetExecutingAssembly()))
                    return false;
                
                // 验证其他关键程序集
                if (!VerifyAssemblyIntegrity(typeof(MultiChainWallet.Core.Models.Wallet).Assembly))
                    return false;
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证应用程序完整性时出错 / Error verifying application integrity");
                return false;
            }
        }

        /// <summary>
        /// 计算运行时完整性哈希，可用于防篡改检查
        /// Calculate runtime integrity hash, can be used for anti-tampering checks
        /// </summary>
        public byte[] CalculateRuntimeIntegrityHash()
        {
            try
            {
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    // 添加程序集哈希
                    _lock.EnterReadLock();
                    try
                    {
                        foreach (var hash in _assemblyHashes.Values)
                        {
                            writer.Write(hash);
                        }
                    }
                    finally
                    {
                        _lock.ExitReadLock();
                    }
                    
                    // 添加进程ID
                    writer.Write(Environment.ProcessId);
                    
                    // 添加应用程序域ID
                    writer.Write(AppDomain.CurrentDomain.Id);
                    
                    // 添加时间因素(按小时)
                    writer.Write(DateTime.UtcNow.Ticks / TimeSpan.TicksPerHour);
                    
                    // 计算综合哈希
                    using (var sha = SHA256.Create())
                    {
                        return sha.ComputeHash(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算运行时完整性哈希时出错 / Error calculating runtime integrity hash");
                return _backupHash;
            }
        }

        #region Private Methods

        private void CalculateAndStoreAssemblyHash(Assembly assembly)
        {
            if (assembly == null) return;
            
            string assemblyName = assembly.GetName().Name;
            byte[] hash = CalculateAssemblyHash(assembly);
            
            _assemblyHashes[assemblyName] = hash;
        }

        private byte[] CalculateAssemblyHash(Assembly assembly)
        {
            try
            {
                string assemblyLocation = assembly.Location;
                
                // 如果程序集位置可用，直接读取文件计算哈希
                if (!string.IsNullOrEmpty(assemblyLocation) && File.Exists(assemblyLocation))
                {
                    using (var fileStream = new FileStream(assemblyLocation, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var sha = SHA256.Create())
                    {
                        return sha.ComputeHash(fileStream);
                    }
                }
                
                // 否则尝试通过嵌入资源获取
                using (var ms = new MemoryStream())
                {
                    string resourceName = assembly.GetName().Name + ".dll";
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            stream.CopyTo(ms);
                            using (var sha = SHA256.Create())
                            {
                                return sha.ComputeHash(ms.ToArray());
                            }
                        }
                    }
                }
                
                // 如果上述方法都失败，使用备用哈希方法
                return GenerateBackupHash(assembly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"计算程序集 {assembly.GetName().Name} 哈希时出错 / Error calculating hash for assembly {assembly.GetName().Name}");
                return GenerateBackupHash(assembly);
            }
        }

        private byte[] GenerateBackupHash(Assembly assembly)
        {
            // 使用程序集名称和公钥标记生成备用哈希
            using (var sha = SHA256.Create())
            {
                var data = Encoding.UTF8.GetBytes(
                    assembly.GetName().Name + "_" +
                    string.Join("", assembly.GetName().GetPublicKeyToken().Select(b => b.ToString("x2")))
                );
                return sha.ComputeHash(data);
            }
        }

        private bool CompareHashes(byte[] hash1, byte[] hash2)
        {
            if (hash1 == null || hash2 == null || hash1.Length != hash2.Length)
                return false;
            
            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                    return false;
            }
            
            return true;
        }

        #endregion
    }
} 