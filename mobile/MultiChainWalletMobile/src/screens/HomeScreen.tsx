import React, { useEffect, useState } from 'react';
import { View, StyleSheet, TouchableOpacity, Text } from 'react-native';
import { TokenList } from '../components/TokenList';
import { Web3Service, TokenInfo } from '../services/Web3Service';

// 初始化Web3Service（实际使用时需要配置正确的RPC URL）
// Initialize Web3Service (need to configure correct RPC URL in production)
const web3Service = new Web3Service('https://mainnet.infura.io/v3/YOUR-PROJECT-ID');

export const HomeScreen: React.FC = ({ navigation }: any) => {
    const [tokens, setTokens] = useState<TokenInfo[]>([]);

    useEffect(() => {
        loadTokens();
    }, []);

    const loadTokens = async () => {
        await web3Service.loadTokenList();
        const tokenList = await web3Service.getAllTokens();
        setTokens(tokenList);
    };

    const handleAddToken = () => {
        navigation.navigate('AddToken');
    };

    const handleTokenPress = (token: TokenInfo) => {
        navigation.navigate('TokenDetails', { token });
    };

    return (
        <View style={styles.container}>
            <View style={styles.header}>
                <Text style={styles.title}>我的代币 / My Tokens</Text>
                <TouchableOpacity
                    style={styles.addButton}
                    onPress={handleAddToken}
                >
                    <Text style={styles.addButtonText}>+</Text>
                </TouchableOpacity>
            </View>
            <TokenList
                tokens={tokens}
                onTokenPress={handleTokenPress}
            />
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#F5F5F5',
    },
    header: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: 16,
        backgroundColor: 'white',
        borderBottomWidth: 1,
        borderBottomColor: '#E0E0E0',
    },
    title: {
        fontSize: 20,
        fontWeight: 'bold',
    },
    addButton: {
        width: 40,
        height: 40,
        borderRadius: 20,
        backgroundColor: '#007AFF',
        justifyContent: 'center',
        alignItems: 'center',
    },
    addButtonText: {
        color: 'white',
        fontSize: 24,
        fontWeight: 'bold',
    },
});
