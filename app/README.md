# 📱 Salvelle App

App React Native + Expo para farmácias de manipulação.

## 🚀 Como usar

### 1. Pelo Replit (recomendado para começar)
1. Acesse replit.com
2. Crie um novo Repl → Template "React Native (Expo)"
3. Substitua o conteúdo pelos arquivos deste projeto
4. Clique em "Run"

### 2. Local
```bash
# Instalar dependências
npm install

# Iniciar
npx expo start

# Escanear QR code com Expo Go no celular
```

## 📁 Estrutura

```
├── App.js              # Entry point
├── app.json            # Configuração Expo
├── package.json        # Dependências
├── src/
│   ├── components/     # Componentes reutilizáveis
│   ├── screens/        # Telas do app
│   │   ├── LoginScreen.js
│   │   ├── RegisterScreen.js
│   │   ├── VerifyCodeScreen.js
│   │   ├── HomeScreen.js
│   │   ├── PrescriptionScreen.js
│   │   ├── FormulaScreen.js
│   │   ├── CartScreen.js
│   │   ├── OrdersScreen.js
│   │   └── ProfileScreen.js
│   ├── services/       # API e Storage
│   ├── hooks/          # useAuth, useBiometrics
│   ├── navigation/     # React Navigation
│   ├── constants/      # Theme e Config
│   └── utils/          # Formatters
└── assets/             # Ícones e imagens
```

## 🔧 Configuração da API

Edite `src/constants/config.js`:
```javascript
export const API_URL = 'https://salvelle.atentbot.com/api';
```

## 📱 Funcionalidades

- ✅ Login com CPF + Senha
- ✅ Verificação WhatsApp 2FA
- ✅ Biometria (Face ID / Digital)
- ✅ Envio de Receita com Câmera
- ✅ Fórmula Personalizada
- ✅ Carrinho de Compras
- ✅ Lista de Pedidos
- ✅ Perfil do Usuário

## 🎨 Assets necessários

Substitua os placeholders em `/assets/`:
- `icon.png` - Ícone do app (1024x1024)
- `splash.png` - Tela de splash
- `adaptive-icon.png` - Ícone Android adaptativo

## 📦 Build para Stores

```bash
# Instalar EAS CLI
npm install -g eas-cli

# Login
eas login

# Build Android
eas build --platform android

# Build iOS
eas build --platform ios
```

## 📞 Suporte

Douglas Braga Builder
Salvelle - 2025
