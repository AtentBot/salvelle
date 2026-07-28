import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  FlatList,
  RefreshControl,
  ActivityIndicator,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { Feather } from '@expo/vector-icons';
import { useFocusEffect } from '@react-navigation/native';
import * as api from '../services/api';
import { formatCurrency, formatDate } from '../utils/formatters';
import { COLORS, GRADIENTS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';

const STATUS_LABELS = {
  PENDING: { label: 'Pendente', color: '#F59E0B', icon: 'clock' },
  CONFIRMED: { label: 'Confirmado', color: '#3B82F6', icon: 'check' },
  PREPARING: { label: 'Em preparação', color: '#0D9488', icon: 'zap' },
  IN_PROGRESS: { label: 'Em produção', color: '#0D9488', icon: 'zap' },
  READY: { label: 'Pronto', color: '#059669', icon: 'check-circle' },
  DELIVERED: { label: 'Entregue', color: '#57534E', icon: 'package' },
  CANCELLED: { label: 'Cancelado', color: '#DC2626', icon: 'x-circle' },
};

const FILTERS = [
  { key: null, label: 'Todos' },
  { key: 'IN_PROGRESS', label: 'Em andamento' },
  { key: 'READY', label: 'Prontos' },
  { key: 'DELIVERED', label: 'Entregues' },
];

const OrdersScreen = ({ navigation }) => {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [filter, setFilter] = useState(null);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);

  useFocusEffect(
    useCallback(() => {
      loadOrders(1, true);
    }, [filter])
  );

  const loadOrders = async (pageNum = 1, reset = false) => {
    if (loadingMore && !reset) return;

    try {
      if (reset) {
        setLoading(true);
      } else {
        setLoadingMore(true);
      }

      const result = await api.getOrders(pageNum, filter);

      if (result.success) {
        const newOrders = result.orders || [];

        if (reset) {
          setOrders(newOrders);
        } else {
          setOrders(prev => [...prev, ...newOrders]);
        }

        setPage(pageNum);
        setHasMore(newOrders.length >= 10);
      }
    } catch (error) {
      console.error('Erro ao carregar pedidos:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
      setLoadingMore(false);
    }
  };

  const onRefresh = () => {
    setRefreshing(true);
    loadOrders(1, true);
  };

  const onEndReached = () => {
    if (hasMore && !loadingMore) {
      loadOrders(page + 1);
    }
  };

  const getStatusInfo = (status) => {
    return STATUS_LABELS[status] || STATUS_LABELS.PENDING;
  };

  const renderOrder = ({ item }) => {
    const statusInfo = getStatusInfo(item.status);

    return (
      <TouchableOpacity
        style={styles.orderCard}
        onPress={() => navigation.navigate('OrderDetail', { orderId: item.id })}
        activeOpacity={0.8}
      >
        <View style={[styles.orderAccent, { backgroundColor: statusInfo.color }]} />
        <View style={styles.orderBody}>
          <View style={styles.orderHeader}>
            <View style={[styles.statusBadge, { backgroundColor: `${statusInfo.color}18` }]}>
              <Feather name={statusInfo.icon} size={13} color={statusInfo.color} />
              <Text style={[styles.statusText, { color: statusInfo.color }]}>
                {statusInfo.label}
              </Text>
            </View>
            <Text style={styles.orderDate}>{formatDate(item.createdAt)}</Text>
          </View>

          <View style={styles.orderInfo}>
            <Text style={styles.orderCode}>
              Pedido #{item.code || item.id?.slice(0, 8).toUpperCase()}
            </Text>
            <Text style={styles.orderDescription} numberOfLines={2}>
              {item.description || `${item.itemCount || 1} item(s)`}
            </Text>
          </View>

          <View style={styles.orderFooter}>
            <Text style={styles.orderTotal}>{formatCurrency(item.total || 0)}</Text>
            <View style={styles.viewButton}>
              <Text style={styles.viewButtonText}>Ver detalhes</Text>
              <Feather name="chevron-right" size={16} color={COLORS.primary} />
            </View>
          </View>
        </View>
      </TouchableOpacity>
    );
  };

  const renderEmpty = () => (
    <View style={styles.emptyContainer}>
      <View style={styles.emptyIconOuter}>
        <View style={styles.emptyIconInner}>
          <Feather name="package" size={48} color={COLORS.primary} />
        </View>
      </View>
      <Text style={styles.emptyTitle}>Nenhum pedido encontrado</Text>
      <Text style={styles.emptyText}>
        {filter
          ? 'Não há pedidos com este filtro'
          : 'Você ainda não fez nenhum pedido'
        }
      </Text>
      <TouchableOpacity onPress={() => navigation.navigate('Home')} activeOpacity={0.8}>
        <LinearGradient colors={GRADIENTS.primary} style={styles.emptyButton}>
          <Feather name="plus" size={18} color={COLORS.white} />
          <Text style={styles.emptyButtonText}>Fazer primeiro pedido</Text>
        </LinearGradient>
      </TouchableOpacity>
    </View>
  );

  const renderFooter = () => {
    if (!loadingMore) return null;
    return (
      <View style={styles.loadingMore}>
        <ActivityIndicator size="small" color={COLORS.primary} />
      </View>
    );
  };

  return (
    <LinearGradient colors={GRADIENTS.background} style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.headerTitle}>Meus Pedidos</Text>
        <Text style={styles.headerSubtitle}>Acompanhe suas encomendas</Text>
      </View>

      {/* Filters */}
      <View style={styles.filtersContainer}>
        <FlatList
          horizontal
          data={FILTERS}
          keyExtractor={(item) => String(item.key)}
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.filtersList}
          renderItem={({ item: filterItem }) => {
            const isActive = filter === filterItem.key;
            return (
              <TouchableOpacity
                style={[styles.filterChip, isActive && styles.filterChipActive]}
                onPress={() => setFilter(filterItem.key)}
                activeOpacity={0.7}
              >
                <Text style={[styles.filterChipText, isActive && styles.filterChipTextActive]}>
                  {filterItem.label}
                </Text>
              </TouchableOpacity>
            );
          }}
        />
      </View>

      {/* List */}
      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={COLORS.primary} />
        </View>
      ) : (
        <FlatList
          data={orders}
          keyExtractor={(item) => item.id}
          renderItem={renderOrder}
          contentContainerStyle={styles.listContent}
          refreshControl={
            <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
          }
          onEndReached={onEndReached}
          onEndReachedThreshold={0.3}
          ListEmptyComponent={renderEmpty}
          ListFooterComponent={renderFooter}
          showsVerticalScrollIndicator={false}
        />
      )}
    </LinearGradient>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1 },

  header: {
    paddingTop: SPACING.xxxl + 10, paddingHorizontal: SPACING.lg, paddingBottom: SPACING.sm,
  },
  headerTitle: { fontSize: FONT_SIZES.xxxl, fontWeight: '800', color: COLORS.text },
  headerSubtitle: { fontSize: FONT_SIZES.md, color: COLORS.textSecondary, marginTop: SPACING.xs },

  filtersContainer: { marginBottom: SPACING.md },
  filtersList: { paddingHorizontal: SPACING.lg, gap: SPACING.sm },
  filterChip: {
    paddingHorizontal: SPACING.lg, paddingVertical: SPACING.sm + 2,
    borderRadius: BORDER_RADIUS.full, backgroundColor: COLORS.white,
    marginRight: SPACING.sm, borderWidth: 1.5, borderColor: COLORS.borderLight,
  },
  filterChipActive: {
    backgroundColor: COLORS.primary, borderColor: COLORS.primary,
  },
  filterChipText: { fontSize: FONT_SIZES.sm, color: COLORS.textSecondary, fontWeight: '600' },
  filterChipTextActive: { color: COLORS.white },

  loadingContainer: { flex: 1, alignItems: 'center', justifyContent: 'center' },

  listContent: { padding: SPACING.lg, paddingBottom: 100 },

  orderCard: {
    flexDirection: 'row', backgroundColor: COLORS.white, borderRadius: BORDER_RADIUS.xxl,
    marginBottom: SPACING.md, overflow: 'hidden', ...SHADOWS.small,
  },
  orderAccent: { width: 5 },
  orderBody: { flex: 1, padding: SPACING.lg },

  orderHeader: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    marginBottom: SPACING.md,
  },
  statusBadge: {
    flexDirection: 'row', alignItems: 'center',
    paddingHorizontal: SPACING.sm + 2, paddingVertical: SPACING.xs + 1,
    borderRadius: BORDER_RADIUS.full, gap: 5,
  },
  statusText: { fontSize: FONT_SIZES.xs, fontWeight: '700' },
  orderDate: { fontSize: FONT_SIZES.xs, color: COLORS.textMuted },

  orderInfo: { marginBottom: SPACING.md },
  orderCode: { fontSize: FONT_SIZES.md, fontWeight: '700', color: COLORS.text, marginBottom: SPACING.xs },
  orderDescription: { fontSize: FONT_SIZES.sm, color: COLORS.textSecondary, lineHeight: 20 },

  orderFooter: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingTop: SPACING.md, borderTopWidth: 1, borderTopColor: COLORS.borderLight,
  },
  orderTotal: { fontSize: FONT_SIZES.lg, fontWeight: '800', color: COLORS.text },
  viewButton: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  viewButtonText: { fontSize: FONT_SIZES.sm, color: COLORS.primary, fontWeight: '600' },

  emptyContainer: {
    flex: 1, alignItems: 'center', justifyContent: 'center',
    padding: SPACING.xl, marginTop: SPACING.xxxl,
  },
  emptyIconOuter: {
    width: 120, height: 120, borderRadius: 60,
    backgroundColor: COLORS.borderLight, alignItems: 'center', justifyContent: 'center',
    marginBottom: SPACING.lg,
  },
  emptyIconInner: {
    width: 88, height: 88, borderRadius: 44,
    backgroundColor: COLORS.primaryMuted, alignItems: 'center', justifyContent: 'center',
  },
  emptyTitle: { fontSize: FONT_SIZES.xl, fontWeight: '700', color: COLORS.text, marginBottom: SPACING.sm },
  emptyText: { fontSize: FONT_SIZES.md, color: COLORS.textSecondary, textAlign: 'center', marginBottom: SPACING.xl },
  emptyButton: {
    flexDirection: 'row', alignItems: 'center', gap: SPACING.sm,
    paddingHorizontal: SPACING.xl, paddingVertical: SPACING.md + 2, borderRadius: BORDER_RADIUS.xxl,
    ...SHADOWS.colored(COLORS.primary),
  },
  emptyButtonText: { fontSize: FONT_SIZES.md, fontWeight: '700', color: COLORS.white },

  loadingMore: { padding: SPACING.lg, alignItems: 'center' },
});

export default OrdersScreen;
