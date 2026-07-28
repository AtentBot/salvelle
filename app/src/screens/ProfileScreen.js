import React, { useState } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  Switch,
  Alert,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { Feather } from '@expo/vector-icons';
import { useAuth } from '../hooks/useAuth';
import { useBiometrics } from '../hooks/useBiometrics';
import { formatCPF, formatPhone } from '../utils/formatters';
import { COLORS, GRADIENTS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';
import { APP_CONFIG } from '../constants/config';

const ProfileScreen = ({ navigation }) => {
  const { user, logout } = useAuth();
  const { isEnabled, isSupported, enableBiometrics, disableBiometrics, getBiometricLabel } = useBiometrics();
  const [biometricsToggle, setBiometricsToggle] = useState(isEnabled);

  const handleBiometricsToggle = async (value) => {
    if (value) {
      const result = await enableBiometrics();
      if (result.success) {
        setBiometricsToggle(true);
        Alert.alert('Sucesso', `${getBiometricLabel()} ativado com sucesso!`);
      } else {
        Alert.alert('Erro', result.message);
      }
    } else {
      await disableBiometrics();
      setBiometricsToggle(false);
    }
  };

  const handleLogout = () => {
    Alert.alert(
      'Sair',
      'Tem certeza que deseja sair da sua conta?',
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Sair',
          style: 'destructive',
          onPress: logout,
        },
      ]
    );
  };

  const getInitials = () => {
    const name = user?.name || user?.fullName || 'U';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  };

  const MenuItem = ({ icon, title, subtitle, onPress, rightElement, showArrow = true }) => (
    <TouchableOpacity
      style={styles.menuItem}
      onPress={onPress}
      disabled={!onPress}
      activeOpacity={onPress ? 0.7 : 1}
    >
      <View style={styles.menuIcon}>
        <Feather name={icon} size={20} color={COLORS.primary} />
      </View>
      <View style={styles.menuContent}>
        <Text style={styles.menuTitle}>{title}</Text>
        {subtitle && <Text style={styles.menuSubtitle}>{subtitle}</Text>}
      </View>
      {rightElement || (showArrow && onPress && (
        <Feather name="chevron-right" size={20} color={COLORS.textMuted} />
      ))}
    </TouchableOpacity>
  );

  return (
    <View style={styles.container}>
      <ScrollView
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
      >
        {/* Dark Teal Header Banner */}
        <LinearGradient
          colors={GRADIENTS.darkTeal}
          style={styles.headerBanner}
          start={{ x: 0, y: 0 }}
          end={{ x: 1, y: 1 }}
        >
          <Text style={styles.headerTitle}>Perfil</Text>
        </LinearGradient>

        {/* Avatar overlapping header */}
        <View style={styles.avatarWrapper}>
          <View style={styles.avatarRing}>
            <LinearGradient
              colors={GRADIENTS.primary}
              style={styles.avatar}
              start={{ x: 0, y: 0 }}
              end={{ x: 1, y: 1 }}
            >
              <Text style={styles.avatarText}>{getInitials()}</Text>
            </LinearGradient>
          </View>
        </View>

        {/* Profile Card */}
        <View style={styles.profileCard}>
          <Text style={styles.userName}>{user?.name || user?.fullName || 'Cliente'}</Text>
          <Text style={styles.userPhone}>{formatPhone(user?.phone || '')}</Text>

          <TouchableOpacity
            style={styles.editButton}
            onPress={() => navigation.navigate('EditProfile')}
          >
            <Feather name="edit-2" size={14} color={COLORS.white} />
            <Text style={styles.editButtonText}>Editar perfil</Text>
          </TouchableOpacity>
        </View>

        {/* Conta */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Conta</Text>
          <View style={styles.menuCard}>
            <MenuItem
              icon="user"
              title="Dados pessoais"
              subtitle={formatCPF(user?.cpf || '')}
              onPress={() => navigation.navigate('EditProfile')}
            />
            <MenuItem
              icon="map-pin"
              title="Endereços"
              subtitle="Gerenciar endereços de entrega"
              onPress={() => navigation.navigate('Addresses')}
            />
            <MenuItem
              icon="lock"
              title="Alterar senha"
              onPress={() => navigation.navigate('ChangePassword')}
            />
          </View>
        </View>

        {/* Segurança */}
        {isSupported && (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Segurança</Text>
            <View style={styles.menuCard}>
              <MenuItem
                icon="smartphone"
                title={getBiometricLabel()}
                subtitle="Login rápido e seguro"
                showArrow={false}
                rightElement={
                  <Switch
                    value={biometricsToggle}
                    onValueChange={handleBiometricsToggle}
                    trackColor={{ false: COLORS.border, true: COLORS.primaryMuted }}
                    thumbColor={biometricsToggle ? COLORS.primary : COLORS.textMuted}
                  />
                }
              />
            </View>
          </View>
        )}

        {/* Histórico */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Histórico</Text>
          <View style={styles.menuCard}>
            <MenuItem
              icon="file-text"
              title="Minhas receitas"
              subtitle="Receitas enviadas"
              onPress={() => navigation.navigate('Prescriptions')}
            />
            <MenuItem
              icon="package"
              title="Meus pedidos"
              subtitle="Histórico de compras"
              onPress={() => navigation.navigate('Orders')}
            />
          </View>
        </View>

        {/* Ajuda */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Ajuda</Text>
          <View style={styles.menuCard}>
            <MenuItem
              icon="help-circle"
              title="Suporte"
              subtitle="Fale conosco"
              onPress={() => {}}
            />
            <MenuItem
              icon="file"
              title="Termos de uso"
              onPress={() => {}}
            />
            <MenuItem
              icon="shield"
              title="Política de privacidade"
              onPress={() => {}}
            />
          </View>
        </View>

        {/* Logout */}
        <TouchableOpacity
          style={styles.logoutButton}
          onPress={handleLogout}
        >
          <Feather name="log-out" size={20} color={COLORS.error} />
          <Text style={styles.logoutText}>Sair da conta</Text>
        </TouchableOpacity>

        {/* Version */}
        <Text style={styles.versionText}>
          Salvelle v{APP_CONFIG.version}
        </Text>

        <View style={{ height: 100 }} />
      </ScrollView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: COLORS.background,
  },
  scrollView: {
    flex: 1,
  },
  scrollContent: {
    paddingBottom: SPACING.lg,
  },
  headerBanner: {
    paddingTop: SPACING.xxxl + 20,
    paddingBottom: 50,
    paddingHorizontal: SPACING.lg,
    borderBottomLeftRadius: BORDER_RADIUS.xxl,
    borderBottomRightRadius: BORDER_RADIUS.xxl,
  },
  headerTitle: {
    fontSize: FONT_SIZES.xxl,
    fontWeight: '700',
    color: COLORS.white,
  },
  avatarWrapper: {
    alignItems: 'center',
    marginTop: -45,
    marginBottom: SPACING.sm,
    zIndex: 10,
  },
  avatarRing: {
    width: 96,
    height: 96,
    borderRadius: 48,
    backgroundColor: COLORS.white,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 3,
    borderColor: COLORS.white,
    ...SHADOWS.medium,
  },
  avatar: {
    width: 90,
    height: 90,
    borderRadius: 45,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarText: {
    fontSize: 32,
    fontWeight: '700',
    color: COLORS.white,
  },
  profileCard: {
    backgroundColor: COLORS.white,
    borderRadius: BORDER_RADIUS.xxl,
    padding: SPACING.xl,
    alignItems: 'center',
    marginHorizontal: SPACING.lg,
    marginBottom: SPACING.xl,
    ...SHADOWS.medium,
  },
  userName: {
    fontSize: FONT_SIZES.xl,
    fontWeight: '700',
    color: COLORS.text,
  },
  userPhone: {
    fontSize: FONT_SIZES.md,
    color: COLORS.textSecondary,
    marginTop: SPACING.xs,
  },
  editButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: SPACING.xs,
    marginTop: SPACING.md,
    paddingVertical: SPACING.sm,
    paddingHorizontal: SPACING.lg,
    backgroundColor: COLORS.primary,
    borderRadius: BORDER_RADIUS.full,
  },
  editButtonText: {
    fontSize: FONT_SIZES.sm,
    fontWeight: '600',
    color: COLORS.white,
  },
  section: {
    marginBottom: SPACING.lg,
    paddingHorizontal: SPACING.lg,
  },
  sectionTitle: {
    fontSize: FONT_SIZES.sm,
    fontWeight: '600',
    color: COLORS.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
    marginBottom: SPACING.sm,
    marginLeft: SPACING.xs,
  },
  menuCard: {
    backgroundColor: COLORS.white,
    borderRadius: BORDER_RADIUS.xxl,
    overflow: 'hidden',
    ...SHADOWS.small,
  },
  menuItem: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: SPACING.md,
    borderBottomWidth: 1,
    borderBottomColor: COLORS.borderLight,
  },
  menuIcon: {
    width: 40,
    height: 40,
    borderRadius: BORDER_RADIUS.md,
    backgroundColor: COLORS.primaryMuted,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: SPACING.md,
  },
  menuContent: {
    flex: 1,
  },
  menuTitle: {
    fontSize: FONT_SIZES.md,
    fontWeight: '500',
    color: COLORS.text,
  },
  menuSubtitle: {
    fontSize: FONT_SIZES.sm,
    color: COLORS.textMuted,
    marginTop: 2,
  },
  logoutButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: SPACING.sm,
    padding: SPACING.md,
    marginTop: SPACING.lg,
    marginHorizontal: SPACING.lg,
    borderWidth: 1.5,
    borderColor: COLORS.error,
    borderRadius: BORDER_RADIUS.xxl,
    backgroundColor: COLORS.white,
  },
  logoutText: {
    fontSize: FONT_SIZES.md,
    fontWeight: '600',
    color: COLORS.error,
  },
  versionText: {
    textAlign: 'center',
    fontSize: FONT_SIZES.xs,
    color: COLORS.textMuted,
    marginTop: SPACING.lg,
  },
});

export default ProfileScreen;
