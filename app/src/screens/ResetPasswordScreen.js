import React, { useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, StyleSheet,
  KeyboardAvoidingView, Platform, ScrollView, Alert, ActivityIndicator,
  StatusBar,
} from 'react-native';
import { Feather } from '@expo/vector-icons';
import * as api from '../services/api';
import { COLORS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';
import SalvelleLogo from '../components/SalvelleLogo';

const ResetPasswordScreen = ({ route, navigation }) => {
  const phone = route?.params?.phone || '';
  const [code, setCode] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);

  const handleReset = async () => {
    if (!code || !newPassword || !confirmPassword) {
      Alert.alert('Atenção', 'Preencha todos os campos.');
      return;
    }
    if (code.length !== 6) {
      Alert.alert('Código inválido', 'O código deve ter 6 dígitos.');
      return;
    }
    if (newPassword.length < 8) {
      Alert.alert('Senha fraca', 'A senha deve ter no mínimo 8 caracteres.');
      return;
    }
    if (newPassword !== confirmPassword) {
      Alert.alert('Senhas diferentes', 'A confirmação não confere.');
      return;
    }

    setLoading(true);
    try {
      const result = await api.resetPassword(phone, code, newPassword, confirmPassword);
      if (result.success) {
        Alert.alert(
          'Senha redefinida',
          'Você já pode entrar com sua nova senha.',
          [{ text: 'OK', onPress: () => navigation.navigate('Login') }]
        );
      } else {
        Alert.alert('Erro', result.message || 'Não foi possível redefinir.');
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
          <View style={styles.topBar}>
            <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backBtn}>
              <Feather name="arrow-left" size={20} color={COLORS.ink} />
            </TouchableOpacity>
            <SalvelleLogo size={28} />
            <View style={{ width: 40 }} />
          </View>

          <View style={styles.hero}>
            <View style={styles.iconBadge}>
              <Feather name="shield" size={20} color={COLORS.primary} />
            </View>
            <Text style={styles.heroTitle}>Nova senha</Text>
            <Text style={styles.heroSub}>
              Enviamos um código de 6 dígitos para o seu WhatsApp. Digite-o abaixo junto com sua nova senha.
            </Text>
          </View>

          <View style={styles.card}>
            <Text style={styles.label}>Código</Text>
            <View style={styles.inputWrap}>
              <Feather name="hash" size={18} color={COLORS.ink3} style={styles.inputIcon} />
              <TextInput
                style={[styles.input, styles.inputMono]}
                placeholder="000000"
                placeholderTextColor={COLORS.ink4}
                value={code}
                onChangeText={(t) => setCode(t.replace(/\D/g, '').slice(0, 6))}
                keyboardType="number-pad"
                maxLength={6}
                editable={!loading}
              />
            </View>

            <Text style={[styles.label, { marginTop: SPACING.lg }]}>Nova senha</Text>
            <View style={styles.inputWrap}>
              <Feather name="lock" size={18} color={COLORS.ink3} style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="Mínimo 8 caracteres"
                placeholderTextColor={COLORS.ink4}
                value={newPassword}
                onChangeText={setNewPassword}
                secureTextEntry={!showPassword}
                editable={!loading}
              />
              <TouchableOpacity onPress={() => setShowPassword(!showPassword)} style={styles.eyeBtn}>
                <Feather name={showPassword ? 'eye-off' : 'eye'} size={18} color={COLORS.ink3} />
              </TouchableOpacity>
            </View>

            <Text style={[styles.label, { marginTop: SPACING.lg }]}>Confirmar senha</Text>
            <View style={styles.inputWrap}>
              <Feather name="lock" size={18} color={COLORS.ink3} style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="Repita a senha"
                placeholderTextColor={COLORS.ink4}
                value={confirmPassword}
                onChangeText={setConfirmPassword}
                secureTextEntry={!showPassword}
                editable={!loading}
              />
            </View>

            <TouchableOpacity
              onPress={handleReset}
              disabled={loading}
              activeOpacity={0.85}
              style={styles.primaryBtn}
            >
              {loading ? (
                <ActivityIndicator color={COLORS.white} />
              ) : (
                <>
                  <Text style={styles.primaryBtnText}>Redefinir senha</Text>
                  <Feather name="check" size={18} color={COLORS.white} />
                </>
              )}
            </TouchableOpacity>

            <TouchableOpacity
              onPress={() => navigation.navigate('ForgotPassword')}
              style={styles.linkBtn}
            >
              <Text style={styles.linkText}>Não recebi o código — reenviar</Text>
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
    backgroundColor: COLORS.background,
  },
  scrollContent: {
    flexGrow: 1,
    paddingHorizontal: SPACING.lg,
    paddingTop: 56,
    paddingBottom: SPACING.xl,
  },
  topBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: SPACING.xxxl,
  },
  backBtn: {
    width: 40,
    height: 40,
    borderRadius: BORDER_RADIUS.full,
    backgroundColor: COLORS.surface,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: COLORS.border,
  },
  hero: {
    marginBottom: SPACING.xl,
  },
  iconBadge: {
    width: 48,
    height: 48,
    borderRadius: BORDER_RADIUS.md,
    backgroundColor: COLORS.primarySoft,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: SPACING.md,
  },
  heroTitle: {
    fontSize: FONT_SIZES.xxl,
    fontWeight: '700',
    color: COLORS.ink,
    letterSpacing: -0.5,
    marginBottom: SPACING.sm,
  },
  heroSub: {
    fontSize: FONT_SIZES.md,
    color: COLORS.ink2,
    lineHeight: 22,
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
  },
  inputIcon: {
    marginRight: SPACING.sm,
  },
  input: {
    flex: 1,
    fontSize: FONT_SIZES.md,
    color: COLORS.ink,
  },
  inputMono: {
    letterSpacing: 4,
    fontWeight: '600',
  },
  eyeBtn: {
    padding: SPACING.xs,
    marginLeft: SPACING.xs,
  },
  primaryBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: SPACING.sm,
    height: 52,
    backgroundColor: COLORS.primary,
    borderRadius: BORDER_RADIUS.md,
    marginTop: SPACING.xl,
    ...SHADOWS.button,
  },
  primaryBtnText: {
    color: COLORS.white,
    fontSize: FONT_SIZES.md,
    fontWeight: '600',
    letterSpacing: 0.2,
  },
  linkBtn: {
    alignItems: 'center',
    paddingVertical: SPACING.md,
    marginTop: SPACING.sm,
  },
  linkText: {
    color: COLORS.ink2,
    fontSize: FONT_SIZES.sm,
    fontWeight: '500',
  },
});

export default ResetPasswordScreen;
