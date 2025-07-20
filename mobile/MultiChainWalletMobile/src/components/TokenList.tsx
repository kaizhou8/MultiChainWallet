import React from 'react';
import { View, Text, FlatList, TouchableOpacity, StyleSheet } from 'react-native';
import { TokenInfo } from '../services/Web3Service';

interface TokenListProps {
    tokens: TokenInfo[];
    onTokenPress: (token: TokenInfo) => void;
}

export const TokenList: React.FC<TokenListProps> = ({ tokens, onTokenPress }) => {
    const renderItem = ({ item }: { item: TokenInfo }) => (
        <TouchableOpacity
            style={styles.tokenItem}
            onPress={() => onTokenPress(item)}
        >
            <View style={styles.tokenInfo}>
                <Text style={styles.symbol}>{item.symbol}</Text>
                <Text style={styles.name}>{item.name}</Text>
            </View>
            <View style={styles.balanceInfo}>
                <Text style={styles.balance}>
                    {parseFloat(item.balance).toFixed(4)} {item.symbol}
                </Text>
                {item.usdPrice && (
                    <Text style={styles.usdValue}>
                        ${(parseFloat(item.balance) * item.usdPrice).toFixed(2)}
                    </Text>
                )}
            </View>
        </TouchableOpacity>
    );

    return (
        <FlatList
            data={tokens}
            renderItem={renderItem}
            keyExtractor={item => item.address}
            style={styles.container}
        />
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
    },
    tokenItem: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        padding: 16,
        borderBottomWidth: 1,
        borderBottomColor: '#E0E0E0',
        backgroundColor: 'white',
    },
    tokenInfo: {
        flex: 1,
    },
    symbol: {
        fontSize: 16,
        fontWeight: 'bold',
    },
    name: {
        fontSize: 14,
        color: '#666',
        marginTop: 4,
    },
    balanceInfo: {
        alignItems: 'flex-end',
    },
    balance: {
        fontSize: 16,
        fontWeight: '500',
    },
    usdValue: {
        fontSize: 14,
        color: '#666',
        marginTop: 4,
    },
});
