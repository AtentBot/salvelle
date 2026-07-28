// Salvelle Design System (mobile)
// Identidade Salvelle — paleta editorial-clínica: petrol + bone + âmbar
// Fonte da verdade web: wwwroot/css/design-system/tokens.css

export const COLORS = {
  // Primária Salvelle — Petróleo
  primary: '#0F6E78',
  primaryDark: '#0A5C66',
  primaryDarker: '#095861',
  primaryLight: '#9FC8C5',
  primarySoft: '#E6F2F1',
  primaryMuted: 'rgba(15, 110, 120, 0.12)',

  // Acento Salvelle — Âmbar (CTA, alertas, marca "Salvelle")
  accent: '#D88B2C',
  accentDark: '#9A5E15',
  accentLight: '#F5DDB0',
  accentSoft: '#FBF1E0',

  // Inks (texto sobre bone)
  ink: '#0A1814',
  ink2: '#364946',
  ink3: '#6B7B78',
  ink4: '#94A19E',
  ink5: '#C8D0CD',

  // Aliases para compat com screens existentes
  text: '#0A1814',
  textSecondary: '#364946',
  textMuted: '#6B7B78',

  // Status (Salvelle)
  success: '#15803D',
  successLight: '#DCFCE7',
  warning: '#B45309',
  warningLight: '#FEF3C7',
  error: '#B91C1C',
  errorLight: '#FEE2E2',

  // Surfaces — bone caloroso
  white: '#FFFFFF',
  background: '#FAF9F5',     // bone
  backgroundAlt: '#F2F1EC',  // bg-subtle
  surface: '#FFFFFF',
  surface2: '#FBFAF6',
  cardGlass: 'rgba(255, 255, 255, 0.92)',

  // Bordas (rules)
  border: '#E5E8E5',
  borderLight: '#F2F1EC',
  borderStrong: '#D6DAD6',
  borderFocus: '#0F6E78',
};

export const GRADIENTS = {
  background: ['#FAF9F5', '#F2F1EC', '#FAF9F5'],
  primary: ['#0F6E78', '#0A5C66'],
  primarySoft: ['#E6F2F1', '#C7E0DE'],
  accent: ['#D88B2C', '#9A5E15'],
  success: ['#15803D', '#0F6E2D'],
  promo: ['#0F6E78', '#095861'],
  dark: ['#0A1814', '#042A2D'],
  darkTeal: ['#0F6E78', '#042A2D'],
  orderAvatar: ['#0F6E78', '#0A5C66'],
  splash: ['#0F6E78', '#095861', '#042A2D'],
  hero: ['#0F6E78', '#0A5C66'],
};

export const SPACING = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  xxl: 24,
  xxxl: 32,
  xxxxl: 48,
};

export const BORDER_RADIUS = {
  xs: 4,
  sm: 6,
  md: 10,
  lg: 14,
  xl: 20,
  xxl: 24,
  full: 999,
};

export const FONT_SIZES = {
  xxs: 10,
  xs: 11,
  sm: 13,
  md: 15,
  lg: 17,
  xl: 20,
  xxl: 24,
  xxxl: 32,
  logo: 28,
  hero: 36,
};

// Sombras calibradas pra fundo bone (Salvelle)
export const SHADOWS = {
  small: {
    shadowColor: '#0A1814',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.04,
    shadowRadius: 2,
    elevation: 1,
  },
  medium: {
    shadowColor: '#0A1814',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.10,
    shadowRadius: 24,
    elevation: 4,
  },
  large: {
    shadowColor: '#0A1814',
    shadowOffset: { width: 0, height: 24 },
    shadowOpacity: 0.18,
    shadowRadius: 48,
    elevation: 12,
  },
  card: {
    shadowColor: '#0A1814',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06,
    shadowRadius: 8,
    elevation: 2,
  },
  button: {
    shadowColor: '#0F6E78',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.25,
    shadowRadius: 12,
    elevation: 6,
  },
  colored: (color) => ({
    shadowColor: color,
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.35,
    shadowRadius: 12,
    elevation: 8,
  }),
  glow: (color) => ({
    shadowColor: color,
    shadowOffset: { width: 0, height: 0 },
    shadowOpacity: 0.3,
    shadowRadius: 16,
    elevation: 6,
  }),
};

export default {
  COLORS,
  GRADIENTS,
  SPACING,
  BORDER_RADIUS,
  FONT_SIZES,
  SHADOWS,
};
