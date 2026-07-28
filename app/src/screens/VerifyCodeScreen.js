import React, { useState, useRef, useEffect } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, StyleSheet,
  KeyboardAvoidingView, Platform, Alert, ActivityIndicator,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { Feather } from '@expo/vector-icons';
import { useAuth } from '../hooks/useAuth';
import { COLORS, GRADIENTS, SPACING, BORDER_RADIUS, FONT_SIZES } from '../constants/theme';

const VerifyCodeScreen = ({ navigation }) => {
  const { verifyCode, resendVerificationCode, pendingVerification } = useAuth();

  const [code, setCode] = useState(['', '', '', '', '', '']);
  const [loading, setLoading] = useState(false);
  const [resendTimer, setResendTimer] = useState(60);
  const inputRefs = useRef([]);

  useEffect(() => {
    inputRefs.current[0]?.focus();

    const timer = setInterval(() => {
      setResendTimer((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);

    return () => clearInterval(timer);
  }, []);

  const handleCodeChange = (index, value) => {
    if (value.length > 1) value = value.slice(-1);

    const newCode = [...code];
    newCode[index] = value;
    setCode(newCode);

    if (value && index < 5) {
      inputRefs.current[index + 1]?.focus();
    }

    if (index === 5 && value) {
      const fullCode = newCode.join('');
      if (fullCode.length === 6) handleVerify(fullCode);
    }
  };

  const handleKeyPress = (index, key) => {
    if (key === 'Backspace' && !code[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
  };

  const handleVerify = async (fullCode) => {
    if (!fullCode) fullCode = code.join('');
    if (fullCode.length !== 6) {
      Alert.alert('Código incompleto', 'Digite os 6 dígitos.');
      return;
    }

    setLoading(true);
    try {
      const result = await verifyCode(fullCode);
      if (!result.success) {
        Alert.alert('Código inválido', result.message || 'Verifique o código.');
        setCode(['', '', '', '', '', '']);
        inputRefs.current[0]?.focus();
      }
    } catch (error) {
      Alert.alert('Erro', 'Não foi possível verificar.');
    } finally {
      setLoading(false);
    }
  };

  const handleResend = async () => {
    if (resendTimer > 0) return;

    try {
      const result = await resendVerificationCode();
      if (result.success) {
        Alert.alert('Código enviado', 'Um novo código foi enviado para seu WhatsApp.');
        setResendTimer(60);
      } else {
        Alert.alert('Erro', result.message || 'Não foi possível reenviar.');
      }
    } catch (error) {
      Alert.alert('Erro', 'Não foi possível reenviar.');
    }
  };

  return (
    <LinearGradient colors={GRADIENTS.background} style={styles.container}>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : 'height'} style={{ flex: 1 }}>
        <View style={styles.content}>
          {/* Back */}
          <TouchableOpacity style={styles.backButton} onPress={() => navigation.goBack()}>
            <View style={styles.backButtonCircle}>
              <Feather name="arrow-left" size={20} color={COLORS.text} />
            </View>
          </TouchableOpacity>

          {/* Icon */}
          <View style={styles.iconWrapper}>
            <LinearGradient
              colors={GRADIENTS.primary}
              style={styles.iconContainer}
              start={{ x: 0, y: 0 }}
              end={{ x: 1, y: 1 }}
            >
              <Feather name="smartphone" size={32} color={COLORS.white} />
            </LinearGradient>
          </View>

          <Text style={styles.title}>Verificação WhatsApp</Text>
          <Text style={styles.subtitle}>
            Digite o código de 6 dígitos enviado para{'\n'}
            <Text style={styles.phoneText}>{pendingVerification?.phone || 'seu WhatsApp'}</Text>
          </Text>

          {/* Code inputs */}
          <View style={styles.codeContainer}>
            {code.map((digit, index) => (
              <TextInput
                key={index}
                ref={(ref) => (inputRefs.current[index] = ref)}
                style={[styles.codeInput, digit && styles.codeInputFilled]}
                value={digit}
                onChangeText={(value) => handleCodeChange(index, value)}
                onKeyPress={({ nativeEvent }) => handleKeyPress(index, nativeEvent.key)}
                keyboardType="number-pad"
                maxLength={1}
                selectTextOnFocus
                editable={!loading}
              />
            ))}
          </View>

          {/* Verify Button */}
          <TouchableOpacity onPress={() => handleVerify()} disabled={loading} activeOpacity={0.8}>
            <LinearGradient
              colors={GRADIENTS.primary}
              style={styles.verifyButton}
              start={{ x: 0, y: 0 }}
              end={{ x: 1, y: 0 }}
            >
              {loading ? (
                <ActivityIndicator color={COLORS.white} />
              ) : (
                <Text style={styles.verifyButtonText}>Verificar</Text>
              )}
            </LinearGradient>
          </TouchableOpacity>

          {/* Resend */}
          <TouchableOpacity
            style={styles.resendButton}
            onPress={handleResend}
            disabled={resendTimer > 0}
          >
            <Text style={[styles.resendText, resendTimer > 0 && styles.resendTextDisabled]}>
              {resendTimer > 0 ? `Reenviar código em ${resendTimer}s` : 'Reenviar código'}
            </Text>
          </TouchableOpacity>
        </View>
      </KeyboardAvoidingView>
    </LinearGradient>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  content: {
    flex: 1,
    padding: SPACING.xl,
    paddingTop: SPACING.xxxl + 20,
  },
  backButton: {
    marginBottom: SPACING.xl,
  },
  backButtonCircle: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: COLORS.white,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: COLORS.border,
  },
  iconWrapper: {
    alignItems: 'center',
    marginBottom: SPACING.lg,
  },
  iconContainer: {
    width: 80,
    height: 80,
    borderRadius: 40,
    alignItems: 'center',
    justifyContent: 'center',
  },
  title: {
    fontSize: FONT_SIZES.xxl,
    fontWeight: '800',
    color: COLORS.text,
    textAlign: 'center',
  },
  subtitle: {
    fontSize: FONT_SIZES.md,
    color: COLORS.textSecondary,
    textAlign: 'center',
    marginTop: SPACING.sm,
    lineHeight: 22,
  },
  phoneText: {
    fontWeight: '600',
    color: COLORS.text,
  },
  codeContainer: {
    flexDirection: 'row',
    justifyContent: 'center',
    gap: SPACING.sm,
    marginVertical: SPACING.xxxl,
  },
  codeInput: {
    width: 52,
    height: 60,
    borderRadius: BORDER_RADIUS.md,
    backgroundColor: COLORS.white,
    borderBottomWidth: 3,
    borderBottomColor: COLORS.border,
    textAlign: 'center',
    fontSize: FONT_SIZES.xl,
    fontWeight: '700',
    color: COLORS.text,
  },
  codeInputFilled: {
    borderBottomColor: COLORS.primary,
  },
  verifyButton: {
    alignItems: 'center',
    justifyContent: 'center',
    padding: SPACING.lg,
    borderRadius: BORDER_RADIUS.xxl,
    shadowColor: COLORS.primary,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 6,
  },
  verifyButtonText: {
    fontSize: FONT_SIZES.lg,
    fontWeight: '700',
    color: COLORS.white,
  },
  resendButton: {
    alignItems: 'center',
    marginTop: SPACING.xl,
  },
  resendText: {
    fontSize: FONT_SIZES.md,
    color: COLORS.primary,
    fontWeight: '500',
  },
  resendTextDisabled: {
    color: COLORS.textMuted,
  },
});

export default VerifyCodeScreen;
