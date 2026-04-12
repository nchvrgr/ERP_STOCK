<template>
  <div class="reportes-page">
    <v-card v-if="!canView" class="pos-card pa-4">
      <div class="text-h6">Reportes</div>
      <div class="text-caption text-medium-emphasis">No tenes permisos para ver reportes.</div>
    </v-card>

    <template v-else>
      <v-card class="pos-card pa-4 mb-4">
        <div class="d-flex flex-wrap align-center gap-3">
          <div>
            <div class="text-h6">Dashboard de reportes</div>
          </div>
          <v-spacer />
          <v-text-field
            v-model="filters.desde"
            label="Desde"
            type="date"
            variant="outlined"
            density="comfortable"
            hide-details
            style="max-width: 180px"
          />
          <v-text-field
            v-model="filters.hasta"
            label="Hasta"
            type="date"
            variant="outlined"
            density="comfortable"
            hide-details
            style="max-width: 180px"
          />
          <v-btn
            color="primary"
            class="text-none"
            :loading="loading"
            @click="loadReports"
          >
            Actualizar
          </v-btn>
        </div>
      </v-card>

      <v-row dense class="mb-4">
        <v-col cols="12" md="6">
          <v-card class="pos-card pa-4">
            <div class="d-flex align-center justify-space-between">
              <div>
                <div class="text-h6">Ventas por dia</div>
                <div class="text-caption text-medium-emphasis">Linea</div>
              </div>
            </div>
            <div v-if="loading" class="mt-3">
              <v-skeleton-loader type="image" height="180" />
            </div>
            <div ref="ventasWrapperRef" v-else :class="['chart-wrapper', 'chart-wrapper--ventas', 'mt-3', { 'chart-wrapper--night': isNightMode }]">
              <svg viewBox="0 0 100 40" preserveAspectRatio="none" class="chart-svg">
                <polyline
                  :points="ventasLine"
                  fill="none"
                  :stroke="ventasLineColor"
                  stroke-width="1.2"
                />
                <circle
                  v-for="(point, idx) in ventasPoints"
                  :key="`dot-${idx}`"
                  :cx="point.x"
                  :cy="point.y"
                  r="1.8"
                  :fill="ventasPointColor"
                />
                <rect
                  v-for="(area, idx) in ventasHitAreas"
                  :key="`ventas-hit-${idx}`"
                  :x="area.x"
                  y="0"
                  :width="area.width"
                  height="35"
                  fill="transparent"
                  @mouseenter="showVentasTooltip($event, idx)"
                  @mousemove="moveVentasTooltip($event)"
                  @mouseleave="hideVentasTooltip"
                />
              </svg>
              <div
                v-if="ventasTooltipState.show"
                class="chart-tooltip"
                :style="{ left: `${ventasTooltipState.x}px`, top: `${ventasTooltipState.y}px` }"
              >
                {{ ventasTooltipState.text }}
              </div>
              <div class="chart-labels">
                <span v-for="(label, index) in ventasAxisLabels" :key="`${label}-${index}`">{{ label }}</span>
              </div>
            </div>
          </v-card>
        </v-col>
        <v-col cols="12" md="6">
          <v-card class="pos-card pa-4">
            <div class="d-flex align-center justify-space-between">
              <div>
                <div class="text-h6">Medios de pago</div>
                <div class="text-caption text-medium-emphasis">Barras</div>
              </div>
            </div>
            <div v-if="loading" class="mt-3">
              <v-skeleton-loader type="image" height="180" />
            </div>
            <div ref="mediosWrapperRef" v-else :class="['chart-wrapper', 'chart-wrapper--medios', 'mt-3', { 'chart-wrapper--night': isNightMode }]">
              <svg viewBox="0 0 100 40" preserveAspectRatio="none" class="chart-svg">
                <rect
                  v-for="(hit, idx) in mediosHitAreas"
                  :key="`medios-hit-${idx}`"
                  :x="hit.x"
                  y="0"
                  :width="hit.width"
                  height="35"
                  fill="transparent"
                  @mouseenter="showMediosTooltip($event, idx)"
                  @mousemove="moveMediosTooltip($event)"
                  @mouseleave="hideMediosTooltip"
                />
                <rect
                  v-for="(bar, idx) in mediosBars"
                  :key="idx"
                  :x="bar.x"
                  :y="bar.y"
                  :width="bar.width"
                  :height="bar.height"
                  :fill="mediosBarColor"
                  rx="1"
                />
              </svg>
              <div
                v-if="mediosTooltipState.show"
                class="chart-tooltip"
                :style="{ left: `${mediosTooltipState.x}px`, top: `${mediosTooltipState.y}px` }"
              >
                {{ mediosTooltipState.text }}
              </div>
              <div class="chart-labels">
                <span v-for="(label, index) in mediosAxisLabels" :key="`${label}-${index}`">{{ label }}</span>
              </div>
            </div>
          </v-card>
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6">
          <v-card class="pos-card pa-4 reportes-table-card">
            <div class="d-flex align-center justify-space-between">
              <div>
                <div class="text-h6">Top productos</div>
                <div class="text-caption text-medium-emphasis">Top 10 mas vendidos</div>
              </div>
            </div>
            <div v-if="loading" class="mt-3">
              <v-skeleton-loader type="table" />
            </div>
            <v-data-table
              v-else
              class="mt-3"
              :headers="topHeaders"
              :items="topProductos"
              density="compact"
              item-key="productoId"
            >
              <template v-slot:[`item.nombre`]="{ item }">
                <div class="reportes-truncate reportes-truncate--nombre">{{ item.nombre || '-' }}</div>
              </template>
              <template v-slot:[`item.sku`]="{ item }">
                <div class="reportes-truncate reportes-truncate--sku">{{ item.sku || '-' }}</div>
              </template>
            </v-data-table>
          </v-card>
        </v-col>

        <v-col cols="12" md="6">
          <v-card class="pos-card pa-4 reportes-table-card">
            <div class="d-flex align-center justify-space-between">
              <div>
                <div class="text-h6">Inmovilizado</div>
                <div class="text-caption text-medium-emphasis">Sin ventas en el rango de fecha</div>
              </div>
            </div>
            <div v-if="loading" class="mt-3">
              <v-skeleton-loader type="table" />
            </div>
            <v-data-table
              v-else
              class="mt-3"
              :headers="inmovilizadoHeaders"
              :items="inmovilizado"
              density="compact"
              item-key="productoId"
            >
              <template v-slot:[`item.nombre`]="{ item }">
                <div class="reportes-truncate reportes-truncate--nombre">{{ item.nombre || '-' }}</div>
              </template>
              <template v-slot:[`item.sku`]="{ item }">
                <div class="reportes-truncate reportes-truncate--sku">{{ item.sku || '-' }}</div>
              </template>
            </v-data-table>
          </v-card>
        </v-col>
      </v-row>
    </template>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" location="top end" timeout="1800">
      <div class="d-flex align-center gap-2">
        <v-icon>{{ snackbar.icon }}</v-icon>
        <span>{{ snackbar.text }}</span>
      </div>
    </v-snackbar>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue';
