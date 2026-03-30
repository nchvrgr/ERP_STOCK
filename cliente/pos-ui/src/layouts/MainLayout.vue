<template>
  <v-layout>
    <v-navigation-drawer
      v-model="drawer"
      :permanent="mdAndUp"
      width="260"
      class="pos-drawer"
    >
      <div class="d-flex flex-column h-100">
        <div class="pa-4">
          <div class="d-flex align-center gap-2">
            <v-icon size="28" color="primary">mdi-storefront</v-icon>
            <div>
              <div class="text-subtitle-1 drawer-brand-title">Viñedos de la Villa</div>
              <div class="text-caption drawer-brand-subtitle">Gestión comercial</div>
            </div>
          </div>
        </div>
        <v-divider />
        <v-list density="comfortable" nav>
          <v-list-item
            v-for="item in visibleItems"
            :key="item.to"
            :to="item.to"
            :prepend-icon="item.icon"
            :title="item.title"
            rounded
          >
            <template #append>
              <span
                v-if="isStockMenuItem(item) && stockAlerts.hasAlerts"
                :class="['stock-alert-dot', `stock-alert-dot--${stockAlertMeta.color}`]"
                :title="stockAlertMeta.label"
                :aria-label="stockAlertMeta.label"
              ></span>
            </template>
          </v-list-item>
        </v-list>
        <v-spacer />
        <v-divider />
        <div class="pa-2">
          <v-list density="comfortable" nav>
            <v-list-item
              v-if="auth.hasPermission('PERM_PRODUCTO_VER')"
              to="/empresa"
              prepend-icon="mdi-domain"
              title="Datos de mi empresa"
              rounded
            />
          </v-list>
        </div>
      </div>
    </v-navigation-drawer>

    <v-app-bar flat color="transparent" class="pos-app-bar">
      <v-btn
        icon="mdi-menu"
        class="d-md-none"
        @click="drawer = !drawer"
        aria-label="Toggle menu"
      />
      <div class="d-flex align-center gap-2">
        <div class="text-h6">{{ currentTitle }}</div>
        <v-chip color="primary" variant="tonal" size="small">Local</v-chip>
      </div>
      <v-spacer />
      <div class="d-flex align-center gap-3">
        <div class="theme-toggle">
          <span class="text-caption theme-toggle-label">Modo nocturno</span>
          <v-switch
            v-model="isDarkMode"
            hide-details
            density="compact"
            color="primary"
            inset
            class="theme-toggle-switch"
          />
        </div>
        <v-btn variant="tonal" color="primary" class="text-none" @click="logout">
          Logout
        </v-btn>
      </div>
    </v-app-bar>

    <v-main>
      <v-container class="app-shell">
        <router-view />
      </v-container>
    </v-main>
  </v-layout>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useDisplay, useTheme } from 'vuetify';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { applyColorMode, POS_COLOR_MODE_DARK, POS_COLOR_MODE_LIGHT } from '../utils/colorMode';
import { useStockAlertsStore } from '../stores/stockAlerts';

const auth = useAuthStore();
const stockAlerts = useStockAlertsStore();
const router = useRouter();
const route = useRoute();
const drawer = ref(true);
const { mdAndUp } = useDisplay();
const theme = useTheme();

const menuItems = [
  {
    title: 'Caja',
    icon: 'mdi-cash-register',
    to: '/caja',
    path: '/caja',
    access: () =>
      auth.hasPermission('PERM_VENTA_CREAR') ||
      auth.hasPermission('PERM_CAJA_MOVIMIENTO') ||
      hasAnyRole(['ENCARGADO', 'ADMIN'])
  },
  {
    title: 'Productos',
    icon: 'mdi-package-variant',
    to: '/productos',
    path: '/productos',
    permission: 'PERM_PRODUCTO_VER'
  },
  {
    title: 'Ingreso',
    icon: 'mdi-file-document-outline',
    to: '/remitos',
    permission: 'PERM_COMPRAS_REGISTRAR'
  },
  {
    title: 'Stock',
    icon: 'mdi-warehouse',
    to: '/stock',
    permission: 'PERM_STOCK_AJUSTAR'
  },
  {
    title: 'Reportes',
    icon: 'mdi-chart-areaspline',
    to: '/reportes',
    permission: 'PERM_REPORTES_VER'
  }
];

const hasAnyRole = (roles) => roles.some((role) => auth.roles.includes(role));

