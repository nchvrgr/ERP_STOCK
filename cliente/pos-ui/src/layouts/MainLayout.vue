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
            <img :src="brandMarkSrc" alt="" class="drawer-brand-mark" />
            <div>
              <div class="text-subtitle-1 drawer-brand-title">Viñedo de la Villa</div>
              <div class="text-caption drawer-brand-subtitle">Gestión comercial</div>
              <div class="text-caption drawer-brand-role">Rol: {{ sessionRoleLabel }}</div>
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
        <v-chip :color="cajaStatusColor" variant="tonal" size="small">Caja {{ cajaStatusLabel }}</v-chip>
        <v-menu location="end" :close-on-content-click="false">
          <template #activator="{ props }">
            <v-btn
              v-bind="props"
              size="small"
              variant="tonal"
              color="primary"
              class="text-none caja-help-btn"
              aria-label="Información de caja"
              title="Información de caja"
            >
              ?
            </v-btn>
          </template>
          <v-card class="caja-info-popover pa-3">
            <div class="text-caption text-medium-emphasis mb-2">Estado de caja</div>
            <div class="d-flex align-center gap-2 caja-info-row">
              <v-icon size="16">mdi-cash-register</v-icon>
              <span>Caja: {{ cajaDisplay }}</span>
            </div>
            <div class="d-flex align-center gap-2 caja-info-row">
              <v-icon size="16">mdi-account</v-icon>
              <span>Cajero: {{ cajeroDisplay }}</span>
            </div>
            <div class="d-flex align-center gap-2 caja-info-row">
              <v-icon size="16">mdi-weather-sunset</v-icon>
              <span>Turno: {{ turnoDisplay }}</span>
            </div>
            <div class="d-flex align-center gap-2 caja-info-row">
              <v-icon size="16">mdi-clock-outline</v-icon>
              <span>Apertura: {{ aperturaDisplay }}</span>
            </div>
          </v-card>
        </v-menu>
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
import brandMarkDark from '../assets/brand-mark-dark.png';
import brandMarkLight from '../assets/brand-mark-light.png';

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
    access: () => isAdminSession.value || auth.hasPermission('PERM_REPORTES_VER')
  },
  {
    title: 'Administrador',
    icon: 'mdi-account',
    to: '/usuarios',
    access: () => isAdminSession.value || auth.hasPermission('PERM_USUARIO_ADMIN')
  }
];

const hasAnyRole = (roles) => roles.some((role) => auth.roles.includes(role));
const isAdminSession = computed(() => auth.erpUsername === 'admin' || hasAnyRole(['ADMIN']));
const sessionRoleLabel = computed(() => (isAdminSession.value ? 'Administrador' : 'Cajero'));

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

const POS_CAJA_SESSION_KEY = 'pos-caja-session';

const cajaInfo = ref({
  estado: 'CERRADA',
  numero: '',
  nombre: '',
  turno: '',
  aperturaAt: ''
});

const cajaStatusLabel = computed(() => (cajaInfo.value.estado === 'ABIERTA' ? 'ABIERTA' : 'CERRADA'));
const cajaStatusColor = computed(() => (cajaInfo.value.estado === 'ABIERTA' ? 'success' : 'error'));

const formatTurno = (value) => {
  if (!value) return 'n/a';
  if (value === 'MANANA') return 'MAÑANA';
  return value;
};

const formatDateTime = (value) => {
  if (!value) return 'n/a';
  try {
    return new Date(value).toLocaleString('es-AR', { hour12: false });
  } catch {
    return value;
  }
};

const cajaDisplay = computed(() => cajaInfo.value.numero || 'n/a');
const cajeroDisplay = computed(() => cajaInfo.value.nombre || 'n/a');
const turnoDisplay = computed(() => formatTurno(cajaInfo.value.turno));
const aperturaDisplay = computed(() => formatDateTime(cajaInfo.value.aperturaAt));

const syncCajaInfo = () => {
  const raw = localStorage.getItem(POS_CAJA_SESSION_KEY);
  if (!raw) {
    cajaInfo.value = { estado: 'CERRADA', numero: '', nombre: '', turno: '', aperturaAt: '' };
    return;
  }

  try {
    const session = JSON.parse(raw);
    if (session?.estado !== 'ABIERTA') {
      cajaInfo.value = { estado: 'CERRADA', numero: '', nombre: '', turno: '', aperturaAt: '' };
      return;
    }

    cajaInfo.value = {
      estado: session.estado || 'CERRADA',
      numero: session.cajaNumero || session.cajaId || '',
      nombre: session.cajaNombre || 'n/a',
      turno: session.turno || '',
      aperturaAt: session.aperturaAt || ''
    };
  } catch {
    cajaInfo.value = { estado: 'CERRADA', numero: '', nombre: '', turno: '', aperturaAt: '' };
  }
};

const handleStorageEvent = (event) => {
  if (event.key === POS_CAJA_SESSION_KEY) {
    syncCajaInfo();
  }
};

const isDarkMode = computed({
  get: () => theme.global.name.value === 'posNightTheme',
  set: (value) => {
    applyColorMode(value ? POS_COLOR_MODE_DARK : POS_COLOR_MODE_LIGHT, theme);
  }
});

const brandMarkSrc = computed(() => (isDarkMode.value ? brandMarkDark : brandMarkLight));

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
  window.addEventListener('storage', handleStorageEvent);
  window.addEventListener('pos-caja-session-changed', syncCajaInfo);
  refreshStockAlerts();
  syncCajaInfo();
});

onBeforeUnmount(() => {
  window.removeEventListener('focus', refreshStockAlerts);
  window.removeEventListener('storage', handleStorageEvent);
  window.removeEventListener('pos-caja-session-changed', syncCajaInfo);
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

.drawer-brand-mark {
  width: 28px;
  height: 28px;
  display: block;
  flex: 0 0 28px;
  object-fit: contain;
}

.drawer-brand-subtitle {
  color: var(--pos-ink-muted);
}

.drawer-brand-role {
  color: var(--pos-ink-muted);
  font-weight: 600;
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

.caja-help-btn {
  min-width: 32px;
  width: 32px;
  height: 32px;
  border-radius: 999px;
  font-weight: 800;
  padding: 0;
}

.caja-info-popover {
  min-width: 280px;
  border: 1px solid var(--pos-border);
  background: var(--pos-card-soft);
}

.caja-info-row {
  font-size: 0.84rem;
  color: var(--pos-ink);
  margin-top: 4px;
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

