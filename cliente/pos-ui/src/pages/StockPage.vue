<template>
  <div class="stock-page">
    <v-tabs v-model="tab" color="primary" class="mb-3">
      <v-tab value="saldos">Saldos</v-tab>
      <v-tab value="alertas">
        <span>Alertas</span>
        <span
          v-if="showGlobalAlertChip"
          :class="['stock-alert-dot', `stock-alert-dot--${globalAlertMeta.color}`]"
          :title="globalAlertMeta.label"
          :aria-label="globalAlertMeta.label"
        ></span>
      </v-tab>
    </v-tabs>

    <v-window v-model="tab">
      <v-window-item value="saldos" class="stock-window-item">
        <v-card class="pos-card pa-4 stock-list-card">
          <div class="d-flex flex-wrap align-center gap-3 mb-4">
            <div class="text-h6">Ajuste de stock</div>
          </div>

          <div class="text-caption text-medium-emphasis stock-section-copy">
            Modificá la cantidad, guardá el motivo y el sistema dejará el ajuste asentado.
          </div>

          <v-row dense class="mt-2 stock-adjust-row">
            <v-col cols="12" md="4">
              <v-autocomplete
                v-model="ajusteProducto"
                :items="ajusteItems"
                :loading="ajusteLoading"
                item-title="label"
                return-object
                label="Producto"
                variant="outlined"
                density="comfortable"
                clearable
                :error-messages="ajusteErrors.producto"
                @update:search="searchAjusteProductos"
              />
            </v-col>
            <v-col cols="12" md="2">
              <v-text-field
                v-model="ajusteCantidadActual"
                label="Actual"
                variant="outlined"
                density="comfortable"
                readonly
              />
            </v-col>
            <v-col cols="12" md="2">
              <v-text-field
                v-model="ajusteCantidadNueva"
                label="Nuevo"
                type="number"
                min="0"
                step="0.01"
                variant="outlined"
                density="comfortable"
                :error-messages="ajusteErrors.cantidad"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="ajusteMotivo"
                label="Motivo"
                variant="outlined"
                density="comfortable"
                :error-messages="ajusteErrors.motivo"
              />
            </v-col>
          </v-row>

          <div class="d-flex justify-end mt-2">
            <v-btn
              color="primary"
              class="text-none"
              :loading="ajusteSaving"
              @click="aplicarAjuste"
            >
              Guardar ajuste
            </v-btn>
          </div>

          <v-divider class="my-4" />

          <div class="d-flex flex-wrap align-center gap-3 mb-4">
            <div class="text-h6">Lista de stock</div>
          </div>
          <v-row dense class="align-center stock-search-row">
            <v-col cols="12" md="4">
              <v-autocomplete
                v-model="saldoProducto"
                :items="saldoProductosLookup"
                :loading="saldoProductosLoading"
                item-title="label"
                return-object
                clearable
                label="Producto"
                prepend-inner-icon="mdi-magnify"
                variant="outlined"
                density="comfortable"
                hide-details
                @update:search="searchSaldoProductos"
                @update:model-value="onSaldoProductoChanged"
              />
            </v-col>
            <v-col cols="12" md="5">
              <v-autocomplete
                v-model="saldoProveedor"
                :items="proveedoresLookup"
                item-title="label"
                return-object
                clearable
                label="Proveedor"
                prepend-inner-icon="mdi-magnify"
                variant="outlined"
                density="comfortable"
                hide-details
                :loading="proveedorLoading"
                @update:search="searchProveedores"
              />
            </v-col>
            <v-col cols="12" md="3" class="d-flex align-center">
              <v-btn
                color="primary"
                variant="tonal"
                class="text-none w-100"
                :loading="saldosLoading"
                @click="loadSaldos"
              >
                Buscar
              </v-btn>
            </v-col>
          </v-row>

          <div class="stock-table-shell mt-3">
            <v-data-table
              :headers="saldoHeaders"
              :items="saldos"
              :items-per-page="10"
              item-key="productoId"
              class="stock-table"
              density="compact"
            >
              <template v-slot:[`item.nombre`]="{ item }">
                <div class="saldo-text-truncate saldo-text-truncate--nombre">{{ item.nombre }}</div>
              </template>
              <template v-slot:[`item.proveedor`]="{ item }">
                <div class="saldo-text-truncate saldo-text-truncate--proveedor">{{ item.proveedor || 'SIN PROVEEDOR' }}</div>
              </template>
              <template v-slot:[`item.sku`]="{ item }">
                <div class="saldo-text-truncate saldo-text-truncate--sku">{{ item.sku }}</div>
              </template>
            </v-data-table>
          </div>
        </v-card>
      </v-window-item>

      <v-window-item value="alertas" class="stock-window-item">
        <v-card class="pos-card pa-4 stock-list-card">
          <div class="d-flex flex-wrap align-center gap-3 mb-4">
            <div class="text-h6">Alertas de stock</div>
            <span
              v-if="showPageAlertChip"
              :class="['stock-alert-dot', `stock-alert-dot--${pageAlertMeta.color}`]"
              :title="pageAlertMeta.label"
              :aria-label="pageAlertMeta.label"
            ></span>
          </div>
          <div class="d-flex flex-wrap align-center gap-3">
            <v-autocomplete
              v-model="alertaProveedor"
              :items="proveedoresLookup"
              item-title="label"
              return-object
              clearable
              label="Proveedor"
              variant="outlined"
              density="comfortable"
              :loading="proveedorLoading"
              style="min-width: 260px"
              @update:search="searchProveedores"
            />
            <v-btn
              color="primary"
              variant="tonal"
              class="text-none"
              :loading="alertasLoading"
              @click="loadAlertas"
            >
              Actualizar
            </v-btn>
            <v-btn
              color="primary"
              class="text-none"
              :disabled="!alertas.length"
              @click="openRemito"
            >
              Generar remito
            </v-btn>
          </div>

          <v-list density="compact" class="mt-3 stock-alert-list">
            <v-list-item v-for="alerta in alertas" :key="alerta.productoId">
              <v-list-item-title>{{ alerta.nombre }}</v-list-item-title>
              <v-list-item-subtitle>
                Stock: {{ alerta.cantidadActual }} / Min: {{ alerta.stockMinimo }} / Deseado: {{ alerta.stockDeseado }}
                <div class="text-caption text-medium-emphasis">
                  Proveedor: {{ alerta.proveedor || 'SIN PROVEEDOR' }}
                </div>
              </v-list-item-subtitle>
              <template #append>
                <v-chip
                  size="small"
                  class="status-chip"
                  :color="alerta.nivel === 'CRITICO' ? 'error' : 'warning'"
                  variant="outlined"
                >
                  {{ alerta.nivel }}
                </v-chip>
              </template>
            </v-list-item>
          </v-list>

          <div v-if="!alertas.length" class="text-caption text-medium-emphasis mt-3">
            Sin alertas.
          </div>
        </v-card>
      </v-window-item>
    </v-window>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" location="top end" timeout="1700">
      <div class="d-flex align-center gap-2">
        <v-icon>{{ snackbar.icon }}</v-icon>
        <span>{{ snackbar.text }}</span>
      </div>
    </v-snackbar>

    <v-dialog v-model="remitoDialog" width="720">
      <v-card>
        <v-card-title>Remito de compra</v-card-title>
        <v-card-text>
          <div class="text-caption text-medium-emphasis mb-2">
            Ajustá cantidades o eliminá productos antes de generar el PDF.
          </div>
          <div class="remito-preview-list">
            <div v-for="item in remitoItems" :key="item.productoId" class="remito-preview-row">
              <div>
                <div class="remito-preview-row__title">{{ item.nombre }} ({{ item.sku }})</div>
                <div class="text-caption text-medium-emphasis">
                  Actual: {{ item.cantidadActual }} / Deseado: {{ item.stockDeseado }}
                </div>
                <div class="text-caption text-medium-emphasis">
                  Proveedor: {{ item.proveedor || 'SIN PROVEEDOR' }}
                </div>
              </div>
              <div class="d-flex align-center gap-2">
                <v-text-field
                  v-model.number="item.cantidad"
                  type="number"
                  min="0"
                  step="0.01"
                  density="compact"
                  variant="outlined"
                  hide-details
                  style="max-width: 120px"
                />
                <v-btn
                  icon="mdi-delete"
                  variant="text"
                  color="error"
                  @click="removeRemitoItem(item.productoId)"
                />
              </div>
            </div>
          </div>
          <div v-if="!remitoItems.length" class="text-caption text-medium-emphasis mt-2">
            Sin productos para remito.
          </div>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" @click="remitoDialog = false">Cancelar</v-btn>
          <v-btn
            variant="tonal"
            color="primary"
            class="text-none"
            :loading="remitoLoading"
            :disabled="!remitoItems.length"
            @click="generarRemitoNormal"
          >
            Generar PDF
          </v-btn>
          <v-btn
            color="primary"
            class="text-none"
            :loading="remitoLoadingPorProveedor"
            :disabled="!remitoItems.length"
            @click="generarRemitoPorProveedor"
          >
            Generar PDF por prov.
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue';
import { useAuthStore } from '../stores/auth';
import { buildApiUrl, getJson, postJson } from '../services/apiClient';
import { getTicketWindowStyles } from '../theme/printStyles';
import { formatMoney } from '../utils/currency';
import {
  getHighestStockAlertLevel,
  getStockAlertMeta,
  useStockAlertsStore
} from '../stores/stockAlerts';

