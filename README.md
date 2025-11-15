# Multi-Chain Cryptocurrency Wallet / 多链加密货币钱包

## Project Introduction / 项目介绍

**English:**  
This is a multi-cryptocurrency wallet application developed in C#.

**中文:**  
这是一个使用 C# 开发的多加密货币钱包应用程序。

### Supported Features / 支持功能

- Multi-chain support (ETH, BTC) / 多链支持（ETH、BTC）
- Wallet creation and import / 钱包创建和导入
- Transaction sending and receiving / 交易发送和接收
- Balance inquiry / 余额查询
- Transaction history / 交易历史
- Secure private key storage / 安全的私钥存储

## Project Structure / 项目结构

```
MultiChainWallet/
├── MultiChainWallet.Core/             # Core business logic
│   ├── Interfaces/                    # Interface definitions
│   ├── Models/                        # Data models
│   └── Services/                      # Service layer
│
├── MultiChainWallet.Infrastructure/    # Infrastructure layer
│   ├── Data/                          # Data access
│   └── Services/                      # Service implementations
│
└── MultiChainWallet.UI/               # User interface
    ├── Pages/                         # MAUI pages
    └── ViewModels/                    # View models
```

## Development Plan / 开发计划

### Phase 1: Basic Framework / 阶段1：基础框架
- [x] Project structure setup / 项目结构搭建
- [x] Core interface definition / 核心接口定义
- [x] Database design / 数据库设计

### Phase 2: ETH Support / 阶段2：ETH 支持
- [ ] ETH wallet creation / ETH 钱包创建
- [ ] ETH balance query / ETH 余额查询
- [ ] ETH transfer function / ETH 转账功能

### Phase 3: BTC Support / 阶段3：BTC 支持
- [ ] BTC wallet creation / BTC 钱包创建
- [ ] BTC balance query / BTC 余额查询
- [ ] BTC transfer function / BTC 转账功能

### Phase 4: UI Implementation / 阶段4：UI 实现
- [ ] Wallet management interface / 钱包管理界面
- [ ] Transaction interface / 交易界面
- [ ] Settings interface / 设置界面

### Phase 5: Security Enhancement / 阶段5：安全增强
- [ ] Private key encryption storage / 私钥加密存储
- [ ] Password protection / 密码保护
- [ ] Backup function / 备份功能

## Technology Stack / 技术栈

- .NET 8
- MAUI
- Entity Framework Core
- SQLite
- Nethereum
- NBitcoin
- BouncyCastle

## Development Environment Requirements / 开发环境要求

- Visual Studio 2022 or higher
- .NET 8 SDK
- MAUI Workload
