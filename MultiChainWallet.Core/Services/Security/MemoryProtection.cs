using System;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Extensions.Logging;

namespace MultiChainWallet.Core.Services.Security
{
    /// <summary>
    /// 内存保护工具类 - 提供安全内存管理和敏感数据清除功能
    /// Memory protection utility class - Provides secure memory management and sensitive data clearing
    /// </summary>
    public static class MemoryProtection
    {
        private static readonly object _lock = new object();
        private static ILogger _logger;

        /// <summary>
        /// 设置日志记录器
        /// Set the logger
        /// </summary>
        public static void SetLogger(ILogger logger)
        {
            lock (_lock)
            {
                _logger = logger;
            }
        }

        /// <summary>
        /// 安全擦除内存中的敏感数据
        /// Securely wipe sensitive data from memory
        /// </summary>
        /// <typeparam name="T">数据类型 / Data type</typeparam>
        /// <param name="data">要擦除的数据数组 / Data array to wipe</param>
        public static void SecureWipe<T>(ref T[] data) where T : struct
        {
            if (data == null) return;
            
            try
            {
                int size = data.Length * Marshal.SizeOf<T>();
                
                // 采用多次覆写策略
                for (int i = 0; i < 3; i++)
                {
                    // 第一遍: 使用0覆写
                    for (int j = 0; j < data.Length; j++)
                        data[j] = default;
                    
                    // 第二遍: 使用随机数据覆写
                    var rnd = new Random();
                    var buffer = new byte[size];
                    rnd.NextBytes(buffer);
                    
                    // 将随机字节复制到结构体数组
                    GCHandle gch = GCHandle.Alloc(data, GCHandleType.Pinned);
                    try
                    {
                        IntPtr targetPtr = gch.AddrOfPinnedObject();
                        Marshal.Copy(buffer, 0, targetPtr, buffer.Length);
                    }
                    finally
                    {
                        gch.Free();
                    }
                    
                    // 第三遍: 再次使用0覆写
                    for (int j = 0; j < data.Length; j++)
                        data[j] = default;
                }
                
                // 使GC尽快回收
                Array.Clear(data, 0, data.Length);
                data = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                LogError(ex, "擦除内存数据失败 / Failed to wipe memory data");
                
                // 即使出错，也尝试使用标准方法清除数据
                if (data != null)
                {
                    Array.Clear(data, 0, data.Length);
                    data = null;
                }
            }
        }
        
        /// <summary>
        /// 安全擦除内存中的敏感字节数据
        /// Securely wipe sensitive byte data from memory
        /// </summary>
        public static void SecureWipeBytes(ref byte[] data)
        {
            SecureWipe(ref data);
        }

        /// <summary>
        /// 安全擦除内存中的敏感字符串数据
        /// Securely wipe sensitive string data
        /// </summary>
        public static void SecureWipeString(ref string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            
            try
            {
                // 将字符串转换为字符数组以便清除
                char[] chars = data.ToCharArray();
                
                // 覆写字符数组
                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = '\0';
                }
                
                // 设置为null并促进垃圾回收
                data = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                LogError(ex, "擦除字符串数据失败 / Failed to wipe string data");
                data = null;
            }
        }

        /// <summary>
        /// 安全分配非托管内存
        /// Securely allocate unmanaged memory
        /// </summary>
        /// <param name="size">要分配的字节数 / Number of bytes to allocate</param>
        /// <returns>指向分配内存的指针 / Pointer to allocated memory</returns>
        public static IntPtr SecureAllocate(int size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            
            try
            {
                // 分配非托管内存
                IntPtr ptr = Marshal.AllocHGlobal(size);
                
                // 使用随机数据初始化内存，避免使用未初始化的内存
                byte[] random = new byte[size];
                new Random().NextBytes(random);
                Marshal.Copy(random, 0, ptr, size);
                
                SecureWipeBytes(ref random);
                
                return ptr;
            }
            catch (Exception ex)
            {
                LogError(ex, "安全分配内存失败 / Failed to securely allocate memory");
                throw new SecurityException("无法安全分配内存 / Cannot securely allocate memory", ex);
            }
        }

