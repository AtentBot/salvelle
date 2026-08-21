/**
 * Salvelle - Employees Module Shared JavaScript
 * Utility functions and helpers for employee management
 */

// ============================================
// Authentication & Session Management
// ============================================

const Auth = {
    /**
     * Get authentication token from localStorage
     */
    getToken() {
        return localStorage.getItem('authToken');
    },

    /**
     * Save authentication data
     */
    saveAuth(token, expiry, employeeData) {
        localStorage.setItem('authToken', token);
        localStorage.setItem('tokenExpiry', expiry);
        localStorage.setItem('employeeData', JSON.stringify(employeeData));
    },

    /**
     * Clear authentication data
     */
    clearAuth() {
        localStorage.removeItem('authToken');
        localStorage.removeItem('tokenExpiry');
        localStorage.removeItem('employeeData');
    },

    /**
     * Check if user is authenticated
     */
    isAuthenticated() {
        const token = this.getToken();
        const expiry = localStorage.getItem('tokenExpiry');
        
        if (!token || !expiry) return false;
        
        const expiryDate = new Date(expiry);
        return expiryDate > new Date();
    },

    /**
     * Get logged in employee data
     */
    getEmployee() {
        const data = localStorage.getItem('employeeData');
        return data ? JSON.parse(data) : null;
    },

    /**
     * Get establishment ID
     */
    getEstablishmentId() {
        const employee = this.getEmployee();
        return employee?.establishment?.id || null;
    }
};

// ============================================
// Formatters
// ============================================

const Format = {
    /**
     * Format CPF: 000.000.000-00
     */
    cpf(value) {
        if (!value) return '';
        const digits = value.replace(/\D/g, '');
        return digits.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4');
    },

    /**
     * Format CNPJ: XX.XXX.XXX/XXXX-XX (alfanumérico, IN RFB nº 2.229/2024).
     * Posições 1-12 aceitam [A-Z0-9]; os 2 DVs finais são numéricos.
     */
    cnpj(value) {
        if (!value) return '';
        const norm = value.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 14);
        return norm.replace(/^([A-Z0-9]{2})([A-Z0-9]{3})([A-Z0-9]{3})([A-Z0-9]{4})(\d{2})/, '$1.$2.$3/$4-$5');
    },

    /**
     * Format phone: (00) 00000-0000
     */
    phone(value) {
        if (!value) return '';
        const digits = value.replace(/\D/g, '');
        if (digits.length === 11) {
            return digits.replace(/(\d{2})(\d{5})(\d{4})/, '($1) $2-$3');
        } else if (digits.length === 10) {
            return digits.replace(/(\d{2})(\d{4})(\d{4})/, '($1) $2-$3');
        }
        return value;
    },

    /**
     * Format CEP: 00000-000
     */
    cep(value) {
        if (!value) return '';
        const digits = value.replace(/\D/g, '');
        return digits.replace(/(\d{5})(\d{3})/, '$1-$2');
    },

    /**
     * Format PIS/PASEP: 000.00000.00-0
     */
    pis(value) {
        if (!value) return '';
        const digits = value.replace(/\D/g, '');
        return digits.replace(/(\d{3})(\d{5})(\d{2})(\d{1})/, '$1.$2.$3-$4');
    },

    /**
     * Format money: R$ 0.000,00
     */
    money(value) {
        if (!value && value !== 0) return 'R$ 0,00';
        return new Intl.NumberFormat('pt-BR', {
            style: 'currency',
            currency: 'BRL'
        }).format(value);
    },

    /**
     * Format date: DD/MM/YYYY
     */
    date(value) {
        if (!value) return '';
        const date = new Date(value + 'T00:00:00');
        return date.toLocaleDateString('pt-BR');
    },

    /**
     * Format datetime: DD/MM/YYYY HH:mm
     */
    datetime(value) {
        if (!value) return '';
        const date = new Date(value);
        return date.toLocaleString('pt-BR');
    },

    /**
     * Format number with thousands separator
     */
    number(value) {
        if (!value && value !== 0) return '0';
        return new Intl.NumberFormat('pt-BR').format(value);
    }
};

