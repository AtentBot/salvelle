import React, { useState, useEffect, useMemo } from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet, ScrollView, ActivityIndicator,
  StatusBar, RefreshControl, TextInput, FlatList,
} from 'react-native';
import { Feather } from '@expo/vector-icons';
import * as api from '../services/api';
import { COLORS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';
import SalvelleLogo from '../components/SalvelleLogo';

const ALL = '__ALL__';

const ICON_BY_CATEGORY = {
  Vitaminas: 'sun',
  Minerais: 'hexagon',
  Aminoácidos: 'link',
  Antioxidantes: 'shield',
  Fitoterápicos: 'feather',
  'Colágeno e Pele': 'heart',
  Probióticos: 'circle',
  'Ácidos Graxos': 'droplet',
  Performance: 'zap',
  Digestão: 'activity',
  Nootrópicos: 'cpu',
  Prebióticos: 'git-branch',
  'Sono e Relaxamento': 'moon',
};
const iconFor = (cat) => ICON_BY_CATEGORY[cat] || 'box';

const CatalogScreen = ({ navigation }) => {
  const [groups, setGroups] = useState([]); // [{category, items: [...]}]
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [errorMsg, setErrorMsg] = useState(null);
  const [activeCategory, setActiveCategory] = useState(ALL);
  const [search, setSearch] = useState('');

  useEffect(() => { load(); }, []);

  const load = async () => {
    try {
      setErrorMsg(null);
      const result = await api.getCatalogIngredients({ limit: 500 });
      if (result?.success && Array.isArray(result.categories)) {
        setGroups(result.categories);
      } else {
        setGroups([]);
      }
    } catch (err) {
      setErrorMsg('Não foi possível carregar a prateleira.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const onRefresh = () => {
    setRefreshing(true);
    load();
  };

  // Filtro de busca client-side
  const filteredGroups = useMemo(() => {
    const q = search.trim().toLowerCase();
    return groups
      .filter((g) => activeCategory === ALL || g.category === activeCategory)
      .map((g) => ({
        ...g,
        items: g.items.filter((it) => !q || it.name.toLowerCase().includes(q)),
      }))
      .filter((g) => g.items.length > 0);
  }, [groups, search, activeCategory]);

  const totalShown = filteredGroups.reduce((acc, g) => acc + g.items.length, 0);

  const handlePickIngredient = (ingredient) => {
    navigation.navigate('Formula', { initialIngredient: ingredient });
  };

  return (
    <View style={styles.container}>
      <StatusBar barStyle="dark-content" backgroundColor={COLORS.background} />

      <View style={styles.header}>
        {navigation.canGoBack() ? (
          <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backBtn}>
            <Feather name="arrow-left" size={20} color={COLORS.ink} />
          </TouchableOpacity>
        ) : (
          <View style={{ width: 40 }} />
        )}
        <SalvelleLogo size={24} />
        <View style={{ width: 40 }} />
      </View>

      <View style={styles.titleBlock}>
        <Text style={styles.title}>Prateleira</Text>
        <Text style={styles.subtitle}>
          {loading ? 'Carregando...' : `${totalShown} ingredientes em ${filteredGroups.length} categorias`}
        </Text>
      </View>

      {/* Busca */}
      <View style={styles.searchWrap}>
        <Feather name="search" size={18} color={COLORS.ink3} style={{ marginRight: SPACING.sm }} />
        <TextInput
          style={styles.searchInput}
          placeholder="Buscar ingrediente (ex: vitamina, magnésio, ômega...)"
          placeholderTextColor={COLORS.ink4}
          value={search}
          onChangeText={setSearch}
        />
        {search ? (
          <TouchableOpacity onPress={() => setSearch('')}>
            <Feather name="x" size={18} color={COLORS.ink3} />
          </TouchableOpacity>
        ) : null}
      </View>

      {/* Tabs de categorias */}
      {!loading && groups.length > 0 && (
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.tabsRow}
          style={styles.tabsScroll}
        >
          <TouchableOpacity
            style={[styles.tab, activeCategory === ALL && styles.tabActive]}
            onPress={() => setActiveCategory(ALL)}
          >
            <Text style={[styles.tabText, activeCategory === ALL && styles.tabTextActive]}>Tudo</Text>
          </TouchableOpacity>
          {groups.map((g) => (
            <TouchableOpacity
              key={g.category}
              style={[styles.tab, activeCategory === g.category && styles.tabActive]}
              onPress={() => setActiveCategory(g.category)}
            >
              <Feather
                name={iconFor(g.category)}
                size={14}
                color={activeCategory === g.category ? COLORS.white : COLORS.primary}
              />
              <Text
                style={[styles.tabText, activeCategory === g.category && styles.tabTextActive]}
                numberOfLines={1}
              >
                {g.category} <Text style={styles.tabCount}>·{g.count}</Text>
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      )}

      {/* Conteúdo */}
      {loading ? (
        <View style={styles.centered}>
          <ActivityIndicator size="large" color={COLORS.primary} />
        </View>
      ) : errorMsg ? (
        <View style={styles.centered}>
          <Feather name="alert-circle" size={32} color={COLORS.error} />
          <Text style={styles.errorText}>{errorMsg}</Text>
          <TouchableOpacity style={styles.retryBtn} onPress={load}>
            <Text style={styles.retryText}>Tentar novamente</Text>
          </TouchableOpacity>
        </View>
      ) : filteredGroups.length === 0 ? (
        <View style={styles.centered}>
          <Feather name="package" size={32} color={COLORS.ink3} />
          <Text style={styles.emptyText}>Nada encontrado.</Text>
        </View>
      ) : (
        <ScrollView
          contentContainerStyle={styles.scrollContent}
          refreshControl={
            <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={COLORS.primary} />
          }
        >
          {filteredGroups.map((g) => (
            <View key={g.category} style={styles.categoryBlock}>
              <View style={styles.categoryHeader}>
                <View style={styles.categoryIconWrap}>
                  <Feather name={iconFor(g.category)} size={16} color={COLORS.primary} />
                </View>
                <Text style={styles.categoryTitle}>{g.category}</Text>
                <Text style={styles.categoryCount}>{g.items.length}</Text>
              </View>

              <View style={styles.itemsGrid}>
                {g.items.map((it) => (
                  <TouchableOpacity
                    key={it.id}
                    style={styles.itemCard}
                    onPress={() => handlePickIngredient(it)}
                    activeOpacity={0.7}
                  >
                    <View style={{ flex: 1 }}>
                      <Text style={styles.itemName} numberOfLines={2}>{it.name}</Text>
                      <View style={styles.itemMetaRow}>
                        {it.controlType && it.controlType !== 'COMUM' ? (
                          <View style={[styles.badge, styles.badgeWarn]}>
                            <Text style={styles.badgeText}>controlado</Text>
                          </View>
                        ) : null}
                        {!it.inStock ? (
                          <View style={[styles.badge, styles.badgeMuted]}>
                            <Text style={styles.badgeText}>sob encomenda</Text>
                          </View>
                        ) : null}
                        <Text style={styles.itemUnit}>{it.defaultUnit}</Text>
                      </View>
                    </View>
                    <View style={styles.addBtn}>
                      <Feather name="plus" size={16} color={COLORS.white} />
                    </View>
                  </TouchableOpacity>
                ))}
              </View>
            </View>
          ))}
          <View style={{ height: 40 }} />
        </ScrollView>
      )}
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
    backgroundColor: COLORS.surface, alignItems: 'center', justifyContent: 'center',
    borderWidth: 1, borderColor: COLORS.border,
  },
  titleBlock: { paddingHorizontal: SPACING.lg, marginBottom: SPACING.md },
  title: { fontSize: FONT_SIZES.xxl, fontWeight: '700', color: COLORS.ink, letterSpacing: -0.5 },
  subtitle: { fontSize: FONT_SIZES.sm, color: COLORS.ink2, marginTop: 4 },

  searchWrap: {
    marginHorizontal: SPACING.lg, marginBottom: SPACING.md,
    flexDirection: 'row', alignItems: 'center',
    backgroundColor: COLORS.surface,
    borderRadius: BORDER_RADIUS.md, paddingHorizontal: SPACING.md,
    height: 48, borderWidth: 1, borderColor: COLORS.border,
    ...SHADOWS.card,
  },
  searchInput: { flex: 1, fontSize: FONT_SIZES.md, color: COLORS.ink },

  tabsScroll: { flexGrow: 0, height: 44, marginBottom: SPACING.md },
  tabsRow: { paddingHorizontal: SPACING.lg, gap: SPACING.sm, alignItems: 'center' },
  tab: {
    flexDirection: 'row', alignItems: 'center', gap: 6,
    paddingHorizontal: SPACING.md, paddingVertical: SPACING.xs + 2,
    borderRadius: BORDER_RADIUS.full,
    backgroundColor: COLORS.surface,
    borderWidth: 1, borderColor: COLORS.border,
    marginRight: SPACING.xs,
  },
  tabActive: { backgroundColor: COLORS.primary, borderColor: COLORS.primary },
  tabText: { fontSize: FONT_SIZES.sm, color: COLORS.ink2, fontWeight: '500' },
  tabTextActive: { color: COLORS.white, fontWeight: '700' },
  tabCount: { fontSize: FONT_SIZES.xs, opacity: 0.7 },

  scrollContent: { paddingHorizontal: SPACING.lg, paddingBottom: SPACING.xl },
  centered: {
    flex: 1, alignItems: 'center', justifyContent: 'center',
    paddingHorizontal: SPACING.xl,
  },
  errorText: { color: COLORS.error, marginTop: SPACING.md, textAlign: 'center' },
  emptyText: { color: COLORS.ink3, marginTop: SPACING.md },
  retryBtn: {
    marginTop: SPACING.md, paddingVertical: SPACING.sm, paddingHorizontal: SPACING.lg,
    borderRadius: BORDER_RADIUS.md, borderWidth: 1, borderColor: COLORS.primary,
  },
  retryText: { color: COLORS.primary, fontWeight: '600' },

  categoryBlock: { marginBottom: SPACING.lg },
  categoryHeader: {
    flexDirection: 'row', alignItems: 'center', gap: SPACING.sm,
    marginBottom: SPACING.sm,
  },
  categoryIconWrap: {
    width: 28, height: 28, borderRadius: BORDER_RADIUS.full,
    backgroundColor: COLORS.primarySoft,
    alignItems: 'center', justifyContent: 'center',
  },
  categoryTitle: { fontSize: FONT_SIZES.md, fontWeight: '700', color: COLORS.ink, flex: 1 },
  categoryCount: { fontSize: FONT_SIZES.xs, color: COLORS.ink3, fontWeight: '600' },

  itemsGrid: { gap: SPACING.sm },
  itemCard: {
    flexDirection: 'row', alignItems: 'center',
    backgroundColor: COLORS.surface,
    borderRadius: BORDER_RADIUS.md,
    padding: SPACING.md,
    borderWidth: 1, borderColor: COLORS.border,
    ...SHADOWS.card,
  },
  itemName: { fontSize: FONT_SIZES.md, color: COLORS.ink, fontWeight: '600' },
  itemMetaRow: { flexDirection: 'row', alignItems: 'center', gap: 6, marginTop: 4 },
  itemUnit: { fontSize: FONT_SIZES.xs, color: COLORS.ink3, fontWeight: '500' },
  badge: {
    paddingHorizontal: SPACING.sm, paddingVertical: 2,
    borderRadius: BORDER_RADIUS.full,
  },
  badgeWarn: { backgroundColor: COLORS.warningLight },
  badgeMuted: { backgroundColor: COLORS.backgroundAlt },
  badgeText: { fontSize: 10, color: COLORS.ink2, fontWeight: '600', textTransform: 'lowercase' },
  addBtn: {
    width: 32, height: 32, borderRadius: BORDER_RADIUS.full,
    backgroundColor: COLORS.primary,
    alignItems: 'center', justifyContent: 'center',
    marginLeft: SPACING.sm,
    ...SHADOWS.button,
  },
});

export default CatalogScreen;
