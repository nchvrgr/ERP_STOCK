import { defineStore } from 'pinia';
import { getJson } from '../services/apiClient';
import { useAuthStore } from './auth';

const STOCK_ALERT_PRIORITY = {
  BAJO: 1,
  MEDIO: 2,
  CRITICO: 3
};
const STOCK_ALERT_LEVELS_DESC = ['CRITICO', 'MEDIO', 'BAJO'];
const createEmptyStockAlertCounts = () => ({ CRITICO: 0, MEDIO: 0, BAJO: 0 });

export const normalizeStockAlertLevel = (value) => {
  const normalized = String(value || '').trim().toUpperCase();
  return STOCK_ALERT_PRIORITY[normalized] ? normalized : '';
};

export const resolveStockAlertLevel = (item) => {
  const minimo = Number(item?.stockMinimo ?? 0);
  const actual = Number(item?.cantidadActual ?? 0);
  if (Number.isNaN(minimo) || minimo <= 0 || Number.isNaN(actual)) return '';

  if (actual < minimo) return 'CRITICO';
  if (actual === minimo) return 'MEDIO';

  const tolerancia = Math.max(Number(item?.toleranciaPct ?? 0), 0);
  const limite = minimo * (1 + tolerancia / 100);
  if (actual <= limite) {
    const nivelBackend = normalizeStockAlertLevel(item?.nivel);
    if (nivelBackend === 'MEDIO' || nivelBackend === 'BAJO') return nivelBackend;
    return 'BAJO';
  }

  return '';
};

export const getHighestStockAlertLevel = (items) =>
  (items || []).reduce((highest, item) => {
    const current = resolveStockAlertLevel(item);
    if (!current) return highest;
    if (!highest) return current;
    return STOCK_ALERT_PRIORITY[current] > STOCK_ALERT_PRIORITY[highest] ? current : highest;
  }, '');

export const getStockAlertMeta = (level) => {
  switch (normalizeStockAlertLevel(level)) {
    case 'CRITICO':
      return { label: 'Crítico', color: 'error' };
    case 'MEDIO':
      return { label: 'Medio', color: 'warning' };
    case 'BAJO':
      return { label: 'Bajo', color: 'info' };
    default:
      return { label: '', color: 'info' };
  }
};

export const getStockAlertMissingUnits = (item) => {
  const minimo = Number(item?.stockMinimo ?? 0);
  const actual = Number(item?.cantidadActual ?? 0);
  if (Number.isNaN(minimo) || Number.isNaN(actual)) return 0;
  return Math.max(minimo - actual, 0);
};

export const getStockAlertDistanceToMinimum = (item) => {
  const minimo = Number(item?.stockMinimo ?? 0);
  const actual = Number(item?.cantidadActual ?? 0);
  if (Number.isNaN(minimo) || Number.isNaN(actual)) return 0;
  return Math.max(actual - minimo, 0);
};

export const isApproachingStockAlertLevel = (level) => {
  const normalized = normalizeStockAlertLevel(level);
  return normalized === 'BAJO' || normalized === 'MEDIO';
};

export const isActionableStockAlert = (item) => {
  return !!resolveStockAlertLevel(item);
};

const formatStockAlertCount = (count) => (count > 99 ? '99+' : String(count || 0));

export const buildStockAlertBadges = (countsByLevel) =>
  STOCK_ALERT_LEVELS_DESC
    .filter((level) => Number(countsByLevel?.[level] ?? 0) > 0)
    .map((level) => {
      const count = Number(countsByLevel[level] ?? 0);
      return {
        level,
        count,
        countLabel: formatStockAlertCount(count),
        ...getStockAlertMeta(level)
      };
    });

export const summarizeStockAlerts = (items) => {
  const countsByLevel = createEmptyStockAlertCounts();
  for (const item of Array.isArray(items) ? items : []) {
    const level = resolveStockAlertLevel(item);
    if (level) countsByLevel[level] += 1;
  }

  const highestLevel = STOCK_ALERT_LEVELS_DESC.find((level) => countsByLevel[level] > 0) || '';
  const count = STOCK_ALERT_LEVELS_DESC.reduce((acc, level) => acc + countsByLevel[level], 0);
  const badges = buildStockAlertBadges(countsByLevel);

  return { count, highestLevel, countsByLevel, badges };
};

export const useStockAlertsStore = defineStore('stockAlerts', {
  state: () => ({
    count: 0,
    highestLevel: '',
    countsByLevel: createEmptyStockAlertCounts(),
    loading: false
  }),
  getters: {
    hasAlerts: (state) => state.count > 0 && !!state.highestLevel,
    chipMeta: (state) => getStockAlertMeta(state.highestLevel),
    badges: (state) => buildStockAlertBadges(state.countsByLevel),
    chipLabel() {
      if (!this.hasAlerts) return '';
      return `${this.chipMeta.label} ${this.count}`;
    }
  },
  actions: {
    reset() {
      this.count = 0;
      this.highestLevel = '';
      this.countsByLevel = createEmptyStockAlertCounts();
      this.loading = false;
    },
    setFromAlerts(items) {
      const summary = summarizeStockAlerts(items);
      this.count = summary.count;
      this.highestLevel = summary.highestLevel;
      this.countsByLevel = summary.countsByLevel;
    },
    async refreshSummary() {
      const auth = useAuthStore();
      if (!auth.isAuthenticated || !auth.hasPermission('PERM_STOCK_AJUSTAR')) {
        this.reset();
        return;
      }

      this.loading = true;
      try {
        const { response, data } = await getJson('/api/v1/stock/alertas');
        if (response.ok) {
          this.setFromAlerts(data || []);
        }
      } catch {
        // Preserve the last known alert state if the refresh fails.
      } finally {
        this.loading = false;
      }
    }
  }
});
