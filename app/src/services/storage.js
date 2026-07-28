import * as SecureStore from 'expo-secure-store';

const KEYS = {
  TOKEN: 'auth_token',
  USER: 'user_data',
  BIOMETRICS: 'biometrics_enabled',
};

// Token
export const saveToken = async (token) => {
  await SecureStore.setItemAsync(KEYS.TOKEN, token);
};

export const getToken = async () => {
  return await SecureStore.getItemAsync(KEYS.TOKEN);
};

export const deleteToken = async () => {
  await SecureStore.deleteItemAsync(KEYS.TOKEN);
};

// User data
export const saveUserData = async (user) => {
  await SecureStore.setItemAsync(KEYS.USER, JSON.stringify(user));
};

export const getUserData = async () => {
  const data = await SecureStore.getItemAsync(KEYS.USER);
  return data ? JSON.parse(data) : null;
};

// Biometrics preference
export const setBiometricsEnabled = async (enabled) => {
  await SecureStore.setItemAsync(KEYS.BIOMETRICS, String(enabled));
};

export const isBiometricsEnabled = async () => {
  const value = await SecureStore.getItemAsync(KEYS.BIOMETRICS);
  return value === 'true';
};

// Clear all
export const clearAllData = async () => {
  await SecureStore.deleteItemAsync(KEYS.TOKEN);
  await SecureStore.deleteItemAsync(KEYS.USER);
};
