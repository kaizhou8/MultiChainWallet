import { WalletService } from '../WalletService';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { ethers } from 'ethers';

// Mock AsyncStorage
const mockSetItem = jest.fn(() => Promise.resolve());
const mockGetItem = jest.fn(() => Promise.resolve(null));
const mockRemoveItem = jest.fn(() => Promise.resolve());

jest.mock('@react-native-async-storage/async-storage', () => ({
    setItem: (...args: any[]) => mockSetItem(...args),
    getItem: (...args: any[]) => mockGetItem(...args),
    removeItem: (...args: any[]) => mockRemoveItem(...args)
}));

describe('WalletService', () => {
    let walletService: WalletService;
    const mockRpcUrl = 'https://mock-rpc.example.com';
    const validPrivateKey = '0x0123456789012345678901234567890123456789012345678901234567890123';
    const mockAddress = '0x14791697260E4c9A71f18484C9f997B308e59325';
    const mockProvider = {
        getNetwork: jest.fn().mockResolvedValue({ chainId: 1 }),
        getBlockNumber: jest.fn().mockResolvedValue(1),
        getGasPrice: jest.fn().mockResolvedValue(ethers.utils.parseUnits('1', 'gwei')),
        estimateGas: jest.fn().mockResolvedValue(ethers.BigNumber.from('21000')),
        sendTransaction: jest.fn().mockResolvedValue({ hash: '0x123' }),
        getBalance: jest.fn().mockResolvedValue(ethers.utils.parseEther('1.0')),
        on: jest.fn()
    };

    beforeEach(() => {
        jest.clearAllMocks();
        walletService = WalletService.getInstance(mockRpcUrl);
        (walletService as any).provider = mockProvider;
    });

    afterEach(async () => {
        await walletService.disconnect();
        mockSetItem.mockClear();
        mockGetItem.mockClear();
        mockRemoveItem.mockClear();
    });

    describe('createWallet', () => {
        it('should create a new wallet', async () => {
            const wallet = await walletService.createWallet();
            expect(wallet).toHaveProperty('address');
            expect(wallet).toHaveProperty('privateKey');
            expect(ethers.utils.isAddress(wallet.address)).toBe(true);
            expect(wallet.privateKey).toMatch(/^0x[0-9a-fA-F]{64}$/);
            expect(mockSetItem).toHaveBeenCalledWith(
                'wallet',
                expect.any(String)
            );
        });

        it('should save wallet to storage', async () => {
            const wallet = await walletService.createWallet();
            expect(mockSetItem).toHaveBeenCalledWith(
                'wallet',
                JSON.stringify(wallet)
            );
        });

        it('should handle creation errors', async () => {
            mockSetItem.mockRejectedValueOnce(new Error('Storage error'));
            await expect(walletService.createWallet())
                .rejects
                .toThrow('Failed to save wallet');
        });
    });

    describe('importWallet', () => {
        it('should import a valid wallet', async () => {
            const wallet = await walletService.importWallet(validPrivateKey);
            expect(wallet.address).toBe(mockAddress);
            expect(wallet.privateKey).toBe(validPrivateKey);
            expect(mockSetItem).toHaveBeenCalledWith(
                'wallet',
                JSON.stringify(wallet)
            );
        });

        it('should save imported wallet to storage', async () => {
            const wallet = await walletService.importWallet(validPrivateKey);
            expect(mockSetItem).toHaveBeenCalledWith(
                'wallet',
                JSON.stringify(wallet)
            );
        });

        it('should reject invalid private key', async () => {
            await expect(walletService.importWallet('invalid-key'))
                .rejects
                .toThrow('Invalid private key');
        });

        it('should handle storage errors', async () => {
            mockSetItem.mockRejectedValueOnce(new Error('Storage error'));
            await expect(walletService.importWallet(validPrivateKey))
                .rejects
                .toThrow('Failed to save wallet');
        });
    });

    describe('loadWallet', () => {
        it('should load wallet from storage', async () => {
            const mockWallet = {
                address: mockAddress,
                privateKey: validPrivateKey
            };

            mockGetItem.mockResolvedValueOnce(JSON.stringify(mockWallet));

            const wallet = await walletService.loadWallet();
            expect(wallet).toEqual(mockWallet);
        });

        it('should return null if no wallet in storage', async () => {
            mockGetItem.mockResolvedValueOnce(null);
            const wallet = await walletService.loadWallet();
            expect(wallet).toBeNull();
        });

        it('should handle invalid wallet data', async () => {
            mockGetItem.mockResolvedValueOnce(JSON.stringify({ invalid: 'data' }));
            const wallet = await walletService.loadWallet();
            expect(wallet).toBeNull();
        });
    });

    describe('getBalance', () => {
        it('should throw error if no wallet is loaded', async () => {
            await expect(walletService.getBalance())
                .rejects
                .toThrow('No wallet loaded');
        });

        it('should return wallet balance', async () => {
            const mockWallet = {
                address: mockAddress,
                privateKey: validPrivateKey
            };
            mockGetItem.mockResolvedValueOnce(JSON.stringify(mockWallet));
            await walletService.loadWallet();

            const balance = await walletService.getBalance();
            expect(balance).toBe('1.0');
        });

        it('should handle provider errors', async () => {
            mockProvider.getBalance.mockRejectedValueOnce(new Error('Provider error'));

            const mockWallet = {
                address: mockAddress,
                privateKey: validPrivateKey
            };
            mockGetItem.mockResolvedValueOnce(JSON.stringify(mockWallet));
            await walletService.loadWallet();

            await expect(walletService.getBalance())
                .rejects
                .toThrow('Provider error');
        });
    });

    describe('signTransaction', () => {
        const mockTransaction = {
            to: '0x1234567890123456789012345678901234567890',
            value: ethers.utils.parseEther('0.1')
        };

        it('should throw error if no wallet is loaded', async () => {
            await expect(walletService.signTransaction(mockTransaction))
                .rejects
                .toThrow('No wallet loaded');
        });

        it('should sign and send transaction', async () => {
            const mockTxHash = '0x123';
            mockProvider.sendTransaction.mockResolvedValueOnce({ hash: mockTxHash });

            const mockWallet = {
                address: mockAddress,
                privateKey: validPrivateKey
            };
            mockGetItem.mockResolvedValueOnce(JSON.stringify(mockWallet));
            await walletService.loadWallet();

            const txHash = await walletService.signTransaction(mockTransaction);
            expect(txHash).toBe(mockTxHash);
        });

        it('should handle transaction errors', async () => {
            const mockWallet = {
                address: mockAddress,
                privateKey: validPrivateKey
            };
            mockGetItem.mockResolvedValueOnce(JSON.stringify(mockWallet));
            await walletService.loadWallet();

            mockProvider.sendTransaction.mockRejectedValueOnce(new Error('Transaction failed'));
            await expect(walletService.signTransaction(mockTransaction))
                .rejects
                .toThrow('Transaction failed');
        });
    });
});
