import { API_URL } from '../constants/config';
import { getToken, deleteToken } from './storage';

let authToken = null;

export const setToken = (token) => { authToken = token; };
export const clearToken = () => { authToken = null; };

export const loadToken = async () => {
  const token = await getToken();
  if (token) authToken = token;
  return token;
};

// Base request
const request = async (endpoint, options = {}) => {
  const headers = { 'Content-Type': 'application/json', ...options.headers };
  if (authToken) headers['Authorization'] = `Bearer ${authToken}`;

  try {
    const response = await fetch(`${API_URL}${endpoint}`, { ...options, headers });
    if (response.status === 401) {
      clearToken();
      await deleteToken();
      throw new Error('SESSION_EXPIRED');
    }
    return await response.json();
  } catch (error) {
    if (error.message === 'SESSION_EXPIRED') throw error;
    throw new Error('Erro de conexão');
  }
};

// Auth
export const login = async (phone, password) => {
  const result = await request('/cliente/auth/login', {
    method: 'POST',
    body: JSON.stringify({ phone: phone.replace(/\D/g, ''), password }),
  });
  if (result.success && (result.sessionToken || result.token)) {
    setToken(result.sessionToken || result.token);
  }
  return result;
};

export const register = async (data) => {
  return request('/cliente/auth/register', {
    method: 'POST',
    body: JSON.stringify({
      fullName: data.fullName,
      cpf: data.cpf.replace(/\D/g, ''),
      phone: data.phone.replace(/\D/g, ''),
      password: data.password,
      consentDataProcessing: true,
    }),
  });
};

export const verifyCode = async (phone, code) => {
  const result = await request('/cliente/auth/verify', {
    method: 'POST',
    body: JSON.stringify({ phone: phone.replace(/\D/g, ''), code }),
  });
  if (result.success && result.token) setToken(result.token);
  return result;
};

export const resendCode = async (phone) => {
  return request('/cliente/auth/resend-code', {
    method: 'POST',
    body: JSON.stringify({ phone: phone.replace(/\D/g, '') }),
  });
};

export const requestPasswordReset = async (phone) => {
  return request('/cliente/auth/request-reset', {
    method: 'POST',
    body: JSON.stringify({ phone: phone.replace(/\D/g, '') }),
  });
};

export const resetPassword = async (phone, code, newPassword, confirmPassword) => {
  return request('/cliente/auth/reset-password', {
    method: 'POST',
    body: JSON.stringify({
      phone: phone.replace(/\D/g, ''),
      code,
      newPassword,
      confirmPassword,
    }),
  });
};

export const getMe = async () => request('/cliente/auth/me');

// Catálogo — prateleira de ingredientes ativos
export const getCatalogIngredients = async ({ category, search, limit } = {}) => {
  const params = new URLSearchParams();
  if (category) params.set('category', category);
  if (search) params.set('search', search);
  if (limit) params.set('limit', String(limit));
  const qs = params.toString();
  return request(`/customer-portal/ingredients${qs ? '?' + qs : ''}`);
};

export const logout = async () => {
  try { await request('/cliente/auth/logout', { method: 'POST' }); } catch (e) {}
  clearToken();
  await deleteToken();
};

// Prescriptions
export const uploadPrescription = async (imageBase64, observations = '') => {
  return request('/cliente/prescriptions/upload', {
    method: 'POST',
    body: JSON.stringify({ imageBase64, observations }),
  });
};

export const getPrescriptions = async () => request('/cliente/prescriptions');

// Cart
export const getCart = async () => request('/cliente/cart');
export const getCartCount = async () => request('/cliente/cart/count');

export const addToCart = async (productId, quantity = 1) => {
  return request('/cliente/cart/add', {
    method: 'POST',
    body: JSON.stringify({ productId, quantity }),
  });
};

export const addFormulaToCart = async (data) => {
  return request('/cliente/cart/add-formula', {
    method: 'POST',
    body: JSON.stringify(data),
  });
};

export const updateCartItem = async (itemId, quantity) => {
  return request('/cliente/cart/update', {
    method: 'POST',
    body: JSON.stringify({ itemId, quantity }),
  });
};

export const removeFromCart = async (itemId) => {
  return request('/cliente/cart/remove', {
    method: 'POST',
    body: JSON.stringify({ itemId }),
  });
};

export const clearCart = async () => {
  return request('/cliente/cart/clear', { method: 'POST' });
};

// Orders
export const getOrders = async (page = 1, status = null) => {
  let url = `/cliente/orders?page=${page}`;
  if (status) url += `&status=${status}`;
  return request(url);
};

export const getOrderDetails = async (orderId) => {
  return request(`/cliente/orders/${orderId}`);
};

export const createOrder = async (data) => {
  return request('/cliente/orders', {
    method: 'POST',
    body: JSON.stringify(data),
  });
};

// Formulas
export const getProductTypes = async () => request('/customer-portal/product-types');

export const searchIngredients = async (query) => {
  return request(`/pricing/ingredient/search?name=${encodeURIComponent(query)}`);
};

export const autocompleteIngredients = async (query, limit = 10) => {
  const r = await request(`/cliente/search?q=${encodeURIComponent(query)}&limit=${limit}`);
  return r?.results || [];
};

export const calculateFormula = async (productType, ingredients, productQuantity = 1) => {
  return request('/cliente/formula/calculate', {
    method: 'POST',
    body: JSON.stringify({ productType, productQuantity, ingredients }),
  });
};

// Profile
export const updateProfile = async (data) => {
  return request('/cliente/profile', {
    method: 'PUT',
    body: JSON.stringify(data),
  });
};
