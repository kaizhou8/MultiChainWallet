using Microsoft.EntityFrameworkCore;
using MultiChainWallet.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MultiChainWallet.Infrastructure.Data
{
    /// <summary>
    /// 通用仓储实现
    /// Generic repository implementation
    /// </summary>
    /// <typeparam name="T">实体类型 / Entity type</typeparam>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly WalletDbContext _context;
        protected readonly DbSet<T> _dbSet;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="context">数据库上下文 / Database context</param>
        public Repository(WalletDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        /// <summary>
        /// 获取所有实体
        /// Get all entities
        /// </summary>
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <summary>
        /// 根据条件获取实体
        /// Get entities by condition
        /// </summary>
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        /// <summary>
        /// 根据ID获取实体
        /// Get entity by ID
        /// </summary>
        public async Task<T> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// 添加实体
        /// Add entity
        /// </summary>
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 更新实体
        /// Update entity
        /// </summary>
        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 删除实体
        /// Delete entity
        /// </summary>
        public async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 批量添加实体
        /// Add entities in bulk
        /// </summary>
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 批量删除实体
        /// Delete entities in bulk
        /// </summary>
        public async Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 检查是否存在满足条件的实体
        /// Check if entity exists by condition
        /// </summary>
        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }
    }
}