import { useTheme } from 'vuetify';
import { useAuthStore } from '../stores/auth';
import { getJson } from '../services/apiClient';
import { formatMoney } from '../utils/currency';

const auth = useAuthStore();
const theme = useTheme();
const canView = computed(() => auth.hasPermission('PERM_REPORTES_VER'));
const isNightMode = computed(() => theme.global.name.value === 'posNightTheme');
const ventasLineColor = computed(() => 'var(--pos-chart-line)');
const ventasPointColor = computed(() => 'var(--pos-chart-point)');
const mediosBarColor = computed(() => 'var(--pos-chart-bar)');

const today = new Date();
const sevenDaysAgo = new Date();
sevenDaysAgo.setDate(today.getDate() - 7);

const filters = reactive({
  desde: sevenDaysAgo.toISOString().slice(0, 10),
  hasta: today.toISOString().slice(0, 10)
});

const loading = ref(false);
const ventasChart = ref(null);
const mediosChart = ref(null);
const topProductos = ref([]);
const inmovilizado = ref([]);

const snackbar = ref({
  show: false,
  text: '',
  color: 'success',
  icon: 'mdi-check-circle'
});

const topHeaders = [
  { title: 'Producto', value: 'nombre' },
  { title: 'SKU', value: 'sku' },
  { title: 'Cantidad', value: 'cantidad', align: 'end' },
  { title: 'Total', value: 'total', align: 'end' }
];