        /// <summary>
        /// 安全释放非托管内存
        /// Securely free unmanaged memory
        /// </summary>
        /// <param name="ptr">要释放的内存指针引用 / Reference to memory pointer to free</param>
        /// <param name="size">内存大小 / Memory size</param>
        public static void SecureFree(ref IntPtr ptr, int size)
        {
            if (ptr == IntPtr.Zero) return;
            
            try
            {
                // 多次覆写内存
                for (int i = 0; i < 3; i++)
                {
                    byte[] overwrite;
                    
                    // 使用不同的模式覆写内存
                    switch (i)
                    {
                        case 0: // 全零
                            overwrite = new byte[size];
                            break;
                        case 1: // 随机数据
                            overwrite = new byte[size];
                            new Random().NextBytes(overwrite);
                            break;
                        default: // 全一
                            overwrite = new byte[size];
                            for (int j = 0; j < overwrite.Length; j++)
                                overwrite[j] = 0xFF;
                            break;
                    }
                    
                    // 复制到非托管内存
                    Marshal.Copy(overwrite, 0, ptr, size);
                    
                    // 清除临时数组
                    Array.Clear(overwrite, 0, overwrite.Length);
                }
                
                // 最终清零
                byte[] zeros = new byte[size];
                Marshal.Copy(zeros, 0, ptr, size);
                
                // 释放内存
                Marshal.FreeHGlobal(ptr);
                ptr = IntPtr.Zero;
            }
            catch (Exception ex)
            {
                LogError(ex, "安全释放内存失败 / Failed to securely free memory");
                
                // 即使出错，也尝试释放内存
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                    ptr = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// 创建安全字符串
        /// Create a secure string
        /// </summary>
        /// <param name="value">原始字符串 / Original string</param>
        /// <returns>安全字符串 / Secure string</returns>
        public static SecureString CreateSecureString(string value)
        {
            if (string.IsNullOrEmpty(value)) return new SecureString();
            
            var secureString = new SecureString();
            foreach (char c in value)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();
            
            return secureString;
        }

        /// <summary>
        /// 将安全字符串转换为非托管字符串
        /// Convert secure string to unmanaged string
        /// </summary>
        /// <param name="secureString">安全字符串 / Secure string</param>
        /// <returns>非托管字符串指针 / Unmanaged string pointer</returns>
        public static IntPtr SecureStringToIntPtr(SecureString secureString)
        {
            if (secureString == null) throw new ArgumentNullException(nameof(secureString));
            
            return Marshal.SecureStringToGlobalAllocUnicode(secureString);
        }

        /// <summary>
        /// 释放安全字符串分配的非托管字符串
        /// Free unmanaged string allocated from secure string
        /// </summary>
        /// <param name="ptr">非托管字符串指针引用 / Reference to unmanaged string pointer</param>
        public static void FreeSecureStringPtr(ref IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return;
            
            Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            ptr = IntPtr.Zero;
        }

        /// <summary>
        /// 锁定对象在内存中的位置，防止被移动
        /// Lock an object's location in memory to prevent it from being moved
        /// </summary>
        /// <typeparam name="T">对象类型 / Object type</typeparam>
        /// <param name="obj">要锁定的对象 / Object to lock</param>
        /// <returns>GCHandle，必须在使用后释放 / GCHandle that must be freed after use</returns>
        public static GCHandle LockObjectInMemory<T>(T obj) where T : class
        {
            return GCHandle.Alloc(obj, GCHandleType.Pinned);
        }

        /// <summary>
        /// 从托管对象数组创建非托管内存拷贝
        /// Create unmanaged memory copy from managed object array
        /// </summary>
        /// <typeparam name="T">对象类型 / Object type</typeparam>
        /// <param name="array">托管数组 / Managed array</param>
        /// <returns>非托管内存指针 / Unmanaged memory pointer</returns>
        public static IntPtr CreateUnmanagedCopy<T>(T[] array) where T : struct
        {
            if (array == null || array.Length == 0)
                throw new ArgumentNullException(nameof(array));
            
            int size = array.Length * Marshal.SizeOf<T>();
            IntPtr ptr = Marshal.AllocHGlobal(size);
            
            GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                IntPtr source = handle.AddrOfPinnedObject();
                CopyMemory(ptr, source, (uint)size);
            }
            finally
            {
                handle.Free();
            }
            
            return ptr;
        }

        // Windows API functions for memory operations
        
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        private static extern void CopyMemory(IntPtr destination, IntPtr source, uint length);

        private static void LogError(Exception ex, string message)
        {
            try
            {
                _logger?.LogError(ex, message);
            }
            catch
            {
                // 忽略日志记录异常
            }
        }
    }
} 