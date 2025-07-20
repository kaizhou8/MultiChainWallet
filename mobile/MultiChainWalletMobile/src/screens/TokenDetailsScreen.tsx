import React, { useEffect, useState } from 'react';
import {
    View,
    Text,
    StyleSheet,
    TouchableOpacity,
    ScrollView,
    RefreshControl,
    Alert
} from 'react-native';
import { TokenInfo } from '../services/Web3Service';

interface TokenDetailsScreenProps {
    route: {
        params: {
            token: TokenInfo;
        };
    };
    navigation: any;
}

export const TokenDetailsScreen: React.FC<TokenDetailsScreenProps> = ({ route, navigation }) => {
    const { token } = route.params;
    const [refreshing, setRefreshing] = useState(false);
    const [transactionHistory, setTransactionHistory] = useState<any[]>([]);

    useEffect(() => {
        loadTransactionHistory();
    }, []);

    const loadTransactionHistory = async () => {
        // TODO: 实现交易历史记录加载
        // TODO: Implement transaction history loading
        setTransactionHistory([]);
    };

    const onRefresh = async () => {
        setRefreshing(true);
        await loadTransactionHistory();
        setRefreshing(false);
    };

    const handleSend = () => {
        navigation.navigate('SendToken', { token });
    };

    const handleRemove = () => {
        Alert.alert(
            '确认删除 / Confirm Removal',
            `确定要删除 ${token.symbol} 代币吗？/ Are you sure you want to remove ${token.symbol} token?`,
            [
                {
                    text: '取消 / Cancel',
                    style: 'cancel'
                },
                {
                    text: '删除 / Remove',
                    style: 'destructive',
                    onPress: async () => {
                        // TODO: 实现代币删除功能
                        // TODO: Implement token removal
                        navigation.goBack();
                    }
                }
            ]
        );
    };

    return (
        <ScrollView
            style={styles.container}
            refreshControl={
                <RefreshControl
                    refreshing={refreshing}
                    onRefresh={onRefresh}
                />
            }
        >
            {/* Token Info Card */}
            <View style={styles.card}>
                <Text style={styles.cardTitle}>代币信息 / Token Information</Text>
                <View style={styles.infoRow}>
                    <Text style={styles.label}>名称 / Name:</Text>
                    <Text style={styles.value}>{token.name}</Text>
                </View>
                <View style={styles.infoRow}>
                    <Text style={styles.label}>符号 / Symbol:</Text>
                    <Text style={styles.value}>{token.symbol}</Text>
                </View>
                <View style={styles.infoRow}>
                    <Text style={styles.label}>合约地址 / Contract:</Text>
                    <Text style={styles.value} numberOfLines={1}>{token.address}</Text>
                </View>
                <View style={styles.infoRow}>
                    <Text style={styles.label}>余额 / Balance:</Text>
                    <Text style={styles.value}>{token.balance} {token.symbol}</Text>
                </View>
                {token.usdPrice && (
                    <View style={styles.infoRow}>
                        <Text style={styles.label}>美元价值 / USD Value:</Text>
                        <Text style={styles.value}>
                            ${(parseFloat(token.balance) * token.usdPrice).toFixed(2)}
                        </Text>
                    </View>
                )}
            </View>

            {/* Action Buttons */}
            <View style={styles.actions}>
                <TouchableOpacity
                    style={[styles.button, styles.sendButton]}
                    onPress={handleSend}
                >
                    <Text style={styles.buttonText}>发送 / Send</Text>
                </TouchableOpacity>
                <TouchableOpacity
                    style={[styles.button, styles.removeButton]}
                    onPress={handleRemove}
                >
                    <Text style={[styles.buttonText, styles.removeButtonText]}>
                        删除 / Remove
                    </Text>
                </TouchableOpacity>
            </View>

            {/* Transaction History */}
            <View style={styles.card}>
                <Text style={styles.cardTitle}>交易历史 / Transaction History</Text>
                {transactionHistory.length === 0 ? (
                    <Text style={styles.noTransactions}>
                        暂无交易记录 / No transactions yet
                    </Text>
                ) : (
                    transactionHistory.map((tx, index) => (
                        <View key={index} style={styles.transaction}>
                            {/* TODO: 显示交易记录 / Display transaction */}
                        </View>
                    ))
                )}
            </View>
        </ScrollView>
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#F5F5F5',
    },
    card: {
        backgroundColor: 'white',
        margin: 16,
        padding: 16,
        borderRadius: 12,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.1,
        shadowRadius: 4,
        elevation: 3,
    },
    cardTitle: {
        fontSize: 18,
        fontWeight: 'bold',
        marginBottom: 16,
    },
    infoRow: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginBottom: 12,
    },
    label: {
        color: '#666',
        flex: 1,
    },
    value: {
        fontWeight: '500',
        flex: 2,
        textAlign: 'right',
    },
    actions: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        paddingHorizontal: 16,
        marginBottom: 16,
    },
    button: {
        flex: 1,
        padding: 16,
        borderRadius: 8,
        alignItems: 'center',
        marginHorizontal: 8,
    },
    sendButton: {
        backgroundColor: '#007AFF',
    },
    removeButton: {
        backgroundColor: 'white',
        borderWidth: 1,
        borderColor: '#FF3B30',
    },
    buttonText: {
        color: 'white',
        fontSize: 16,
        fontWeight: '500',
    },
    removeButtonText: {
        color: '#FF3B30',
    },
    noTransactions: {
        textAlign: 'center',
        color: '#666',
        padding: 16,
    },
    transaction: {
        borderBottomWidth: 1,
        borderBottomColor: '#E0E0E0',
        paddingVertical: 12,
    },
});