// ============================================
// Validators
// ============================================

const Validate = {
    /**
     * Validate CPF
     */
    cpf(cpf) {
        const digits = cpf.replace(/\D/g, '');
        
        if (digits.length !== 11) return false;
        if (/^(\d)\1+$/.test(digits)) return false;
        
        let sum = 0;
        for (let i = 0; i < 9; i++) {
            sum += parseInt(digits.charAt(i)) * (10 - i);
        }
        let digit1 = 11 - (sum % 11);
        digit1 = digit1 > 9 ? 0 : digit1;
        
        sum = 0;
        for (let i = 0; i < 10; i++) {
            sum += parseInt(digits.charAt(i)) * (11 - i);
        }
        let digit2 = 11 - (sum % 11);
        digit2 = digit2 > 9 ? 0 : digit2;
        
        return digits.charAt(9) == digit1 && digits.charAt(10) == digit2;
    },

    /**
     * Validate email
     */
    email(email) {
        const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return regex.test(email);
    },

    /**
     * Validate phone
     */
    phone(phone) {
        const digits = phone.replace(/\D/g, '');
        return digits.length === 10 || digits.length === 11;
    },

    /**
     * Validate CEP
     */
    cep(cep) {
        const digits = cep.replace(/\D/g, '');
        return digits.length === 8;
    },

    /**
     * Validate password strength
     */
    password(password) {
        const errors = [];
        
        if (password.length < 8) {
            errors.push('Senha deve ter no mínimo 8 caracteres');
        }
        if (!/[a-z]/.test(password)) {
            errors.push('Senha deve conter letras minúsculas');
        }
        if (!/[A-Z]/.test(password)) {
            errors.push('Senha deve conter letras maiúsculas');
        }
        if (!/\d/.test(password)) {
            errors.push('Senha deve conter números');
        }
        if (!/[^a-zA-Z\d]/.test(password)) {
            errors.push('Senha deve conter símbolos especiais');
        }
        
        return {
            valid: errors.length === 0,
            errors: errors
        };
    }
};

// ============================================
// Masks (Input Formatters)
// ============================================

const Mask = {
    /**
     * Apply CPF mask to input
     */
    cpf(input) {
        input.addEventListener('input', function(e) {
            let value = e.target.value.replace(/\D/g, '');
            if (value.length <= 11) {
                value = value.replace(/(\d{3})(\d)/, '$1.$2');
                value = value.replace(/(\d{3})(\d)/, '$1.$2');
                value = value.replace(/(\d{3})(\d{1,2})$/, '$1-$2');
                e.target.value = value;
            }
        });
    },

    /**
     * Apply phone mask to input
     */
    phone(input) {
        input.addEventListener('input', function(e) {
            let value = e.target.value.replace(/\D/g, '');
            if (value.length <= 11) {
                value = value.replace(/(\d{2})(\d)/, '($1) $2');
                value = value.replace(/(\d{5})(\d)/, '$1-$2');
                e.target.value = value;
            }
        });
    },

    /**
     * Apply CEP mask to input
     */
    cep(input) {
        input.addEventListener('input', function(e) {
            let value = e.target.value.replace(/\D/g, '');
            if (value.length <= 8) {
                value = value.replace(/(\d{5})(\d)/, '$1-$2');
                e.target.value = value;
            }
        });
    },

    /**
     * Apply PIS mask to input
     */
    pis(input) {
        input.addEventListener('input', function(e) {
            let value = e.target.value.replace(/\D/g, '');
            if (value.length <= 11) {
                value = value.replace(/(\d{3})(\d)/, '$1.$2');
                value = value.replace(/(\d{5})(\d)/, '$1.$2');
                value = value.replace(/(\d{2})(\d{1})$/, '$1-$2');
                e.target.value = value;
            }
        });
    },

    /**
     * Apply money mask to input
     */
    money(input) {
        input.addEventListener('input', function(e) {
            let value = e.target.value.replace(/\D/g, '');
            if (value) {
                value = (parseInt(value) / 100).toFixed(2);
                e.target.value = value.replace('.', ',');
            }
        });
    },

    /**
     * Apply all masks to elements by class
     */
    applyAll() {
        document.querySelectorAll('.cpf-mask').forEach(this.cpf);
        document.querySelectorAll('.phone-mask').forEach(this.phone);
        document.querySelectorAll('.cep-mask').forEach(this.cep);
        document.querySelectorAll('.pis-mask').forEach(this.pis);
        document.querySelectorAll('.money-mask').forEach(this.money);
    }
};

