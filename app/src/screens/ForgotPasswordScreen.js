import React, { useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, StyleSheet,
  KeyboardAvoidingView, Platform, ScrollView, Alert, ActivityIndicator,
  StatusBar,
} from 'react-native';
import { Feather } from '@expo/vector-icons';
import * as api from '../services/api';
import { formatPhone } from '../utils/formatters';
import { COLORS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';
import SalvelleLogo from '../components/SalvelleLogo';

const ForgotPasswordScreen = ({ navigation }) => {
  const [phone, setPhone] = useState('');
  const [loading, setLoading] = useState(false);

  const handleRequest = async () => {
    const phoneDigits = phone.replace(/\D/g, '');
    if (phoneDigits.length < 10) {
      Alert.alert('WhatsApp inválido', 'Digite o número com DDD.');
      return;
    }
    setLoading(true);
    try {
      const result = await api.requestPasswordReset(phoneDigits);
      if (result.success) {
        Alert.alert(
          'Código enviado',
          'Verifique seu WhatsApp e digite o código na próxima tela.',
          [{ text: 'OK', onPress: () => navigation.navigate('ResetPassword', { phone: phoneDigits }) }]
        );
      } else {
        Alert.alert('Erro', result.message || 'Não foi possível enviar o código.');
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
          {/* Top bar */}
          <View style={styles.topBar}>
            <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backBtn}>
              <Feather name="arrow-left" size={20} color={COLORS.ink} />
            </TouchableOpacity>
            <SalvelleLogo size={28} />
            <View style={{ width: 40 }} />
          </View>

          {/* Hero */}
          <View style={styles.hero}>
            <View style={styles.iconBadge}>
              <Feather name="lock" size={20} color={COLORS.primary} />
            </View>
            <Text style={styles.heroTitle}>Esqueceu a senha?</Text>
            <Text style={styles.heroSub}>
              Digite o WhatsApp cadastrado e enviaremos um código de 6 dígitos.
            </Text>
          </View>

          {/* Form */}
          <View style={styles.card}>
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

            <TouchableOpacity
              onPress={handleRequest}
              disabled={loading}
              activeOpacity={0.85}
              style={styles.primaryBtn}
            >
              {loading ? (
                <ActivityIndicator color={COLORS.white} />
              ) : (
                <>
                  <Text style={styles.primaryBtnText}>Enviar código</Text>
                  <Feather name="send" size={18} color={COLORS.white} />
                </>
              )}
            </TouchableOpacity>

            <TouchableOpacity onPress={() => navigation.goBack()} style={styles.linkBtn}>
              <Text style={styles.linkText}>Voltar para login</Text>
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
  primaryBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: SPACING.sm,
    height: 52,
    backgroundColor: COLORS.primary,
    borderRadius: BORDER_RADIUS.md,
    marginTop: SPACING.lg,
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

export default ForgotPasswordScreen;