const auth = useAuthStore();
const stockAlerts = useStockAlertsStore();

const tab = ref('saldos');

const saldos = ref([]);
const saldosLoading = ref(false);
const saldoProducto = ref(null);
const saldoProductosLookup = ref([]);
const saldoProductosLoading = ref(false);
const saldoProveedor = ref(null);

const ajusteItems = ref([]);
const ajusteLoading = ref(false);
const ajusteProducto = ref(null);
const ajusteCantidadActual = ref(0);
const ajusteCantidadNueva = ref('');
const ajusteMotivo = ref('');
const ajusteSaving = ref(false);
const ajusteErrors = reactive({
  producto: '',
  cantidad: '',
  motivo: ''
});

const alertas = ref([]);
const alertasLoading = ref(false);
const alertasLoaded = ref(false);
const alertaProveedor = ref(null);
const proveedoresLookup = ref([]);
const proveedorLoading = ref(false);

const remitoDialog = ref(false);
const remitoItems = ref([]);
const remitoLoading = ref(false);
const remitoLoadingPorProveedor = ref(false);

const snackbar = ref({
  show: false,
  text: '',
  color: 'success',
  icon: 'mdi-check-circle'
});

const saldoHeaders = [
  { title: 'Producto', value: 'nombre' },
  { title: 'Proveedor', value: 'proveedor' },
  { title: 'SKU', value: 'sku' },
  { title: 'Cantidad', value: 'cantidadActual', align: 'end' }
];

