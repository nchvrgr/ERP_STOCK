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
          <v-btn-toggle
            v-model="periodPreset"
            mandatory
            density="comfortable"
            class="mr-2"
            @update:model-value="applyPeriodPreset"
          >
            <v-btn value="day" class="text-none">Dia</v-btn>
            <v-btn value="week" class="text-none">Semana</v-btn>
            <v-btn value="month" class="text-none">Mes</v-btn>
            <v-btn value="custom" class="text-none">Personalizado</v-btn>
          </v-btn-toggle>
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
        <v-col cols="12" md="4">
          <v-card class="pos-card pa-4">
            <div class="text-caption text-medium-emphasis">Ingreso total (rango)</div>
            <div class="text-h6">{{ formatMoney(resumenVentas.totalIngresos) }}</div>
          </v-card>
        </v-col>
        <v-col cols="12" md="4">
          <v-card class="pos-card pa-4">
            <div class="text-caption text-medium-emphasis">Ingreso facturado</div>
            <div class="text-h6">{{ formatMoney(resumenVentas.totalFacturado) }}</div>
          </v-card>
        </v-col>
        <v-col cols="12" md="4">
          <v-card class="pos-card pa-4">
            <div class="text-caption text-medium-emphasis">Ingreso cotizado</div>
            <div class="text-h6">{{ formatMoney(resumenVentas.totalCotizado) }}</div>
          </v-card>
        </v-col>
      </v-row>

      <v-row dense class="mb-4">
        <v-col cols="12">
          <v-card class="pos-card pa-4">
            <div class="d-flex align-center justify-space-between flex-wrap gap-2">
              <div>
                <div class="text-h6">Ventas por día</div>
                <div class="text-caption text-medium-emphasis">Comparación con período anterior equivalente</div>
              </div>
              <div class="chart-legend">
                <span class="chart-legend__item chart-legend__item--current">Período actual</span>
                <span class="chart-legend__item chart-legend__item--previous">Período anterior</span>
              </div>
            </div>
            <div class="text-caption text-medium-emphasis mt-1">
              {{ currentPeriodLabel }} vs {{ previousPeriodLabel }}
            </div>
            <div v-if="loading" class="mt-3">
              <v-skeleton-loader type="image" height="190" />
            </div>
            <div ref="ventasWrapperRef" v-else class="chart-wrapper chart-wrapper--minimal mt-3">
              <svg viewBox="0 0 100 42" preserveAspectRatio="none" class="chart-svg chart-svg--wide">
                <g v-for="tick in ventasScale.ticks" :key="`ventas-tick-${tick}`">
                  <line
                    :x1="chartPlot.left"
                    :y1="chartPlot.bottom - ((tick / ventasScale.max) * (chartPlot.bottom - chartPlot.top))"
                    x2="100"
                    :y2="chartPlot.bottom - ((tick / ventasScale.max) * (chartPlot.bottom - chartPlot.top))"
                    class="chart-grid-line"
                  />
                  <text
                    x="1"
                    :y="chartPlot.bottom - ((tick / ventasScale.max) * (chartPlot.bottom - chartPlot.top))"
                    class="chart-axis-label"
                    dominant-baseline="middle"
                  >
                    {{ formatCompactAmount(tick) }}
                  </text>
                </g>
                <path v-if="ventasAreaCurrent" :d="ventasAreaCurrent" class="chart-area-current" />
                <polyline
                  v-if="ventasLinePrevious"
                  :points="ventasLinePrevious"
                  class="chart-line-previous"
                />
                <polyline
                  v-if="ventasLineCurrent"
                  :points="ventasLineCurrent"
                  class="chart-line-current"
                />
                <circle
                  v-for="(point, idx) in ventasPointsCurrent"
                  :key="`current-dot-${idx}`"
                  :cx="point.x"
                  :cy="point.y"
                  r="0.72"
                  class="chart-dot-current"
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
              <div class="chart-labels chart-labels--dense" :style="{ '--chart-cols': ventasAxisLabels.length || 1 }">
                <span v-for="(label, index) in ventasAxisLabels" :key="`ventas-label-${index}`">{{ label }}</span>
              </div>
            </div>
          </v-card>
        </v-col>

        <v-col cols="12">
          <v-card class="pos-card pa-4">
            <div class="d-flex align-center justify-space-between flex-wrap gap-2">
              <div>
                <div class="text-h6">Medios de pago</div>
                <div class="text-caption text-medium-emphasis">Comparación con período anterior equivalente</div>
              </div>
              <div class="chart-legend">
                <span class="chart-legend__item chart-legend__item--current">Período actual</span>
                <span class="chart-legend__item chart-legend__item--previous">Período anterior</span>
              </div>
            </div>
            <div v-if="loading" class="mt-3">
              <v-skeleton-loader type="image" height="190" />
            </div>
            <div ref="mediosWrapperRef" v-else class="chart-wrapper chart-wrapper--minimal mt-3">
              <svg viewBox="0 0 100 42" preserveAspectRatio="none" class="chart-svg chart-svg--wide">
                <g v-for="tick in mediosScale.ticks" :key="`medios-tick-${tick}`">
                  <line
                    :x1="chartPlot.left"
                    :y1="chartPlot.bottom - ((tick / mediosScale.max) * (chartPlot.bottom - chartPlot.top))"
                    x2="100"
                    :y2="chartPlot.bottom - ((tick / mediosScale.max) * (chartPlot.bottom - chartPlot.top))"
                    class="chart-grid-line"
                  />
                  <text
                    x="1"
                    :y="chartPlot.bottom - ((tick / mediosScale.max) * (chartPlot.bottom - chartPlot.top))"
                    class="chart-axis-label"
                    dominant-baseline="middle"
                  >
                    {{ formatCompactAmount(tick) }}
                  </text>
                </g>
                <rect
                  v-for="(bar, idx) in mediosBarsPrevious"
                  :key="`medio-prev-${idx}`"
                  :x="bar.x"
                  :y="bar.y"
                  :width="bar.width"
                  :height="bar.height"
                  class="chart-bar-previous"
                  rx="0.35"
                />
                <rect
                  v-for="(bar, idx) in mediosBarsCurrent"
                  :key="`medio-current-${idx}`"
                  :x="bar.x"
                  :y="bar.y"
                  :width="bar.width"
                  :height="bar.height"
                  class="chart-bar-current"
                  rx="0.35"
                />
                <rect
                  v-for="(area, idx) in mediosHitAreas"
                  :key="`medios-hit-${idx}`"
                  :x="area.x"
                  y="0"
                  :width="area.width"
                  height="35"
                  fill="transparent"
                  @mouseenter="showMediosTooltip($event, idx)"
                  @mousemove="moveMediosTooltip($event)"
                  @mouseleave="hideMediosTooltip"
                />
              </svg>
              <div
                v-if="mediosTooltipState.show"
                class="chart-tooltip"
                :style="{ left: `${mediosTooltipState.x}px`, top: `${mediosTooltipState.y}px` }"
              >
                {{ mediosTooltipState.text }}
              </div>
              <div class="chart-labels chart-labels--dense" :style="{ '--chart-cols': mediosCompareRows.length || 1 }">
                <span v-for="(label, index) in mediosAxisLabels" :key="`medios-label-${index}`">{{ label }}</span>
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
              <v-btn-toggle v-model="topOrderBy" mandatory density="comfortable">
                <v-btn value="monto" class="text-none">Por importe</v-btn>
                <v-btn value="cantidad" class="text-none">Por cantidad</v-btn>
              </v-btn-toggle>
            </div>
            <div v-if="loading" class="mt-3">
              <v-skeleton-loader type="table" />
            </div>
            <v-data-table
              v-else
              class="mt-3"
              :headers="topHeaders"
              :items="topProductosOrdenados"
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
              <div class="d-flex align-center ga-2">
                <v-text-field
                  v-model.number="inmovilizadoMaxCantidadVendida"
                  label="Vendidos max"
                  type="number"
                  min="0"
                  step="0.01"
                  variant="outlined"
                  density="compact"
                  hide-details
                  style="max-width: 130px"
                />
                <v-text-field
                  v-model.number="inmovilizadoMinDias"
                  label="Dias min"
                  type="number"
                  min="1"
                  step="1"
                  variant="outlined"
                  density="compact"
                  hide-details
                  style="max-width: 120px"
                />
              </div>
            </div>
            <div v-if="loading" class="mt-3">
              <v-skeleton-loader type="table" />
            </div>
            <v-data-table
              v-else
              class="mt-3"
              :headers="inmovilizadoHeaders"
              :items="inmovilizadoFiltrado"
              density="compact"
              item-key="productoId"
            >
              <template v-slot:[`item.nombre`]="{ item }">
                <div class="reportes-truncate reportes-truncate--nombre">{{ item.nombre || '-' }}</div>
              </template>
              <template v-slot:[`item.sku`]="{ item }">
                <div class="reportes-truncate reportes-truncate--sku">{{ item.sku || '-' }}</div>
              </template>
              <template v-slot:[`item.cantidadVendidaPeriodo`]="{ item }">
                {{ Number(item.cantidadVendidaPeriodo || 0).toFixed(2) }}
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
const canView = computed(() => auth.erpUsername === 'admin' || auth.roles.includes('ADMIN') || auth.hasPermission('PERM_REPORTES_VER'));
const isNightMode = computed(() => theme.global.name.value === 'posNightTheme');

