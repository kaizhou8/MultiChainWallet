using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MultiChainWallet.Core.Utilities
{
    /// <summary>
    /// 反射助手类，用于优化混淆代码中的反射操作性能
    /// Reflection helper class for optimizing reflection performance in obfuscated code
    /// </summary>
    public static class ReflectionHelper
    {
        // 类型缓存
        // Type cache
        private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();
        
        // 方法缓存
        // Method cache
        private static readonly Dictionary<string, MethodInfo> MethodCache = new Dictionary<string, MethodInfo>();
        
        // 属性缓存
        // Property cache
        private static readonly Dictionary<string, PropertyInfo> PropertyCache = new Dictionary<string, PropertyInfo>();
        
        // 字段缓存
        // Field cache
        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>();
        
        // 缓存锁对象
        // Cache lock object
        private static readonly object CacheLock = new object();
        
        /// <summary>
        /// 获取类型，使用缓存提高性能
        /// Get type with caching for improved performance
        /// </summary>
        /// <param name="typeName">类型名称 / Type name</param>
        /// <param name="assembly">程序集，如果为null则搜索所有已加载的程序集 / Assembly, if null will search all loaded assemblies</param>
        /// <returns>找到的类型 / Found type</returns>
        public static Type GetType(string typeName, Assembly assembly = null)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;
                
            string cacheKey = assembly != null ? $"{assembly.FullName}|{typeName}" : typeName;
            
            // 尝试从缓存获取
            // Try to get from cache
            if (TypeCache.TryGetValue(cacheKey, out Type cachedType))
                return cachedType;
                
            lock (CacheLock)
            {
                // 再次检查缓存（双重检查锁定模式）
                // Check cache again (double-check locking pattern)
                if (TypeCache.TryGetValue(cacheKey, out cachedType))
                    return cachedType;
                
                Type foundType = null;
                
                if (assembly != null)
                {
                    // 在指定程序集中查找
                    // Search in specified assembly
                    foundType = assembly.GetTypes().FirstOrDefault(t => 
                        t.FullName == typeName || 
                        t.Name == typeName ||
                        // 处理混淆后的类型名称
                        // Handle obfuscated type names
                        (t.FullName != null && t.FullName.EndsWith(typeName, StringComparison.OrdinalIgnoreCase)) ||
                        (t.Name != null && t.Name.EndsWith(typeName, StringComparison.OrdinalIgnoreCase)));
                }
                else
                {
                    // 尝试直接获取类型
                    // Try to get type directly
                    foundType = Type.GetType(typeName);
                    
                    if (foundType == null)
                    {
                        // 在所有已加载的程序集中搜索
                        // Search in all loaded assemblies
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            try
                            {
                                foundType = asm.GetTypes().FirstOrDefault(t => 
                                    t.FullName == typeName || 
                                    t.Name == typeName ||
                                    // 处理混淆后的类型名称
                                    // Handle obfuscated type names
                                    (t.FullName != null && t.FullName.EndsWith(typeName, StringComparison.OrdinalIgnoreCase)) ||
                                    (t.Name != null && t.Name.EndsWith(typeName, StringComparison.OrdinalIgnoreCase)));
                                    
                                if (foundType != null)
                                    break;
                            }
                            catch (ReflectionTypeLoadException)
                            {
                                // 忽略无法加载的程序集
                                // Ignore assemblies that can't be loaded
                                continue;
                            }
                        }
                    }
                }
                
                // 添加到缓存
                // Add to cache
                if (foundType != null)
                    TypeCache[cacheKey] = foundType;
                    
                return foundType;
            }
        }
        
        /// <summary>
        /// 通过部分名称查找类型
        /// Find type by partial name
        /// </summary>
        /// <param name="partialName">部分类型名称 / Partial type name</param>
        /// <param name="assembly">程序集，如果为null则搜索所有已加载的程序集 / Assembly, if null will search all loaded assemblies</param>
        /// <returns>找到的类型 / Found type</returns>
        public static Type FindTypeByPartialName(string partialName, Assembly assembly = null)
        {
            if (string.IsNullOrEmpty(partialName))
                return null;
                
            string cacheKey = $"Partial|{(assembly != null ? assembly.FullName + "|" : "")}{partialName}";
            
            // 尝试从缓存获取
            // Try to get from cache
            if (TypeCache.TryGetValue(cacheKey, out Type cachedType))
                return cachedType;
                
            lock (CacheLock)
            {
                // 再次检查缓存
                // Check cache again
                if (TypeCache.TryGetValue(cacheKey, out cachedType))
                    return cachedType;
                
                Type foundType = null;
                
                if (assembly != null)
                {
                    // 在指定程序集中查找
                    // Search in specified assembly
                    foundType = assembly.GetTypes().FirstOrDefault(t => 
                        (t.FullName != null && t.FullName.Contains(partialName)) ||
                        (t.Name != null && t.Name.Contains(partialName)));
                }
                else
                {
                    // 在所有已加载的程序集中搜索
                    // Search in all loaded assemblies
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            foundType = asm.GetTypes().FirstOrDefault(t => 
                                (t.FullName != null && t.FullName.Contains(partialName)) ||
                                (t.Name != null && t.Name.Contains(partialName)));
                                
                            if (foundType != null)
                                break;
                        }
                        catch (ReflectionTypeLoadException)
                        {
                            // 忽略无法加载的程序集
                            // Ignore assemblies that can't be loaded
                            continue;
                        }
                    }
                }
                
                // 添加到缓存
                // Add to cache
                if (foundType != null)
                    TypeCache[cacheKey] = foundType;
                    
                return foundType;
            }
        }
        
        /// <summary>
        /// 获取方法信息，使用缓存提高性能
        /// Get method info with caching for improved performance
        /// </summary>
        /// <param name="type">类型 / Type</param>
        /// <param name="methodName">方法名称 / Method name</param>
        /// <param name="parameterTypes">参数类型数组，如果为null则忽略参数类型 / Parameter types array, if null parameter types are ignored</param>
        /// <returns>找到的方法信息 / Found method info</returns>
        public static MethodInfo GetMethod(Type type, string methodName, Type[] parameterTypes = null)
        {
            if (type == null || string.IsNullOrEmpty(methodName))
                return null;
                
            string cacheKey = $"{type.FullName}|{methodName}|{(parameterTypes != null ? string.Join(",", parameterTypes.Select(t => t.FullName)) : "null")}";
            
            // 尝试从缓存获取
            // Try to get from cache
            if (MethodCache.TryGetValue(cacheKey, out MethodInfo cachedMethod))
                return cachedMethod;
                
            lock (CacheLock)
            {
                // 再次检查缓存
                // Check cache again
                if (MethodCache.TryGetValue(cacheKey, out cachedMethod))
                    return cachedMethod;
                
                MethodInfo foundMethod;
                
                if (parameterTypes != null)
                {
                    // 使用指定的参数类型查找方法
                    // Find method with specified parameter types
                    foundMethod = type.GetMethod(methodName, parameterTypes);
                    
                    if (foundMethod == null)
                    {
                        // 尝试查找名称相似的方法
                        // Try to find method with similar name
                        foundMethod = type.GetMethods()
                            .FirstOrDefault(m => 
                                m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase) &&
                                m.GetParameters().Length == parameterTypes.Length &&
                                m.GetParameters().Zip(parameterTypes, (p, t) => p.ParameterType.IsAssignableFrom(t)).All(match => match));
                    }
                }
                else
                {
                    // 查找任何匹配名称的方法
                    // Find any method matching the name
                    foundMethod = type.GetMethod(methodName);
                    
                    if (foundMethod == null)
                    {
                        // 尝试查找名称相似的方法
                        // Try to find method with similar name
                        foundMethod = type.GetMethods()
                            .FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
                    }
                }
                
                // 添加到缓存
                // Add to cache
                if (foundMethod != null)
                    MethodCache[cacheKey] = foundMethod;
                    
                return foundMethod;
            }
        }
        
        /// <summary>
        /// 通过部分名称查找方法
        /// Find method by partial name
        /// </summary>
        /// <param name="type">类型 / Type</param>
        /// <param name="partialMethodName">部分方法名称 / Partial method name</param>
        /// <returns>找到的方法信息 / Found method info</returns>
        public static MethodInfo FindMethodByPartialName(Type type, string partialMethodName)
        {
            if (type == null || string.IsNullOrEmpty(partialMethodName))
                return null;
                
            string cacheKey = $"Partial|{type.FullName}|{partialMethodName}";
            
            // 尝试从缓存获取
            // Try to get from cache
            if (MethodCache.TryGetValue(cacheKey, out MethodInfo cachedMethod))
                return cachedMethod;
                
            lock (CacheLock)
            {
                // 再次检查缓存
                // Check cache again
                if (MethodCache.TryGetValue(cacheKey, out cachedMethod))
                    return cachedMethod;
                
                // 查找名称包含指定部分的方法
                // Find method with name containing the specified part
                var foundMethod = type.GetMethods()
                    .FirstOrDefault(m => m.Name.Contains(partialMethodName));
                
                // 添加到缓存
                // Add to cache
                if (foundMethod != null)
                    MethodCache[cacheKey] = foundMethod;
                    
                return foundMethod;
            }
        }
        
        /// <summary>
        /// 获取属性信息，使用缓存提高性能
        /// Get property info with caching for improved performance
        /// </summary>
        /// <param name="type">类型 / Type</param>
        /// <param name="propertyName">属性名称 / Property name</param>
        /// <returns>找到的属性信息 / Found property info</returns>
        public static PropertyInfo GetProperty(Type type, string propertyName)
        {
            if (type == null || string.IsNullOrEmpty(propertyName))
                return null;
                
            string cacheKey = $"{type.FullName}|{propertyName}";
            
            // 尝试从缓存获取
            // Try to get from cache
            if (PropertyCache.TryGetValue(cacheKey, out PropertyInfo cachedProperty))
                return cachedProperty;
                
            lock (CacheLock)
            {
                // 再次检查缓存
                // Check cache again
                if (PropertyCache.TryGetValue(cacheKey, out cachedProperty))
                    return cachedProperty;
                
                // 查找属性
                // Find property
                var foundProperty = type.GetProperty(propertyName);
                
                if (foundProperty == null)
                {
                    // 尝试查找名称相似的属性
                    // Try to find property with similar name
                    foundProperty = type.GetProperties()
                        .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                }
                
                // 添加到缓存
                // Add to cache
                if (foundProperty != null)
                    PropertyCache[cacheKey] = foundProperty;
                    
                return foundProperty;
            }
        }
        
        /// <summary>
        /// 通过部分名称查找属性
        /// Find property by partial name
        /// </summary>
        /// <param name="type">类型 / Type</param>
        /// <param name="partialPropertyName">部分属性名称 / Partial property name</param>
        /// <returns>找到的属性信息 / Found property info</returns>
        public static PropertyInfo FindPropertyByPartialName(Type type, string partialPropertyName)
        {
            if (type == null || string.IsNullOrEmpty(partialPropertyName))
                return null;
                
            string cacheKey = $"Partial|{type.FullName}|{partialPropertyName}";
            
            // 尝试从缓存获取
            // Try to get from cache
            if (PropertyCache.TryGetValue(cacheKey, out PropertyInfo cachedProperty))
                return cachedProperty;
                
            lock (CacheLock)
            {
                // 再次检查缓存
                // Check cache again
                if (PropertyCache.TryGetValue(cacheKey, out cachedProperty))
                    return cachedProperty;
                
                // 查找名称包含指定部分的属性
                // Find property with name containing the specified part
                var foundProperty = type.GetProperties()
                    .FirstOrDefault(p => p.Name.Contains(partialPropertyName));
                
                // 添加到缓存
                // Add to cache
                if (foundProperty != null)
                    PropertyCache[cacheKey] = foundProperty;
                    
                return foundProperty;
            }
        }
        
        /// <summary>
        /// 预热反射缓存，提前加载常用类型
        /// Warm up reflection cache by preloading commonly used types
        /// </summary>
        /// <param name="assembly">要预热的程序集 / Assembly to warm up</param>
        /// <param name="typeNamePatterns">类型名称模式数组 / Array of type name patterns</param>
        public static void WarmUpCache(Assembly assembly, params string[] typeNamePatterns)
        {
            if (assembly == null)
                return;
                
            try
            {
                var types = assembly.GetTypes();
                
                foreach (var pattern in typeNamePatterns)
                {
                    foreach (var type in types)
                    {
                        if (type.FullName != null && type.FullName.Contains(pattern))
                        {
                            // 缓存类型
                            // Cache type
                            string typeKey = $"{assembly.FullName}|{type.FullName}";
                            TypeCache[typeKey] = type;
                            
                            // 缓存公共方法
                            // Cache public methods
                            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                            {
                                string methodKey = $"{type.FullName}|{method.Name}|null";
                                MethodCache[methodKey] = method;
                            }
                            
                            // 缓存公共属性
                            // Cache public properties
                            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                            {
                                string propertyKey = $"{type.FullName}|{property.Name}";
                                PropertyCache[propertyKey] = property;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 忽略预热过程中的错误
                // Ignore errors during warm-up process
            }
        }
        
        /// <summary>
        /// 清除反射缓存
        /// Clear reflection cache
        /// </summary>
        public static void ClearCache()
        {
            lock (CacheLock)
            {
                TypeCache.Clear();
                MethodCache.Clear();
                PropertyCache.Clear();
                FieldCache.Clear();
            }
        }
    }
} 