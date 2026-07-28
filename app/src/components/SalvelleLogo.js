import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { COLORS, FONT_SIZES } from '../constants/theme';

/**
 * Salvelle brand mark — letra "S" (marca reduzida, serif) + wordmark "SALVELLE"
 * em serif (Cormorant Garamond quando disponível), caixa alta, peso 600
 */
const SalvelleLogo = ({
  size = 28,
  color = COLORS.primary,
  accent = COLORS.accent,
  textColor,
  inverted = false,
  showText = true,
  style,
}) => {
  const glyphColor = color;
  const finalTextColor = textColor || (inverted ? COLORS.white : COLORS.ink);

  return (
    <View style={[styles.row, style]}>
      <Text style={[styles.mark, { color: glyphColor, fontSize: size }]}>S</Text>
      {showText && (
        <Text
          style={[
            styles.brandName,
            { color: finalTextColor, marginLeft: size * 0.3, fontSize: size * 0.72 },
          ]}
        >
          Salvelle
        </Text>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  mark: {
    fontFamily: 'serif',
    fontWeight: '600',
  },
  brandName: {
    fontFamily: 'serif',
    fontWeight: '600',
    letterSpacing: 2,
    textTransform: 'uppercase',
  },
});

export default SalvelleLogo;
