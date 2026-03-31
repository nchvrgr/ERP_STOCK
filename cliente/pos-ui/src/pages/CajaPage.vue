<template>
  <div class="caja-page">
    <v-tabs v-if="availableTabs.length > 1" v-model="tab" color="primary" class="mb-4">
      <v-tab v-if="canUsePos" value="ventas">Ventas</v-tab>
      <v-tab v-if="canUseCaja" value="movimientos">Movimientos stock</v-tab>
      <v-tab v-if="canUseCaja" value="historial">Historial caja</v-tab>
    </v-tabs>

    <v-window v-model="tab">
      <v-window-item v-if="canUsePos" value="ventas" class="caja-window-item">
        <PosPage />
      </v-window-item>

      <v-window-item v-if="canUseCaja" value="movimientos" class="caja-window-item">
        <StockMovimientosPanel />
      </v-window-item>

      <v-window-item v-if="canUseCaja" value="historial" class="caja-window-item">
        <v-card class="pos-card pa-4 caja-list-card">
          <div class="d-flex flex-wrap align-center gap-3">
            <div class="text-h6">Historial de caja</div>
            <v-spacer />
            <v-text-field
              v-model="historialFrom"
              type="date"
              label="Desde"
              variant="outlined"
              density="comfortable"
              hide-details
              style="max-width: 220px"
            />
            <v-text-field
              v-model="historialTo"
              type="date"
              label="Hasta"
              variant="outlined"
              density="comfortable"
              hide-details
              style="max-width: 220px"
            />
            <v-btn color="primary" variant="tonal" class="text-none" :loading="historialLoading" @click="loadHistorial">
              Buscar
            </v-btn>
          </div>
          <div class="caja-table-shell mt-4">
            <v-data-table
              :headers="historialHeaders"
              :items="historialRows"
              :items-per-page="10"
              item-key="sesionId"
              class="caja-table"
              density="compact"
            >
              <template v-slot:[`item.aperturaSort`]="{ item }">{{ formatDate(item.aperturaAt) }}</template>
              <template v-slot:[`item.cierreSort`]="{ item }">{{ formatDate(item.cierreAt) }}</template>
              <template v-slot:[`item.turno`]="{ item }">{{ formatTurno(item.turno) }}</template>
              <template v-slot:[`item.montoInicial`]="{ item }">{{ formatMoney(item.montoInicial) }}</template>
              <template v-slot:[`item.totalContadoSort`]="{ item }">{{ formatMoney(item.totalContado) }}</template>
              <template v-slot:[`item.diferencia`]="{ item }">{{ formatMoney(item.diferencia) }}</template>
              <template v-slot:[`item.ventasSort`]="{ item }">
                <span v-if="item.ventaDesde && item.ventaHasta">De {{ item.ventaDesde }} a {{ item.ventaHasta }}</span>
                <span v-else>-</span>
              </template>
              <template v-slot:[`item.acciones`]="{ item }">
                <v-btn variant="tonal" size="small" class="text-none" @click="printHistorial(item)">
                  Imprimir PDF
                </v-btn>
              </template>
            </v-data-table>
          </div>
        </v-card>
      </v-window-item>
    </v-window>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" location="top end" timeout="1800">
      <div class="d-flex align-center gap-2">
        <v-icon>{{ snackbar.icon }}</v-icon>
        <span>{{ snackbar.text }}</span>
      </div>
    </v-snackbar>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import PosPage from './PosPage.vue';
import StockMovimientosPanel from '../components/StockMovimientosPanel.vue';
import { useAuthStore } from '../stores/auth';
import { getJson } from '../services/apiClient';
import { formatMoney } from '../utils/currency';

const auth = useAuthStore();
const tab = ref('ventas');

