using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Services;
using MultiChainWallet.Infrastructure.Data;
using MultiChainWallet.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using MultiChainWallet.UI.Pages;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using MultiChainWallet.Core.Interfaces;
// 引入生物识别命名空间
// Import biometric namespaces
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using MultiChainWallet.Core.Services.MarketData;
using MultiChainWallet.Core.Extensions;
using MultiChainWallet.UI.Extensions;
using MultiChainWallet.UI.Services;

namespace MultiChainWallet.UI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // 配置
        // Configuration
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MultiChainWallet.UI.appsettings.json");
        
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
        
        builder.Configuration.AddConfiguration(configuration);

        // 配置数据库
        // Configure database
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "wallets.db");
        builder.Services.AddDbContext<WalletDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // 注册安全服务
        // Register security services
        builder.Services.AddSingleton<ICryptoService, EnhancedCryptoService>();
        builder.Services.AddSingleton<EnhancedSecurityService>();
        builder.Services.AddSingleton<SecurePrivateKeyManager>();
        builder.Services.AddSingleton<SecureConfigManager>();
        builder.Services.AddSingleton<SecureTransactionSigner>();

        // 注册生物识别服务
        // Register biometric services
        builder.Services.AddSingleton<IBiometricService, BiometricService>();

        // 注册市场数据服务
        // Register market data services
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<CoinGeckoApiClient>();
        builder.Services.AddSingleton<IMarketDataCacheService, FileMarketDataCacheService>();
        builder.Services.AddSingleton<IMarketDataService, MarketDataService>();

        // 注册硬件钱包服务
        // Register hardware wallet services
        builder.Services.AddSingleton<LedgerHardwareWallet>();
        builder.Services.AddSingleton<TrezorHardwareWallet>();
        builder.Services.AddSingleton<KeepKeyHardwareWallet>();
        builder.Services.AddSingleton<HardwareWalletManager>();

        // 注册硬件钱包接口实现
        // Register hardware wallet interface implementations
        builder.Services.AddSingleton<IEnumerable<IHardwareWallet>>(provider => 
        {
            return new List<IHardwareWallet>
            {
                provider.GetRequiredService<LedgerHardwareWallet>(),
                provider.GetRequiredService<TrezorHardwareWallet>(),
                provider.GetRequiredService<KeepKeyHardwareWallet>()
            };
        });

        // 注册服务
        // Register services
        builder.Services.AddSingleton<IWalletService>(provider =>
        {
            var dbContext = provider.GetRequiredService<WalletDbContext>();
            var logger = provider.GetRequiredService<ILogger<WalletService>>();
            var cryptoService = provider.GetRequiredService<ICryptoService>();
            var walletRepository = provider.GetRequiredService<IWalletRepository>();
            
            var ethereumWallet = new EthereumWallet(
                configuration["Blockchain:Ethereum:InfuraUrl"] ?? "https://mainnet.infura.io/v3/YOUR-PROJECT-ID");
            
            // 创建比特币钱包（使用测试网）
            // Create Bitcoin wallet (using testnet)
            var bitcoinWallet = new BitcoinWallet(
                Network.TestNet,
                configuration["Blockchain:Bitcoin:RpcUrl"] ?? "http://localhost:18332",
                configuration["Blockchain:Bitcoin:RpcUsername"] ?? "your-rpc-username",
                configuration["Blockchain:Bitcoin:RpcPassword"] ?? "your-rpc-password"
            );

            // 从配置中获取加密密钥
            // Get encryption key from configuration
            string encryptionKey = configuration["Security:EncryptionKey"] ?? "YOUR-ENCRYPTION-KEY";

            return new WalletService(
                dbContext, 
                ethereumWallet, 
                bitcoinWallet,
                encryptionKey,
                logger,
                walletRepository,
                cryptoService);
        });

        // 注册仓储
        // Register repositories
        builder.Services.AddSingleton<IWalletRepository, WalletRepository>();
        builder.Services.AddSingleton<Repository>();

        // 注册页面
        // Register pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<CreateWalletPage>();
        builder.Services.AddTransient<WalletListPage>();
        builder.Services.AddTransient<SendTransactionPage>(provider => 
        {
            var walletService = provider.GetRequiredService<IWalletService>();
            var biometricService = provider.GetRequiredService<IBiometricService>();
            return new SendTransactionPage(walletService, biometricService);
        });
        builder.Services.AddTransient<BiometricSettingsPage>();

        // 注册市场数据页面
        // Register market data pages
        builder.Services.AddSingleton<MarketDataPage>();
        builder.Services.AddTransient<CryptoDetailsPage>();
        
        // 注册硬件钱包页面
        // Register hardware wallet pages
        builder.Services.AddTransient<HardwareWalletConnectionPage>();
        builder.Services.AddTransient<HardwareWalletAddressPage>();
        builder.Services.AddTransient<HardwareWalletTransactionPage>();

        // 添加日志服务 / Add logging services
        builder.Services.AddLogging(logging =>
        {
            logging.AddDebug();
        });

        // 添加核心服务 / Add core services
        builder.Services.AddSecurityServices();

        // 添加UI服务 / Add UI services
        builder.Services.AddUIServices();

        // 替换Core层的交易确认服务为UI层的实现
        // Replace Core layer's transaction confirmation service with UI layer's implementation
        builder.Services.AddSingleton<ITransactionConfirmationService, UITransactionConfirmationService>();

        return builder.Build();
    }
}
