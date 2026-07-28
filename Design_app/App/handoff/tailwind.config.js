/**
 * Salvelle — Tailwind config (handoff for devs)
 * Drop into tailwind.config.js (or merge into your existing theme).
 * Keeps token names in sync with ds/tokens.css.
 */
module.exports = {
  theme: {
    extend: {
      colors: {
        petrol: {
          50:  '#E6F2F1',
          100: '#C7E0DE',
          200: '#9FC8C5',
          300: '#6FAAA9',
          400: '#338A87',
          500: '#0F6E78', // primary
          600: '#0A5C66',
          700: '#095861', // deep
          800: '#074044',
          900: '#042A2D',
        },
        amber: {
          50:  '#FBF1E0',
          100: '#F5DDB0',
          300: '#E5A857',
          500: '#D88B2C', // accent
          700: '#9A5E15',
        },
        ink: {
          DEFAULT: '#0A1814',
          2: '#364946',
          3: '#6B7B78',
          4: '#94A19E',
          5: '#C8D0CD',
        },
        bone:    '#FAF9F5',
        rule:    '#E5E8E5',
        surface: '#FFFFFF',
      },
      fontFamily: {
        sans:    ['"General Sans"', 'Inter Tight', 'system-ui', 'sans-serif'],
        display: ['"Cabinet Grotesk"', 'sans-serif'],
        mono:    ['"JetBrains Mono"', 'ui-monospace', 'monospace'],
      },
      fontSize: {
        mono:    '12px',
        xs:      '12px',
        sm:      '13px',
        base:    '15px',
        md:      '17px',
        lg:      '20px',
        h3:      '24px',
        h2:      '32px',
        h1:      '44px',
        display: '60px',
      },
      borderRadius: {
        xs:   '4px',
        sm:   '6px',
        md:   '10px',
        lg:   '14px',
        xl:   '20px',
        pill: '999px',
      },
      boxShadow: {
        1: '0 1px 2px rgba(10,24,20,.04), 0 0 0 1px rgba(10,24,20,.04)',
        2: '0 1px 2px rgba(10,24,20,.06), 0 8px 24px -8px rgba(10,24,20,.10)',
        3: '0 2px 4px rgba(10,24,20,.06), 0 24px 48px -12px rgba(10,24,20,.18)',
        focus: '0 0 0 3px #E6F2F1',
      },
    },
  },
};
