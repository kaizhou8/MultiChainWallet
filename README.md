# Multi-Chain Cryptocurrency Wallet

## Project Introduction

This is a multi-cryptocurrency wallet application developed in C#.

### Supported Features

- Multi-chain support (ETH, BTC)
- Wallet creation and import
- Transaction sending and receiving
- Balance inquiry
- Transaction history
- Secure private key storage

## Project Structure

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

## Development Plan

### Phase 1: Basic Framework
- [x] Project structure setup
- [x] Core interface definition
- [x] Database design

### Phase 2: ETH Support
- [ ] ETH wallet creation
- [ ] ETH balance query
- [ ] ETH transfer function

### Phase 3: BTC Support
- [ ] BTC wallet creation
- [ ] BTC balance query
- [ ] BTC transfer function

### Phase 4: UI Implementation
- [ ] Wallet management interface
- [ ] Transaction interface
- [ ] Settings interface

### Phase 5: Security Enhancement
- [ ] Private key encryption storage
- [ ] Password protection
- [ ] Backup function

## Technology Stack

- .NET 8
- MAUI
- Entity Framework Core
- SQLite
- Nethereum
- NBitcoin
- BouncyCastle

## Development Environment Requirements

- Visual Studio 2022 or higher
- .NET 8 SDK
- MAUI Workload