const globalAlertMeta = computed(() => stockAlerts.chipMeta);
const showGlobalAlertChip = computed(() => stockAlerts.hasAlerts);
const pageAlertLevel = computed(() =>
  alertasLoaded.value ? getHighestStockAlertLevel(alertas.value) : stockAlerts.highestLevel
);
const pageAlertCount = computed(() => (alertasLoaded.value ? alertas.value.length : stockAlerts.count));
const pageAlertMeta = computed(() => getStockAlertMeta(pageAlertLevel.value));
const showPageAlertChip = computed(() => pageAlertCount.value > 0 && !!pageAlertLevel.value);

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

const mapSaldoItems = (items) =>
  (items || [])
    .slice()
    .sort((a, b) => (a.nombre || '').localeCompare(b.nombre || '', 'es'))
    .map((item) => ({
      ...item,
      label: `${item.nombre} (${item.sku})`
    }));

const mapSaldoLookup = (items) =>
  (items || [])
    .slice()
    .sort((a, b) => (a.nombre || '').localeCompare(b.nombre || '', 'es'))
    .map((item) => ({
      ...item,
      label: `${item.nombre} (${item.sku})`
    }));

const mapProveedorItems = (items) =>
  (items || [])
    .slice()
    .sort((a, b) => (a.name || '').localeCompare(b.name || '', 'es'))
    .map((item) => ({
      ...item,
      label: `${item.name} (${item.telefono || 'sin tel'})`
    }));

const formatDate = (value) => {
  if (!value) return '-';
  try {
    return new Date(value).toLocaleString('es-AR');
  } catch {
    return value;
  }
};