const today = new Date();
const sevenDaysAgo = new Date();
sevenDaysAgo.setDate(today.getDate() - 7);

const filters = reactive({
  desde: sevenDaysAgo.toISOString().slice(0, 10),
  hasta: today.toISOString().slice(0, 10)
});
const periodPreset = ref('week');
const topOrderBy = ref('monto');
const inmovilizadoMinDias = ref(30);
const inmovilizadoMaxCantidadVendida = ref(3);

const loading = ref(false);
const ventasChart = ref(null);
const ventasChartPrevious = ref(null);
const mediosChart = ref(null);
const mediosChartPrevious = ref(null);
const topProductos = ref([]);
const inmovilizado = ref([]);
const resumenVentas = reactive({
  totalIngresos: 0,
  totalFacturado: 0,
  totalCotizado: 0
});

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
  { title: 'Vendidos', value: 'cantidadVendidaPeriodo', align: 'end' },
  { title: 'Stock', value: 'stockActual', align: 'end' },
  { title: 'Ultimo mov', value: 'ultimoMovimiento' },
  { title: 'Dias', value: 'diasSinMovimiento', align: 'end' }
];

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

const shortDateLabel = (value) => {
  if (!value) return '-';
  try {
    return new Date(value).toLocaleDateString('es-AR', { day: '2-digit', month: 'short' });
  } catch {
    return String(value);
  }
};

