# 多链加密货币钱包 API 文档 / Multi-Chain Cryptocurrency Wallet API Documentation

## 核心接口 / Core Interfaces

### IWallet 接口 / IWallet Interface
```csharp
public interface IWallet
{
    Task<string> CreateWalletAsync(string password);
    Task<decimal> GetBalanceAsync(string address);
    Task<string> SendTransactionAsync(string toAddress, decimal amount);
    Task<List<Transaction>> GetTransactionHistoryAsync(string address);
}
```

### IERC20Service 接口 / IERC20Service Interface
```csharp
public interface IERC20Service
{
    Task<decimal> GetTokenBalanceAsync(string contractAddress, string walletAddress);
    Task<string> SendTokenAsync(string contractAddress, string toAddress, decimal amount);
    Task<List<TokenTransaction>> GetTokenTransactionHistoryAsync(string contractAddress, string address);
}
```

### IAddressValidator 接口 / IAddressValidator Interface
```csharp
public interface IAddressValidator
{
    bool ValidateAddress(string address, CoinType coinType);
    string FormatAddress(string address, CoinType coinType);
}
```

## 服务类 / Service Classes

### WalletService
主要钱包服务类，管理所有加密货币钱包操作。
Main wallet service class that manages all cryptocurrency wallet operations.

#### 方法 / Methods
- `CreateWallet(string password)`: 创建新钱包 / Create new wallet
- `ImportWallet(string privateKey)`: 导入现有钱包 / Import existing wallet
- `ExportWallet(string password)`: 导出钱包 / Export wallet
- `GetBalance(string address)`: 获取余额 / Get balance

### BackupService
数据备份服务类，处理钱包数据的备份和恢复。
Data backup service class that handles wallet data backup and restoration.

#### 方法 / Methods
- `CreateBackup(string password)`: 创建备份 / Create backup
- `RestoreFromBackup(string backupFile, string password)`: 从备份恢复 / Restore from backup
- `VerifyBackup(string backupFile)`: 验证备份 / Verify backup

### SecurityService
安全服务类，处理加密和安全相关操作。
Security service class that handles encryption and security-related operations.

#### 方法 / Methods
- `EncryptPrivateKey(string privateKey, string password)`: 加密私钥 / Encrypt private key
- `DecryptPrivateKey(string encryptedKey, string password)`: 解密私钥 / Decrypt private key
- `ValidatePassword(string password)`: 验证密码强度 / Validate password strength

## 数据模型 / Data Models

### WalletAccount
```csharp
public class WalletAccount
{
    public string Address { get; set; }
    public string EncryptedPrivateKey { get; set; }
    public CoinType CoinType { get; set; }
    public decimal Balance { get; set; }
}
```

### Transaction
```csharp
public class Transaction
{
    public string TransactionId { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public TransactionStatus Status { get; set; }
}
```

### BackupData
```csharp
public class BackupData
{
    public List<WalletAccount> Accounts { get; set; }
    public List<TokenInfo> Tokens { get; set; }
    public Dictionary<string, object> Settings { get; set; }
    public string Version { get; set; }
}
```

## 错误处理 / Error Handling
所有API方法都可能抛出以下异常：
All API methods may throw the following exceptions:

- `WalletException`: 钱包操作错误 / Wallet operation errors
- `SecurityException`: 安全相关错误 / Security-related errors
- `ValidationException`: 输入验证错误 / Input validation errors
- `BackupException`: 备份操作错误 / Backup operation errors

## 示例代码 / Example Code

### 创建新钱包 / Create New Wallet
```csharp
var walletService = new WalletService();
string address = await walletService.CreateWalletAsync("strong-password");
```

### 发送交易 / Send Transaction
```csharp
var walletService = new WalletService();
string txId = await walletService.SendTransactionAsync("0x...", "0x...", 1.5m);
```

### 创建备份 / Create Backup
```csharp
var backupService = new BackupService();
string backupPath = await backupService.CreateBackupAsync("backup-password");
```