const inmovilizadoHeaders = [
  { title: 'Producto', value: 'nombre' },
  { title: 'SKU', value: 'sku' },
  { title: 'Stock', value: 'stockActual', align: 'end' },
  { title: 'Ultimo mov', value: 'ultimoMovimiento' },
  { title: 'Dias', value: 'diasSinMovimiento', align: 'end' }
];

const getVentasTooltip = (idx) => {
  const label = ventasLabels.value[idx] || '-';
  const value = ventasSeries.value[idx] || 0;
  return `${label}: ${formatMoney(value)}`;
};

const getMediosTooltip = (idx) => {
  const label = mediosLabels.value[idx] || '-';
  const value = mediosSeries.value[idx] || 0;
  return `${label}: ${formatMoney(value)}`;
};

const ventasTooltipState = ref({ show: false, text: '', x: 0, y: 0 });
const mediosTooltipState = ref({ show: false, text: '', x: 0, y: 0 });
const ventasWrapperRef = ref(null);
const mediosWrapperRef = ref(null);

const tooltipPosition = (event, wrapperRef) => {
  const rect = wrapperRef?.value?.getBoundingClientRect?.();
  if (!rect) return { x: 12, y: 12 };
  const tooltipWidth = Math.min(220, Math.max(140, rect.width - 16));
  const tooltipHeight = 44;
  const x = Math.max(8, Math.min(event.clientX - rect.left + 12, rect.width - tooltipWidth - 8));
  const y = Math.max(8, Math.min(event.clientY - rect.top - 10, rect.height - tooltipHeight - 8));
  return { x, y };
};

const showVentasTooltip = (event, idx) => {
  const pos = tooltipPosition(event, ventasWrapperRef);
  ventasTooltipState.value = {
    show: true,
    text: getVentasTooltip(idx),
    x: pos.x,
    y: pos.y
  };
};

const moveVentasTooltip = (event) => {
  if (!ventasTooltipState.value.show) return;
  const pos = tooltipPosition(event, ventasWrapperRef);
  ventasTooltipState.value = {
    ...ventasTooltipState.value,
    x: pos.x,
    y: pos.y
  };
};

const hideVentasTooltip = () => {
  ventasTooltipState.value.show = false;
};

const showMediosTooltip = (event, idx) => {
  const pos = tooltipPosition(event, mediosWrapperRef);
  mediosTooltipState.value = {
    show: true,
    text: getMediosTooltip(idx),
    x: pos.x,
    y: pos.y
  };
};

const moveMediosTooltip = (event) => {
  if (!mediosTooltipState.value.show) return;
  const pos = tooltipPosition(event, mediosWrapperRef);
  mediosTooltipState.value = {
    ...mediosTooltipState.value,
    x: pos.x,
    y: pos.y
  };
};

const hideMediosTooltip = () => {
  mediosTooltipState.value.show = false;
};

