import { defineStore } from 'pinia';
import { getJson } from '../services/apiClient';
import { useAuthStore } from './auth';

const STOCK_ALERT_PRIORITY = {
  BAJO: 1,
  MEDIO: 2,
  CRITICO: 3
};

export const normalizeStockAlertLevel = (value) => {
  const normalized = String(value || '').trim().toUpperCase();
  return STOCK_ALERT_PRIORITY[normalized] ? normalized : '';
};

export const getHighestStockAlertLevel = (items) =>
  (items || []).reduce((highest, item) => {
    const current = normalizeStockAlertLevel(item?.nivel);
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
      return { label: 'Bajo', color: 'warning' };
    default:
      return { label: '', color: 'info' };
  }
};

export const useStockAlertsStore = defineStore('stockAlerts', {
  state: () => ({
    count: 0,
    highestLevel: '',
    loading: false
  }),
  getters: {
    hasAlerts: (state) => state.count > 0 && !!state.highestLevel,
    chipMeta: (state) => getStockAlertMeta(state.highestLevel),
    chipLabel() {
      if (!this.hasAlerts) return '';
      return `${this.chipMeta.label} ${this.count}`;
    }
  },
  actions: {
    reset() {
      this.count = 0;
      this.highestLevel = '';
      this.loading = false;
    },
    setFromAlerts(items) {
      const alerts = Array.isArray(items) ? items : [];
      this.count = alerts.length;
      this.highestLevel = getHighestStockAlertLevel(alerts);
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