const currentPeriodLabel = computed(() => `${shortDateLabel(filters.desde)} - ${shortDateLabel(filters.hasta)}`);

const currentRangeUtc = computed(() => {
  const start = filters.desde
    ? new Date(`${filters.desde}T00:00:00.000Z`)
    : new Date();
  const end = filters.hasta
    ? new Date(`${filters.hasta}T23:59:59.999Z`)
    : new Date();
  const safeStart = Number.isNaN(start.getTime()) ? new Date() : start;
  const safeEnd = Number.isNaN(end.getTime()) ? new Date() : end;
  return safeStart <= safeEnd ? { start: safeStart, end: safeEnd } : { start: safeEnd, end: safeStart };
});

const previousRangeUtc = computed(() => {
  const dayMs = 24 * 60 * 60 * 1000;
  const { start, end } = currentRangeUtc.value;
  const days = Math.max(1, Math.floor((end.getTime() - start.getTime()) / dayMs) + 1);
  const prevEnd = new Date(start.getTime() - 1);
  const prevStart = new Date(prevEnd.getTime() - (days - 1) * dayMs);
  return { start: prevStart, end: prevEnd };
});

const previousPeriodLabel = computed(() => {
  const { start, end } = previousRangeUtc.value;
  return `${shortDateLabel(start.toISOString())} - ${shortDateLabel(end.toISOString())}`;
});