const hasAnyRole = (roles) => roles.some((role) => auth.roles.includes(role));
const canUsePos = computed(() => auth.hasPermission('PERM_VENTA_CREAR'));
const canUseCaja = computed(
  () =>
    auth.hasPermission('PERM_CAJA_MOVIMIENTO') ||
    hasAnyRole(['ENCARGADO', 'ADMIN']) ||
    canUsePos.value
);
const availableTabs = computed(() => {
  const tabs = [];
  if (canUsePos.value) tabs.push('ventas');
  if (canUseCaja.value) tabs.push('movimientos');
  if (canUseCaja.value) tabs.push('historial');
  return tabs;
});
const historialLoading = ref(false);
const historialFrom = ref('');
const historialTo = ref('');
const historial = ref([]);
const historialRows = computed(() =>
  historial.value.map((row) => ({
    ...row,
    aperturaSort: toDateSortValue(row.aperturaAt),
    cierreSort: toDateSortValue(row.cierreAt),
    totalContadoSort: Number(row.totalContado ?? 0),
    ventasSort: getVentasSortValue(row)
  }))
);

const snackbar = ref({
  show: false,
  text: '',
  color: 'success',
  icon: 'mdi-check-circle'
});
const historialHeaders = [
  { title: 'Cajero', value: 'cajero' },
  { title: 'Turno', value: 'turno' },
  { title: 'Apertura', value: 'aperturaSort' },
  { title: 'Cierre', value: 'cierreSort' },
  { title: 'Monto inicial', value: 'montoInicial' },
  { title: 'Total contado', value: 'totalContadoSort' },
  { title: 'Diferencia', value: 'diferencia' },
  { title: 'Ventas', value: 'ventasSort' },
  { title: '', value: 'acciones', align: 'end', sortable: false }
];

function toDateSortValue(value) {
  if (!value) return 0;
  const timestamp = new Date(value).getTime();
  return Number.isNaN(timestamp) ? 0 : timestamp;
}

function getVentasSortValue(row) {
  const desde = Number(row?.ventaDesde ?? 0);
  const hasta = Number(row?.ventaHasta ?? 0);

  if (desde > 0 && hasta >= desde) {
    return (hasta - desde) + 1;
  }

  if (desde > 0 || hasta > 0) {
    return 1;
  }

  return 0;
}

const formatDate = (value) => {
  if (!value) return '-';
  try {
    return new Date(value).toLocaleString('es-AR', { hour12: false });
  } catch {
    return value;
  }
};

const formatTurno = (value) => {
  if (!value) return '-';
  if (value === 'MANANA') return 'MAÑANA';
  return value;
};

const flash = (type, text) => {
  snackbar.value = {
    show: true,
    text,
    color: type === 'success' ? 'success' : 'error',
    icon: type === 'success' ? 'mdi-check-circle' : 'mdi-alert-circle'
  };
};

const handleAppSnackbar = (event) => {
  const detail = event?.detail;
  if (!detail?.text) return;
  flash(detail.type === 'success' ? 'success' : 'error', detail.text);
};

const extractProblemMessage = (data) => {
  if (!data) return 'Error inesperado.';
  if (data.errors) {
    const firstKey = Object.keys(data.errors)[0];
    if (firstKey && data.errors[firstKey]?.length) {
      return `${firstKey}: ${data.errors[firstKey][0]}`;
    }
  }
  return data.detail || data.title || 'Error inesperado.';
};

const loadHistorial = async () => {
  historialLoading.value = true;
  try {
    const params = new URLSearchParams();
    if (historialFrom.value) params.set('from', historialFrom.value);
    if (historialTo.value) params.set('to', historialTo.value);
    const { response, data } = await getJson(`/api/v1/caja/sesiones/historial?${params.toString()}`);
    if (!response.ok) throw new Error(extractProblemMessage(data));
    historial.value = data || [];
  } catch (err) {
    flash('error', err?.message || 'No se pudo cargar historial.');
  } finally {
    historialLoading.value = false;
  }
};

