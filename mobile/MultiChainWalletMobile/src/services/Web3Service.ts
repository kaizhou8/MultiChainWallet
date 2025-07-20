import { ethers } from 'ethers';
import AsyncStorage from '@react-native-async-storage/async-storage';
import 'react-native-get-random-values';

const ERC20_ABI = [
    'function name() view returns (string)',
    'function symbol() view returns (string)',
    'function decimals() view returns (uint8)',
    'function balanceOf(address) view returns (uint)',
    'function transfer(address to, uint amount) returns (bool)',
    'event Transfer(address indexed from, address indexed to, uint amount)'
];

export interface TokenInfo {
    address: string;
    name: string;
    symbol: string;
    decimals: number;
    balance: string;
    usdPrice?: number;
}

export class Web3Service {
    private provider: ethers.providers.JsonRpcProvider;
    private tokenList: Map<string, TokenInfo>;

    constructor(rpcUrl: string) {
        this.provider = new ethers.providers.JsonRpcProvider(rpcUrl);
        this.tokenList = new Map();
    }

    async addToken(contractAddress: string): Promise<TokenInfo | null> {
        try {
            const contract = new ethers.Contract(contractAddress, ERC20_ABI, this.provider);
            const [name, symbol, decimals] = await Promise.all([
                contract.name(),
                contract.symbol(),
                contract.decimals()
            ]);

            const tokenInfo: TokenInfo = {
                address: contractAddress,
                name,
                symbol,
                decimals,
                balance: '0'
            };

            this.tokenList.set(contractAddress, tokenInfo);
            await this.saveTokenList();
            return tokenInfo;
        } catch (error) {
            console.error('Error adding token:', error);
            return null;
        }
    }

    async getTokenBalance(contractAddress: string, walletAddress: string): Promise<string> {
        try {
            const contract = new ethers.Contract(contractAddress, ERC20_ABI, this.provider);
            const balance = await contract.balanceOf(walletAddress);
            const decimals = await contract.decimals();
            return ethers.utils.formatUnits(balance, decimals);
        } catch (error) {
            console.error('Error getting token balance:', error);
            return '0';
        }
    }

    async transferToken(
        contractAddress: string,
        toAddress: string,
        amount: string,
        wallet: ethers.Wallet
    ): Promise<string | null> {
        try {
            const contract = new ethers.Contract(contractAddress, ERC20_ABI, wallet);
            const decimals = await contract.decimals();
            const parsedAmount = ethers.utils.parseUnits(amount, decimals);
            const tx = await contract.transfer(toAddress, parsedAmount);
            const receipt = await tx.wait();
            return receipt.transactionHash;
        } catch (error) {
            console.error('Error transferring token:', error);
            return null;
        }
    }

    private async saveTokenList(): Promise<void> {
        try {
            const tokenArray = Array.from(this.tokenList.values());
            await AsyncStorage.setItem('tokenList', JSON.stringify(tokenArray));
        } catch (error) {
            console.error('Error saving token list:', error);
        }
    }

    async loadTokenList(): Promise<void> {
        try {
            const tokenListJson = await AsyncStorage.getItem('tokenList');
            if (tokenListJson) {
                const tokenArray: TokenInfo[] = JSON.parse(tokenListJson);
                this.tokenList.clear();
                tokenArray.forEach(token => {
                    this.tokenList.set(token.address, token);
                });
            }
        } catch (error) {
            console.error('Error loading token list:', error);
        }
    }

    async getAllTokens(): Promise<TokenInfo[]> {
        return Array.from(this.tokenList.values());
    }
}