const ventasLabelsCurrent = computed(() => ventasChart.value?.labels || []);
const ventasLabelsPrevious = computed(() => ventasChartPrevious.value?.labels || []);
const ventasSeriesCurrentRaw = computed(() => ventasChart.value?.series?.[0]?.data || []);
const ventasSeriesPreviousRaw = computed(() => ventasChartPrevious.value?.series?.[0]?.data || []);

const ventasSlotCount = computed(() => Math.max(ventasSeriesCurrentRaw.value.length, ventasSeriesPreviousRaw.value.length));
const normalizeSeries = (series, count) => Array.from({ length: count }, (_, idx) => Number(series[idx] || 0));
const ventasSeriesCurrent = computed(() => normalizeSeries(ventasSeriesCurrentRaw.value, ventasSlotCount.value));
const ventasSeriesPrevious = computed(() => normalizeSeries(ventasSeriesPreviousRaw.value, ventasSlotCount.value));

const mediosLabelsCurrent = computed(() => mediosChart.value?.labels || []);
const mediosLabelsPrevious = computed(() => mediosChartPrevious.value?.labels || []);
const mediosSeriesCurrent = computed(() => mediosChart.value?.series?.[0]?.data || []);
const mediosSeriesPrevious = computed(() => mediosChartPrevious.value?.series?.[0]?.data || []);
const topProductosOrdenados = computed(() => {
  const items = [...topProductos.value];
  if (topOrderBy.value === 'cantidad') {
    return items.sort((a, b) => Number(b.cantidad || 0) - Number(a.cantidad || 0));
  }

  return items.sort((a, b) => Number(b.total || 0) - Number(a.total || 0));
});
const inmovilizadoFiltrado = computed(() =>
  (inmovilizado.value || []).filter(
    (item) => Number(item.diasSinMovimiento || 0) >= Number(inmovilizadoMinDias.value || 0)
      && Number(item.cantidadVendidaPeriodo || 0) <= Number(inmovilizadoMaxCantidadVendida.value || 0)
  )
);
const chartPlot = {
  left: 9,
  top: 6,
  right: 98,
  bottom: 35
};

const buildAxisLabels = (labels, maxVisible = 6) => {
  if (!labels.length) return [];
  if (labels.length <= maxVisible) return labels;
  const step = Math.ceil(labels.length / maxVisible);
  return labels.map((label, index) =>
    index === 0 || index === labels.length - 1 || index % step === 0 ? label : ''
  );
};
const ventasAxisSourceLabels = computed(() =>
  ventasLabelsCurrent.value.length ? ventasLabelsCurrent.value : ventasLabelsPrevious.value
);
const ventasAxisLabels = computed(() => buildAxisLabels(ventasAxisSourceLabels.value, 10));

const niceStep = (rawStep) => {
  const magnitude = 10 ** Math.floor(Math.log10(Math.max(rawStep, 1)));
  const normalized = rawStep / magnitude;
  if (normalized <= 1) return magnitude;
  if (normalized <= 2) return 2 * magnitude;
  if (normalized <= 5) return 5 * magnitude;
  return 10 * magnitude;
};

const buildTicks = (maxValue, tickCount = 4) => {
  const max = Math.max(1, Number(maxValue || 0));
  const step = niceStep(max / tickCount);
  const ceiling = Math.ceil(max / step) * step;
  const ticks = [];
  for (let value = 0; value <= ceiling; value += step) {
    ticks.push(value);
  }
  return { ticks, max: ceiling };
};

