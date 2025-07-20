interface PriceResponse {
    [key: string]: {
        usd: number;
    };
}

export class PriceService {
    private static instance: PriceService;
    private cache: Map<string, { price: number; timestamp: number }> = new Map();
    private readonly CACHE_DURATION = 5 * 60 * 1000; // 5分钟缓存 / 5 minutes cache

    private constructor() {}

    public static getInstance(): PriceService {
        if (!PriceService.instance) {
            PriceService.instance = new PriceService();
        }
        return PriceService.instance;
    }

    async getTokenPrice(contractAddress: string): Promise<number> {
        // 检查缓存 / Check cache
        const cached = this.cache.get(contractAddress);
        if (cached && Date.now() - cached.timestamp < this.CACHE_DURATION) {
            return cached.price;
        }

        try {
            // 使用CoinGecko API / Using CoinGecko API
            const response = await fetch(
                `https://api.coingecko.com/api/v3/simple/token_price/ethereum?contract_addresses=${contractAddress}&vs_currencies=usd`
            );
            
            if (!response.ok) {
                throw new Error('Failed to fetch price');
            }

            const data: PriceResponse = await response.json();
            const price = data[contractAddress.toLowerCase()]?.usd ?? 0;

            // 更新缓存 / Update cache
            this.cache.set(contractAddress, {
                price,
                timestamp: Date.now()
            });

            return price;
        } catch (error) {
            console.error('Error fetching token price:', error);
            return 0;
        }
    }

    async getTokenPrices(contractAddresses: string[]): Promise<Map<string, number>> {
        const prices = new Map<string, number>();
        
        try {
            const addressList = contractAddresses.join(',');
            const response = await fetch(
                `https://api.coingecko.com/api/v3/simple/token_price/ethereum?contract_addresses=${addressList}&vs_currencies=usd`
            );
            
            if (!response.ok) {
                throw new Error('Failed to fetch prices');
            }

            const data: PriceResponse = await response.json();
            
            for (const address of contractAddresses) {
                const price = data[address.toLowerCase()]?.usd ?? 0;
                prices.set(address, price);
                
                // 更新缓存 / Update cache
                this.cache.set(address, {
                    price,
                    timestamp: Date.now()
                });
            }
        } catch (error) {
            console.error('Error fetching token prices:', error);
            // 在出错时返回0价格 / Return 0 price on error
            for (const address of contractAddresses) {
                prices.set(address, 0);
            }
        }

        return prices;
    }

    clearCache(): void {
        this.cache.clear();
    }
}
