import React, { useState, useEffect } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, StyleSheet,
  KeyboardAvoidingView, Platform, ScrollView, Alert, ActivityIndicator,
  StatusBar,
} from 'react-native';
import { Feather } from '@expo/vector-icons';
import { useAuth } from '../hooks/useAuth';
import { useBiometrics } from '../hooks/useBiometrics';
import { formatPhone } from '../utils/formatters';
import { COLORS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';
import SalvelleLogo from '../components/SalvelleLogo';

const LoginScreen = ({ navigation }) => {
  const { login, loginWithBiometrics } = useAuth();
  const { canUseBiometrics, authenticate, getBiometricLabel, getBiometricIcon } = useBiometrics();

  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [biometricsAvailable, setBiometricsAvailable] = useState(false);

  useEffect(() => {
    checkBiometrics();
  }, []);

  const checkBiometrics = async () => {
    const canUse = await canUseBiometrics();
    setBiometricsAvailable(canUse);
    if (canUse) handleBiometricLogin();
  };

  const handleBiometricLogin = async () => {
    const authResult = await authenticate({ promptMessage: `Entrar com ${getBiometricLabel()}` });
    if (authResult.success) {
      setLoading(true);
      const result = await loginWithBiometrics();
      setLoading(false);
      if (!result.success) Alert.alert('Sessão expirada', 'Por favor, faça login novamente.');
    }
  };

  const handleLogin = async () => {
    const phoneDigits = phone.replace(/\D/g, '');
    if (!phoneDigits || !password) {
      Alert.alert('Atenção', 'Preencha todos os campos.');
      return;
    }
    if (phoneDigits.length < 10) {
      Alert.alert('WhatsApp inválido', 'Digite o número com DDD.');
      return;
    }

    setLoading(true);
    try {
      const result = await login(phoneDigits, password);
      if (result.success) {
        if (result.requiresVerification) navigation.navigate('VerifyCode');
      } else {
        Alert.alert('Erro', result.message || 'WhatsApp ou senha incorretos.');
      }
    } catch (error) {
      Alert.alert('Erro', 'Não foi possível conectar.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <StatusBar barStyle="dark-content" backgroundColor={COLORS.background} />
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        style={{ flex: 1 }}
      >
        <ScrollView
          contentContainerStyle={styles.scrollContent}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          {/* Brand */}
          <View style={styles.brandBlock}>
            <SalvelleLogo size={36} />
          </View>

          {/* Hero */}
          <View style={styles.hero}>
            <Text style={styles.heroTitle}>Bem-vindo de volta.</Text>
            <Text style={styles.heroSub}>Entre com seu WhatsApp.</Text>
          </View>

          {/* Form card */}
          <View style={styles.card}>
            {/* WhatsApp */}
            <Text style={styles.label}>WhatsApp</Text>
            <View style={styles.inputWrap}>
              <Feather name="phone" size={18} color={COLORS.ink3} style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="(11) 90000-0000"
                placeholderTextColor={COLORS.ink4}
                value={phone}
                onChangeText={(t) => setPhone(formatPhone(t))}
                keyboardType="phone-pad"
                maxLength={15}
                editable={!loading}
              />
            </View>

            {/* Senha */}
            <Text style={[styles.label, { marginTop: SPACING.lg }]}>Senha</Text>
            <View style={styles.inputWrap}>
              <Feather name="lock" size={18} color={COLORS.ink3} style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="••••••••••"
                placeholderTextColor={COLORS.ink4}
                value={password}
                onChangeText={setPassword}
                secureTextEntry={!showPassword}
                editable={!loading}
              />
              <TouchableOpacity onPress={() => setShowPassword(!showPassword)} style={styles.eyeBtn}>
                <Feather name={showPassword ? 'eye-off' : 'eye'} size={18} color={COLORS.ink3} />
              </TouchableOpacity>
            </View>

            {/* Esqueci minha senha */}
            <TouchableOpacity
              onPress={() => navigation.navigate('ForgotPassword')}
              style={styles.forgotBtn}
              disabled={loading}
            >
              <Text style={styles.forgotText}>Esqueci minha senha</Text>
            </TouchableOpacity>

            {/* Entrar */}
            <TouchableOpacity
              onPress={handleLogin}
              disabled={loading}
              activeOpacity={0.85}
              style={styles.primaryBtn}
            >
              {loading ? (
                <ActivityIndicator color={COLORS.white} />
              ) : (
                <>
                  <Text style={styles.primaryBtnText}>Entrar</Text>
                  <Feather name="arrow-right" size={18} color={COLORS.white} />
                </>
              )}
            </TouchableOpacity>

            {/* Biometrics */}
            {biometricsAvailable && (
              <TouchableOpacity
                onPress={handleBiometricLogin}
                disabled={loading}
                style={styles.biometricBtn}
              >
                <Text style={styles.biometricIcon}>{getBiometricIcon()}</Text>
                <Text style={styles.biometricText}>Entrar com {getBiometricLabel()}</Text>
              </TouchableOpacity>
            )}
          </View>

          {/* Cadastro */}
          <View style={styles.footer}>
            <Text style={styles.footerText}>Não tem conta?</Text>
            <TouchableOpacity onPress={() => navigation.navigate('Register')}>
              <Text style={styles.footerLink}>Cadastre-se</Text>
            </TouchableOpacity>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: COLORS.background, // bone
  },
  scrollContent: {
    flexGrow: 1,
    paddingHorizontal: SPACING.lg,
    paddingTop: 56,
    paddingBottom: SPACING.xl,
  },
  brandBlock: {
    alignItems: 'flex-start',
    marginBottom: SPACING.xxxl,
  },
  hero: {
    marginBottom: SPACING.xl,
  },
  heroTitle: {
    fontSize: FONT_SIZES.xxl,
    fontWeight: '700',
    color: COLORS.ink,
    letterSpacing: -0.5,
    marginBottom: SPACING.xs,
  },
  heroSub: {
    fontSize: FONT_SIZES.md,
    color: COLORS.ink2,
  },
  card: {
    backgroundColor: COLORS.surface,
    borderRadius: BORDER_RADIUS.lg,
    padding: SPACING.xl,
    borderWidth: 1,
    borderColor: COLORS.border,
    ...SHADOWS.card,
  },
  label: {
    fontSize: FONT_SIZES.sm,
    fontWeight: '600',
    color: COLORS.ink2,
    marginBottom: SPACING.sm,
    letterSpacing: 0.2,
  },
  inputWrap: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: COLORS.backgroundAlt,
    borderRadius: BORDER_RADIUS.md,
    paddingHorizontal: SPACING.md,
    height: 52,
    borderWidth: 1,
    borderColor: 'transparent',
  },
  inputIcon: {
    marginRight: SPACING.sm,
  },
  input: {
    flex: 1,
    fontSize: FONT_SIZES.md,
    color: COLORS.ink,
  },
  eyeBtn: {
    padding: SPACING.xs,
    marginLeft: SPACING.xs,
  },
  forgotBtn: {
    alignSelf: 'flex-end',
    paddingVertical: SPACING.sm,
    marginTop: SPACING.sm,
    marginBottom: SPACING.md,
  },
  forgotText: {
    color: COLORS.primary,
    fontSize: FONT_SIZES.sm,
    fontWeight: '600',
  },
  primaryBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: SPACING.sm,
    height: 52,
    backgroundColor: COLORS.primary,
    borderRadius: BORDER_RADIUS.md,
    ...SHADOWS.button,
  },
  primaryBtnText: {
    color: COLORS.white,
    fontSize: FONT_SIZES.md,
    fontWeight: '600',
    letterSpacing: 0.2,
  },
  biometricBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: SPACING.sm,
    marginTop: SPACING.md,
    paddingVertical: SPACING.md,
    borderRadius: BORDER_RADIUS.md,
    borderWidth: 1,
    borderColor: COLORS.border,
    backgroundColor: COLORS.surface2,
  },
  biometricIcon: {
    fontSize: 20,
  },
  biometricText: {
    color: COLORS.ink2,
    fontSize: FONT_SIZES.md,
    fontWeight: '500',
  },
  footer: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    gap: SPACING.xs,
    marginTop: SPACING.xxl,
  },
  footerText: {
    color: COLORS.ink2,
    fontSize: FONT_SIZES.md,
  },
  footerLink: {
    color: COLORS.primary,
    fontSize: FONT_SIZES.md,
    fontWeight: '700',
  },
});

export default LoginScreen;
