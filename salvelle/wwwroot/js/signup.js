/**
 * Salvelle - Signup Process JavaScript
 * Handles registration, verification, and payment flow
 */

// ============================================
// SIGNUP FORM - STEP 1: REGISTRATION
// ============================================
document.addEventListener('DOMContentLoaded', function () {
    const signupForm = document.getElementById('signupForm');

    if (signupForm) {
        // Real-time CNPJ validation
        const cnpjInput = document.getElementById('cnpj');
        if (cnpjInput) {
            cnpjInput.addEventListener('input', function (e) {
                this.value = formatCNPJ(this.value);
                validateCNPJField(this);
            });
        }

        // Real-time WhatsApp validation
        const whatsappInput = document.getElementById('whatsapp');
        if (whatsappInput) {
            whatsappInput.addEventListener('input', function (e) {
                this.value = formatPhone(this.value);
                validatePhoneField(this);
            });
        }

        // Password strength indicator
        const passwordInput = document.getElementById('password');
        const passwordConfirm = document.getElementById('passwordConfirm');

        if (passwordInput) {
            passwordInput.addEventListener('input', function () {
                updatePasswordStrength(this.value);
            });
        }

        if (passwordConfirm) {
            passwordConfirm.addEventListener('input', function () {
                validatePasswordMatch();
            });
        }

        // Form submission
        signupForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            if (!validateSignupForm()) {
                const firstError = signupForm.querySelector('.is-invalid');
                if (firstError) {
                    firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    firstError.focus();
                }
                showAlert('warning', 'Corrija os campos destacados antes de continuar.');
                return;
            }

            const submitBtn = this.querySelector('button[type="submit"]');
            const originalText = submitBtn.innerHTML;
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Processando...';
            submitBtn.disabled = true;

            const formData = {
                nomeFantasia: document.getElementById('nomeFantasia').value.trim(),
                razaoSocial: document.getElementById('razaoSocial').value.trim(),
                cnpj: normalizeCNPJ(document.getElementById('cnpj').value),
                whatsApp: document.getElementById('whatsapp').value.replace(/\D/g, ''),
                email: document.getElementById('email').value.trim(),
                planId: document.getElementById('planId')?.value || null,
                password: document.getElementById('password').value,
                acceptTerms: document.getElementById('acceptTerms').checked
            };

            try {
                const response = await fetch('/api/signup/register', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(formData)
                });

                const data = await response.json();

                if (response.ok) {
                    // Store for next step
                    sessionStorage.setItem('signupWhatsApp', formData.whatsApp);
                    sessionStorage.setItem('signupEstablishmentId', data.establishmentId);
                    // Plano escolhido na Pricing: carrega até o checkout (cartão) no fim do signup.
                    if (formData.planId) {
                        sessionStorage.setItem('signupPlanId', formData.planId);
                    } else {
                        sessionStorage.removeItem('signupPlanId');
                    }

                    showAlert('success', 'Cadastro realizado! Enviamos um código para seu WhatsApp.');

                    // Redirect to verification
                    setTimeout(() => {
                        window.location.href = `/signup/verify?whatsapp=${formData.whatsApp}`;
                    }, 1500);
                } else {
                    showAlert('danger', data.message || 'Erro ao processar cadastro.');
                    submitBtn.innerHTML = originalText;
                    submitBtn.disabled = false;
                }
            } catch (error) {
                console.error('Signup error:', error);
                showAlert('danger', 'Erro ao conectar com o servidor. Tente novamente.');
                submitBtn.innerHTML = originalText;
                submitBtn.disabled = false;
            }
        });
    }
});

