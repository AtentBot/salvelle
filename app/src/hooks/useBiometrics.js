import { useState, useEffect } from 'react';
import * as LocalAuthentication from 'expo-local-authentication';
import * as storage from '../services/storage';

export const useBiometrics = () => {
  const [isSupported, setIsSupported] = useState(false);
  const [isEnabled, setIsEnabled] = useState(false);
  const [biometricType, setBiometricType] = useState(null);

  useEffect(() => {
    checkSupport();
    loadPreference();
  }, []);

  const checkSupport = async () => {
    const compatible = await LocalAuthentication.hasHardwareAsync();
    const enrolled = await LocalAuthentication.isEnrolledAsync();
    setIsSupported(compatible && enrolled);

    if (compatible) {
      const types = await LocalAuthentication.supportedAuthenticationTypesAsync();
      if (types.includes(LocalAuthentication.AuthenticationType.FACIAL_RECOGNITION)) {
        setBiometricType('face');
      } else if (types.includes(LocalAuthentication.AuthenticationType.FINGERPRINT)) {
        setBiometricType('fingerprint');
      }
    }
  };

  const loadPreference = async () => {
    const enabled = await storage.isBiometricsEnabled();
    setIsEnabled(enabled);
  };

  const canUseBiometrics = async () => {
    const compatible = await LocalAuthentication.hasHardwareAsync();
    const enrolled = await LocalAuthentication.isEnrolledAsync();
    const enabled = await storage.isBiometricsEnabled();
    return compatible && enrolled && enabled;
  };

  const authenticate = async (options = {}) => {
    try {
      const result = await LocalAuthentication.authenticateAsync({
        promptMessage: options.promptMessage || 'Autenticar',
        fallbackLabel: options.fallbackLabel || 'Usar senha',
        disableDeviceFallback: false,
        cancelLabel: 'Cancelar',
      });
      return result;
    } catch (error) {
      return { success: false, error: error.message };
    }
  };

  const enableBiometrics = async () => {
    await storage.setBiometricsEnabled(true);
    setIsEnabled(true);
  };

  const disableBiometrics = async () => {
    await storage.setBiometricsEnabled(false);
    setIsEnabled(false);
  };

  const getBiometricLabel = () => {
    if (biometricType === 'face') return 'Face ID';
    if (biometricType === 'fingerprint') return 'Digital';
    return 'Biometria';
  };

  const getBiometricIcon = () => {
    if (biometricType === 'face') return '😊';
    if (biometricType === 'fingerprint') return '👆';
    return '🔐';
  };

  return {
    isSupported,
    isEnabled,
    biometricType,
    canUseBiometrics,
    authenticate,
    enableBiometrics,
    disableBiometrics,
    getBiometricLabel,
    getBiometricIcon,
  };
};

export default useBiometrics;