const formatCompactAmount = (value) => {
  const abs = Math.abs(Number(value || 0));
  if (abs >= 1_000_000) return `${(value / 1_000_000).toFixed(abs >= 10_000_000 ? 0 : 1).replace(/\.0$/, '')}M`;
  if (abs >= 1_000) return `${(value / 1_000).toFixed(abs >= 10_000 ? 0 : 1).replace(/\.0$/, '')}k`;
  return `${Math.round(value)}`;
};

const buildLinePoints = (values, maxValue) => {
  if (!values.length) return [];
  const safeMax = maxValue > 0 ? maxValue : 1;
  const width = chartPlot.right - chartPlot.left;
  const height = chartPlot.bottom - chartPlot.top;
  return values.map((value, index) => {
    const x = chartPlot.left + ((index / Math.max(values.length - 1, 1)) * width);
    const y = chartPlot.bottom - ((Number(value || 0) / safeMax) * height);
    return { x, y };
  });
};

const ventasCompareMax = computed(() => Math.max(
  1,
  ...ventasSeriesCurrent.value,
  ...ventasSeriesPrevious.value
));
const ventasScale = computed(() => buildTicks(ventasCompareMax.value, 4));
const ventasPointsCurrent = computed(() => buildLinePoints(ventasSeriesCurrent.value, ventasCompareMax.value));
const ventasPointsPrevious = computed(() => buildLinePoints(ventasSeriesPrevious.value, ventasCompareMax.value));
const pointsToPolyline = (points) => points.map((point) => `${point.x},${point.y}`).join(' ');
const ventasLineCurrent = computed(() => pointsToPolyline(ventasPointsCurrent.value));
const ventasLinePrevious = computed(() => pointsToPolyline(ventasPointsPrevious.value));
const ventasAreaCurrent = computed(() => {
  const points = ventasPointsCurrent.value;
  if (!points.length) return '';
  const first = points[0];
  const last = points[points.length - 1];
  return `M ${first.x} ${chartPlot.bottom} L ${points.map((p) => `${p.x} ${p.y}`).join(' L ')} L ${last.x} ${chartPlot.bottom} Z`;
});

const ventasHitAreas = computed(() => {
  const count = ventasSlotCount.value;
  if (!count) return [];
  const step = (chartPlot.right - chartPlot.left) / count;
  return Array.from({ length: count }, (_, idx) => ({
    x: Math.max(chartPlot.left, chartPlot.left + idx * step),
    width: step
  }));
});

const mediosCompareRows = computed(() => {
  const labelSet = new Set([...mediosLabelsCurrent.value, ...mediosLabelsPrevious.value]);
  const currentMap = new Map((mediosLabelsCurrent.value || []).map((label, idx) => [label, Number(mediosSeriesCurrent.value[idx] || 0)]));
  const previousMap = new Map((mediosLabelsPrevious.value || []).map((label, idx) => [label, Number(mediosSeriesPrevious.value[idx] || 0)]));

  return Array.from(labelSet).map((label) => ({
    label,
    current: currentMap.get(label) || 0,
    previous: previousMap.get(label) || 0
  }));
});

const mediosAxisLabels = computed(() => buildAxisLabels(mediosCompareRows.value.map((item) => item.label), 9));
const mediosCompareMax = computed(() => Math.max(1, ...mediosCompareRows.value.map((x) => Math.max(x.current, x.previous))));
const mediosScale = computed(() => buildTicks(mediosCompareMax.value, 4));
const mediosBarsCurrent = computed(() => {
  const rows = mediosCompareRows.value;
  if (!rows.length) return [];
  const step = (chartPlot.right - chartPlot.left) / rows.length;
  const clusterWidth = Math.min(7, step * 0.62);
  const barWidth = clusterWidth / 2;
  return rows.map((row, index) => {
    const height = ((row.current || 0) / mediosCompareMax.value) * (chartPlot.bottom - chartPlot.top);
    const clusterStart = chartPlot.left + (index * step) + ((step - clusterWidth) / 2);
    return {
      x: clusterStart + barWidth + 0.25,
      y: chartPlot.bottom - height,
      width: Math.max(0.45, barWidth - 0.25),
      height
    };
  });
});