// ============================================
// VERIFICATION FORM - STEP 2: CODE VERIFICATION
// ============================================
document.addEventListener('DOMContentLoaded', function () {
    const verifyForm = document.getElementById('verifyForm');

    if (verifyForm) {
        const codeInputs = document.querySelectorAll('.code-input');

        // Auto-focus on next input
        codeInputs.forEach((input, index) => {
            input.addEventListener('input', function (e) {
                if (this.value.length === 1 && index < codeInputs.length - 1) {
                    codeInputs[index + 1].focus();
                }

                // Auto-submit when all 6 digits are filled
                if (index === codeInputs.length - 1 && this.value.length === 1) {
                    const code = Array.from(codeInputs).map(inp => inp.value).join('');
                    if (code.length === 6) {
                        submitVerificationCode(code);
                    }
                }
            });

            // Handle backspace
            input.addEventListener('keydown', function (e) {
                if (e.key === 'Backspace' && !this.value && index > 0) {
                    codeInputs[index - 1].focus();
                    codeInputs[index - 1].value = '';
                }
            });

            // Allow only numbers
            input.addEventListener('keypress', function (e) {
                if (!/[0-9]/.test(e.key)) {
                    e.preventDefault();
                }
            });
        });

        // Manual form submission
        verifyForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const code = Array.from(codeInputs).map(inp => inp.value).join('');
            submitVerificationCode(code);
        });

        // Resend code button
        const resendBtn = document.getElementById('resendCode');
        if (resendBtn) {
            resendBtn.addEventListener('click', async function (e) {
                e.preventDefault();

                const whatsapp = new URLSearchParams(window.location.search).get('whatsapp');
                if (!whatsapp) {
                    showAlert('danger', 'WhatsApp não encontrado. Volte ao cadastro.');
                    return;
                }

                this.disabled = true;
                this.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Enviando...';

                try {
                    const response = await fetch('/api/signup/resend-code', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ whatsApp: whatsapp })
                    });

                    if (response.ok) {
                        showAlert('success', 'Novo código enviado para seu WhatsApp!');
                        startResendCooldown();
                    } else {
                        showAlert('danger', 'Erro ao reenviar código. Tente novamente.');
                    }
                } catch (error) {
                    showAlert('danger', 'Erro ao conectar com o servidor.');
                } finally {
                    this.disabled = false;
                    this.textContent = 'Reenviar código';
                }
            });

            startResendCooldown();
        }
    }
});

async function submitVerificationCode(code) {
    if (code.length !== 6) {
        showAlert('warning', 'Por favor, digite o código de 6 dígitos.');
        return;
    }

    const whatsapp = new URLSearchParams(window.location.search).get('whatsapp');
    if (!whatsapp) {
        showAlert('danger', 'WhatsApp não encontrado. Volte ao cadastro.');
        return;
    }

    const verifyBtn = document.querySelector('#verifyForm button[type="submit"]');
    if (verifyBtn) {
        verifyBtn.disabled = true;
        verifyBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Verificando...';
    }

    try {
        const response = await fetch('/api/signup/verify', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                code: code,
                whatsApp: whatsapp
            })
        });

        const data = await response.json();

        if (response.ok) {
            showAlert('success', 'Código verificado com sucesso!');
            sessionStorage.setItem('signupEstablishmentId', data.establishmentId);

            setTimeout(() => {
                window.location.href = data.redirectTo || `/signup/complete-profile?establishmentId=${data.establishmentId}`;
            }, 1000);
        } else {
            showAlert('danger', data.message || 'Código inválido. Tente novamente.');
            if (verifyBtn) {
                verifyBtn.disabled = false;
                verifyBtn.innerHTML = 'Verificar';
            }
        }
    } catch (error) {
        console.error('Verification error:', error);
        showAlert('danger', 'Erro ao verificar código. Tente novamente.');
        if (verifyBtn) {
            verifyBtn.disabled = false;
            verifyBtn.innerHTML = 'Verificar';
        }
    }
}

