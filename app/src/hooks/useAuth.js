import React, { createContext, useContext, useState, useEffect } from 'react';
import * as api from '../services/api';
import * as storage from '../services/storage';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [pendingVerification, setPendingVerification] = useState(null);

  useEffect(() => {
    checkAuth();
  }, []);

  const checkAuth = async () => {
    try {
      setIsLoading(true);
      const token = await api.loadToken();
      
      if (!token) {
        setIsAuthenticated(false);
        setUser(null);
        return;
      }

      const result = await api.getMe();
      
      if (result.success) {
        setUser(result.customer || result.data);
        setIsAuthenticated(true);
        await storage.saveUserData(result.customer || result.data);
      } else {
        await storage.clearAllData();
        api.clearToken();
        setIsAuthenticated(false);
        setUser(null);
      }
    } catch (error) {
      setIsAuthenticated(false);
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (phone, password) => {
    try {
      const result = await api.login(phone, password);

      if (result.success) {
        if (result.requiresVerification) {
          setPendingVerification({ phone: result.phone });
          return { success: true, requiresVerification: true };
        }

        const token = result.sessionToken || result.token;
        if (token) await storage.saveToken(token);
        setUser(result.customer);
        setIsAuthenticated(true);
        if (result.customer) await storage.saveUserData(result.customer);
        return { success: true };
      }

      return result;
    } catch (error) {
      return { success: false, message: error.message };
    }
  };

  const loginWithBiometrics = async () => {
    try {
      const token = await storage.getToken();
      if (!token) return { success: false, message: 'Nenhuma sessão' };
      
      api.setToken(token);
      const result = await api.getMe();
      
      if (result.success) {
        setUser(result.customer || result.data);
        setIsAuthenticated(true);
        return { success: true };
      }
      
      await storage.clearAllData();
      return { success: false, message: 'Sessão expirada' };
    } catch (error) {
      return { success: false, message: error.message };
    }
  };

  const verifyCode = async (code) => {
    if (!pendingVerification) {
      return { success: false, message: 'Nenhuma verificação pendente' };
    }
    
    try {
      const result = await api.verifyCode(pendingVerification.phone, code);
      
      if (result.success) {
        await storage.saveToken(result.token);
        setUser(result.customer);
        setIsAuthenticated(true);
        setPendingVerification(null);
        await storage.saveUserData(result.customer);
        return { success: true };
      }
      
      return result;
    } catch (error) {
      return { success: false, message: error.message };
    }
  };

  const register = async (data) => {
    try {
      const result = await api.register(data);
      
      if (result.success) {
        setPendingVerification({ phone: data.phone });
        return { success: true, requiresVerification: true };
      }
      
      return result;
    } catch (error) {
      return { success: false, message: error.message };
    }
  };

  const resendVerificationCode = async () => {
    if (!pendingVerification) {
      return { success: false, message: 'Nenhuma verificação pendente' };
    }
    return await api.resendCode(pendingVerification.phone);
  };

  const logout = async () => {
    try { await api.logout(); } catch (e) {}
    await storage.clearAllData();
    api.clearToken();
    setUser(null);
    setIsAuthenticated(false);
    setPendingVerification(null);
  };

  const updateUser = (userData) => {
    setUser(userData);
    storage.saveUserData(userData);
  };

  return (
    <AuthContext.Provider value={{
      user,
      isAuthenticated,
      isLoading,
      pendingVerification,
      login,
      loginWithBiometrics,
      verifyCode,
      register,
      resendVerificationCode,
      logout,
      updateUser,
      checkAuth,
    }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth deve ser usado dentro de AuthProvider');
  return context;
};

export default useAuth;