const mediosBarsPrevious = computed(() => {
  const rows = mediosCompareRows.value;
  if (!rows.length) return [];
  const step = (chartPlot.right - chartPlot.left) / rows.length;
  const clusterWidth = Math.min(7, step * 0.62);
  const barWidth = clusterWidth / 2;
  return rows.map((row, index) => {
    const height = ((row.previous || 0) / mediosCompareMax.value) * (chartPlot.bottom - chartPlot.top);
    const clusterStart = chartPlot.left + (index * step) + ((step - clusterWidth) / 2);
    return {
      x: clusterStart,
      y: chartPlot.bottom - height,
      width: Math.max(0.45, barWidth - 0.25),
      height
    };
  });
});

const mediosHitAreas = computed(() => {
  const len = mediosCompareRows.value.length;
  if (!len) return [];
  const step = (chartPlot.right - chartPlot.left) / len;
  return Array.from({ length: len }, (_, i) => ({
    x: chartPlot.left + (i * step),
    width: step
  }));
});

const getVentasTooltip = (idx) => {
  const currentLabel = ventasLabelsCurrent.value[idx] || `Actual ${idx + 1}`;
  const previousLabel = ventasLabelsPrevious.value[idx] || `Anterior ${idx + 1}`;
  const currentValue = ventasSeriesCurrent.value[idx] || 0;
  const previousValue = ventasSeriesPrevious.value[idx] || 0;
  return `${currentLabel}: ${formatMoney(currentValue)} | ${previousLabel}: ${formatMoney(previousValue)}`;
};

const getMediosTooltip = (idx) => {
  const row = mediosCompareRows.value[idx];
  if (!row) return '-';
  return `${row.label}: ${formatMoney(row.current)} actual | ${formatMoney(row.previous)} anterior`;
};

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

  params.set('ordenTop', topOrderBy.value === 'cantidad' ? 'cantidad' : 'monto');
  params.set('minDiasSinMovimiento', String(Math.max(1, Number(inmovilizadoMinDias.value || 1))));
  params.set('maxCantidadVendida', String(Math.max(0, Number(inmovilizadoMaxCantidadVendida.value || 0))));
  return params.toString();
};

const buildRangeOnlyQuery = (start, end) => {
  const params = new URLSearchParams();
  params.set('desde', start.toISOString());
  params.set('hasta', end.toISOString());
  return params.toString();
};

const applyPeriodPreset = (preset) => {
  const now = new Date();
  const end = new Date(now);
  let start = new Date(now);

  if (preset === 'day') {
    start = new Date(now);
  } else if (preset === 'week') {
    start = new Date(now);
    start.setDate(now.getDate() - 6);
  } else if (preset === 'month') {
    start = new Date(now);
    start.setDate(now.getDate() - 29);
  } else {
    return;
  }

  filters.desde = start.toISOString().slice(0, 10);
  filters.hasta = end.toISOString().slice(0, 10);
};

