using System.IO;
using Microsoft.Data.Sqlite;
using Dapper;

namespace MultiChainWallet.Infrastructure.Data
{
    /// <summary>
    /// 数据库设置类
    /// Database setup class
    /// </summary>
    public static class DatabaseSetup
    {
        /// <summary>
        /// 数据库连接字符串
        /// Database connection string
        /// </summary>
        private static string ConnectionString => $"Data Source={GetDatabasePath()}";

        /// <summary>
        /// 初始化数据库
        /// Initialize database
        /// </summary>
        public static void InitializeDatabase()
        {
            CreateDatabaseDirectory();
            CreateTables();
        }

        /// <summary>
        /// 获取数据库路径
        /// Get database path
        /// </summary>
        private static string GetDatabasePath()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MultiChainWallet");
            return Path.Combine(appDataPath, "wallet.db");
        }

        /// <summary>
        /// 创建数据库目录
        /// Create database directory
        /// </summary>
        private static void CreateDatabaseDirectory()
        {
            var directory = Path.GetDirectoryName(GetDatabasePath());
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// 创建数据库表
        /// Create database tables
        /// </summary>
        private static void CreateTables()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // 创建钱包账户表 / Create wallet accounts table
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS WalletAccounts (
                    Address TEXT PRIMARY KEY,
                    EncryptedPrivateKey TEXT NOT NULL,
                    ChainType INTEGER NOT NULL,
                    Name TEXT NOT NULL
                )");

            // 创建交易记录表 / Create transactions table
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Transactions (
                    Hash TEXT PRIMARY KEY,
                    FromAddress TEXT NOT NULL,
                    ToAddress TEXT NOT NULL,
                    Amount DECIMAL(18,8) NOT NULL,
                    TokenContractAddress TEXT,
                    TransactionTime DATETIME NOT NULL,
                    Status INTEGER NOT NULL,
                    ChainType INTEGER NOT NULL,
                    GasPrice DECIMAL(18,8),
                    GasLimit INTEGER,
                    GasUsed INTEGER,
                    BlockHeight INTEGER,
                    Confirmations INTEGER NOT NULL DEFAULT 0,
                    Note TEXT
                )");

            // 创建代币余额表 / Create token balances table
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS TokenBalances (
                    WalletAddress TEXT NOT NULL,
                    TokenContractAddress TEXT NOT NULL,
                    TokenName TEXT NOT NULL,
                    TokenSymbol TEXT NOT NULL,
                    Decimals INTEGER NOT NULL,
                    Balance DECIMAL(36,18) NOT NULL,
                    UsdPrice DECIMAL(18,8),
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    PRIMARY KEY (WalletAddress, TokenContractAddress)
                )");
        }
    }
}