const printHistorial = (row) => {
  const win = window.open('', '_blank', 'width=900,height=680');
  if (!win) return;

  const rangeVentas = row.ventaDesde && row.ventaHasta
    ? `Ventas realizadas de la ${row.ventaDesde} a la ${row.ventaHasta}`
    : 'Sin ventas registradas en el turno';

  win.document.write(`
    <html>
      <head>
        <title>Historial caja</title>
        <style>
          body { font-family: Arial, sans-serif; padding: 16px; }
          h2 { margin: 0 0 8px; }
          .line { margin: 4px 0; }
        </style>
      </head>
      <body>
        <h2>CIERRE DE CAJA</h2>
        <div class="line"><strong>Cajero:</strong> ${row.cajero || '-'}</div>
        <div class="line"><strong>Turno:</strong> ${formatTurno(row.turno)}</div>
        <div class="line"><strong>Apertura:</strong> ${formatDate(row.aperturaAt)}</div>
        <div class="line"><strong>Cierre:</strong> ${formatDate(row.cierreAt)}</div>
        <hr />
        <div class="line"><strong>Monto inicial:</strong> ${formatMoney(row.montoInicial)}</div>
        <div class="line"><strong>Total Efectivo:</strong> ${formatMoney(row.totalEfectivo)}</div>
        <div class="line"><strong>Total Tarjeta:</strong> ${formatMoney(row.totalTarjeta)}</div>
        <div class="line"><strong>Total Transferencia:</strong> ${formatMoney(row.totalTransferencia)}</div>
        <div class="line"><strong>Total OTRO:</strong> ${formatMoney(row.totalOtro)}</div>
        <div class="line"><strong>Total Aplicativo:</strong> ${formatMoney(row.totalAplicativo)}</div>
        <div class="line"><strong>Total contado:</strong> ${formatMoney(row.totalContado)}</div>
        <div class="line"><strong>Diferencia:</strong> ${formatMoney(row.diferencia)}</div>
        <div class="line"><strong>Motivo diferencia:</strong> ${row.motivoDiferencia || '-'}</div>
        <hr />
        <div class="line"><strong>${rangeVentas}</strong></div>
      </body>
    </html>
  `);
  win.document.close();
  win.focus();
  win.print();
  setTimeout(() => win.close(), 300);
};

watch(
  availableTabs,
  (tabs) => {
    if (!tabs.includes(tab.value)) {
      tab.value = tabs[0] || 'ventas';
    }
  },
  { immediate: true }
);

const handleCajaSessionChanged = async () => {
  await loadHistorial();
};

onMounted(() => {
  loadHistorial();
  window.addEventListener('pos-caja-session-changed', handleCajaSessionChanged);
  window.addEventListener('app-snackbar', handleAppSnackbar);
});

onBeforeUnmount(() => {
  window.removeEventListener('pos-caja-session-changed', handleCajaSessionChanged);
  window.removeEventListener('app-snackbar', handleAppSnackbar);
});
</script>

<style scoped>
.caja-page {
  animation: fade-in 0.3s ease;
}

.caja-window-item {
  height: 100%;
}

.caja-list-card {
  min-height: calc(100vh - 180px);
  display: flex;
  flex-direction: column;
}

.caja-table-shell {
  flex: 1;
  min-height: 0;
  display: flex;
}

.caja-table {
  flex: 1;
  min-height: 0;
}

.caja-table :deep(.v-table) {
  height: 100%;
}

.caja-table :deep(.v-table__wrapper) {
  flex: 1;
  min-height: 0;
  overflow: auto;
}

.caja-table :deep(.v-data-table-footer) {
  margin-top: auto;
}

.ticket {
  border: 1px dashed rgba(169, 138, 111, 0.45);
  padding: 12px;
  border-radius: 10px;
  font-family: 'IBM Plex Sans', sans-serif;
}

.ticket-line {
  font-size: 0.9rem;
  margin-bottom: 4px;
}

.text-error {
  color: #dc2626;
}

@media print {
  .caja-page :deep(.pos-card),
  .caja-page :deep(.v-app-bar),
  .caja-page :deep(.v-navigation-drawer) {
    display: none !important;
  }

  .ticket {
    border: none;
  }
}
</style>

