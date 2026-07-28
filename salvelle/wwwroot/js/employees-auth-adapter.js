/**
 * Salvelle - Employees Auth Adapter
 * Adapta as views de Employees para trabalhar com o sistema de autenticação existente
 * que usa cookies em vez de localStorage
 */

(function() {
    'use strict';

    // ============================================
    // Cookie Helper Functions
    // ============================================
    
    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) {
            return parts.pop().split(';').shift();
        }
        return null;
    }

    function setCookie(name, value, days = 7) {
        const expires = new Date();
        expires.setTime(expires.getTime() + (days * 24 * 60 * 60 * 1000));
        document.cookie = `${name}=${value};expires=${expires.toUTCString()};path=/;SameSite=Strict`;
    }

    function deleteCookie(name) {
        document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/;`;
    }

    // ============================================
    // Override Auth Module
    // ============================================
    
    if (window.FormulaClear && window.FormulaClear.Auth) {
        // Sobrescrever getToken para usar cookie SessionId
        window.FormulaClear.Auth.getToken = function() {
            return getCookie('SessionId');
        };

        // Sobrescrever saveAuth para usar cookies
        window.FormulaClear.Auth.saveAuth = function(token, expiry, employeeData) {
            // O cookie SessionId é gerenciado pelo servidor
            // Apenas salvar dados do employee no sessionStorage para uso local
            if (employeeData) {
                sessionStorage.setItem('currentEmployee', JSON.stringify(employeeData));
            }
        };

        // Sobrescrever clearAuth para fazer logout via servidor
        window.FormulaClear.Auth.clearAuth = function() {
            return fetch('/Account/Logout', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include'
            }).then(() => {
                sessionStorage.clear();
                window.location.href = '/Account/Login';
            }).catch(err => {
                console.error('Erro ao fazer logout:', err);
                sessionStorage.clear();
                window.location.href = '/Account/Login';
            });
        };

        // Sobrescrever isAuthenticated para verificar cookie
        window.FormulaClear.Auth.isAuthenticated = function() {
            const sessionId = getCookie('SessionId');
            return sessionId !== null && sessionId !== '';
        };

        // Sobrescrever getEmployee para usar sessionStorage em vez de localStorage
        window.FormulaClear.Auth.getEmployee = function() {
            const data = sessionStorage.getItem('currentEmployee');
            return data ? JSON.parse(data) : null;
        };

        // Sobrescrever getEstablishmentId
        window.FormulaClear.Auth.getEstablishmentId = function() {
            const employee = this.getEmployee();
            return employee?.establishment?.id || null;
        };

    }

    // ============================================
    // Override API Module
    // ============================================
    
    if (window.FormulaClear && window.FormulaClear.API) {
        // Sobrescrever getHeaders para não incluir Authorization
        // O cookie é enviado automaticamente pelo browser
        window.FormulaClear.API.getHeaders = function() {
            return {
                'Content-Type': 'application/json'
            };
        };

        // Sobrescrever métodos HTTP para incluir credentials
        const originalGet = window.FormulaClear.API.get;
        window.FormulaClear.API.get = async function(endpoint) {
            const response = await fetch(`${this.baseURL}${endpoint}`, {
                method: 'GET',
                headers: this.getHeaders(),
                credentials: 'include'  // Incluir cookies
            });
            
            if (!response.ok) {
                if (response.status === 401) {
                    window.location.href = '/Account/Login';
                    throw new Error('Não autenticado');
                }
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            return await response.json();
        };

        const originalPost = window.FormulaClear.API.post;
        window.FormulaClear.API.post = async function(endpoint, data) {
            const response = await fetch(`${this.baseURL}${endpoint}`, {
                method: 'POST',
                headers: this.getHeaders(),
                body: JSON.stringify(data),
                credentials: 'include'  // Incluir cookies
            });
            
            if (!response.ok) {
                if (response.status === 401) {
                    window.location.href = '/Account/Login';
                    throw new Error('Não autenticado');
                }
                const error = await response.json();
                throw new Error(error.error || `HTTP error! status: ${response.status}`);
            }
            
            return await response.json();
        };

        const originalPut = window.FormulaClear.API.put;
        window.FormulaClear.API.put = async function(endpoint, data) {
            const response = await fetch(`${this.baseURL}${endpoint}`, {
                method: 'PUT',
                headers: this.getHeaders(),
                body: JSON.stringify(data),
                credentials: 'include'  // Incluir cookies
            });
            
            if (!response.ok) {
                if (response.status === 401) {
                    window.location.href = '/Account/Login';
                    throw new Error('Não autenticado');
                }
                const error = await response.json();
                throw new Error(error.error || `HTTP error! status: ${response.status}`);
            }
            
            return await response.json();
        };

        const originalDelete = window.FormulaClear.API.delete;
        window.FormulaClear.API.delete = async function(endpoint) {
            const response = await fetch(`${this.baseURL}${endpoint}`, {
                method: 'DELETE',
                headers: this.getHeaders(),
                credentials: 'include'  // Incluir cookies
            });
            
            if (!response.ok) {
                if (response.status === 401) {
                    window.location.href = '/Account/Login';
                    throw new Error('Não autenticado');
                }
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            return await response.json();
        };

    }

    // ============================================
    // Authentication Check on Page Load
    // ============================================
    
    document.addEventListener('DOMContentLoaded', function() {
        // Verificar autenticação em páginas protegidas
        const protectedPaths = ['/Employees/List', '/Employees/Create', '/Employees/Details', '/Employees/Edit'];
        const currentPath = window.location.pathname;
        
        if (protectedPaths.some(path => currentPath.startsWith(path))) {
            if (!getCookie('SessionId')) {
                console.warn('⚠️ Sessão não encontrada, redirecionando para login...');
                window.location.href = '/Account/Login';
            }
        }
    });

    // ============================================
    // Intercept Fetch Requests
    // ============================================
    
    // Interceptar todos os fetch para adicionar credentials automaticamente
    const originalFetch = window.fetch;
    window.fetch = function(...args) {
        let [resource, config] = args;
        
        // Se é uma requisição para a API, adicionar credentials
        if (typeof resource === 'string' && resource.startsWith('/api/')) {
            config = config || {};
            config.credentials = 'include';
        }
        
        return originalFetch(resource, config)
            .then(response => {
                // Se receber 401, redirecionar para login
                if (response.status === 401) {
                    const isLoginPage = window.location.pathname.includes('/Login');
                    if (!isLoginPage) {
                        console.warn('⚠️ Sessão expirada, redirecionando para login...');
                        window.location.href = '/Account/Login';
                    }
                }
                return response;
            });
    };

    // ============================================
    // Logout Handler
    // ============================================
    
    // Adicionar handler global para botões de logout
    window.handleLogout = function() {
        if (confirm('Deseja realmente sair?')) {
            window.FormulaClear.Auth.clearAuth();
        }
    };

    // ============================================
    // Session Activity Tracker
    // ============================================
    
    // Rastrear atividade para manter sessão viva
    let activityTimeout;
    const ACTIVITY_CHECK_INTERVAL = 5 * 60 * 1000; // 5 minutos

    function trackActivity() {
        clearTimeout(activityTimeout);
        
        activityTimeout = setTimeout(() => {
            // Verificar se sessão ainda está válida
            if (getCookie('SessionId')) {
                // Fazer ping no servidor para manter sessão viva
                fetch('/api/Employees/ping', {
                    method: 'GET',
                    credentials: 'include'
                }).catch(err => {
                    console.warn('⚠️ Erro ao verificar sessão:', err);
                });
            }
        }, ACTIVITY_CHECK_INTERVAL);
    }

    // Rastrear eventos de atividade
    ['click', 'keypress', 'scroll', 'mousemove'].forEach(event => {
        document.addEventListener(event, trackActivity, { passive: true, once: true });
    });

    // Iniciar rastreamento
    trackActivity();

    // ============================================
    // Debug Helpers (apenas em desenvolvimento)
    // ============================================
    
    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
        window.FormulaClearDebug = {
            getSessionId: () => getCookie('SessionId'),
            getCurrentEmployee: () => sessionStorage.getItem('currentEmployee'),
            isAuthenticated: () => window.FormulaClear.Auth.isAuthenticated(),
            clearSession: () => {
                deleteCookie('SessionId');
                sessionStorage.clear();
            }
        };
        
    }
})();