// ============================================
// API Helper
// ============================================

const API = {
    /**
     * Base URL for API
     */
    baseURL: '/api',

    /**
     * Get headers with auth token
     */
    getHeaders() {
        return {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${Auth.getToken()}`
        };
    },

    /**
     * GET request
     */
    async get(endpoint) {
        const response = await fetch(`${this.baseURL}${endpoint}`, {
            method: 'GET',
            headers: this.getHeaders()
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        return await response.json();
    },

    /**
     * POST request
     */
    async post(endpoint, data) {
        const response = await fetch(`${this.baseURL}${endpoint}`, {
            method: 'POST',
            headers: this.getHeaders(),
            body: JSON.stringify(data)
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || `HTTP error! status: ${response.status}`);
        }
        
        return await response.json();
    },

    /**
     * PUT request
     */
    async put(endpoint, data) {
        const response = await fetch(`${this.baseURL}${endpoint}`, {
            method: 'PUT',
            headers: this.getHeaders(),
            body: JSON.stringify(data)
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || `HTTP error! status: ${response.status}`);
        }
        
        return await response.json();
    },

    /**
     * DELETE request
     */
    async delete(endpoint) {
        const response = await fetch(`${this.baseURL}${endpoint}`, {
            method: 'DELETE',
            headers: this.getHeaders()
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        return await response.json();
    }
};

// ============================================
// Utility Functions
// ============================================

const Utils = {
    /**
     * Debounce function
     */
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    },

    /**
     * Show toast notification
     */
    toast(message, type = 'info') {
        // Bootstrap Toast implementation
        const toastHTML = `
            <div class="toast align-items-center text-white bg-${type} border-0" role="alert">
                <div class="d-flex">
                    <div class="toast-body">${message}</div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>
        `;
        
        let container = document.querySelector('.toast-container');
        if (!container) {
            container = document.createElement('div');
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            document.body.appendChild(container);
        }
        
        container.insertAdjacentHTML('beforeend', toastHTML);
        const toastElement = container.lastElementChild;
        const toast = new bootstrap.Toast(toastElement);
        toast.show();
        
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });
    },

    /**
     * Copy text to clipboard
     */
    async copyToClipboard(text) {
        try {
            await navigator.clipboard.writeText(text);
            this.toast('Copiado para a área de transferência!', 'success');
            return true;
        } catch (err) {
            console.error('Erro ao copiar:', err);
            this.toast('Erro ao copiar texto', 'danger');
            return false;
        }
    },

    /**
     * Generate random color
     */
    randomColor() {
        const colors = [
            '#4f46e5', '#7c3aed', '#db2777', '#dc2626', 
            '#ea580c', '#ca8a04', '#16a34a', '#0891b2', '#2563eb'
        ];
        return colors[Math.floor(Math.random() * colors.length)];
    },

    /**
     * Get color for string (consistent)
     */
    stringToColor(str) {
        const colors = [
            '#4f46e5', '#7c3aed', '#db2777', '#dc2626', 
            '#ea580c', '#ca8a04', '#16a34a', '#0891b2', '#2563eb'
        ];
        const index = str.charCodeAt(0) % colors.length;
        return colors[index];
    },

    /**
     * Get initials from name
     */
    getInitials(name) {
        return name.split(' ')
            .map(n => n[0])
            .slice(0, 2)
            .join('')
            .toUpperCase();
    },

    /**
     * Calculate age from birthdate
     */
    calculateAge(birthdate) {
        const birth = new Date(birthdate + 'T00:00:00');
        const today = new Date();
        let age = today.getFullYear() - birth.getFullYear();
        const monthDiff = today.getMonth() - birth.getMonth();
        
        if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())) {
            age--;
        }
        
        return age;
    },

    /**
     * Calculate work time from hire date
     */
    calculateWorkTime(hireDate) {
        const hire = new Date(hireDate + 'T00:00:00');
        const now = new Date();
        const diff = now - hire;
        const days = Math.floor(diff / (1000 * 60 * 60 * 24));
        const years = Math.floor(days / 365);
        const months = Math.floor((days % 365) / 30);
        
        if (years > 0) {
            return `${years} ano${years > 1 ? 's' : ''} ${months > 0 ? `e ${months} mês${months > 1 ? 'es' : ''}` : ''}`;
        }
        return `${months} mês${months > 1 ? 'es' : ''}`;
    },

    /**
     * Search address by CEP
     */
    async searchCEP(cep) {
        const digits = cep.replace(/\D/g, '');
        
        if (digits.length !== 8) {
            throw new Error('CEP inválido');
        }
        
        const response = await fetch(`https://viacep.com.br/ws/${digits}/json/`);
        const data = await response.json();
        
        if (data.erro) {
            throw new Error('CEP não encontrado');
        }
        
        return {
            cep: data.cep,
            street: data.logradouro,
            neighborhood: data.bairro,
            city: data.localidade,
            state: data.uf
        };
    },

    /**
     * Export table to CSV
     */
    exportToCSV(data, filename = 'export.csv') {
        const csvContent = data.map(row => 
            Object.values(row).map(val => `"${val}"`).join(',')
        ).join('\n');
        
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        link.click();
    },

    /**
     * Print element
     */
    printElement(elementId) {
        const element = document.getElementById(elementId);
        const printWindow = window.open('', '', 'height=600,width=800');
        
        printWindow.document.write('<html><head><title>Imprimir</title>');
        printWindow.document.write('<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css">');
        printWindow.document.write('</head><body>');
        printWindow.document.write(element.innerHTML);
        printWindow.document.write('</body></html>');
        
        printWindow.document.close();
        printWindow.focus();
        
        setTimeout(() => {
            printWindow.print();
            printWindow.close();
        }, 250);
    }
};

// ============================================
// ViaCEP Integration
// ============================================

const ViaCEP = {
    /**
     * Search address by CEP and fill form fields
     */
    async search(cep, form) {
        try {
            const address = await Utils.searchCEP(cep);
            
            const streetInput = form.querySelector('[name="street"]');
            const neighborhoodInput = form.querySelector('[name="neighborhood"]');
            const cityInput = form.querySelector('[name="city"]');
            const stateInput = form.querySelector('[name="state"]');
            
            if (streetInput) streetInput.value = address.street;
            if (neighborhoodInput) neighborhoodInput.value = address.neighborhood;
            if (cityInput) cityInput.value = address.city;
            if (stateInput) stateInput.value = address.state;
            
            const numberInput = form.querySelector('[name="number"]');
            if (numberInput) numberInput.focus();
            
            return address;
        } catch (error) {
            Utils.toast(error.message, 'danger');
            throw error;
        }
    }
};

// ============================================
// Initialize on DOM Load
// ============================================

document.addEventListener('DOMContentLoaded', function() {
    // Apply masks to all inputs with mask classes
    Mask.applyAll();
    
    // Check authentication on protected pages
    const publicPages = ['/Employees/Login', '/Employees/GenerateHash'];
    if (!publicPages.includes(window.location.pathname) && !Auth.isAuthenticated()) {
        window.location.href = '/Employees/Login';
    }
});

// ============================================
// Export for use in other scripts
// ============================================

// Make utilities available globally
window.FormulaClear = {
    Auth,
    Format,
    Validate,
    Mask,
    API,
    Utils,
    ViaCEP
};
