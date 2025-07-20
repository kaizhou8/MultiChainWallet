import { ethers } from 'ethers';
import AsyncStorage from '@react-native-async-storage/async-storage';
import 'react-native-get-random-values';

export interface WalletInfo {
    address: string;
    privateKey: string;
}

export class WalletService {
    private static instance: WalletService;
    private currentWallet: ethers.Wallet | null = null;
    private provider: ethers.providers.JsonRpcProvider;

    private constructor(rpcUrl: string) {
        this.provider = new ethers.providers.JsonRpcProvider(rpcUrl);
        this.provider.on('error', (error) => {
            console.error('Provider error:', error);
        });
    }

    public static getInstance(rpcUrl: string): WalletService {
        if (!WalletService.instance) {
            WalletService.instance = new WalletService(rpcUrl);
        }
        return WalletService.instance;
    }

    async createWallet(): Promise<WalletInfo> {
        let wallet: ethers.Wallet;
        try {
            wallet = ethers.Wallet.createRandom();
        } catch (error) {
            console.error('Error creating wallet:', error);
            this.currentWallet = null;
            throw new Error('Failed to create wallet');
        }

        const walletInfo: WalletInfo = {
            address: wallet.address,
            privateKey: wallet.privateKey
        };

        try {
            await this.saveWallet(walletInfo);
            this.currentWallet = wallet.connect(this.provider);
            return walletInfo;
        } catch (error) {
            console.error('Error saving wallet:', error);
            this.currentWallet = null;
            throw new Error('Failed to save wallet');
        }
    }

    async importWallet(privateKey: string): Promise<WalletInfo> {
        if (!privateKey.match(/^0x[0-9a-fA-F]{64}$/)) {
            throw new Error('Invalid private key');
        }

        let wallet: ethers.Wallet;
        try {
            wallet = new ethers.Wallet(privateKey);
        } catch (error) {
            console.error('Error creating wallet from private key:', error);
            throw new Error('Failed to import wallet');
        }

        const walletInfo: WalletInfo = {
            address: wallet.address,
            privateKey: wallet.privateKey
        };

        try {
            await this.saveWallet(walletInfo);
            this.currentWallet = wallet.connect(this.provider);
            return walletInfo;
        } catch (error) {
            console.error('Error saving wallet:', error);
            this.currentWallet = null;
            throw new Error('Failed to save wallet');
        }
    }

    async loadWallet(): Promise<WalletInfo | null> {
        try {
            const walletJson = await AsyncStorage.getItem('wallet');
            if (!walletJson) return null;

            const walletInfo: WalletInfo = JSON.parse(walletJson);
            
            // Basic validation of wallet data
            if (!walletInfo.address || !walletInfo.privateKey) {
                console.error('Invalid wallet data in storage');
                return null;
            }

            try {
                const wallet = new ethers.Wallet(walletInfo.privateKey);
                if (wallet.address.toLowerCase() !== walletInfo.address.toLowerCase()) {
                    console.error('Wallet address mismatch');
                    return null;
                }
                this.currentWallet = wallet.connect(this.provider);
                return walletInfo;
            } catch (error) {
                console.error('Error creating wallet from stored data:', error);
                return null;
            }
        } catch (error) {
            console.error('Error loading wallet:', error);
            this.currentWallet = null;
            return null;
        }
    }

    private async saveWallet(walletInfo: WalletInfo): Promise<void> {
        try {
            await AsyncStorage.setItem('wallet', JSON.stringify(walletInfo));
        } catch (error) {
            console.error('Error saving wallet:', error);
            throw new Error('Failed to save wallet');
        }
    }

    async getBalance(): Promise<string> {
        if (!this.currentWallet) {
            throw new Error('No wallet loaded');
        }

        try {
            const balance = await this.provider.getBalance(this.currentWallet.address);
            return ethers.utils.formatEther(balance);
        } catch (error) {
            console.error('Error getting balance:', error);
            throw new Error('Provider error');
        }
    }

    getCurrentWallet(): ethers.Wallet | null {
        return this.currentWallet;
    }

    async signTransaction(transaction: ethers.providers.TransactionRequest): Promise<string> {
        if (!this.currentWallet) {
            throw new Error('No wallet loaded');
        }

        try {
            const tx = await this.currentWallet.sendTransaction(transaction);
            return tx.hash;
        } catch (error) {
            console.error('Error signing transaction:', error);
            throw new Error('Transaction failed');
        }
    }

    async disconnect(): Promise<void> {
        try {
            this.currentWallet = null;
            await AsyncStorage.removeItem('wallet');
        } catch (error) {
            console.error('Error disconnecting wallet:', error);
            throw error;
        }
    }
}
