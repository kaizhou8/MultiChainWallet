using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 安全的私钥管理器
    /// Secure private key manager
    /// </summary>
    public class SecurePrivateKeyManager : IDisposable
    {
        private readonly EnhancedSecurityService _securityService;
        private readonly ILogger<SecurePrivateKeyManager> _logger;
        private readonly Dictionary<string, PrivateKeyContainer> _privateKeys;
        private readonly SemaphoreSlim _semaphore;
        private readonly Timer _cleanupTimer;
        private readonly TimeSpan _keyExpirationTime;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public SecurePrivateKeyManager(
            EnhancedSecurityService securityService,
            ILogger<SecurePrivateKeyManager> logger)
        {
            _securityService = securityService;
            _logger = logger;
            _privateKeys = new Dictionary<string, PrivateKeyContainer>();
            _semaphore = new SemaphoreSlim(1, 1);
            _keyExpirationTime = TimeSpan.FromMinutes(5); // 5分钟后过期 / Expire after 5 minutes
            
            // 创建定期清理过期密钥的定时器
            // Create timer for periodic cleanup of expired keys
            _cleanupTimer = new Timer(CleanupExpiredKeys, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// 加载私钥
        /// Load private key
        /// </summary>
        public async Task<string> LoadPrivateKeyAsync(string walletId, string encryptedPrivateKey, string password)
        {
            try
            {
                await _semaphore.WaitAsync();
                
                // 检查是否已经加载
                // Check if already loaded
                if (_privateKeys.TryGetValue(walletId, out var container) && !container.IsExpired)
                {
                    container.LastAccessTime = DateTime.UtcNow;
                    return container.PrivateKey;
                }
                
                // 解密私钥
                // Decrypt private key
                string privateKey = await _securityService.DecryptPrivateKeyAsync(encryptedPrivateKey, password);
                
                // 存储在内存中
                // Store in memory
                _privateKeys[walletId] = new PrivateKeyContainer
                {
                    PrivateKey = privateKey,
                    LastAccessTime = DateTime.UtcNow
                };
                
                return privateKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载私钥失败 / Failed to load private key");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 获取私钥（如果已加载）
        /// Get private key (if loaded)
        /// </summary>
        public async Task<string> GetPrivateKeyAsync(string walletId)
        {
            try
            {
                await _semaphore.WaitAsync();
                
                if (_privateKeys.TryGetValue(walletId, out var container) && !container.IsExpired)
                {
                    container.LastAccessTime = DateTime.UtcNow;
                    return container.PrivateKey;
                }
                
                return null; // 未加载或已过期 / Not loaded or expired
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 清除私钥
        /// Clear private key
        /// </summary>
        public async Task ClearPrivateKeyAsync(string walletId)
        {
            try
            {
                await _semaphore.WaitAsync();
                
                if (_privateKeys.TryGetValue(walletId, out var container))
                {
                    // 安全地清除内存
                    // Securely clear memory
                    if (container.PrivateKey != null)
                    {
                        byte[] privateKeyBytes = System.Text.Encoding.UTF8.GetBytes(container.PrivateKey);
                        _securityService.SecurelyClearMemory(privateKeyBytes);
                        container.PrivateKey = null;
                    }
                    
                    _privateKeys.Remove(walletId);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 清除所有私钥
        /// Clear all private keys
        /// </summary>
        public async Task ClearAllPrivateKeysAsync()
        {
            try
            {
                await _semaphore.WaitAsync();
                
                foreach (var container in _privateKeys.Values)
                {
                    if (container.PrivateKey != null)
                    {
                        byte[] privateKeyBytes = System.Text.Encoding.UTF8.GetBytes(container.PrivateKey);
                        _securityService.SecurelyClearMemory(privateKeyBytes);
                        container.PrivateKey = null;
                    }
                }
                
                _privateKeys.Clear();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 清理过期的密钥
        /// Cleanup expired keys
        /// </summary>
        private async void CleanupExpiredKeys(object state)
        {
            try
            {
                await _semaphore.WaitAsync();
                
                var keysToRemove = new List<string>();
                
                foreach (var kvp in _privateKeys)
                {
                    if (kvp.Value.IsExpired)
                    {
                        keysToRemove.Add(kvp.Key);
                        
                        // 安全地清除内存
                        // Securely clear memory
                        if (kvp.Value.PrivateKey != null)
                        {
                            byte[] privateKeyBytes = System.Text.Encoding.UTF8.GetBytes(kvp.Value.PrivateKey);
                            _securityService.SecurelyClearMemory(privateKeyBytes);
                            kvp.Value.PrivateKey = null;
                        }
                    }
                }
                
                foreach (var key in keysToRemove)
                {
                    _privateKeys.Remove(key);
                }
                
                if (keysToRemove.Count > 0)
                {
                    _logger.LogInformation("已清理 {Count} 个过期的私钥 / Cleaned up {Count} expired private keys", keysToRemove.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期私钥时发生错误 / Error occurred while cleaning up expired private keys");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _semaphore?.Dispose();
            ClearAllPrivateKeysAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 私钥容器类
        /// Private key container class
        /// </summary>
        private class PrivateKeyContainer
        {
            /// <summary>
            /// 私钥
            /// Private key
            /// </summary>
            public string PrivateKey { get; set; }
            
            /// <summary>
            /// 最后访问时间
            /// Last access time
            /// </summary>
            public DateTime LastAccessTime { get; set; }
            
            /// <summary>
            /// 是否已过期
            /// Whether expired
            /// </summary>
            public bool IsExpired => DateTime.UtcNow - LastAccessTime > TimeSpan.FromMinutes(5);
        }
    }
} 