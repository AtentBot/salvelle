import React, { useState, useEffect, useRef } from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet, ScrollView, Alert,
  ActivityIndicator, TextInput, StatusBar,
} from 'react-native';
import { Feather } from '@expo/vector-icons';
import * as api from '../services/api';
import { formatCurrency } from '../utils/formatters';
import { COLORS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';
import SalvelleLogo from '../components/SalvelleLogo';

// Sugestões pré-populadas (ingredientes comuns) — mostradas antes de qualquer busca
const POPULAR_INGREDIENTS = [
  'Vitamina C',
  'Vitamina D3',
  'Vitamina B12',
  'Magnésio Glicinato',
  'Colágeno',
  'Ômega 3',
  'Zinco',
  'Melatonina',
  'Coenzima Q10',
  'Cafeína',
];

const ICON_BY_TYPE = (name) => {
  const n = (name || '').toLowerCase();
  if (n.includes('capsul')) return 'circle';
  if (n.includes('creme') || n.includes('pomada')) return 'droplet';
  if (n.includes('solu') || n.includes('xarop')) return 'thermometer';
  if (n.includes('sache')) return 'package';
  if (n.includes('gel')) return 'cloud-drizzle';
  return 'box';
};

const FormulaScreen = ({ navigation, route }) => {
  const initialTypeId = route?.params?.productTypeId;
  const initialIngredient = route?.params?.initialIngredient;

  const [productTypes, setProductTypes] = useState([]);
  const [selectedType, setSelectedType] = useState(null);
  const [ingredients, setIngredients] = useState(() =>
    initialIngredient
      ? [{
          ...initialIngredient,
          quantity: initialIngredient.defaultQuantity || 100,
          unit: initialIngredient.defaultUnit || initialIngredient.unit || 'mg',
        }]
      : []
  );
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState([]);
  const [searching, setSearching] = useState(false);
  const [calculating, setCalculating] = useState(false);
  const [totalPrice, setTotalPrice] = useState(0);
  const [loading, setLoading] = useState(true);
  const [suggestionsLoading, setSuggestionsLoading] = useState(false);
  const [suggestions, setSuggestions] = useState([]);
  const searchSeq = useRef(0);

  useEffect(() => { loadProductTypes(); }, []);
  useEffect(() => { loadSuggestions(); }, []);

  // Autocomplete com debounce + guarda contra respostas fora de ordem
  useEffect(() => {
    const q = searchQuery.trim();
    if (q.length < 2) {
      setSearchResults([]);
      setSearching(false);
      return;
    }
    setSearching(true);
    const seq = ++searchSeq.current;
    const timer = setTimeout(async () => {
      try {
        const arr = await api.autocompleteIngredients(q);
        if (seq === searchSeq.current) setSearchResults(Array.isArray(arr) ? arr : []);
      } catch {
        if (seq === searchSeq.current) setSearchResults([]);
      } finally {
        if (seq === searchSeq.current) setSearching(false);
      }
    }, 300);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  useEffect(() => {
    if (selectedType && ingredients.length > 0) calculatePrice();
    else setTotalPrice(0);
  }, [selectedType, ingredients]);

  const loadProductTypes = async () => {
    try {
      const result = await api.getProductTypes();
      const list = result?.productTypes || result?.data || result || [];
      if (Array.isArray(list) && list.length > 0) {
        setProductTypes(list);
        const initial = initialTypeId
          ? list.find((t) => t.id === initialTypeId)
          : list[0];
        setSelectedType(initial || list[0]);
      }
    } catch (error) {
      // silencioso: tela ainda funciona pra buscar ingredientes
    } finally {
      setLoading(false);
    }
  };

  const loadSuggestions = async () => {
    setSuggestionsLoading(true);
    try {
      const promises = POPULAR_INGREDIENTS.slice(0, 6).map((q) =>
        api.autocompleteIngredients(q)
          .then((arr) => (Array.isArray(arr) ? arr[0] : null))
          .catch(() => null)
      );
      const results = (await Promise.all(promises)).filter(Boolean);
      // dedupe por id
      const seen = new Set();
      const deduped = results.filter((r) => {
        if (!r?.id || seen.has(r.id)) return false;
        seen.add(r.id);
        return true;
      });
      setSuggestions(deduped);
    } catch {
      setSuggestions([]);
    } finally {
      setSuggestionsLoading(false);
    }
  };

  const addIngredient = (ingredient) => {
    if (ingredients.some((i) => i.id === ingredient.id)) {
      Alert.alert('Já adicionado', 'Esse ingrediente já está na sua fórmula.');
      return;
    }
    setIngredients([
      ...ingredients,
      { ...ingredient, quantity: ingredient.defaultQuantity || 100, unit: ingredient.defaultUnit || 'mg' },
    ]);
    setSearchQuery('');
    setSearchResults([]);
  };

  const updateQty = (id, qty) => {
    setIngredients(ingredients.map((i) => i.id === id ? { ...i, quantity: parseFloat(qty) || 0 } : i));
  };

  const updateUnit = (id, unit) => {
    setIngredients(ingredients.map((i) => i.id === id ? { ...i, unit } : i));
  };

  const removeIngredient = (id) => {
    setIngredients(ingredients.filter((i) => i.id !== id));
  };

  const calculatePrice = async () => {
    if (!selectedType || ingredients.length === 0) return;
    setCalculating(true);
    try {
      const result = await api.calculateFormula(
        selectedType.name,
        ingredients.map((i) => ({ rawMaterialId: i.id, name: i.name, quantity: i.quantity, unit: i.unit }))
      );
      if (result?.success) setTotalPrice(result.totalPrice || 0);
    } catch {}
    setCalculating(false);
  };

  const addToCart = async () => {
    if (!selectedType || ingredients.length === 0) {
      Alert.alert('Atenção', 'Escolha um tipo e adicione ao menos um ingrediente.');
      return;
    }
    try {
      const result = await api.addFormulaToCart({
        productTypeId: selectedType.id,
        ingredients: ingredients.map((i) => ({
          ingredientId: i.id, ingredientName: i.name, quantity: i.quantity, unit: i.unit,
        })),
      });
      if (result?.success) {
        Alert.alert('Adicionado ao carrinho!', 'Sua fórmula personalizada foi adicionada.',
          [{ text: 'Ver carrinho', onPress: () => navigation.navigate('Cart') }, { text: 'Continuar', style: 'cancel' }]
        );
      } else {
        Alert.alert('Erro', result?.message || 'Não foi possível adicionar ao carrinho.');
      }
    } catch {
      Alert.alert('Erro', 'Ocorreu um erro. Tente novamente.');
    }
  };

  return (
    <View style={styles.container}>
      <StatusBar barStyle="dark-content" backgroundColor={COLORS.background} />

      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backBtn}>
          <Feather name="arrow-left" size={20} color={COLORS.ink} />
        </TouchableOpacity>
        <SalvelleLogo size={24} />
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={styles.scrollContent}
        keyboardShouldPersistTaps="handled"
        showsVerticalScrollIndicator={false}
      >
        <Text style={styles.title}>Monte sua fórmula</Text>
        <Text style={styles.subtitle}>
          Escolha a forma farmacêutica e adicione os ativos. O preço é calculado em tempo real.
        </Text>

        {/* Tipo de produto */}
        <View style={styles.section}>
          <Text style={styles.sectionLabel}>1. Forma farmacêutica</Text>
          {loading ? (
            <View style={styles.skelRow}>
              {[1, 2, 3, 4].map((i) => <View key={i} style={styles.skelCard} />)}
            </View>
          ) : productTypes.length === 0 ? (
            <View style={styles.emptyTypes}>
              <Feather name="info" size={16} color={COLORS.ink3} />
              <Text style={styles.emptyTypesText}>
                Nenhuma forma disponível agora. Tenta enviar uma receita.
              </Text>
            </View>
          ) : (
            <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.typesRow}>
              {productTypes.map((pt) => {
                const isSelected = selectedType?.id === pt.id;
                return (
                  <TouchableOpacity
                    key={pt.id}
                    style={[styles.typeCard, isSelected && styles.typeCardActive]}
                    onPress={() => setSelectedType(pt)}
                    activeOpacity={0.85}
                  >
                    <View style={[styles.typeIconWrap, isSelected && styles.typeIconWrapActive]}>
                      <Feather
                        name={ICON_BY_TYPE(pt.name)}
                        size={20}
                        color={isSelected ? COLORS.white : COLORS.primary}
                      />
                    </View>
                    <Text style={[styles.typeName, isSelected && styles.typeNameActive]}>{pt.name}</Text>
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          )}
        </View>

        {/* Busca de ingredientes */}
        <View style={styles.section}>
          <Text style={styles.sectionLabel}>2. Ingredientes</Text>
          <View style={styles.searchWrap}>
            <Feather name="search" size={18} color={COLORS.ink3} style={{ marginRight: SPACING.sm }} />
            <TextInput
              style={styles.searchInput}
              placeholder="Buscar (ex: vitamina C, melatonina...)"
              placeholderTextColor={COLORS.ink4}
              value={searchQuery}
              onChangeText={setSearchQuery}
            />
            {searching ? <ActivityIndicator size="small" color={COLORS.primary} /> : null}
            {searchQuery && !searching ? (
              <TouchableOpacity onPress={() => { setSearchQuery(''); setSearchResults([]); }}>
                <Feather name="x" size={18} color={COLORS.ink3} />
              </TouchableOpacity>
            ) : null}
          </View>

          {searchResults.length > 0 && (
            <View style={styles.resultsCard}>
              {searchResults.map((item) => (
                <TouchableOpacity
                  key={item.id}
                  style={styles.resultItem}
                  onPress={() => addIngredient(item)}
                  activeOpacity={0.7}
                >
                  <View style={{ flex: 1 }}>
                    <Text style={styles.resultName}>{item.name}</Text>
                    {item.category ? (
                      <Text style={styles.resultMeta}>{item.category}</Text>
                    ) : null}
                  </View>
                  <View style={styles.addCircle}>
                    <Feather name="plus" size={16} color={COLORS.white} />
                  </View>
                </TouchableOpacity>
              ))}
            </View>
          )}

          {/* Sugestões — só aparecem quando não tem busca ativa */}
          {!searchQuery && (
            <View style={{ marginTop: SPACING.md }}>
              <Text style={styles.suggestionsLabel}>Sugestões populares</Text>
              {suggestionsLoading ? (
                <View style={styles.skelRow}>
                  {[1, 2, 3].map((i) => <View key={i} style={styles.skelCard} />)}
                </View>
              ) : (
                <View style={styles.chipsWrap}>
                  {(suggestions.length > 0 ? suggestions : POPULAR_INGREDIENTS.map((n) => ({ id: 'name:' + n, name: n })))
                    .map((s) => (
                      <TouchableOpacity
                        key={s.id || s.name}
                        style={styles.chip}
                        onPress={() => {
                          if (s.id && !String(s.id).startsWith('name:')) {
                            addIngredient(s);
                          } else {
                            setSearchQuery(s.name);
                          }
                        }}
                      >
                        <Feather name="plus" size={12} color={COLORS.primary} />
                        <Text style={styles.chipText}>{s.name}</Text>
                      </TouchableOpacity>
                    ))}
                </View>
              )}
            </View>
          )}
        </View>

        {/* Lista de ingredientes adicionados */}
        {ingredients.length > 0 && (
          <View style={styles.section}>
            <Text style={styles.sectionLabel}>3. Sua fórmula ({ingredients.length})</Text>
            {ingredients.map((ing) => (
              <View key={ing.id} style={styles.ingCard}>
                <View style={styles.ingHead}>
                  <Text style={styles.ingName} numberOfLines={1}>{ing.name}</Text>
                  <TouchableOpacity onPress={() => removeIngredient(ing.id)}>
                    <Feather name="trash-2" size={16} color={COLORS.error} />
                  </TouchableOpacity>
                </View>
                <View style={styles.ingControls}>
                  <TextInput
                    style={styles.qtyInput}
                    keyboardType="numeric"
                    value={String(ing.quantity)}
                    onChangeText={(t) => updateQty(ing.id, t)}
                  />
                  <View style={styles.unitSelector}>
                    {['mg', 'g', 'ml'].map((u) => (
                      <TouchableOpacity
                        key={u}
                        style={[styles.unitBtn, ing.unit === u && styles.unitBtnActive]}
                        onPress={() => updateUnit(ing.id, u)}
                      >
                        <Text style={[styles.unitText, ing.unit === u && styles.unitTextActive]}>{u}</Text>
                      </TouchableOpacity>
                    ))}
                  </View>
                </View>
              </View>
            ))}
          </View>
        )}

        {/* Resumo */}
        {ingredients.length > 0 && (
          <View style={styles.summary}>
            <View style={styles.summaryRow}>
              <Text style={styles.summaryLabel}>Tipo</Text>
              <Text style={styles.summaryValue}>{selectedType?.name || '—'}</Text>
            </View>
            <View style={styles.summaryRow}>
              <Text style={styles.summaryLabel}>Ingredientes</Text>
              <Text style={styles.summaryValue}>{ingredients.length}</Text>
            </View>
            <View style={styles.divider} />
            <View style={styles.summaryRow}>
              <Text style={styles.totalLabel}>Total estimado</Text>
              {calculating ? (
                <ActivityIndicator size="small" color={COLORS.primary} />
              ) : (
                <Text style={styles.totalValue}>{formatCurrency(totalPrice)}</Text>
              )}
            </View>
          </View>
        )}

        {ingredients.length > 0 && (
          <TouchableOpacity onPress={addToCart} style={styles.cta} activeOpacity={0.85}>
            <Feather name="shopping-cart" size={18} color={COLORS.white} />
            <Text style={styles.ctaText}>Adicionar ao carrinho</Text>
          </TouchableOpacity>
        )}

        <View style={{ height: 60 }} />
      </ScrollView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: COLORS.background },
  header: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingHorizontal: SPACING.lg, paddingTop: 56, paddingBottom: SPACING.md,
  },
  backBtn: {
    width: 40, height: 40, borderRadius: BORDER_RADIUS.full,
    backgroundColor: COLORS.surface,
    alignItems: 'center', justifyContent: 'center',
    borderWidth: 1, borderColor: COLORS.border,
  },
  scrollContent: { paddingHorizontal: SPACING.lg, paddingBottom: SPACING.xl },
  title: { fontSize: FONT_SIZES.xxl, fontWeight: '700', color: COLORS.ink, letterSpacing: -0.5 },
  subtitle: { fontSize: FONT_SIZES.sm, color: COLORS.ink2, marginTop: 4, marginBottom: SPACING.lg, lineHeight: 20 },
  section: { marginBottom: SPACING.xl },
  sectionLabel: {
    fontSize: FONT_SIZES.sm, fontWeight: '600', color: COLORS.ink2,
    marginBottom: SPACING.sm, letterSpacing: 0.2,
  },

  // Tipo de produto
  typesRow: { gap: SPACING.sm, paddingRight: SPACING.lg },
  typeCard: {
    width: 96, padding: SPACING.md, borderRadius: BORDER_RADIUS.lg,
    backgroundColor: COLORS.surface, alignItems: 'center',
    borderWidth: 1, borderColor: COLORS.border, marginRight: SPACING.sm,
    ...SHADOWS.card,
  },
  typeCardActive: {
    backgroundColor: COLORS.primarySoft,
    borderColor: COLORS.primary,
  },
  typeIconWrap: {
    width: 40, height: 40, borderRadius: BORDER_RADIUS.full,
    backgroundColor: COLORS.primarySoft,
    alignItems: 'center', justifyContent: 'center',
    marginBottom: SPACING.sm,
  },
  typeIconWrapActive: { backgroundColor: COLORS.primary },
  typeName: { fontSize: FONT_SIZES.xs, color: COLORS.ink2, fontWeight: '500', textAlign: 'center' },
  typeNameActive: { color: COLORS.primary, fontWeight: '700' },

  emptyTypes: {
    flexDirection: 'row', alignItems: 'center', gap: SPACING.sm,
    padding: SPACING.md, backgroundColor: COLORS.backgroundAlt,
    borderRadius: BORDER_RADIUS.md,
  },
  emptyTypesText: { flex: 1, fontSize: FONT_SIZES.sm, color: COLORS.ink2 },

  // Skeleton
  skelRow: { flexDirection: 'row', gap: SPACING.sm },
  skelCard: {
    width: 80, height: 80, borderRadius: BORDER_RADIUS.lg,
    backgroundColor: COLORS.backgroundAlt,
  },

  // Search
  searchWrap: {
    flexDirection: 'row', alignItems: 'center',
    backgroundColor: COLORS.surface,
    borderRadius: BORDER_RADIUS.md, paddingHorizontal: SPACING.md,
    height: 52, borderWidth: 1, borderColor: COLORS.border,
    ...SHADOWS.card,
  },
  searchInput: { flex: 1, fontSize: FONT_SIZES.md, color: COLORS.ink },

  resultsCard: {
    marginTop: SPACING.sm,
    backgroundColor: COLORS.surface,
    borderRadius: BORDER_RADIUS.lg,
    borderWidth: 1, borderColor: COLORS.border,
    overflow: 'hidden',
    ...SHADOWS.card,
  },
  resultItem: {
    flexDirection: 'row', alignItems: 'center',
    padding: SPACING.md,
    borderBottomWidth: 1, borderBottomColor: COLORS.borderLight,
  },
  resultName: { fontSize: FONT_SIZES.md, color: COLORS.ink, fontWeight: '500' },
  resultMeta: { fontSize: FONT_SIZES.xs, color: COLORS.ink3, marginTop: 2 },
  addCircle: {
    width: 32, height: 32, borderRadius: BORDER_RADIUS.full,
    backgroundColor: COLORS.primary,
    alignItems: 'center', justifyContent: 'center',
  },

  // Sugestões
  suggestionsLabel: {
    fontSize: FONT_SIZES.xs, fontWeight: '600', color: COLORS.ink3,
    marginBottom: SPACING.sm, textTransform: 'uppercase', letterSpacing: 0.5,
  },
  chipsWrap: { flexDirection: 'row', flexWrap: 'wrap', gap: SPACING.sm },
  chip: {
    flexDirection: 'row', alignItems: 'center', gap: 4,
    paddingVertical: SPACING.xs + 2, paddingHorizontal: SPACING.md,
    backgroundColor: COLORS.surface,
    borderRadius: BORDER_RADIUS.full,
    borderWidth: 1, borderColor: COLORS.border,
  },
  chipText: { fontSize: FONT_SIZES.sm, color: COLORS.ink, fontWeight: '500' },

  // Ingrediente adicionado
  ingCard: {
    backgroundColor: COLORS.surface,
    borderRadius: BORDER_RADIUS.lg,
    padding: SPACING.md,
    marginBottom: SPACING.sm,
    borderWidth: 1, borderColor: COLORS.border,
    ...SHADOWS.card,
  },
  ingHead: {
    flexDirection: 'row', alignItems: 'center',
    justifyContent: 'space-between', marginBottom: SPACING.sm,
  },
  ingName: { fontSize: FONT_SIZES.md, fontWeight: '600', color: COLORS.ink, flex: 1, marginRight: SPACING.sm },
  ingControls: { flexDirection: 'row', alignItems: 'center', gap: SPACING.sm },
  qtyInput: {
    width: 80, height: 40, backgroundColor: COLORS.backgroundAlt,
    borderRadius: BORDER_RADIUS.md, paddingHorizontal: SPACING.sm,
    fontSize: FONT_SIZES.md, color: COLORS.ink, textAlign: 'center', fontWeight: '600',
  },
  unitSelector: { flexDirection: 'row', gap: 4 },
  unitBtn: {
    paddingHorizontal: SPACING.md, paddingVertical: SPACING.xs + 2,
    borderRadius: BORDER_RADIUS.md, backgroundColor: COLORS.backgroundAlt,
  },
  unitBtnActive: { backgroundColor: COLORS.primary },
  unitText: { fontSize: FONT_SIZES.xs, color: COLORS.ink3, fontWeight: '600' },
  unitTextActive: { color: COLORS.white },

  // Summary
  summary: {
    backgroundColor: COLORS.surface,
    borderRadius: BORDER_RADIUS.lg,
    padding: SPACING.lg,
    borderWidth: 1, borderColor: COLORS.border,
    marginBottom: SPACING.md,
    ...SHADOWS.card,
  },
  summaryRow: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    marginBottom: SPACING.sm,
  },
  summaryLabel: { fontSize: FONT_SIZES.sm, color: COLORS.ink2 },
  summaryValue: { fontSize: FONT_SIZES.md, color: COLORS.ink, fontWeight: '600' },
  divider: { height: 1, backgroundColor: COLORS.border, marginVertical: SPACING.sm },
  totalLabel: { fontSize: FONT_SIZES.md, fontWeight: '700', color: COLORS.ink },
  totalValue: { fontSize: FONT_SIZES.xl, fontWeight: '800', color: COLORS.primary, letterSpacing: -0.5 },

  // CTA
  cta: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
    gap: SPACING.sm, height: 52,
    backgroundColor: COLORS.primary,
    borderRadius: BORDER_RADIUS.md,
    ...SHADOWS.button,
  },
  ctaText: { color: COLORS.white, fontSize: FONT_SIZES.md, fontWeight: '600' },
});

export default FormulaScreen;
