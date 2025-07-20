import React, { useState, useEffect } from 'react';
import {
    View,
    Text,
    TextInput,
    TouchableOpacity,
    StyleSheet,
    Alert,
    ActivityIndicator,
    KeyboardAvoidingView,
    Platform,
    ScrollView
} from 'react-native';
import { ethers } from 'ethers';
import { TokenInfo, Web3Service } from '../services/Web3Service';

interface SendTokenScreenProps {
    route: {
        params: {
            token: TokenInfo;
        };
    };
    navigation: any;
}

export const SendTokenScreen: React.FC<SendTokenScreenProps> = ({ route, navigation }) => {
    const { token } = route.params;
    const [toAddress, setToAddress] = useState('');
    const [amount, setAmount] = useState('');
    const [loading, setLoading] = useState(false);
    const [gasEstimate, setGasEstimate] = useState<string | null>(null);

    useEffect(() => {
        if (toAddress && amount) {
            estimateGas();
        }
    }, [toAddress, amount]);

    const estimateGas = async () => {
        // TODO: 实现gas估算
        // TODO: Implement gas estimation
        setGasEstimate('0.0001 ETH');
    };

    const validateInput = () => {
        if (!ethers.utils.isAddress(toAddress)) {
            Alert.alert('错误 / Error', '无效的接收地址 / Invalid recipient address');
            return false;
        }

        const amountNum = parseFloat(amount);
        if (isNaN(amountNum) || amountNum <= 0) {
            Alert.alert('错误 / Error', '无效的金额 / Invalid amount');
            return false;
        }

        const balance = parseFloat(token.balance);
        if (amountNum > balance) {
            Alert.alert('错误 / Error', '余额不足 / Insufficient balance');
            return false;
        }

        return true;
    };

    const handleSend = async () => {
        if (!validateInput()) return;

        try {
            setLoading(true);
            // TODO: 实现发送代币功能
            // TODO: Implement token sending
            Alert.alert(
                '成功 / Success',
                '代币已发送 / Token sent successfully',
                [{ text: 'OK', onPress: () => navigation.goBack() }]
            );
        } catch (error) {
            console.error(error);
            Alert.alert('错误 / Error', '发送失败 / Failed to send token');
        } finally {
            setLoading(false);
        }
    };

    return (
        <KeyboardAvoidingView
            style={styles.container}
            behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        >
            <ScrollView style={styles.scrollView}>
                {/* Token Info */}
                <View style={styles.tokenInfo}>
                    <Text style={styles.tokenSymbol}>{token.symbol}</Text>
                    <Text style={styles.tokenBalance}>
                        余额 / Balance: {token.balance} {token.symbol}
                    </Text>
                </View>

                {/* Input Fields */}
                <View style={styles.inputContainer}>
                    <Text style={styles.label}>接收地址 / To Address</Text>
                    <TextInput
                        style={styles.input}
                        value={toAddress}
                        onChangeText={setToAddress}
                        placeholder="0x..."
                        autoCapitalize="none"
                    />

                    <Text style={styles.label}>数量 / Amount</Text>
                    <View style={styles.amountContainer}>
                        <TextInput
                            style={styles.amountInput}
                            value={amount}
                            onChangeText={setAmount}
                            keyboardType="decimal-pad"
                            placeholder="0.0"
                        />
                        <Text style={styles.amountSymbol}>{token.symbol}</Text>
                    </View>

                    {token.usdPrice && amount && !isNaN(parseFloat(amount)) && (
                        <Text style={styles.usdValue}>
                            ≈ ${(parseFloat(amount) * token.usdPrice).toFixed(2)}
                        </Text>
                    )}
                </View>

                {/* Gas Estimate */}
                {gasEstimate && (
                    <View style={styles.gasContainer}>
                        <Text style={styles.gasLabel}>
                            预计矿工费用 / Estimated Gas Fee:
                        </Text>
                        <Text style={styles.gasValue}>{gasEstimate}</Text>
                    </View>
                )}
            </ScrollView>

            {/* Send Button */}
            <TouchableOpacity
                style={[styles.sendButton, loading && styles.sendButtonDisabled]}
                onPress={handleSend}
                disabled={loading}
            >
                {loading ? (
                    <ActivityIndicator color="white" />
                ) : (
                    <Text style={styles.sendButtonText}>
                        发送 / Send
                    </Text>
                )}
            </TouchableOpacity>
        </KeyboardAvoidingView>
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: 'white',
    },
    scrollView: {
        flex: 1,
    },
    tokenInfo: {
        alignItems: 'center',
        padding: 16,
        borderBottomWidth: 1,
        borderBottomColor: '#E0E0E0',
    },
    tokenSymbol: {
        fontSize: 24,
        fontWeight: 'bold',
    },
    tokenBalance: {
        fontSize: 16,
        color: '#666',
        marginTop: 8,
    },
    inputContainer: {
        padding: 16,
    },
    label: {
        fontSize: 16,
        marginBottom: 8,
        color: '#333',
    },
    input: {
        borderWidth: 1,
        borderColor: '#E0E0E0',
        borderRadius: 8,
        padding: 12,
        fontSize: 16,
        marginBottom: 16,
    },
    amountContainer: {
        flexDirection: 'row',
        alignItems: 'center',
        marginBottom: 8,
    },
    amountInput: {
        flex: 1,
        borderWidth: 1,
        borderColor: '#E0E0E0',
        borderRadius: 8,
        padding: 12,
        fontSize: 16,
    },
    amountSymbol: {
        marginLeft: 12,
        fontSize: 16,
        fontWeight: '500',
    },
    usdValue: {
        fontSize: 14,
        color: '#666',
        marginBottom: 16,
    },
    gasContainer: {
        padding: 16,
        backgroundColor: '#F5F5F5',
        marginHorizontal: 16,
        borderRadius: 8,
    },
    gasLabel: {
        fontSize: 14,
        color: '#666',
    },
    gasValue: {
        fontSize: 16,
        fontWeight: '500',
        marginTop: 4,
    },
    sendButton: {
        backgroundColor: '#007AFF',
        margin: 16,
        padding: 16,
        borderRadius: 8,
        alignItems: 'center',
    },
    sendButtonDisabled: {
        backgroundColor: '#CCCCCC',
    },
    sendButtonText: {
        color: 'white',
        fontSize: 18,
        fontWeight: '600',
    },
});
