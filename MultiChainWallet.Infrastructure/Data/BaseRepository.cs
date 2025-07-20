using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Data;

namespace MultiChainWallet.Infrastructure.Data
{
    /// <summary>
    /// 基础仓储类
    /// Base repository class
    /// </summary>
    public abstract class BaseRepository
    {
        /// <summary>
        /// 数据库连接字符串
        /// Database connection string
        /// </summary>
        protected readonly string _connectionString;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        protected BaseRepository()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MultiChainWallet",
                "wallet.db");
            _connectionString = $"Data Source={dbPath}";
        }

        /// <summary>
        /// 带连接字符串的构造函数（用于测试）
        /// Constructor with connection string (for testing)
        /// </summary>
        /// <param name="connectionString">数据库连接字符串 / Database connection string</param>
        protected BaseRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 执行查询并返回结果集
        /// Execute query and return result set
        /// </summary>
        protected async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<T>(sql, param);
        }

        /// <summary>
        /// 执行查询并返回单个结果
        /// Execute query and return single result
        /// </summary>
        protected async Task<T> QuerySingleOrDefaultAsync<T>(string sql, object param = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QuerySingleOrDefaultAsync<T>(sql, param);
        }

        /// <summary>
        /// 执行命令
        /// Execute command
        /// </summary>
        protected async Task<int> ExecuteAsync(string sql, object param = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.ExecuteAsync(sql, param);
        }

        /// <summary>
        /// 在事务中执行命令
        /// Execute command in transaction
        /// </summary>
        protected async Task ExecuteInTransactionAsync(Func<SqliteConnection, IDbTransaction, Task> action)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await action(connection, transaction);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
