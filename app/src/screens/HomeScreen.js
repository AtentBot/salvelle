import React, { useState, useEffect } from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet, ScrollView, RefreshControl,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { Feather } from '@expo/vector-icons';
import { useAuth } from '../hooks/useAuth';
import * as api from '../services/api';
import { formatCurrency, formatRelativeDate } from '../utils/formatters';
import { COLORS, GRADIENTS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';

const HomeScreen = ({ navigation }) => {
  const { user } = useAuth();
  const [refreshing, setRefreshing] = useState(false);
  const [orders, setOrders] = useState([]);
  const [cartCount, setCartCount] = useState(0);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      const ordersResult = await api.getOrders(1, 'IN_PROGRESS');
      if (ordersResult.success) setOrders(ordersResult.orders?.slice(0, 3) || []);

      const cartResult = await api.getCartCount();
      if (cartResult.success) setCartCount(cartResult.count || 0);
    } catch (error) {
      console.error('Erro:', error);
    }
  };

  const onRefresh = async () => {
    setRefreshing(true);
    await loadData();
    setRefreshing(false);
  };

  const getFirstName = () => {
    const name = user?.name || user?.fullName || 'Cliente';
    return name.split(' ')[0];
  };

  const getStatusColor = (order) => {
    const status = order?.status?.toUpperCase?.() || '';
    if (status.includes('READY') || status.includes('DONE')) return COLORS.success;
    if (status.includes('PROGRESS') || status.includes('PREP')) return COLORS.accent;
    return COLORS.primary;
  };

  return (
    <LinearGradient colors={GRADIENTS.background} style={styles.container}>
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={COLORS.primary} />}
        showsVerticalScrollIndicator={false}
      >
        {/* Header */}
        <View style={styles.header}>
          <View style={styles.logoContainer}>
            <View style={styles.logoDot} />
            <Text style={styles.logoText}>Salvelle</Text>
          </View>
          <TouchableOpacity style={styles.cartButton} onPress={() => navigation.navigate('Cart')}>
            <View style={styles.cartButtonInner}>
              <Feather name="shopping-bag" size={22} color={COLORS.text} />
            </View>
            {cartCount > 0 && (
              <View style={styles.cartBadge}>
                <Text style={styles.cartBadgeText}>{cartCount}</Text>
              </View>
            )}
          </TouchableOpacity>
        </View>

        {/* Greeting */}
        <View style={styles.greetingContainer}>
          <Text style={styles.greetingLabel}>Bem-vindo de volta,</Text>
          <Text style={styles.greetingName}>{getFirstName()}</Text>
        </View>

        {/* Quick Actions */}
        <View style={styles.actionsContainer}>
          {/* Receita */}
          <TouchableOpacity style={styles.actionCardReceita} onPress={() => navigation.navigate('Prescription')}>
            <View style={styles.receitaAccent} />
            <View style={styles.actionCardBody}>
              <View style={styles.actionIconWrap}>
                <Feather name="file-text" size={22} color={COLORS.primary} />
              </View>
              <Text style={styles.actionTitle}>Receita</Text>
              <Text style={styles.actionSubtitle}>Enviar para orçar</Text>
            </View>
          </TouchableOpacity>

          {/* Formula - hero card */}
          <TouchableOpacity style={styles.actionCardFormulaWrap} onPress={() => navigation.navigate('Formula')}>
            <LinearGradient colors={GRADIENTS.dark} style={styles.actionCardFormula}>
              <View style={styles.formulaAccentBar} />
              <View style={styles.actionCardBody}>
                <View style={styles.actionIconWrapDark}>
                  <Feather name="star" size={22} color={COLORS.accent} />
                </View>
                <Text style={styles.actionTitleWhite}>Fórmula</Text>
                <Text style={styles.actionSubtitleLight}>Personalizada</Text>
              </View>
              <View style={styles.formulaGlow} />
            </LinearGradient>
          </TouchableOpacity>

          {/* Catálogo */}
          <TouchableOpacity
            style={styles.actionCardCatalogo}
            onPress={() => navigation.jumpTo('SearchTab')}
            activeOpacity={0.85}
          >
            <View style={styles.catalogoPattern}>
              <View style={styles.patternDot1} />
              <View style={styles.patternDot2} />
              <View style={styles.patternDot3} />
            </View>
            <View style={styles.actionCardBody}>
              <View style={styles.actionIconWrap}>
                <Feather name="grid" size={22} color={COLORS.primary} />
              </View>
              <Text style={styles.actionTitle}>Catálogo</Text>
              <Text style={styles.actionSubtitle}>Ver produtos</Text>
            </View>
          </TouchableOpacity>
        </View>

        {/* Promo Banner */}
        <View style={styles.promoBannerWrap}>
          <LinearGradient colors={GRADIENTS.promo} style={styles.promoBanner}>
            <View style={styles.promoGeometric1} />
            <View style={styles.promoGeometric2} />
            <View style={styles.promoGeometric3} />

            <View style={styles.promoContent}>
              <View style={styles.promoTag}>
                <Feather name="zap" size={10} color={COLORS.text} />
                <Text style={styles.promoTagText}>Oferta especial</Text>
              </View>
              <Text style={styles.promoTitle}>15% OFF na primeira fórmula</Text>
              <Text style={styles.promoSubtitle}>Use o código FORMULA15</Text>
              <TouchableOpacity style={styles.promoButton} activeOpacity={0.85}>
                <Text style={styles.promoButtonText}>Resgatar agora</Text>
                <Feather name="arrow-right" size={14} color={COLORS.text} />
              </TouchableOpacity>
            </View>
          </LinearGradient>
        </View>

        {/* Orders Section */}
        <View style={styles.sectionHeader}>
          <View>
            <Text style={styles.sectionTitle}>Pedidos em andamento</Text>
            <Text style={styles.sectionCount}>
              {orders.length > 0 ? `${orders.length} pedido${orders.length > 1 ? 's' : ''} ativo${orders.length > 1 ? 's' : ''}` : 'Nenhum pedido'}
            </Text>
          </View>
          <TouchableOpacity style={styles.sectionLinkButton} onPress={() => navigation.navigate('OrdersTab')}>
            <Text style={styles.sectionLink}>Ver todos</Text>
            <Feather name="chevron-right" size={14} color={COLORS.primary} />
          </TouchableOpacity>
        </View>

        {orders.length > 0 ? (
          orders.map((order) => (
            <View key={order.id} style={styles.orderCard}>
              <View style={[styles.orderStatusBar, { backgroundColor: getStatusColor(order) }]} />
              <View style={styles.orderCardContent}>
                <View style={styles.orderHeader}>
                  <View style={styles.orderInfo}>
                    <Text style={styles.orderId}>Pedido #{order.code || order.id?.slice(0, 8)}</Text>
                    <Text style={styles.orderType}>{order.type || 'Fórmula personalizada'}</Text>
                  </View>
                  <TouchableOpacity style={styles.trackButton} onPress={() => navigation.navigate('OrderDetail', { orderId: order.id })}>
                    <Text style={styles.trackButtonText}>Acompanhar</Text>
                    <Feather name="chevron-right" size={12} color={COLORS.primary} />
                  </TouchableOpacity>
                </View>
                <View style={styles.orderFooter}>
                  <View style={styles.orderTime}>
                    <Feather name="clock" size={12} color={COLORS.textMuted} />
                    <Text style={styles.orderTimeText}>{formatRelativeDate(order.createdAt)}</Text>
                  </View>
                </View>
              </View>
            </View>
          ))
        ) : (
          <View style={styles.emptyOrders}>
            <View style={styles.emptyIllustration}>
              <View style={styles.emptyCircleOuter}>
                <View style={styles.emptyCircleInner}>
                  <Feather name="package" size={32} color={COLORS.primary} />
                </View>
              </View>
              <View style={styles.emptyAccentDot1} />
              <View style={styles.emptyAccentDot2} />
            </View>
            <Text style={styles.emptyText}>Nenhum pedido em andamento</Text>
            <Text style={styles.emptySubtext}>Que tal fazer seu primeiro pedido?</Text>
            <TouchableOpacity
              style={styles.emptyButton}
              onPress={() => navigation.navigate('Prescription')}
              activeOpacity={0.8}
            >
              <Text style={styles.emptyButtonText}>Enviar receita</Text>
            </TouchableOpacity>
          </View>
        )}

        <View style={{ height: 100 }} />
      </ScrollView>
    </LinearGradient>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1 },
  scrollContent: { padding: SPACING.lg, paddingTop: SPACING.xxxl + 20 },

  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: SPACING.xl },
  logoContainer: { flexDirection: 'row', alignItems: 'center', gap: SPACING.xs },
  logoDot: { width: 10, height: 10, borderRadius: 5, backgroundColor: COLORS.primary },
  logoText: { fontSize: FONT_SIZES.xl, fontWeight: '800', color: COLORS.text, letterSpacing: -0.5 },
  cartButton: { position: 'relative' },
  cartButtonInner: {
    width: 44, height: 44, borderRadius: 22,
    backgroundColor: COLORS.cardGlass, alignItems: 'center', justifyContent: 'center',
    borderWidth: 1, borderColor: COLORS.border,
  },
  cartBadge: {
    position: 'absolute', top: -2, right: -2, backgroundColor: COLORS.primary,
    borderRadius: 10, minWidth: 20, height: 20, alignItems: 'center', justifyContent: 'center',
    borderWidth: 2, borderColor: COLORS.background,
  },
  cartBadgeText: { fontSize: 10, fontWeight: '700', color: COLORS.white },

  greetingContainer: { marginBottom: SPACING.xl },
  greetingLabel: { fontSize: FONT_SIZES.sm, color: COLORS.textMuted, marginBottom: 2, letterSpacing: 0.2 },
  greetingName: { fontSize: FONT_SIZES.xxxl, fontWeight: '800', color: COLORS.text, letterSpacing: -0.8 },

  actionsContainer: { flexDirection: 'row', gap: SPACING.sm, marginBottom: SPACING.xl },
  actionCardReceita: {
    flex: 1, backgroundColor: COLORS.cardGlass, borderRadius: BORDER_RADIUS.lg,
    overflow: 'hidden', flexDirection: 'row', borderWidth: 1, borderColor: COLORS.border,
  },
  receitaAccent: { width: 4, backgroundColor: COLORS.primary },
  actionCardBody: { flex: 1, padding: SPACING.md, alignItems: 'center' },
  actionIconWrap: {
    width: 44, height: 44, borderRadius: 14,
    backgroundColor: COLORS.primaryMuted, alignItems: 'center', justifyContent: 'center', marginBottom: SPACING.sm,
  },
  actionIconWrapDark: {
    width: 44, height: 44, borderRadius: 14,
    backgroundColor: 'rgba(245, 158, 11, 0.15)', alignItems: 'center', justifyContent: 'center', marginBottom: SPACING.sm,
  },
  actionTitle: { fontSize: FONT_SIZES.sm, fontWeight: '700', color: COLORS.text, marginBottom: 2 },
  actionTitleWhite: { fontSize: FONT_SIZES.sm, fontWeight: '700', color: COLORS.white, marginBottom: 2 },
  actionSubtitle: { fontSize: 10, color: COLORS.textMuted, textAlign: 'center' },
  actionSubtitleLight: { fontSize: 10, color: 'rgba(255,255,255,0.7)', textAlign: 'center' },

  actionCardFormulaWrap: { flex: 1, borderRadius: BORDER_RADIUS.lg, overflow: 'hidden' },
  actionCardFormula: { flex: 1, overflow: 'hidden', position: 'relative' },
  formulaAccentBar: { position: 'absolute', top: 0, left: 0, right: 0, height: 3, backgroundColor: COLORS.accent },
  formulaGlow: {
    position: 'absolute', bottom: -20, right: -20, width: 60, height: 60, borderRadius: 30,
    backgroundColor: 'rgba(245, 158, 11, 0.08)',
  },

  actionCardCatalogo: {
    flex: 1, backgroundColor: COLORS.cardGlass, borderRadius: BORDER_RADIUS.lg,
    overflow: 'hidden', borderWidth: 1, borderColor: COLORS.border, position: 'relative',
  },
  catalogoPattern: { position: 'absolute', top: 0, left: 0, right: 0, bottom: 0 },
  patternDot1: { position: 'absolute', top: 8, right: 8, width: 6, height: 6, borderRadius: 3, backgroundColor: COLORS.primaryMuted },
  patternDot2: { position: 'absolute', top: 20, right: 18, width: 4, height: 4, borderRadius: 2, backgroundColor: COLORS.primaryMuted },
  patternDot3: { position: 'absolute', bottom: 10, left: 10, width: 8, height: 8, borderRadius: 4, backgroundColor: COLORS.primaryMuted },

  promoBannerWrap: { marginBottom: SPACING.xl, borderRadius: BORDER_RADIUS.xxl, overflow: 'hidden', ...SHADOWS.medium },
  promoBanner: { padding: SPACING.lg + 4, position: 'relative', overflow: 'hidden' },
  promoGeometric1: { position: 'absolute', top: -30, right: -30, width: 100, height: 100, borderRadius: 50, backgroundColor: 'rgba(255,255,255,0.06)' },
  promoGeometric2: { position: 'absolute', bottom: -20, right: 40, width: 60, height: 60, borderRadius: 12, backgroundColor: 'rgba(255,255,255,0.04)', transform: [{ rotate: '45deg' }] },
  promoGeometric3: { position: 'absolute', top: 10, right: 60, width: 30, height: 30, borderRadius: 15, backgroundColor: 'rgba(245, 158, 11, 0.12)' },
  promoContent: { position: 'relative', zIndex: 1 },
  promoTag: {
    flexDirection: 'row', alignItems: 'center', backgroundColor: COLORS.accent,
    paddingHorizontal: SPACING.sm + 2, paddingVertical: 4, borderRadius: BORDER_RADIUS.sm,
    alignSelf: 'flex-start', gap: 4, marginBottom: SPACING.md,
  },
  promoTagText: { fontSize: 10, fontWeight: '800', color: COLORS.text, textTransform: 'uppercase', letterSpacing: 0.5 },
  promoTitle: { fontSize: FONT_SIZES.xl, fontWeight: '800', color: COLORS.white, marginBottom: 4, letterSpacing: -0.3 },
  promoSubtitle: { fontSize: FONT_SIZES.sm, color: 'rgba(255,255,255,0.8)', marginBottom: SPACING.md },
  promoButton: {
    flexDirection: 'row', alignItems: 'center', gap: SPACING.xs,
    backgroundColor: COLORS.accent, paddingHorizontal: SPACING.lg, paddingVertical: SPACING.sm + 2,
    borderRadius: 9999, alignSelf: 'flex-start',
  },
  promoButtonText: { fontSize: FONT_SIZES.sm, fontWeight: '700', color: COLORS.text },

  sectionHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: SPACING.md },
  sectionTitle: { fontSize: FONT_SIZES.lg, fontWeight: '800', color: COLORS.text, letterSpacing: -0.3 },
  sectionCount: { fontSize: FONT_SIZES.xs, color: COLORS.textMuted, marginTop: 2 },
  sectionLinkButton: { flexDirection: 'row', alignItems: 'center', gap: 2 },
  sectionLink: { fontSize: FONT_SIZES.sm, fontWeight: '600', color: COLORS.primary },

  orderCard: {
    flexDirection: 'row', backgroundColor: COLORS.cardGlass, borderRadius: BORDER_RADIUS.lg,
    marginBottom: SPACING.sm, overflow: 'hidden', borderWidth: 1, borderColor: COLORS.border,
  },
  orderStatusBar: { width: 4 },
  orderCardContent: { flex: 1, padding: SPACING.md },
  orderHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  orderInfo: { flex: 1 },
  orderId: { fontSize: FONT_SIZES.sm, fontWeight: '700', color: COLORS.text },
  orderType: { fontSize: FONT_SIZES.xs, color: COLORS.textMuted, marginTop: 1 },
  orderFooter: { flexDirection: 'row', alignItems: 'center', marginTop: SPACING.sm, paddingTop: SPACING.sm, borderTopWidth: 1, borderTopColor: COLORS.borderLight },
  orderTime: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  orderTimeText: { fontSize: FONT_SIZES.xs, color: COLORS.textMuted },
  trackButton: {
    flexDirection: 'row', alignItems: 'center', gap: 2,
    backgroundColor: COLORS.primaryMuted, paddingHorizontal: SPACING.md, paddingVertical: SPACING.sm, borderRadius: 9999,
  },
  trackButtonText: { fontSize: FONT_SIZES.xs, fontWeight: '600', color: COLORS.primary },

  emptyOrders: { alignItems: 'center', paddingVertical: SPACING.xxxxl, paddingHorizontal: SPACING.lg },
  emptyIllustration: { position: 'relative', marginBottom: SPACING.lg, width: 100, height: 100, alignItems: 'center', justifyContent: 'center' },
  emptyCircleOuter: { width: 88, height: 88, borderRadius: 44, backgroundColor: COLORS.primaryMuted, alignItems: 'center', justifyContent: 'center' },
  emptyCircleInner: { width: 64, height: 64, borderRadius: 32, backgroundColor: COLORS.cardGlass, alignItems: 'center', justifyContent: 'center', borderWidth: 2, borderColor: COLORS.primary },
  emptyAccentDot1: { position: 'absolute', top: 4, right: 4, width: 10, height: 10, borderRadius: 5, backgroundColor: COLORS.accent },
  emptyAccentDot2: { position: 'absolute', bottom: 8, left: 0, width: 7, height: 7, borderRadius: 3.5, backgroundColor: COLORS.primary, opacity: 0.4 },
  emptyText: { fontSize: FONT_SIZES.md, fontWeight: '700', color: COLORS.text, marginBottom: 4 },
  emptySubtext: { fontSize: FONT_SIZES.sm, color: COLORS.textMuted, marginBottom: SPACING.lg, textAlign: 'center' },
  emptyButton: { backgroundColor: COLORS.primary, paddingHorizontal: SPACING.xl, paddingVertical: SPACING.sm + 2, borderRadius: 9999 },
  emptyButtonText: { fontSize: FONT_SIZES.sm, fontWeight: '700', color: COLORS.white },
});

export default HomeScreen;
