import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  ActivityIndicator,
  RefreshControl,
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

// Ordem da linha do tempo de produção (CANCELLED é tratado à parte)
const TIMELINE = ['PENDING', 'CONFIRMED', 'IN_PROGRESS', 'READY', 'DELIVERED'];

const getStatusInfo = (status) => STATUS_LABELS[status] || STATUS_LABELS.PENDING;

const OrderDetailScreen = ({ route, navigation }) => {
  const { orderId } = route.params || {};
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);

  const loadOrder = async () => {
    try {
      setError(null);
      const result = await api.getOrderDetails(orderId);
      if (result.success && result.order) {
        setOrder(result.order);
      } else {
        setError(result.message || 'Pedido não encontrado');
      }
    } catch (e) {
      setError('Erro ao carregar o pedido');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      loadOrder();
    }, [orderId])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadOrder();
  };

  const renderHeader = () => (
    <View style={styles.header}>
      <TouchableOpacity style={styles.backButton} onPress={() => navigation.goBack()}>
        <View style={styles.backButtonCircle}>
          <Feather name="arrow-left" size={20} color={COLORS.primary} />
        </View>
      </TouchableOpacity>
      <Text style={styles.headerTitle}>Detalhes do pedido</Text>
      <View style={{ width: 44 }} />
    </View>
  );

  if (loading) {
    return (
      <LinearGradient colors={GRADIENTS.background} style={styles.container}>
        {renderHeader()}
        <View style={styles.centered}>
          <ActivityIndicator size="large" color={COLORS.primary} />
        </View>
      </LinearGradient>
    );
  }

  if (error || !order) {
    return (
      <LinearGradient colors={GRADIENTS.background} style={styles.container}>
        {renderHeader()}
        <View style={styles.centered}>
          <View style={styles.errorIcon}>
            <Feather name="alert-circle" size={40} color={COLORS.error} />
          </View>
          <Text style={styles.errorTitle}>{error || 'Pedido não encontrado'}</Text>
          <TouchableOpacity style={styles.retryButton} onPress={onRefresh} activeOpacity={0.8}>
            <Text style={styles.retryButtonText}>Tentar novamente</Text>
          </TouchableOpacity>
        </View>
      </LinearGradient>
    );
  }

  const statusInfo = getStatusInfo(order.status);
  const isCancelled = order.status === 'CANCELLED';
  const currentStep = TIMELINE.indexOf(order.status);

  return (
    <LinearGradient colors={GRADIENTS.background} style={styles.container}>
      {renderHeader()}

      <ScrollView
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={COLORS.primary} />}
      >
        {/* Status hero */}
        <View style={styles.statusCard}>
          <View style={[styles.statusIconWrap, { backgroundColor: `${statusInfo.color}18` }]}>
            <Feather name={statusInfo.icon} size={26} color={statusInfo.color} />
          </View>
          <View style={styles.statusInfo}>
            <Text style={styles.orderCode}>Pedido #{order.orderNumber || order.id?.slice(0, 8).toUpperCase()}</Text>
            <Text style={[styles.statusLabel, { color: statusInfo.color }]}>
              {order.statusDisplay || statusInfo.label}
            </Text>
            <Text style={styles.orderDate}>{formatDate(order.createdAt)}</Text>
          </View>
        </View>

        {/* Linha do tempo */}
        {!isCancelled && (
          <View style={styles.card}>
            <Text style={styles.sectionTitle}>Acompanhamento</Text>
            {TIMELINE.map((step, index) => {
              const info = getStatusInfo(step);
              const done = index <= currentStep;
              const isLast = index === TIMELINE.length - 1;
              return (
                <View key={step} style={styles.timelineRow}>
                  <View style={styles.timelineMarkerCol}>
                    <View style={[styles.timelineDot, done && { backgroundColor: COLORS.primary, borderColor: COLORS.primary }]}>
                      {done && <Feather name="check" size={11} color={COLORS.white} />}
                    </View>
                    {!isLast && <View style={[styles.timelineLine, index < currentStep && { backgroundColor: COLORS.primary }]} />}
                  </View>
                  <Text style={[styles.timelineLabel, done && styles.timelineLabelActive]}>
                    {info.label}
                  </Text>
                </View>
              );
            })}
          </View>
        )}

        {isCancelled && (
          <View style={[styles.card, styles.cancelledCard]}>
            <Feather name="x-circle" size={18} color={COLORS.error} />
            <Text style={styles.cancelledText}>Este pedido foi cancelado.</Text>
          </View>
        )}

        {/* Itens */}
        <View style={styles.card}>
          <Text style={styles.sectionTitle}>Itens</Text>
          {(order.items || []).map((item) => (
            <View key={item.id} style={styles.itemRow}>
              <View style={styles.itemQtyBadge}>
                <Text style={styles.itemQtyText}>{item.quantity}x</Text>
              </View>
              <View style={styles.itemInfo}>
                <Text style={styles.itemName}>{item.productName}</Text>
                {!!item.notes && <Text style={styles.itemNotes}>{item.notes}</Text>}
              </View>
              <Text style={styles.itemPrice}>{formatCurrency(item.totalPrice)}</Text>
            </View>
          ))}
        </View>

        {/* Resumo de valores */}
        <View style={styles.card}>
          <Text style={styles.sectionTitle}>Resumo</Text>
          <View style={styles.summaryRow}>
            <Text style={styles.summaryLabel}>Subtotal</Text>
            <Text style={styles.summaryValue}>{formatCurrency(order.subtotal)}</Text>
          </View>
          {order.discount > 0 && (
            <View style={styles.summaryRow}>
              <Text style={styles.summaryLabel}>Desconto</Text>
              <Text style={[styles.summaryValue, { color: COLORS.success }]}>- {formatCurrency(order.discount)}</Text>
            </View>
          )}
          {order.deliveryFee > 0 && (
            <View style={styles.summaryRow}>
              <Text style={styles.summaryLabel}>Entrega</Text>
              <Text style={styles.summaryValue}>{formatCurrency(order.deliveryFee)}</Text>
            </View>
          )}
          <View style={[styles.summaryRow, styles.totalRow]}>
            <Text style={styles.totalLabel}>Total</Text>
            <Text style={styles.totalValue}>{formatCurrency(order.total)}</Text>
          </View>
        </View>

        {/* Entrega e pagamento */}
        <View style={styles.card}>
          <Text style={styles.sectionTitle}>Informações</Text>
          {!!order.establishmentName && (
            <View style={styles.infoRow}>
              <Feather name="home" size={15} color={COLORS.textMuted} />
              <Text style={styles.infoLabel}>Farmácia</Text>
              <Text style={styles.infoValue}>{order.establishmentName}</Text>
            </View>
          )}
          <View style={styles.infoRow}>
            <Feather name="truck" size={15} color={COLORS.textMuted} />
            <Text style={styles.infoLabel}>Entrega</Text>
            <Text style={styles.infoValue}>{order.deliveryTypeDisplay || order.deliveryType}</Text>
          </View>
          <View style={styles.infoRow}>
            <Feather name="credit-card" size={15} color={COLORS.textMuted} />
            <Text style={styles.infoLabel}>Pagamento</Text>
            <Text style={styles.infoValue}>{order.paymentStatusDisplay || order.paymentStatus}</Text>
          </View>
        </View>

        {/* Observações */}
        {!!order.customerNotes && (
          <View style={styles.card}>
            <Text style={styles.sectionTitle}>Observações</Text>
            <Text style={styles.notesText}>{order.customerNotes}</Text>
          </View>
        )}

        <View style={{ height: 40 }} />
      </ScrollView>
    </LinearGradient>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1 },
  centered: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: SPACING.xl },

  header: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingTop: SPACING.xxxl + 10, paddingHorizontal: SPACING.lg, paddingBottom: SPACING.md,
  },
  backButton: { padding: 2 },
  backButtonCircle: {
    width: 44, height: 44, borderRadius: 22,
    backgroundColor: COLORS.primaryMuted, alignItems: 'center', justifyContent: 'center',
  },
  headerTitle: { fontSize: FONT_SIZES.lg, fontWeight: '700', color: COLORS.text },

  scrollContent: { padding: SPACING.lg, paddingTop: SPACING.sm },

  statusCard: {
    flexDirection: 'row', alignItems: 'center', gap: SPACING.md,
    backgroundColor: COLORS.white, borderRadius: BORDER_RADIUS.xxl,
    padding: SPACING.lg, marginBottom: SPACING.md, ...SHADOWS.small,
  },
  statusIconWrap: {
    width: 54, height: 54, borderRadius: 27, alignItems: 'center', justifyContent: 'center',
  },
  statusInfo: { flex: 1 },
  orderCode: { fontSize: FONT_SIZES.md, fontWeight: '800', color: COLORS.text },
  statusLabel: { fontSize: FONT_SIZES.sm, fontWeight: '700', marginTop: 2 },
  orderDate: { fontSize: FONT_SIZES.xs, color: COLORS.textMuted, marginTop: 2 },

  card: {
    backgroundColor: COLORS.white, borderRadius: BORDER_RADIUS.xxl,
    padding: SPACING.lg, marginBottom: SPACING.md, ...SHADOWS.small,
  },
  sectionTitle: { fontSize: FONT_SIZES.md, fontWeight: '800', color: COLORS.text, marginBottom: SPACING.md },

  timelineRow: { flexDirection: 'row', alignItems: 'flex-start' },
  timelineMarkerCol: { alignItems: 'center', width: 28 },
  timelineDot: {
    width: 22, height: 22, borderRadius: 11, borderWidth: 2, borderColor: COLORS.border,
    backgroundColor: COLORS.white, alignItems: 'center', justifyContent: 'center',
  },
  timelineLine: { width: 2, height: 26, backgroundColor: COLORS.border, marginVertical: 2 },
  timelineLabel: {
    fontSize: FONT_SIZES.sm, color: COLORS.textMuted, marginLeft: SPACING.md,
    paddingTop: 2, fontWeight: '600',
  },
  timelineLabelActive: { color: COLORS.text, fontWeight: '700' },

  cancelledCard: { flexDirection: 'row', alignItems: 'center', gap: SPACING.sm },
  cancelledText: { fontSize: FONT_SIZES.sm, color: COLORS.error, fontWeight: '600' },

  itemRow: {
    flexDirection: 'row', alignItems: 'center', gap: SPACING.md,
    paddingVertical: SPACING.sm, borderBottomWidth: 1, borderBottomColor: COLORS.borderLight,
  },
  itemQtyBadge: {
    minWidth: 34, height: 34, borderRadius: 10, paddingHorizontal: 6,
    backgroundColor: COLORS.primaryMuted, alignItems: 'center', justifyContent: 'center',
  },
  itemQtyText: { fontSize: FONT_SIZES.sm, fontWeight: '700', color: COLORS.primary },
  itemInfo: { flex: 1 },
  itemName: { fontSize: FONT_SIZES.sm, fontWeight: '600', color: COLORS.text },
  itemNotes: { fontSize: FONT_SIZES.xs, color: COLORS.textMuted, marginTop: 1 },
  itemPrice: { fontSize: FONT_SIZES.sm, fontWeight: '700', color: COLORS.text },

  summaryRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: SPACING.sm },
  summaryLabel: { fontSize: FONT_SIZES.sm, color: COLORS.textSecondary },
  summaryValue: { fontSize: FONT_SIZES.sm, fontWeight: '600', color: COLORS.text },
  totalRow: { marginBottom: 0, marginTop: SPACING.xs, paddingTop: SPACING.md, borderTopWidth: 1, borderTopColor: COLORS.borderLight },
  totalLabel: { fontSize: FONT_SIZES.md, fontWeight: '800', color: COLORS.text },
  totalValue: { fontSize: FONT_SIZES.lg, fontWeight: '800', color: COLORS.primary },

  infoRow: { flexDirection: 'row', alignItems: 'center', gap: SPACING.sm, marginBottom: SPACING.md },
  infoLabel: { fontSize: FONT_SIZES.sm, color: COLORS.textMuted, width: 90 },
  infoValue: { flex: 1, fontSize: FONT_SIZES.sm, fontWeight: '600', color: COLORS.text, textAlign: 'right' },

  notesText: { fontSize: FONT_SIZES.sm, color: COLORS.textSecondary, lineHeight: 20 },

  errorIcon: {
    width: 80, height: 80, borderRadius: 40, backgroundColor: COLORS.errorLight,
    alignItems: 'center', justifyContent: 'center', marginBottom: SPACING.lg,
  },
  errorTitle: { fontSize: FONT_SIZES.md, fontWeight: '700', color: COLORS.text, textAlign: 'center', marginBottom: SPACING.lg },
  retryButton: { backgroundColor: COLORS.primary, paddingHorizontal: SPACING.xl, paddingVertical: SPACING.md, borderRadius: BORDER_RADIUS.full },
  retryButtonText: { fontSize: FONT_SIZES.sm, fontWeight: '700', color: COLORS.white },
});

export default OrderDetailScreen;