const loadReports = async () => {
  if (!canView.value) return;
  loading.value = true;
  try {
    const query = buildQuery();
    const previousRangeQuery = buildRangeOnlyQuery(previousRangeUtc.value.start, previousRangeUtc.value.end);

    const [resumenResp, ventasResp, ventasRespPrevious, mediosResp, mediosRespPrevious, topResp, inmResp] = await Promise.all([
      getJson(`/api/v1/reportes/resumen-ventas?${query}`),
      getJson(`/api/v1/reportes/ventas-por-dia?${query}`),
      getJson(`/api/v1/reportes/ventas-por-dia?${previousRangeQuery}`),
      getJson(`/api/v1/reportes/medios-pago?${query}`),
      getJson(`/api/v1/reportes/medios-pago?${previousRangeQuery}`),
      getJson(`/api/v1/reportes/top-productos?${query}&top=10`),
      getJson(`/api/v1/reportes/stock-inmovilizado?${query}`)
    ]);

    if (!resumenResp.response.ok) throw new Error(extractProblemMessage(resumenResp.data));
    if (!ventasResp.response.ok) throw new Error(extractProblemMessage(ventasResp.data));
    if (!ventasRespPrevious.response.ok) throw new Error(extractProblemMessage(ventasRespPrevious.data));
    if (!mediosResp.response.ok) throw new Error(extractProblemMessage(mediosResp.data));
    if (!mediosRespPrevious.response.ok) throw new Error(extractProblemMessage(mediosRespPrevious.data));
    if (!topResp.response.ok) throw new Error(extractProblemMessage(topResp.data));
    if (!inmResp.response.ok) throw new Error(extractProblemMessage(inmResp.data));

    resumenVentas.totalIngresos = Number(resumenResp.data?.totalIngresos || 0);
    resumenVentas.totalFacturado = Number(resumenResp.data?.totalFacturado || 0);
    resumenVentas.totalCotizado = Number(resumenResp.data?.totalNoFacturado || 0);

    ventasChart.value = ventasResp.data;
    ventasChartPrevious.value = ventasRespPrevious.data;
    mediosChart.value = mediosResp.data;
    mediosChartPrevious.value = mediosRespPrevious.data;
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

.chart-svg--wide {
  height: 196px;
}

.chart-wrapper--minimal {
  border-radius: 14px;
  background: linear-gradient(180deg, color-mix(in srgb, var(--pos-card) 92%, white 8%), var(--pos-card));
}

.chart-grid-line {
  stroke: color-mix(in srgb, var(--pos-ink-muted) 22%, transparent);
  stroke-width: 0.16;
}

.chart-area-current {
  fill: color-mix(in srgb, #22c55e 7%, transparent);
}

.chart-line-current {
  fill: none;
  stroke: #22c55e;
  stroke-width: 0.52;
}

.chart-line-previous {
  fill: none;
  stroke: color-mix(in srgb, var(--pos-ink-muted) 75%, #94a3b8 25%);
  stroke-width: 0.42;
  stroke-dasharray: 1.2 1.8;
}

.chart-dot-current {
  fill: #16a34a;
}

.chart-bar-current {
  fill: color-mix(in srgb, #22c55e 66%, #ffffff 34%);
}

.chart-bar-previous {
  fill: color-mix(in srgb, var(--pos-ink-muted) 38%, #ffffff 62%);
}

.chart-legend {
  display: flex;
  align-items: center;
  gap: 10px;
}

.chart-legend__item {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 0.74rem;
  color: var(--pos-ink-muted);
}

.chart-legend__item::before {
  content: '';
  width: 8px;
  height: 8px;
  border-radius: 999px;
  display: inline-block;
}

.chart-legend__item--current::before {
  background: #22c55e;
}

.chart-legend__item--previous::before {
  background: color-mix(in srgb, var(--pos-ink-muted) 70%, #fff 30%);
}

.chart-labels {
  display: grid;
  grid-template-columns: repeat(var(--chart-cols, 1), minmax(0, 1fr));
  font-size: 0.75rem;
  color: var(--pos-chart-label);
  margin-top: 6px;
  gap: 8px;
}

.chart-labels--dense {
  font-size: 0.71rem;
}

.chart-labels span {
  min-width: 0;
  text-align: center;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.chart-axis-label {
  fill: color-mix(in srgb, var(--pos-chart-label) 78%, #ffffff 22%);
  font-size: 1.8px;
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