const formatDateTime = (value) => {
  if (!value) return '-';
  try {
    return new Date(value).toLocaleString('es-AR');
  } catch {
    return value;
  }
};

const printVentaTicket = async (ventaNumero) => {
  if (!ventaNumero || printingVentaTicket.value) return;
  printingVentaTicket.value = true;
  try {
    const { response, data } = await getJson(`/api/v1/ventas/numero/${ventaNumero}/ticket`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    const ventaData = data?.venta;
    const pagosData = data?.pagos || [];
    if (!ventaData) {
      throw new Error('No se encontro el detalle de la venta.');
    }

    const win = window.open('', '_blank', 'width=380,height=600');
    if (!win) return;

    const itemsRows = (ventaData.items || [])
      .map((item) => {
        const subtotal = item.subtotal ?? item.cantidad * item.precioUnitario;
        return `
          <tr>
            <td>${item.nombre}</td>
            <td style="text-align:right">${item.cantidad}</td>
            <td style="text-align:right">${formatMoney(item.precioUnitario)}</td>
            <td style="text-align:right">${formatMoney(subtotal)}</td>
          </tr>`;
      })
      .join('');

    const pagosRows = (pagosData || [])
      .map(
        (pago) =>
          `<tr><td>${pago.medioPago}</td><td style="text-align:right">${formatMoney(pago.monto)}</td></tr>`
      )
      .join('');

    win.document.write(`
      <html>
        <head>
          <title>Ticket venta</title>
          <style>
            ${getTicketWindowStyles()}
          </style>
          <script>
            function openPrintPreview() {
              const cloned = document.documentElement.cloneNode(true);
              const actions = cloned.querySelector('.ticket-actions');
              if (actions) actions.remove();
              window.opener?.postMessage({
                type: 'stock-open-ticket-preview',
                html: '<!DOCTYPE html>' + cloned.outerHTML
              }, '*');
            }
          <\/script>
        </head>
        <body>
          <div class="ticket-actions">
            <button type="button" onclick="openPrintPreview()">Imprimir</button>
            <button type="button" onclick="window.close()">Cerrar</button>
          </div>
          <h1>Ticket de venta</h1>
          <div>Venta Nro: ${ventaData.numero ?? '-'}</div>
          <div>Fecha: ${formatDateTime(ventaData.createdAt)}</div>
          <table>
            <thead>
              <tr>
                <th>Producto</th>
                <th style="text-align:right">Cant</th>
                <th style="text-align:right">Precio</th>
                <th style="text-align:right">Subtotal</th>
              </tr>
            </thead>
            <tbody>
              ${itemsRows}
            </tbody>
          </table>
          <div style="margin-top:8px"><strong>Total: ${formatMoney(ventaData.totalNeto)}</strong></div>
          <div style="margin-top:8px">Pagos</div>
          <table>
            <tbody>
              ${pagosRows || '<tr><td>Sin detalle</td><td></td></tr>'}
            </tbody>
          </table>
        </body>
      </html>
    `);
    win.document.close();
    win.focus();
  } catch (err) {
    flash('error', err?.message || 'No se pudo imprimir el ticket.');
  } finally {
    printingVentaTicket.value = false;
  }
};

const openDesktopTicketPreview = async (html) => {
  if (!html) return;

  if (typeof window.desktopBridge?.openTicketPreview !== 'function') {
    flash('error', 'La app de escritorio no tiene habilitada la vista previa de impresion.');
    return;
  }

  try {
    await window.desktopBridge.openTicketPreview({
      html,
      title: 'Vista previa de impresion'
    });
  } catch (err) {
    flash('error', err?.message || 'No se pudo abrir la vista previa de impresion.');
  }
};

const handleTicketPreviewMessage = (event) => {
  if (event?.data?.type !== 'stock-open-ticket-preview') return;
  openDesktopTicketPreview(event.data.html);
};

const loadSaldos = async () => {
  saldosLoading.value = true;
  try {
    const params = new URLSearchParams();
    const productSearch = saldoProducto.value?.sku || saldoProducto.value?.nombre || '';
    if (productSearch.trim()) params.set('search', productSearch.trim());
    if (saldoProveedor.value?.id) params.set('proveedorId', saldoProveedor.value.id);
    const { response, data } = await getJson(`/api/v1/stock/saldos?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    saldos.value = (data || [])
      .slice()
      .sort((a, b) => (a.nombre || '').localeCompare(b.nombre || '', 'es'));
    ajusteItems.value = mapSaldoItems(saldos.value);
    saldoProductosLookup.value = mapSaldoLookup(saldos.value);
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar saldos.');
  } finally {
    saldosLoading.value = false;
  }
};

const searchSaldoProductos = async (term) => {
  saldoProductosLoading.value = true;
  try {
    const params = new URLSearchParams();
    if (term && term.trim()) params.set('search', term.trim());
    if (saldoProveedor.value?.id) params.set('proveedorId', saldoProveedor.value.id);
    const { response, data } = await getJson(`/api/v1/stock/saldos?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    saldoProductosLookup.value = mapSaldoLookup(data || []);
  } catch (err) {
    flash('error', err?.message || 'No se pudieron buscar productos.');
  } finally {
    saldoProductosLoading.value = false;
  }
};

const onSaldoProductoChanged = async () => {
  await loadSaldos();
};

const searchAjusteProductos = async (term) => {
  ajusteLoading.value = true;
  try {
    const params = new URLSearchParams();
    if (term && term.trim()) params.set('search', term.trim());
    if (saldoProveedor.value?.id) params.set('proveedorId', saldoProveedor.value.id);
    const { response, data } = await getJson(`/api/v1/stock/saldos?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    ajusteItems.value = mapSaldoItems(data || []);
  } catch (err) {
    flash('error', err?.message || 'No se pudieron buscar productos.');
  } finally {
    ajusteLoading.value = false;
  }
};

const resetAjusteErrors = () => {
  ajusteErrors.producto = '';
  ajusteErrors.cantidad = '';
  ajusteErrors.motivo = '';
};

const validateAjuste = () => {
  resetAjusteErrors();
  if (!ajusteProducto.value) {
    ajusteErrors.producto = 'Selecciona un producto.';
  }

  if (ajusteCantidadNueva.value === '' || Number.isNaN(Number(ajusteCantidadNueva.value))) {
    ajusteErrors.cantidad = 'Cantidad invalida.';
  } else if (Number(ajusteCantidadNueva.value) < 0) {
    ajusteErrors.cantidad = 'Cantidad no puede ser negativa.';
  }

  if (!ajusteMotivo.value.trim()) {
    ajusteErrors.motivo = 'El motivo es obligatorio.';
  }

  return !ajusteErrors.producto && !ajusteErrors.cantidad && !ajusteErrors.motivo;
};

const aplicarAjuste = async () => {
  if (ajusteSaving.value) return;
  if (!validateAjuste()) return;

  const actual = Number(ajusteCantidadActual.value || 0);
  const nuevo = Number(ajusteCantidadNueva.value);
  const delta = nuevo - actual;

  if (delta === 0) {
    flash('error', 'La cantidad no cambio.');
    return;
  }

  ajusteSaving.value = true;
  try {
    const payload = {
      tipo: 'AJUSTE',
      motivo: ajusteMotivo.value.trim(),
      items: [
        {
          productoId: ajusteProducto.value.productoId,
          cantidad: Math.abs(delta),
          esIngreso: delta > 0
        }
      ]
    };

    const { response, data } = await postJson('/api/v1/stock/ajustes', payload);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    flash('success', 'Stock ajustado');
    ajusteMotivo.value = '';
    await loadSaldos();
    await stockAlerts.refreshSummary();
    if (tab.value === 'alertas') {
      await loadAlertas();
    }
  } catch (err) {
    flash('error', err?.message || 'No se pudo ajustar el stock.');
  } finally {
    ajusteSaving.value = false;
  }
};

const loadAlertas = async () => {
  alertasLoading.value = true;
  try {
    const params = new URLSearchParams();
    if (alertaProveedor.value?.id) {
      params.set('proveedorId', alertaProveedor.value.id);
    }
    const url = params.toString() ? `/api/v1/stock/alertas?${params.toString()}` : '/api/v1/stock/alertas';
    const { response, data } = await getJson(url);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    alertas.value = (data || [])
      .slice()
      .sort((a, b) => (a.nombre || '').localeCompare(b.nombre || '', 'es'));
    alertasLoaded.value = true;
    await stockAlerts.refreshSummary();
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar alertas.');
  } finally {
    alertasLoading.value = false;
  }
};

const shouldIncludeInRemito = (alerta) => {
  const minimo = Number(alerta.stockMinimo ?? 0);
  const tolerancia = Number(alerta.toleranciaPct ?? 0);
  const limite = minimo * (1 + tolerancia / 100);
  return Number(alerta.cantidadActual ?? 0) <= limite;
};

const openRemito = async () => {
  if (alertasLoading.value) return;
  await loadAlertas();
  const filtradas = alertas.value.filter(shouldIncludeInRemito);
  remitoItems.value = filtradas.map((alerta) => ({
    productoId: alerta.productoId,
    nombre: alerta.nombre,
    sku: alerta.sku,
    proveedorId: alerta.proveedorId || null,
    proveedor: alerta.proveedor,
    cantidadActual: alerta.cantidadActual,
    stockDeseado: alerta.stockDeseado ?? 0,
    cantidad: (() => {
      const deseado = Number(alerta.stockDeseado ?? 0);
      const minimo = Number(alerta.stockMinimo ?? 0);
      const objetivo = deseado > 0 ? deseado : minimo;
      const sugerido = alerta.sugerido ?? Math.max(objetivo - alerta.cantidadActual, 0);
      return sugerido;
    })()
  }));
  remitoDialog.value = true;
};

const removeRemitoItem = (productoId) => {
  remitoItems.value = remitoItems.value.filter((item) => item.productoId !== productoId);
};

const getFilenameFromContentDisposition = (contentDisposition, fallbackName) => {
  if (!contentDisposition) return fallbackName;
  const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]).replace(/"/g, '') || fallbackName;
  }

  const plainMatch = contentDisposition.match(/filename="?([^";]+)"?/i);
  return plainMatch?.[1] || fallbackName;
};

const requestRemitoPdf = async (items, proveedorId = null, fallbackName = 'remito-alertas.pdf') => {
  const response = await fetch(buildApiUrl('/api/v1/stock/alertas/remito'), {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(auth.token ? { Authorization: `Bearer ${auth.token}` } : {})
    },
    body: JSON.stringify({ items, proveedorId })
  });

  if (!response.ok) {
    let message = 'No se pudo generar el remito.';
    try {
      const data = await response.json();
      message = extractProblemMessage(data);
    } catch {
      // ignore
    }
    throw new Error(message);
  }

  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = getFilenameFromContentDisposition(response.headers.get('content-disposition'), fallbackName);
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
};

const generarRemitoNormal = async () => {
  if (remitoLoading.value) return;
  const items = remitoItems.value
    .filter((item) => Number(item.cantidad) > 0)
    .map((item) => ({ productoId: item.productoId, cantidad: Number(item.cantidad) }));

  if (!items.length) {
    flash('error', 'No hay cantidades para generar.');
    return;
  }

  remitoLoading.value = true;
  try {
    const proveedorId = alertaProveedor.value?.id || null;
    await requestRemitoPdf(items, proveedorId, 'remito-alertas.pdf');
    flash('success', 'Remito generado');
    remitoDialog.value = false;
  } catch (err) {
    flash('error', err?.message || 'No se pudo generar el remito.');
  } finally {
    remitoLoading.value = false;
  }
};

const generarRemitoPorProveedor = async () => {
  if (remitoLoadingPorProveedor.value) return;

  const items = remitoItems.value
    .filter((item) => Number(item.cantidad) > 0)
    .map((item) => ({
      productoId: item.productoId,
      cantidad: Number(item.cantidad),
      proveedorId: item.proveedorId,
      proveedor: item.proveedor || 'SIN_PROVEEDOR'
    }));

  if (!items.length) {
    flash('error', 'No hay cantidades para generar.');
    return;
  }

  remitoLoadingPorProveedor.value = true;
  try {
    const groups = new Map();
    for (const item of items) {
      const key = item.proveedorId || `SIN:${item.proveedor}`;
      if (!groups.has(key)) {
        groups.set(key, {
          proveedorId: item.proveedorId || null,
          items: []
        });
      }
      groups.get(key).items.push({ productoId: item.productoId, cantidad: item.cantidad });
    }

    for (const group of groups.values()) {
      await requestRemitoPdf(group.items, group.proveedorId, 'remito-alertas.pdf');
    }

    flash('success', 'Remitos por proveedor generados');
    remitoDialog.value = false;
  } catch (err) {
    flash('error', err?.message || 'No se pudieron generar los remitos por proveedor.');
  } finally {
    remitoLoadingPorProveedor.value = false;
  }
};

const searchProveedores = async (term) => {
  proveedorLoading.value = true;
  try {
    const params = new URLSearchParams();
    if (term && term.trim()) params.set('search', term.trim());
    params.set('activo', 'true');
    const { response, data } = await getJson(`/api/v1/proveedores?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    proveedoresLookup.value = mapProveedorItems(data || []);
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar proveedores.');
  } finally {
    proveedorLoading.value = false;
  }
};

watch(tab, (value) => {
  if (value === 'saldos') {
    loadSaldos();
  }
  if (value === 'alertas') {
    loadAlertas();
  }
});

watch(alertaProveedor, () => {
  if (tab.value === 'alertas') {
    loadAlertas();
  }
});

watch(saldoProveedor, () => {
  saldoProducto.value = null;
  searchSaldoProductos('');
  if (tab.value === 'saldos') {
    loadSaldos();
  }
});

watch(ajusteProducto, (value) => {
  if (!value) {
    ajusteCantidadActual.value = 0;
    ajusteCantidadNueva.value = '';
    return;
  }

  ajusteCantidadActual.value = value.cantidadActual ?? 0;
  ajusteCantidadNueva.value = (value.cantidadActual ?? 0).toString();
});

onMounted(() => {
  stockAlerts.refreshSummary();
  searchProveedores('');
  searchSaldoProductos('');
  loadSaldos();
});

onBeforeUnmount(() => {});
</script>

<style scoped>
.stock-page {
  animation: fade-in 0.3s ease;
}

.stock-window-item {
  height: 100%;
}

.stock-list-card {
  min-height: calc(100vh - 180px);
  display: flex;
  flex-direction: column;
}

.stock-section-copy,
.stock-adjust-row,
.stock-search-row {
  width: 100%;
  max-width: 1040px;
  margin-inline: auto;
}

.stock-table-shell {
  flex: 1;
  min-height: 0;
  display: flex;
}

.stock-table {
  flex: 1;
  min-height: 0;
}

.stock-table :deep(.v-table) {
  height: 100%;
}

.stock-table :deep(.v-table__wrapper) {
  flex: 1;
  min-height: 0;
  overflow: auto;
}

.stock-table :deep(.v-data-table-footer) {
  margin-top: auto;
}

.stock-alert-dot {
  width: 12px;
  height: 12px;
  margin-left: 8px;
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

.stock-alert-list :deep(.v-list) {
  background-color: transparent;
}

.stock-alert-list :deep(.v-list-item) {
  background-color: transparent;
  border: 1px solid var(--pos-list-item-border);
  border-radius: 6px;
  margin-bottom: 8px;
}

.stock-alert-dot--secondary {
  background: rgba(var(--v-theme-secondary), 0.2);
  box-shadow: inset 0 0 0 1px rgba(var(--v-theme-secondary), 0.42);
}

.stock-alert-dot--info {
  background: rgba(var(--v-theme-info), 0.2);
  box-shadow: inset 0 0 0 1px rgba(var(--v-theme-info), 0.42);
}

.remito-preview-list {
  display: grid;
  gap: 10px;
}

.remito-preview-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px 14px;
  border-radius: 14px;
  background: var(--pos-remito-preview-bg);
}

.remito-preview-row__title {
  font-weight: 700;
  color: var(--pos-remito-preview-title);
}

@media (max-width: 960px) {
  .remito-preview-row {
    flex-direction: column;
    align-items: flex-start;
  }
}

.saldo-text-truncate {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.saldo-text-truncate--nombre {
  max-width: 280px;
}

.saldo-text-truncate--proveedor {
  max-width: 200px;
}

.saldo-text-truncate--sku {
  max-width: 130px;
}

</style>

