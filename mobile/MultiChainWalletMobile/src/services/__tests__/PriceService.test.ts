import { PriceService } from '../PriceService';
import fetchMock from 'jest-fetch-mock';

fetchMock.enableMocks();

describe('PriceService', () => {
    let priceService: PriceService;

    beforeEach(() => {
        fetchMock.resetMocks();
        priceService = PriceService.getInstance();
        priceService.clearCache();
    });

    describe('getTokenPrice', () => {
        const mockAddress = '0x123456789';
        const mockPrice = 1.23;

        it('should fetch and return token price', async () => {
            fetchMock.mockResponseOnce(JSON.stringify({
                [mockAddress.toLowerCase()]: {
                    usd: mockPrice
                }
            }));

            const price = await priceService.getTokenPrice(mockAddress);
            expect(price).toBe(mockPrice);
            expect(fetchMock).toHaveBeenCalledTimes(1);
        });

        it('should return cached price within cache duration', async () => {
            fetchMock.mockResponseOnce(JSON.stringify({
                [mockAddress.toLowerCase()]: {
                    usd: mockPrice
                }
            }));

            await priceService.getTokenPrice(mockAddress);
            const secondPrice = await priceService.getTokenPrice(mockAddress);

            expect(secondPrice).toBe(mockPrice);
            expect(fetchMock).toHaveBeenCalledTimes(1);
        });

        it('should return 0 on API error', async () => {
            fetchMock.mockRejectOnce(new Error('API Error'));

            const price = await priceService.getTokenPrice(mockAddress);
            expect(price).toBe(0);
        });
    });

    describe('getTokenPrices', () => {
        const mockAddresses = ['0x123', '0x456'];
        const mockPrices = {
            '0x123': 1.23,
            '0x456': 4.56
        };

        it('should fetch and return multiple token prices', async () => {
            fetchMock.mockResponseOnce(JSON.stringify({
                [mockAddresses[0].toLowerCase()]: { usd: mockPrices['0x123'] },
                [mockAddresses[1].toLowerCase()]: { usd: mockPrices['0x456'] }
            }));

            const prices = await priceService.getTokenPrices(mockAddresses);
            expect(prices.get(mockAddresses[0])).toBe(mockPrices['0x123']);
            expect(prices.get(mockAddresses[1])).toBe(mockPrices['0x456']);
            expect(fetchMock).toHaveBeenCalledTimes(1);
        });

        it('should return 0 prices on API error', async () => {
            fetchMock.mockRejectOnce(new Error('API Error'));

            const prices = await priceService.getTokenPrices(mockAddresses);
            expect(prices.get(mockAddresses[0])).toBe(0);
            expect(prices.get(mockAddresses[1])).toBe(0);
        });
    });
});
