import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  Alert,
  ActivityIndicator,
  RefreshControl,
  TextInput,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { Feather } from '@expo/vector-icons';
import { useFocusEffect } from '@react-navigation/native';
import * as api from '../services/api';
import { formatCurrency } from '../utils/formatters';
import { COLORS, GRADIENTS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';

const CartScreen = ({ navigation }) => {
  const [cart, setCart] = useState({ items: [], total: 0 });
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [updatingItem, setUpdatingItem] = useState(null);
  const [couponCode, setCouponCode] = useState('');
  const [discount, setDiscount] = useState(0);

  useFocusEffect(
    useCallback(() => {
      loadCart();
    }, [])
  );

  const loadCart = async () => {
    try {
      const result = await api.getCart();
      if (result.success) {
        setCart({
          items: result.items || [],
          total: result.total || 0,
          subtotal: result.subtotal || 0,
        });
      }
    } catch (error) {
      console.error('Erro ao carregar carrinho:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const onRefresh = () => {
    setRefreshing(true);
    loadCart();
  };

  const updateQuantity = async (itemId, newQuantity) => {
    if (newQuantity < 1) {
      removeItem(itemId);
      return;
    }

    setUpdatingItem(itemId);
    try {
      const result = await api.updateCartItem(itemId, newQuantity);
      if (result.success) {
        loadCart();
      } else {
        Alert.alert('Erro', result.message || 'Não foi possível atualizar.');
      }
    } catch (error) {
      Alert.alert('Erro', 'Não foi possível atualizar o item.');
    } finally {
      setUpdatingItem(null);
    }
  };

  const removeItem = async (itemId) => {
    Alert.alert(
      'Remover item',
      'Deseja remover este item do carrinho?',
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Remover',
          style: 'destructive',
          onPress: async () => {
            setUpdatingItem(itemId);
            try {
              const result = await api.removeFromCart(itemId);
              if (result.success) {
                loadCart();
              }
            } catch (error) {
              Alert.alert('Erro', 'Não foi possível remover o item.');
            } finally {
              setUpdatingItem(null);
            }
          }
        }
      ]
    );
  };

  const applyCoupon = () => {
    if (!couponCode.trim()) return;

    if (couponCode.toUpperCase() === 'FORMULA15') {
      const discountValue = cart.total * 0.15;
      setDiscount(discountValue);
      Alert.alert('Cupom aplicado!', '15% de desconto aplicado.');
    } else {
      Alert.alert('Cupom inválido', 'O código informado não é válido.');
    }
  };

  const checkout = async () => {
    if (cart.items.length === 0) {
      Alert.alert('Carrinho vazio', 'Adicione itens ao carrinho primeiro.');
      return;
    }

    Alert.alert(
      'Finalizar pedido',
      `Total: ${formatCurrency(cart.total - discount)}`,
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Confirmar',
          onPress: async () => {
            try {
              const result = await api.createOrder({
                couponCode: discount > 0 ? couponCode : null,
              });

              if (result.success) {
                Alert.alert(
                  'Pedido realizado!',
                  'Seu pedido foi enviado com sucesso.',
                  [{ text: 'Ver pedido', onPress: () => navigation.navigate('Orders') }]
                );
                loadCart();
              } else {
                Alert.alert('Erro', result.message || 'Não foi possível finalizar.');
              }
            } catch (error) {
              Alert.alert('Erro', 'Ocorreu um erro ao finalizar o pedido.');
            }
          }
        }
      ]
    );
  };

  const clearCart = () => {
    Alert.alert(
      'Limpar carrinho',
      'Deseja remover todos os itens?',
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Limpar',
          style: 'destructive',
          onPress: async () => {
            try {
              await api.clearCart();
              loadCart();
            } catch (error) {
              Alert.alert('Erro', 'Não foi possível limpar o carrinho.');
            }
          }
        }
      ]
    );
  };

  if (loading) {
    return (
      <LinearGradient colors={GRADIENTS.background} style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={COLORS.primary} />
      </LinearGradient>
    );
  }

  return (
    <LinearGradient colors={GRADIENTS.background} style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity style={styles.backButton} onPress={() => navigation.goBack()}>
          <View style={styles.backButtonCircle}>
            <Feather name="arrow-left" size={20} color={COLORS.primary} />
          </View>
        </TouchableOpacity>
        <View style={styles.headerCenter}>
          <Text style={styles.headerTitle}>Carrinho</Text>
          {cart.items.length > 0 && (
            <View style={styles.itemCountBadge}>
              <Text style={styles.itemCountText}>{cart.items.length}</Text>
            </View>
          )}
        </View>
        {cart.items.length > 0 ? (
          <TouchableOpacity onPress={clearCart} style={styles.clearBtn}>
            <Feather name="trash" size={16} color={COLORS.error} />
            <Text style={styles.clearButton}>Limpar</Text>
          </TouchableOpacity>
        ) : (
          <View style={{ width: 50 }} />
        )}
      </View>

      {cart.items.length === 0 ? (
        <View style={styles.emptyContainer}>
          <View style={styles.emptyIconOuter}>
            <View style={styles.emptyIconInner}>
              <Feather name="shopping-bag" size={48} color={COLORS.primary} />
            </View>
          </View>
          <Text style={styles.emptyTitle}>Carrinho vazio</Text>
          <Text style={styles.emptyText}>
            Adicione fórmulas ou produtos ao seu carrinho
          </Text>
          <TouchableOpacity onPress={() => navigation.navigate('Home')} activeOpacity={0.8}>
            <LinearGradient colors={GRADIENTS.primary} style={styles.emptyButton}>
              <Feather name="compass" size={18} color={COLORS.white} />
              <Text style={styles.emptyButtonText}>Explorar produtos</Text>
            </LinearGradient>
          </TouchableOpacity>
        </View>
      ) : (
        <>
          <ScrollView
            style={styles.scrollView}
            contentContainerStyle={styles.scrollContent}
            refreshControl={
              <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
            }
            showsVerticalScrollIndicator={false}
          >
            {/* Cart items */}
            {cart.items.map((item) => (
              <View key={item.id} style={styles.cartItem}>
                <View style={styles.itemAccent} />
                <View style={styles.itemBody}>
                  <View style={styles.itemIcon}>
                    <Feather
                      name={item.type === 'formula' ? 'droplet' : 'package'}
                      size={20}
                      color={COLORS.primary}
                    />
                  </View>
                  <View style={styles.itemInfo}>
                    <Text style={styles.itemName} numberOfLines={2}>
                      {item.name || 'Fórmula personalizada'}
                    </Text>
                    {item.description && (
                      <Text style={styles.itemDescription} numberOfLines={1}>
                        {item.description}
                      </Text>
                    )}
                    <Text style={styles.itemPrice}>
                      {formatCurrency(item.price || 0)}
                    </Text>
                  </View>
                  <View style={styles.quantityControls}>
                    <TouchableOpacity
                      style={styles.quantityButton}
                      onPress={() => updateQuantity(item.id, item.quantity - 1)}
                      disabled={updatingItem === item.id}
                    >
                      <Feather name="minus" size={14} color={COLORS.primary} />
                    </TouchableOpacity>

                    {updatingItem === item.id ? (
                      <ActivityIndicator size="small" color={COLORS.primary} />
                    ) : (
                      <Text style={styles.quantityText}>{item.quantity}</Text>
                    )}

                    <TouchableOpacity
                      style={[styles.quantityButton, styles.quantityButtonPlus]}
                      onPress={() => updateQuantity(item.id, item.quantity + 1)}
                      disabled={updatingItem === item.id}
                    >
                      <Feather name="plus" size={14} color={COLORS.white} />
                    </TouchableOpacity>
                  </View>
                  <TouchableOpacity
                    style={styles.removeButton}
                    onPress={() => removeItem(item.id)}
                  >
                    <Feather name="trash-2" size={16} color={COLORS.error} />
                  </TouchableOpacity>
                </View>
              </View>
            ))}

            {/* Coupon section */}
            <View style={styles.couponSection}>
              <View style={styles.couponLabelRow}>
                <Feather name="tag" size={16} color={COLORS.primary} />
                <Text style={styles.sectionLabel}>Cupom de desconto</Text>
              </View>
              <View style={styles.couponInputContainer}>
                <TextInput
                  style={styles.couponInput}
                  placeholder="Digite o código"
                  placeholderTextColor={COLORS.textMuted}
                  value={couponCode}
                  onChangeText={setCouponCode}
                  autoCapitalize="characters"
                />
                <TouchableOpacity onPress={applyCoupon} activeOpacity={0.8}>
                  <LinearGradient colors={GRADIENTS.primary} style={styles.couponButton}>
                    <Text style={styles.couponButtonText}>Aplicar</Text>
                  </LinearGradient>
                </TouchableOpacity>
              </View>
              {discount > 0 && (
                <View style={styles.couponApplied}>
                  <View style={styles.couponSuccessBadge}>
                    <Feather name="check" size={12} color={COLORS.white} />
                  </View>
                  <Text style={styles.couponAppliedText}>
                    {couponCode.toUpperCase()} — {formatCurrency(discount)} de desconto
                  </Text>
                </View>
              )}
            </View>

            {/* Summary */}
            <View style={styles.summaryCard}>
              <View style={styles.summaryRow}>
                <Text style={styles.summaryLabel}>Subtotal</Text>
                <Text style={styles.summaryValue}>{formatCurrency(cart.total)}</Text>
              </View>
              {discount > 0 && (
                <View style={styles.summaryRow}>
                  <Text style={styles.discountLabel}>Desconto</Text>
                  <Text style={styles.discountValue}>-{formatCurrency(discount)}</Text>
                </View>
              )}
              <View style={styles.divider} />
              <View style={styles.summaryRow}>
                <Text style={styles.totalLabel}>Total</Text>
                <Text style={styles.totalValue}>
                  {formatCurrency(cart.total - discount)}
                </Text>
              </View>
            </View>

            <View style={{ height: 130 }} />
          </ScrollView>

          {/* Checkout footer */}
          <View style={styles.checkoutContainer}>
            <TouchableOpacity onPress={checkout} activeOpacity={0.8}>
              <LinearGradient colors={GRADIENTS.primary} style={styles.checkoutButton}>
                <View style={styles.checkoutLeft}>
                  <Feather name="shield" size={20} color={COLORS.white} />
                  <Text style={styles.checkoutButtonText}>Finalizar pedido</Text>
                </View>
                <Text style={styles.checkoutTotal}>
                  {formatCurrency(cart.total - discount)}
                </Text>
              </LinearGradient>
            </TouchableOpacity>
          </View>
        </>
      )}
    </LinearGradient>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1 },
  loadingContainer: { flex: 1, alignItems: 'center', justifyContent: 'center' },

  header: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingTop: SPACING.xxxl + 10, paddingHorizontal: SPACING.lg, paddingBottom: SPACING.md,
  },
  backButton: { padding: 2 },
  backButtonCircle: {
    width: 44, height: 44, borderRadius: 22,
    backgroundColor: COLORS.primaryMuted, alignItems: 'center', justifyContent: 'center',
  },
  headerCenter: { flexDirection: 'row', alignItems: 'center', gap: SPACING.sm },
  headerTitle: { fontSize: FONT_SIZES.lg, fontWeight: '700', color: COLORS.text },
  itemCountBadge: {
    backgroundColor: COLORS.primary, borderRadius: 12,
    minWidth: 24, height: 24, alignItems: 'center', justifyContent: 'center',
    paddingHorizontal: 6,
  },
  itemCountText: { fontSize: FONT_SIZES.xs, fontWeight: '700', color: COLORS.white },
  clearBtn: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  clearButton: { fontSize: FONT_SIZES.sm, color: COLORS.error, fontWeight: '600' },

  emptyContainer: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: SPACING.xl },
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

  scrollView: { flex: 1 },
  scrollContent: { padding: SPACING.lg },

  cartItem: {
    flexDirection: 'row', backgroundColor: COLORS.white, borderRadius: BORDER_RADIUS.lg,
    marginBottom: SPACING.sm, overflow: 'hidden', ...SHADOWS.small,
  },
  itemAccent: { width: 4, backgroundColor: COLORS.primary },
  itemBody: { flex: 1, flexDirection: 'row', alignItems: 'center', padding: SPACING.md },
  itemIcon: {
    width: 44, height: 44, borderRadius: BORDER_RADIUS.md,
    backgroundColor: COLORS.primaryMuted, alignItems: 'center', justifyContent: 'center',
    marginRight: SPACING.md,
  },
  itemInfo: { flex: 1 },
  itemName: { fontSize: FONT_SIZES.md, fontWeight: '600', color: COLORS.text },
  itemDescription: { fontSize: FONT_SIZES.xs, color: COLORS.textMuted, marginTop: 2 },
  itemPrice: { fontSize: FONT_SIZES.sm, fontWeight: '700', color: COLORS.primary, marginTop: SPACING.xs },

  quantityControls: { flexDirection: 'row', alignItems: 'center', gap: SPACING.sm, marginRight: SPACING.sm },
  quantityButton: {
    width: 30, height: 30, borderRadius: 15,
    backgroundColor: COLORS.primaryMuted, alignItems: 'center', justifyContent: 'center',
  },
  quantityButtonPlus: { backgroundColor: COLORS.primary },
  quantityText: { fontSize: FONT_SIZES.md, fontWeight: '700', color: COLORS.text, minWidth: 20, textAlign: 'center' },
  removeButton: { padding: SPACING.sm },

  couponSection: { marginTop: SPACING.lg, marginBottom: SPACING.lg },
  couponLabelRow: { flexDirection: 'row', alignItems: 'center', gap: SPACING.sm, marginBottom: SPACING.sm },
  sectionLabel: { fontSize: FONT_SIZES.md, fontWeight: '600', color: COLORS.text },
  couponInputContainer: { flexDirection: 'row', gap: SPACING.sm },
  couponInput: {
    flex: 1, backgroundColor: COLORS.white, borderRadius: BORDER_RADIUS.xxl,
    paddingVertical: 14, paddingHorizontal: SPACING.lg,
    fontSize: FONT_SIZES.md, color: COLORS.text, borderWidth: 2, borderColor: COLORS.border,
  },
  couponButton: {
    paddingHorizontal: SPACING.xl, borderRadius: BORDER_RADIUS.xxl, justifyContent: 'center',
  },
  couponButtonText: { fontSize: FONT_SIZES.md, fontWeight: '700', color: COLORS.white },
  couponApplied: {
    flexDirection: 'row', alignItems: 'center', gap: SPACING.sm, marginTop: SPACING.sm,
    backgroundColor: 'rgba(5,150,105,0.08)', borderRadius: BORDER_RADIUS.md,
    paddingVertical: SPACING.sm, paddingHorizontal: SPACING.md,
  },
  couponSuccessBadge: {
    width: 22, height: 22, borderRadius: 11,
    backgroundColor: COLORS.success, alignItems: 'center', justifyContent: 'center',
  },
  couponAppliedText: { fontSize: FONT_SIZES.sm, color: COLORS.success, fontWeight: '600' },

  summaryCard: {
    backgroundColor: COLORS.white, borderRadius: BORDER_RADIUS.xxl, padding: SPACING.lg + 4,
    borderWidth: 1, borderColor: COLORS.borderLight, ...SHADOWS.medium,
  },
  summaryRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: SPACING.sm },
  summaryLabel: { fontSize: FONT_SIZES.md, color: COLORS.textSecondary },
  summaryValue: { fontSize: FONT_SIZES.md, color: COLORS.text, fontWeight: '600' },
  discountLabel: { fontSize: FONT_SIZES.md, color: COLORS.success },
  discountValue: { fontSize: FONT_SIZES.md, color: COLORS.success, fontWeight: '600' },
  divider: { height: 1, backgroundColor: COLORS.borderLight, marginVertical: SPACING.md },
  totalLabel: { fontSize: FONT_SIZES.lg, fontWeight: '700', color: COLORS.text },
  totalValue: { fontSize: FONT_SIZES.xl, fontWeight: '800', color: COLORS.primary },

  checkoutContainer: {
    position: 'absolute', bottom: 0, left: 0, right: 0,
    padding: SPACING.lg, paddingBottom: SPACING.xxxl,
    backgroundColor: COLORS.cardGlass,
    borderTopWidth: 1, borderTopColor: COLORS.borderLight,
  },
  checkoutButton: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingVertical: 18, paddingHorizontal: SPACING.xl, borderRadius: BORDER_RADIUS.xxl,
    ...SHADOWS.colored(COLORS.primary),
  },
  checkoutLeft: { flexDirection: 'row', alignItems: 'center', gap: SPACING.sm },
  checkoutButtonText: { fontSize: FONT_SIZES.lg, fontWeight: '700', color: COLORS.white },
  checkoutTotal: { fontSize: FONT_SIZES.lg, fontWeight: '800', color: COLORS.white },
});

export default CartScreen;