function startResendCooldown() {
    const resendBtn = document.getElementById('resendCode');
    if (!resendBtn) return;

    let seconds = 60;
    resendBtn.disabled = true;
    resendBtn.textContent = `Reenviar em ${seconds}s`;

    const interval = setInterval(() => {
        seconds--;
        resendBtn.textContent = `Reenviar em ${seconds}s`;

        if (seconds <= 0) {
            clearInterval(interval);
            resendBtn.disabled = false;
            resendBtn.textContent = 'Reenviar código';
        }
    }, 1000);
}

// ============================================
// COMPLETE PROFILE FORM - STEP 3
// ============================================
document.addEventListener('DOMContentLoaded', function () {
    const profileForm = document.getElementById('completeProfileForm');

    if (profileForm) {
        const cpfInput = document.getElementById('cpf');
        if (cpfInput) {
            cpfInput.addEventListener('input', function () {
                this.value = formatCPF(this.value);
            });
        }

        profileForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const establishmentId = new URLSearchParams(window.location.search).get('establishmentId')
                || sessionStorage.getItem('signupEstablishmentId');

            if (!establishmentId) {
                showAlert('danger', 'Sessão expirada. Por favor, reinicie o cadastro.');
                return;
            }

            const submitBtn = this.querySelector('button[type="submit"]');
            const originalText = submitBtn.innerHTML;
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Salvando...';
            submitBtn.disabled = true;

            const formData = {
                establishmentId: establishmentId,
                fullName: document.getElementById('fullName').value.trim(),
                cpf: document.getElementById('cpf').value.replace(/\D/g, '')
            };

            try {
                const response = await fetch('/api/signup/complete-profile', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(formData)
                });

                const data = await response.json();

                if (response.ok) {
                    showAlert('success', 'Perfil completado com sucesso!');
                    sessionStorage.removeItem('signupEstablishmentId');
                    sessionStorage.removeItem('signupWhatsApp');

                    setTimeout(() => {
                        window.location.href = data.redirectTo || '/login';
                    }, 1500);
                } else {
                    showAlert('danger', data.message || 'Erro ao completar perfil.');
                    submitBtn.innerHTML = originalText;
                    submitBtn.disabled = false;
                }
            } catch (error) {
                console.error('Profile error:', error);
                showAlert('danger', 'Erro ao conectar com o servidor.');
                submitBtn.innerHTML = originalText;
                submitBtn.disabled = false;
            }
        });
    }
});