const flash = (type, text) => {
  snackbar.value = {
    show: true,
    text,
    color: type === 'success' ? 'success' : 'error',
    icon: type === 'success' ? 'mdi-check-circle' : 'mdi-alert-circle'
  };
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

const ventasLabels = computed(() => ventasChart.value?.labels || []);
const ventasSeries = computed(() => ventasChart.value?.series?.[0]?.data || []);
const mediosLabels = computed(() => mediosChart.value?.labels || []);
const mediosSeries = computed(() => mediosChart.value?.series?.[0]?.data || []);
const buildAxisLabels = (labels, maxVisible = 6) => {
  if (!labels.length) return [];
  if (labels.length <= maxVisible) return labels;
  const step = Math.ceil(labels.length / maxVisible);
  return labels.map((label, index) =>
    index === 0 || index === labels.length - 1 || index % step === 0 ? label : ''
  );
};
const ventasAxisLabels = computed(() => buildAxisLabels(ventasLabels.value, 7));
const mediosAxisLabels = computed(() => buildAxisLabels(mediosLabels.value, 6));

const buildLinePoints = (values) => {
  if (!values.length) return [];
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = max - min || 1;
  return values.map((value, index) => {
    const x = (index / Math.max(values.length - 1, 1)) * 100;
    const y = 35 - ((value - min) / range) * 25;
    return { x, y };
  });
};

const ventasPoints = computed(() => buildLinePoints(ventasSeries.value));
const ventasLine = computed(() => ventasPoints.value.map((point) => `${point.x},${point.y}`).join(' '));
const ventasHitAreas = computed(() => {
  const points = ventasPoints.value;
  if (!points.length) return [];
  const step = 100 / points.length;
  return points.map((p) => ({
    x: Math.max(0, p.x - step / 2),
    width: step
  }));
});

const mediosBars = computed(() => {
  const values = mediosSeries.value;
  if (!values.length) return [];
  const max = Math.max(...values) || 1;
  const barWidth = 100 / values.length - 4;
  return values.map((value, index) => {
    const height = (value / max) * 28;
    return {
      x: index * (barWidth + 4) + 2,
      y: 35 - height,
      width: barWidth,
      height
    };
  });
});

const mediosHitAreas = computed(() => {
  const len = mediosSeries.value.length;
  if (!len) return [];
  const step = 100 / len;
  return Array.from({ length: len }, (_, i) => ({
    x: i * step,
    width: step
  }));
});

const buildQuery = () => {
  const params = new URLSearchParams();
  if (filters.desde) {
    const desdeUtc = new Date(`${filters.desde}T00:00:00.000Z`).toISOString();
    params.set('desde', desdeUtc);
  }
  if (filters.hasta) {
    const hastaUtc = new Date(`${filters.hasta}T23:59:59.999Z`).toISOString();
    params.set('hasta', hastaUtc);
  }
  return params.toString();
};

const loadReports = async () => {
  if (!canView.value) return;
  loading.value = true;
  try {
    const query = buildQuery();

    const [ventasResp, mediosResp, topResp, inmResp] = await Promise.all([
      getJson(`/api/v1/reportes/ventas-por-dia?${query}`),
      getJson(`/api/v1/reportes/medios-pago?${query}`),
      getJson(`/api/v1/reportes/top-productos?${query}&top=10`),
      getJson(`/api/v1/reportes/stock-inmovilizado?${query}`)
    ]);

    if (!ventasResp.response.ok) throw new Error(extractProblemMessage(ventasResp.data));
    if (!mediosResp.response.ok) throw new Error(extractProblemMessage(mediosResp.data));
    if (!topResp.response.ok) throw new Error(extractProblemMessage(topResp.data));
    if (!inmResp.response.ok) throw new Error(extractProblemMessage(inmResp.data));

    ventasChart.value = ventasResp.data;
    mediosChart.value = mediosResp.data;
    topProductos.value = topResp.data?.rows || [];
    inmovilizado.value = (inmResp.data?.rows || []).map((row) => ({
      ...row,
      ultimoMovimiento: row.ultimoMovimiento ? new Date(row.ultimoMovimiento).toLocaleDateString('es-AR') : '-'
    }));
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar los reportes.');
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadReports();
});
</script>

<style scoped>
.reportes-page {
  animation: fade-in 0.3s ease;
}

.chart-wrapper {
  border: 1px solid var(--pos-chart-card-border);
  border-radius: 12px;
  padding: 12px;
  background: var(--pos-chart-card-bg);
  box-shadow: var(--pos-chart-card-shadow);
  position: relative;
}

.chart-svg {
  width: 100%;
  height: 180px;
}

.chart-labels {
  display: flex;
  justify-content: space-between;
  font-size: 0.75rem;
  color: var(--pos-chart-label);
  margin-top: 6px;
  gap: 8px;
}

.chart-labels span {
  flex: 1;
  min-width: 0;
  text-align: center;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.chart-tooltip {
  position: absolute;
  z-index: 2;
  background: var(--pos-chart-tooltip-bg);
  color: var(--pos-chart-tooltip-text);
  font-size: 0.75rem;
  padding: 6px 8px;
  border-radius: 6px;
  pointer-events: none;
  max-width: min(220px, calc(100% - 16px));
  white-space: normal;
  border: 1px solid var(--pos-chart-tooltip-border);
  box-shadow: var(--pos-chart-tooltip-shadow);
}

.reportes-truncate {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.reportes-truncate--nombre {
  max-width: 280px;
}

.reportes-truncate--sku {
  max-width: 160px;
}

.reportes-table-card {
  min-height: 470px;
}
</style>
