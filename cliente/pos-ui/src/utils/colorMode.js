import { applyPaletteVariables, getThemeNameForMode } from '../theme/palette';

export const POS_COLOR_MODE_STORAGE_KEY = 'pos-color-mode';
export const POS_COLOR_MODE_LIGHT = 'light';
export const POS_COLOR_MODE_DARK = 'dark';

export const getStoredColorMode = () => {
  if (typeof window === 'undefined') return POS_COLOR_MODE_LIGHT;

  const stored = window.localStorage.getItem(POS_COLOR_MODE_STORAGE_KEY);
  return stored === POS_COLOR_MODE_DARK ? POS_COLOR_MODE_DARK : POS_COLOR_MODE_LIGHT;
};

export const getThemeName = (mode) => (
  getThemeNameForMode(mode)
);

export const applyColorMode = (mode, theme) => {
  const nextMode = mode === POS_COLOR_MODE_DARK ? POS_COLOR_MODE_DARK : POS_COLOR_MODE_LIGHT;

  if (typeof window !== 'undefined') {
    window.localStorage.setItem(POS_COLOR_MODE_STORAGE_KEY, nextMode);
  }

  if (typeof document !== 'undefined') {
    document.documentElement.setAttribute('data-pos-theme', nextMode);
    document.documentElement.style.colorScheme = nextMode;
    applyPaletteVariables(nextMode, document.documentElement);
  }

  if (theme?.global?.name) {
    theme.global.name.value = getThemeName(nextMode);
  }

  return nextMode;
};
