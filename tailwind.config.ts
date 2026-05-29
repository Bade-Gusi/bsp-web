/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        // 品牌色
        primary: { DEFAULT: '#22D97E', 50: '#E6FBF0', 100: '#C2F4DB', 200: '#8BEBB7', 300: '#4DE093', 400: '#22D97E', 500: '#1AB865', 600: '#12964E', 700: '#0B7438', 800: '#055226', 900: '#023014' },
        accent: { DEFAULT: '#06D8A0', 50: '#E0FBF4', 100: '#B3F5E3', 200: '#80EDCF', 300: '#4DE5BA', 400: '#26DDA9', 500: '#06D8A0', 600: '#05B888', 700: '#049870', 800: '#037858', 900: '#025840' },
        purple: { DEFAULT: '#A855F7', 50: '#F3E8FF', 100: '#E0C6FF', 200: '#C89DFF', 300: '#B074FF', 400: '#A855F7', 500: '#9333EA', 600: '#7C22CE', 700: '#6519A8', 800: '#4E1282', 900: '#370B5C' },
        // 背景色 (暗色主题)
        surface: { DEFAULT: '#0B0E0F', 50: '#F2F5F3', 100: '#DEE3DF', 200: '#C0CCBF', 300: '#9EAE9F', 400: '#7C8C80', 500: '#5B6B5F', 600: '#3C4B40', 700: '#262D30', 800: '#1C2123', 900: '#14181A', 950: '#0B0E0F' },
        card: '#14181A',
        elevated: '#1C2123',
        hover: '#262D30',
        border: '#2A3330',
      },
      fontFamily: {
        sans: ['"Microsoft YaHei"', '"Noto Sans SC"', 'system-ui', 'sans-serif'],
        mono: ['"JetBrains Mono"', '"Cascadia Code"', 'monospace'],
      },
      borderRadius: {
        sm: '8px',
        md: '12px',
        lg: '16px',
        xl: '24px',
        full: '999px',
      },
      boxShadow: {
        glow: '0 0 20px rgba(34, 217, 126, 0.35)',
        'glow-lg': '0 0 28px rgba(34, 217, 126, 0.55)',
        card: '0 4px 20px rgba(0,0,0,0.45)',
        surface: '0 8px 40px rgba(0,0,0,0.55)',
      },
      animation: {
        'pulse-glow': 'pulseGlow 2s ease-in-out infinite',
        'fade-in': 'fadeIn 0.4s cubic-bezier(0.16, 1, 0.3, 1)',
        'slide-up': 'slideUp 0.5s cubic-bezier(0.16, 1, 0.3, 1)',
        'scale-in': 'scaleIn 0.4s cubic-bezier(0.34, 1.56, 0.64, 1)',
        'float': 'float 4s ease-in-out infinite',
        'aurora': 'aurora 3s ease-in-out infinite',
        'shimmer': 'shimmer 2s ease-in-out infinite',
      },
      keyframes: {
        pulseGlow: { '0%, 100%': { boxShadow: '0 0 20px rgba(34, 217, 126, 0.3)' }, '50%': { boxShadow: '0 0 28px rgba(34, 217, 126, 0.6)' } },
        fadeIn: { from: { opacity: '0', transform: 'translateY(8px)' }, to: { opacity: '1', transform: 'translateY(0)' } },
        slideUp: { from: { opacity: '0', transform: 'translateY(24px)' }, to: { opacity: '1', transform: 'translateY(0)' } },
        scaleIn: { from: { opacity: '0', transform: 'scale(0.95)' }, to: { opacity: '1', transform: 'scale(1)' } },
        float: { '0%, 100%': { transform: 'translateY(0)' }, '50%': { transform: 'translateY(-8px)' } },
        aurora: { '0%, 100%': { opacity: '0.4' }, '50%': { opacity: '0.8' } },
        shimmer: { '0%, 100%': { opacity: '0.4' }, '50%': { opacity: '0.8' } },
      },
    },
  },
  plugins: [],
}
