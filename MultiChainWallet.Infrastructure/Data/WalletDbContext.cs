using Microsoft.EntityFrameworkCore;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Infrastructure.Data
{
    /// <summary>
    /// 钱包数据库上下文
    /// Wallet database context
    /// </summary>
    public class WalletDbContext : DbContext
    {
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="options">数据库配置选项 / Database configuration options</param>
        public WalletDbContext(DbContextOptions<WalletDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// 钱包账户表
        /// Wallet accounts table
        /// </summary>
        public DbSet<WalletAccount> WalletAccounts { get; set; }

        /// <summary>
        /// 交易记录表
        /// Transaction records table
        /// </summary>
        public DbSet<Transaction> Transactions { get; set; }

        /// <summary>
        /// 代币余额表
        /// Token balances table
        /// </summary>
        public DbSet<TokenBalance> TokenBalances { get; set; }

        /// <summary>
        /// 模型创建配置
        /// Model creation configuration
        /// </summary>
        /// <param name="modelBuilder">模型构建器 / Model builder</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 钱包账户配置 / Wallet account configuration
            modelBuilder.Entity<WalletAccount>(entity =>
            {
                entity.HasKey(e => e.Address);
                entity.Property(e => e.Address).IsRequired();
                entity.Property(e => e.EncryptedPrivateKey).IsRequired();
                entity.Property(e => e.ChainType).IsRequired();
                entity.Property(e => e.Name).IsRequired();
            });

            // 交易记录配置 / Transaction record configuration
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Hash);
                entity.Property(e => e.Hash).IsRequired();
                entity.Property(e => e.FromAddress).IsRequired();
                entity.Property(e => e.ToAddress).IsRequired();
                entity.Property(e => e.Amount).HasColumnType("decimal(18,8)");
                entity.Property(e => e.TransactionTime).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.ChainType).IsRequired();
                entity.Property(e => e.GasPrice).HasColumnType("decimal(18,8)");
            });

            // 代币余额配置 / Token balance configuration
            modelBuilder.Entity<TokenBalance>(entity =>
            {
                entity.HasKey(e => new { e.WalletAddress, e.TokenContractAddress });
                entity.Property(e => e.WalletAddress).IsRequired();
                entity.Property(e => e.TokenContractAddress).IsRequired();
                entity.Property(e => e.TokenName).IsRequired();
                entity.Property(e => e.TokenSymbol).IsRequired();
                entity.Property(e => e.Decimals).IsRequired();
                entity.Property(e => e.Balance).HasColumnType("decimal(36,18)");
                entity.Property(e => e.UsdPrice).HasColumnType("decimal(18,8)");
            });
        }
    }
}
