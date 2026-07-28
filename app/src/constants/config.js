// Configuração da API
// Em dev, troque para o IP local: 'http://192.168.x.x:5000/api'
const DEV_API_URL = 'http://localhost:5000/api';
const PROD_API_URL = 'https://salvelle.atentbot.com/api';

export const API_URL = __DEV__ ? DEV_API_URL : PROD_API_URL;

// Configurações do app
export const APP_CONFIG = {
  name: 'Salvelle',
  version: '1.0.0',
  promoCode: 'FORMULA15',
};

// Endpoints
export const ENDPOINTS = {
  LOGIN: '/cliente/auth/login',
  REGISTER: '/cliente/auth/register',
  VERIFY: '/cliente/auth/verify',
  RESEND: '/cliente/auth/resend-code',
  ME: '/cliente/auth/me',
  LOGOUT: '/cliente/auth/logout',
  PRESCRIPTIONS: '/cliente/prescriptions',
  UPLOAD: '/cliente/prescriptions/upload',
  CART: '/cliente/cart',
  ORDERS: '/cliente/orders',
  PRODUCT_TYPES: '/customer-portal/product-types',
  INGREDIENTS: '/pricing/ingredient/search',
  CALCULATE: '/pricing/formula/calculate',
};