const visibleItems = computed(() =>
  menuItems.filter((item) => {
    if (typeof item.access === 'function') {
      return item.access();
    }
    const hasPerm = item.permission ? auth.hasPermission(item.permission) : false;
    const hasRole = item.roles ? hasAnyRole(item.roles) : false;
    if (!item.permission && !item.roles) return true;
    return hasPerm || hasRole;
  })
);

const currentTitle = computed(() => {
  if (route.path.startsWith('/productos') && route.query.tab === 'proveedores') {
    return 'Proveedores';
  }
  if (route.path.startsWith('/productos') && route.query.tab === 'listas-precio') {
    return 'Categorías';
  }
  if (route.path.startsWith('/empresa')) {
    return 'Datos de mi empresa';
  }

  const match = menuItems.find((item) => {
    const path = item.path || item.to;
    return typeof path === 'string' && route.path.startsWith(path);
  });
  return match ? match.title : 'Caja';
});

const stockAlertMeta = computed(() => stockAlerts.chipMeta);

const isDarkMode = computed({
  get: () => theme.global.name.value === 'posNightTheme',
  set: (value) => {
    applyColorMode(value ? POS_COLOR_MODE_DARK : POS_COLOR_MODE_LIGHT, theme);
  }
});

const isStockMenuItem = (item) => (item?.path || item?.to) === '/stock';

const refreshStockAlerts = () => {
  if (!auth.hasPermission('PERM_STOCK_AJUSTAR')) {
    stockAlerts.reset();
    return;
  }
  stockAlerts.refreshSummary();
};

watch(
  () => route.path,
  (path) => {
    if (path.startsWith('/stock')) {
      refreshStockAlerts();
    }
  }
);

onMounted(() => {
  window.addEventListener('focus', refreshStockAlerts);
  refreshStockAlerts();
});

onBeforeUnmount(() => {
  window.removeEventListener('focus', refreshStockAlerts);
});

const logout = () => {
  stockAlerts.reset();
  auth.logout();
  router.replace('/login');
};
</script>

<style scoped>
.pos-drawer {
  border-right: 1px solid var(--pos-border);
  background:
    linear-gradient(180deg, var(--pos-drawer-top) 0%, var(--pos-drawer-bottom) 100%),
    linear-gradient(135deg, var(--pos-drawer-overlay-a), var(--pos-drawer-overlay-b));
}

.pos-app-bar {
  padding: 8px 16px;
  position: sticky;
  top: 0;
  z-index: 20;
  backdrop-filter: blur(6px);
  border-bottom: 1px solid var(--pos-border);
  background: var(--pos-appbar-bg) !important;
}

.gap-2 {
  gap: 8px;
}

.gap-3 {
  gap: 12px;
}

.drawer-brand-title {
  color: var(--pos-accent-dark);
  font-weight: 700;
}

.drawer-brand-subtitle {
  color: var(--pos-ink-muted);
}

.theme-toggle {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 6px 10px 6px 14px;
  border: 1px solid var(--pos-border);
  border-radius: 999px;
  background: var(--pos-card-soft);
}

.theme-toggle-label {
  color: var(--pos-ink-muted);
  font-weight: 600;
}

.theme-toggle-switch {
  margin: 0;
}

.theme-toggle-switch :deep(.v-selection-control) {
  min-height: auto;
}

.theme-toggle-switch :deep(.v-label) {
  display: none;
}

.stock-alert-dot {
  width: 12px;
  height: 12px;
  border-radius: 999px;
  display: inline-block;
  flex: 0 0 12px;
}

.stock-alert-dot--warning {
  background: rgba(var(--v-theme-warning), 0.2);
  box-shadow: inset 0 0 0 1px rgba(var(--v-theme-warning), 0.42);
}

.stock-alert-dot--error {
  background: rgba(var(--v-theme-error), 0.2);
  box-shadow: inset 0 0 0 1px rgba(var(--v-theme-error), 0.42);
}

.stock-alert-dot--secondary {
  background: rgba(var(--v-theme-secondary), 0.2);
  box-shadow: inset 0 0 0 1px rgba(var(--v-theme-secondary), 0.42);
}

.stock-alert-dot--info {
  background: rgba(var(--v-theme-info), 0.2);
  box-shadow: inset 0 0 0 1px rgba(var(--v-theme-info), 0.42);
}

</style>

