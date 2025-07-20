import '@testing-library/jest-native/extend-expect';
import fetchMock from 'jest-fetch-mock';

// Setup fetch mock
fetchMock.enableMocks();

// Mock console methods
global.console = {
    ...console,
    // Uncomment to ignore specific log levels
    // log: jest.fn(),
    // debug: jest.fn(),
    // info: jest.fn(),
    warn: jest.fn(),
    error: jest.fn(),
};

// Mock AsyncStorage
jest.mock('@react-native-async-storage/async-storage', () => ({
    setItem: jest.fn(),
    getItem: jest.fn(),
    removeItem: jest.fn(),
}));

// Mock ethers provider
jest.mock('ethers', () => {
    const original = jest.requireActual('ethers');
    return {
        ...original,
        providers: {
            JsonRpcProvider: jest.fn().mockImplementation(() => ({
                getBalance: jest.fn().mockResolvedValue(original.BigNumber.from('1000000000000000000')),
                getGasPrice: jest.fn().mockResolvedValue(original.BigNumber.from('20000000000')),
            })),
        },
    };
});

// Mock react-native-get-random-values
jest.mock('react-native-get-random-values', () => ({
    getRandomValues: (arr) => {
        for (let i = 0; i < arr.length; i++) {
            arr[i] = Math.floor(Math.random() * 256);
        }
        return arr;
    },
}));