function formatCPF(value) {
    value = value.replace(/\D/g, '');
    value = value.substring(0, 11);

    if (value.length > 9) {
        value = value.replace(/^(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4');
    } else if (value.length > 6) {
        value = value.replace(/^(\d{3})(\d{3})(\d{0,3})/, '$1.$2.$3');
    } else if (value.length > 3) {
        value = value.replace(/^(\d{3})(\d{0,3})/, '$1.$2');
    }

    return value;
}

// ============================================
// VALIDATION FUNCTIONS
// ============================================

function validateSignupForm() {
    let isValid = true;

    // Nome Fantasia
    const nomeFantasia = document.getElementById('nomeFantasia');
    if (nomeFantasia && !nomeFantasia.value.trim()) {
        setFieldError(nomeFantasia, 'Nome fantasia é obrigatório');
        isValid = false;
    } else if (nomeFantasia) {
        clearFieldError(nomeFantasia);
    }

    // Razão Social
    const razaoSocial = document.getElementById('razaoSocial');
    if (razaoSocial && !razaoSocial.value.trim()) {
        setFieldError(razaoSocial, 'Razão social é obrigatória');
        isValid = false;
    } else if (razaoSocial) {
        clearFieldError(razaoSocial);
    }

    // CNPJ
    const cnpj = document.getElementById('cnpj');
    if (cnpj && !isValidCNPJ(cnpj.value)) {
        setFieldError(cnpj, 'CNPJ inválido');
        isValid = false;
    } else if (cnpj) {
        clearFieldError(cnpj);
    }

    // WhatsApp
    const whatsapp = document.getElementById('whatsapp');
    if (whatsapp) {
        const cleanedPhone = whatsapp.value.replace(/\D/g, '');
        const whatsappValidation = isValidWhatsApp(cleanedPhone);
        if (!whatsappValidation.valid) {
            setFieldError(whatsapp, whatsappValidation.message);
            isValid = false;
        } else {
            clearFieldError(whatsapp);
        }
    }

    // Email
    const email = document.getElementById('email');
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (email && !emailRegex.test(email.value)) {
        setFieldError(email, 'E-mail inválido');
        isValid = false;
    } else if (email) {
        clearFieldError(email);
    }

    // Password
    const password = document.getElementById('password');
    if (password && password.value.length < 8) {
        setFieldError(password, 'Senha deve ter pelo menos 8 caracteres');
        isValid = false;
    } else if (password && (!/[A-Z]/.test(password.value) || !/\d/.test(password.value))) {
        setFieldError(password, 'Senha deve conter letra maiúscula e número');
        isValid = false;
    } else if (password) {
        clearFieldError(password);
    }

    // Password Confirm
    const passwordConfirm = document.getElementById('passwordConfirm');
    if (passwordConfirm && password && passwordConfirm.value !== password.value) {
        setFieldError(passwordConfirm, 'Senhas não conferem');
        isValid = false;
    } else if (passwordConfirm) {
        clearFieldError(passwordConfirm);
    }

    // Accept Terms
    const acceptTerms = document.getElementById('acceptTerms');
    if (acceptTerms && !acceptTerms.checked) {
        showAlert('warning', 'Você deve aceitar os termos de uso.');
        isValid = false;
    }

    return isValid;
}

// Pesos do Módulo 11 do CNPJ (IN RFB nº 2.229/2024 - alfanumérico).
const CNPJ_PESOS = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

// Normaliza para o formato canônico: remove máscara e converte para MAIÚSCULO.
// Preserva letras (suporta CNPJ alfanumérico); retrocompatível com numérico.
function normalizeCNPJ(value) {
    return (value || '').toUpperCase().replace(/[^A-Z0-9]/g, '');
}

// Calcula um dígito verificador para uma base de 12 ou 13 caracteres.
// valor do caractere = ASCII - 48 ('0'..'9' => 0..9, 'A'..'Z' => 17..42).
function cnpjDV(base) {
    const pesos = CNPJ_PESOS.slice(CNPJ_PESOS.length - base.length);
    let soma = 0;
    for (let i = 0; i < base.length; i++) {
        soma += (base.charCodeAt(i) - 48) * pesos[i];
    }
    const resto = soma % 11;
    return String(resto < 2 ? 0 : 11 - resto);
}

function isValidCNPJ(cnpj) {
    cnpj = normalizeCNPJ(cnpj);

    // 12 posições alfanuméricas (raiz + ordem) + 2 dígitos verificadores numéricos.
    if (!/^[A-Z0-9]{12}[0-9]{2}$/.test(cnpj)) return false;

    // Rejeita sequências repetidas (regra de negócio herdada da validação numérica).
    const base = cnpj.slice(0, 12);
    if (new Set(base).size === 1) return false;

    const d1 = cnpjDV(base);
    const d2 = cnpjDV(base + d1);

    return cnpj.slice(12) === d1 + d2;
}

function validatePasswordMatch() {
    const password = document.getElementById('password');
    const passwordConfirm = document.getElementById('passwordConfirm');

    if (!password || !passwordConfirm) return;

    if (passwordConfirm.value && passwordConfirm.value !== password.value) {
        setFieldError(passwordConfirm, 'Senhas não conferem');
    } else {
        clearFieldError(passwordConfirm);
    }
}

function updatePasswordStrength(password) {
    const strengthBar = document.getElementById('passwordStrength');
    const strengthText = document.getElementById('strengthText');

    if (!strengthBar || !strengthText) return;

    let strength = 0;

    if (password.length >= 8) strength++;
    if (password.length >= 10) strength++;
    if (password.length >= 12) strength++;
    if (/[a-z]/.test(password) && /[A-Z]/.test(password)) strength++;
    if (/\d/.test(password)) strength++;
    if (/[^a-zA-Z0-9]/.test(password)) strength++;

    const percentage = (strength / 6) * 100;
    strengthBar.style.width = percentage + '%';

    if (percentage < 40) {
        strengthBar.className = 'progress-bar bg-danger';
        strengthText.textContent = 'Fraca';
        strengthText.className = 'text-danger small';
    } else if (percentage < 70) {
        strengthBar.className = 'progress-bar bg-warning';
        strengthText.textContent = 'Média';
        strengthText.className = 'text-warning small';
    } else {
        strengthBar.className = 'progress-bar bg-success';
        strengthText.textContent = 'Forte';
        strengthText.className = 'text-success small';
    }
}

function validateCNPJField(field) {
    if (isValidCNPJ(field.value)) {
        field.classList.remove('is-invalid');
        field.classList.add('is-valid');
    } else {
        field.classList.remove('is-valid');
        if (field.value.length === 18) { // Fully formatted
            field.classList.add('is-invalid');
        }
    }
}

function validatePhoneField(field) {
    const cleaned = field.value.replace(/\D/g, '');
    const validation = isValidWhatsApp(cleaned);
    
    if (validation.valid) {
        field.classList.remove('is-invalid');
        field.classList.add('is-valid');
        clearFieldError(field);
    } else {
        field.classList.remove('is-valid');
        if (cleaned.length >= 2) {
            field.classList.add('is-invalid');
            setFieldError(field, validation.message);
        }
    }
}

function isValidWhatsApp(phone) {
    phone = phone.replace(/\D/g, '');
    
    // Deve ter 10 ou 11 dígitos
    if (phone.length < 10 || phone.length > 11) {
        return { valid: false, message: 'WhatsApp deve ter 10 ou 11 dígitos' };
    }
    
    // Não pode ser número repetido
    if (/^(\d)\1+$/.test(phone)) {
        return { valid: false, message: 'Número de WhatsApp inválido' };
    }
    
    // Validar DDD (11-99, exceto inválidos)
    const ddd = parseInt(phone.substring(0, 2));
    const dddsValidos = [
        11, 12, 13, 14, 15, 16, 17, 18, 19, // SP
        21, 22, 24, // RJ
        27, 28, // ES
        31, 32, 33, 34, 35, 37, 38, // MG
        41, 42, 43, 44, 45, 46, // PR
        47, 48, 49, // SC
        51, 53, 54, 55, // RS
        61, // DF
        62, 64, // GO
        63, // TO
        65, 66, // MT
        67, // MS
        68, // AC
        69, // RO
        71, 73, 74, 75, 77, // BA
        79, // SE
        81, 82, 83, 84, 85, 86, 87, 88, 89, // PE, AL, PB, RN, CE, PI
        91, 92, 93, 94, 95, 96, 97, 98, 99  // PA, AM, MA
    ];
    
    if (!dddsValidos.includes(ddd)) {
        return { valid: false, message: `DDD ${ddd} inválido` };
    }
    
    // Celular com 11 dígitos deve começar com 9 após o DDD
    if (phone.length === 11 && phone.charAt(2) !== '9') {
        return { valid: false, message: 'Celular deve começar com 9' };
    }
    
    // Não pode começar com 0 após o DDD
    if (phone.charAt(2) === '0') {
        return { valid: false, message: 'Número inválido após o DDD' };
    }
    
    return { valid: true, message: '' };
}

// ============================================
// FORMATTING FUNCTIONS
// ============================================

function formatCNPJ(value) {
    // Posições 1-12 aceitam [A-Z0-9]; os 2 DVs finais só dígitos.
    value = normalizeCNPJ(value);
    value = value.substring(0, 14);

    if (value.length > 12) {
        value = value.replace(/^([A-Z0-9]{2})([A-Z0-9]{3})([A-Z0-9]{3})([A-Z0-9]{4})(\d{2})/, '$1.$2.$3/$4-$5');
    } else if (value.length > 8) {
        value = value.replace(/^([A-Z0-9]{2})([A-Z0-9]{3})([A-Z0-9]{3})([A-Z0-9]{0,4})/, '$1.$2.$3/$4');
    } else if (value.length > 5) {
        value = value.replace(/^([A-Z0-9]{2})([A-Z0-9]{3})([A-Z0-9]{0,3})/, '$1.$2.$3');
    } else if (value.length > 2) {
        value = value.replace(/^([A-Z0-9]{2})([A-Z0-9]{0,3})/, '$1.$2');
    }

    return value;
}

function formatPhone(value) {
    value = value.replace(/\D/g, '');
    value = value.substring(0, 11);

    if (value.length > 10) {
        value = value.replace(/^(\d{2})(\d{5})(\d{4})/, '($1) $2-$3');
    } else if (value.length > 6) {
        value = value.replace(/^(\d{2})(\d{4})(\d{0,4})/, '($1) $2-$3');
    } else if (value.length > 2) {
        value = value.replace(/^(\d{2})(\d{0,5})/, '($1) $2');
    }

    return value;
}

// ============================================
// UI HELPERS
// ============================================

function setFieldError(field, message) {
    field.classList.add('is-invalid');
    field.classList.remove('is-valid');
    field.style.borderColor = '#dc3545';
    field.style.boxShadow = '0 0 0 3px rgba(220,53,69,.15)';

    let feedback = field.parentNode.querySelector('.field-error-msg');
    if (!feedback) {
        feedback = document.createElement('div');
        feedback.className = 'field-error-msg';
        field.parentNode.appendChild(feedback);
    }
    feedback.style.cssText = 'color:#dc3545;font-size:12px;margin-top:4px;font-weight:500;';
    feedback.textContent = message;
}

function clearFieldError(field) {
    field.classList.remove('is-invalid');
    field.style.borderColor = '';
    field.style.boxShadow = '';
    const feedback = field.parentNode.querySelector('.field-error-msg');
    if (feedback) feedback.remove();
}

function showAlert(type, message) {
    // Remove alertas existentes
    const existingAlerts = document.querySelectorAll('.signup-alert');
    existingAlerts.forEach(alert => alert.remove());

    // Criar novo alerta
    const alert = document.createElement('div');
    alert.className = `alert alert-${type} signup-alert alert-dismissible fade show`;
    alert.style.cssText = 'margin-bottom: 1rem; border-radius: 8px;';
    
    const icon = type === 'success' ? 'check-circle' : 
                 type === 'danger' ? 'exclamation-circle' : 
                 type === 'warning' ? 'exclamation-triangle' : 'info-circle';
    
    alert.innerHTML = `
        <i class="bi bi-${icon}-fill me-2"></i>${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    // Inserir no início do formulário ativo
    const form = document.getElementById('signupForm') || 
                 document.getElementById('verifyForm') || 
                 document.getElementById('completeProfileForm');
    
    if (form) {
        form.insertBefore(alert, form.firstChild);
        // Scroll para o alerta
        alert.scrollIntoView({ behavior: 'smooth', block: 'center' });
    } else {
        // Fallback: adicionar ao body como flutuante
        alert.style.cssText = `
            position: fixed;
            top: 20px;
            left: 50%;
            transform: translateX(-50%);
            z-index: 9999;
            min-width: 300px;
            max-width: 90%;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        `;
        document.body.appendChild(alert);
    }

    // Auto-remover após 6 segundos
    setTimeout(() => {
        alert.classList.remove('show');
        setTimeout(() => alert.remove(), 300);
    }, 6000);
}
