using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 通用仓储接口
    /// Generic repository interface
    /// </summary>
    /// <typeparam name="T">实体类型 / Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// 获取所有实体
        /// Get all entities
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// 根据条件获取实体
        /// Get entities by condition
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 根据ID获取实体
        /// Get entity by ID
        /// </summary>
        Task<T> GetByIdAsync(object id);

        /// <summary>
        /// 添加实体
        /// Add entity
        /// </summary>
        Task AddAsync(T entity);

        /// <summary>
        /// 更新实体
        /// Update entity
        /// </summary>
        Task UpdateAsync(T entity);

        /// <summary>
        /// 删除实体
        /// Delete entity
        /// </summary>
        Task DeleteAsync(T entity);

        /// <summary>
        /// 批量添加实体
        /// Add entities in bulk
        /// </summary>
        Task AddRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// 批量删除实体
        /// Delete entities in bulk
        /// </summary>
        Task DeleteRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// 检查是否存在满足条件的实体
        /// Check if entity exists by condition
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    }
}
