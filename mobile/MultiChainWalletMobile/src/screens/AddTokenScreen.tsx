import React, { useState } from 'react';
import {
    View,
    Text,
    TextInput,
    TouchableOpacity,
    StyleSheet,
    ActivityIndicator,
    Alert
} from 'react-native';
import { ethers } from 'ethers';
import { Web3Service } from '../services/Web3Service';

export const AddTokenScreen: React.FC = ({ navigation }: any) => {
    const [address, setAddress] = useState('');
    const [loading, setLoading] = useState(false);
    const [tokenInfo, setTokenInfo] = useState<{
        name: string;
        symbol: string;
        decimals: number;
    } | null>(null);

    const validateAddress = async () => {
        if (!ethers.utils.isAddress(address)) {
            Alert.alert('错误 / Error', '无效的以太坊地址 / Invalid Ethereum address');
            return false;
        }
        return true;
    };

    const handleAddressChange = async (text: string) => {
        setAddress(text);
        setTokenInfo(null);
        
        if (text.length === 42) { // 以太坊地址长度 / Ethereum address length
            setLoading(true);
            try {
                if (await validateAddress()) {
                    const web3Service = new Web3Service('https://mainnet.infura.io/v3/YOUR-PROJECT-ID');
                    const token = await web3Service.addToken(text);
                    if (token) {
                        setTokenInfo({
                            name: token.name,
                            symbol: token.symbol,
                            decimals: token.decimals
                        });
                    }
                }
            } catch (error) {
                console.error(error);
                Alert.alert('错误 / Error', '获取代币信息失败 / Failed to fetch token info');
            } finally {
                setLoading(false);
            }
        }
    };

    const handleAddToken = async () => {
        if (!tokenInfo) return;

        try {
            setLoading(true);
            const web3Service = new Web3Service('https://mainnet.infura.io/v3/YOUR-PROJECT-ID');
            const success = await web3Service.addToken(address);
            
            if (success) {
                Alert.alert(
                    '成功 / Success',
                    '代币已添加 / Token added successfully',
                    [{ text: 'OK', onPress: () => navigation.goBack() }]
                );
            } else {
                Alert.alert('错误 / Error', '添加代币失败 / Failed to add token');
            }
        } catch (error) {
            console.error(error);
            Alert.alert('错误 / Error', '添加代币失败 / Failed to add token');
        } finally {
            setLoading(false);
        }
    };

    return (
        <View style={styles.container}>
            <Text style={styles.label}>合约地址 / Contract Address</Text>
            <TextInput
                style={styles.input}
                value={address}
                onChangeText={handleAddressChange}
                placeholder="0x..."
                autoCapitalize="none"
            />

            {loading && (
                <ActivityIndicator style={styles.loader} size="large" color="#007AFF" />
            )}

            {tokenInfo && (
                <View style={styles.tokenInfo}>
                    <Text style={styles.infoLabel}>代币信息 / Token Info:</Text>
                    <Text>名称 / Name: {tokenInfo.name}</Text>
                    <Text>符号 / Symbol: {tokenInfo.symbol}</Text>
                    <Text>小数位数 / Decimals: {tokenInfo.decimals}</Text>
                </View>
            )}

            <TouchableOpacity
                style={[styles.button, !tokenInfo && styles.buttonDisabled]}
                onPress={handleAddToken}
                disabled={!tokenInfo || loading}
            >
                <Text style={styles.buttonText}>
                    添加代币 / Add Token
                </Text>
            </TouchableOpacity>
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
        padding: 16,
        backgroundColor: 'white',
    },
    label: {
        fontSize: 16,
        marginBottom: 8,
        fontWeight: '500',
    },
    input: {
        borderWidth: 1,
        borderColor: '#E0E0E0',
        borderRadius: 8,
        padding: 12,
        fontSize: 16,
        marginBottom: 16,
    },
    loader: {
        marginVertical: 16,
    },
    tokenInfo: {
        backgroundColor: '#F5F5F5',
        padding: 16,
        borderRadius: 8,
        marginBottom: 16,
    },
    infoLabel: {
        fontSize: 16,
        fontWeight: 'bold',
        marginBottom: 8,
    },
    button: {
        backgroundColor: '#007AFF',
        padding: 16,
        borderRadius: 8,
        alignItems: 'center',
    },
    buttonDisabled: {
        backgroundColor: '#CCCCCC',
    },
    buttonText: {
        color: 'white',
        fontSize: 16,
        fontWeight: '500',
    },
});
