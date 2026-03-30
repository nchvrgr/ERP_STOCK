<template>
  <v-card class="pos-card pa-4 stock-movements-card">
    <div class="d-flex flex-wrap align-center gap-3 mb-4">
      <div>
        <div class="text-h6">Movimientos de stock</div>
        <div class="text-caption text-medium-emphasis">
          Filtra por producto, venta o rango de fechas.
        </div>
      </div>
    </div>

    <v-row dense class="mb-2">
      <v-col cols="12" md="4">
        <v-autocomplete
          v-model="selectedProducto"
          :items="productoOptions"
          :loading="productoLoading"
          item-title="label"
          return-object
          clearable
          label="Producto"
          prepend-inner-icon="mdi-magnify"
          variant="outlined"
          density="comfortable"
          hide-details
          @update:search="searchProductos"
        />
      </v-col>
      <v-col cols="12" md="2">
        <v-text-field
          v-model="filters.ventaNumero"
          label="N° de venta"
          variant="outlined"
          density="comfortable"
          hide-details
          inputmode="numeric"
        />
      </v-col>
      <v-col cols="12" md="2">
        <v-select
          v-model="filters.facturada"
          :items="facturacionOptions"
          item-title="label"
          item-value="value"
          label="Facturación"
          variant="outlined"
          density="comfortable"
          hide-details
        />
      </v-col>
      <v-col cols="12" md="2">
        <v-text-field
          v-model="filters.desde"
          label="Desde"
          type="date"
          variant="outlined"
          density="comfortable"
          hide-details
        />
      </v-col>
      <v-col cols="12" md="2">
        <v-text-field
          v-model="filters.hasta"
          label="Hasta"
          type="date"
          variant="outlined"
          density="comfortable"
          hide-details
        />
      </v-col>
    </v-row>

    <div class="d-flex flex-wrap align-center justify-space-between gap-3">
      <div class="text-caption text-medium-emphasis">
        {{ movimientos.length }} movimientos encontrados
      </div>
      <v-btn color="primary" variant="tonal" class="text-none" :loading="movLoading" @click="loadMovimientos">
        Buscar
      </v-btn>
    </div>

    <div class="movement-list mt-4">
      <v-card
        v-for="mov in movimientos"
        :key="mov.id"
        class="movement-card"
        variant="outlined"
      >
        <div class="movement-card__header">
          <div>
            <div class="d-flex flex-wrap align-center gap-2">
              <v-chip size="small" color="primary" variant="tonal">{{ mov.tipo }}</v-chip>
              <v-chip
                v-if="mov.ventaNumero && mov.ventaFacturada !== null && mov.ventaFacturada !== undefined"
                size="small"
                :color="mov.ventaFacturada ? 'success' : 'warning'"
                variant="tonal"
              >
                {{ mov.ventaFacturada ? 'Facturada' : 'No facturada' }}
              </v-chip>
            </div>
            <div class="movement-card__title mt-2">{{ mov.motivo || 'Sin motivo' }}</div>
            <div class="text-caption text-medium-emphasis mt-1">
              {{ formatDate(mov.fecha) }}
            </div>
            <div v-if="mov.ventaNumero" class="text-caption text-medium-emphasis">
              Venta N° {{ mov.ventaNumero }}
            </div>
          </div>
          <v-btn
            v-if="mov.ventaNumero"
            size="small"
            variant="tonal"
            color="primary"
            class="text-none"
            @click="printVentaTicket(mov.ventaNumero)"
          >
            Imprimir ticket
          </v-btn>
        </div>

        <v-divider class="my-3" />

        <div class="movement-items">
          <div v-for="item in mov.items" :key="item.id" class="movement-item-row">
            <div>
              <div class="movement-item-row__name">{{ item.nombre }}</div>
              <div class="text-caption text-medium-emphasis">SKU {{ item.sku || '-' }}</div>
            </div>
            <div class="movement-item-row__meta">
              <span>{{ item.esIngreso ? 'Ingreso' : 'Egreso' }}</span>
              <strong>{{ formatQuantity(item.cantidad) }}</strong>
            </div>
          </div>
        </div>
      </v-card>
    </div>

    <div v-if="!movLoading && !movimientos.length" class="text-caption text-medium-emphasis mt-4">
      No hay movimientos para los filtros elegidos.
    </div>
  </v-card>
</template>

<script setup>
import { onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import { getJson } from '../services/apiClient';
import { formatMoney } from '../utils/currency';

const movimientos = ref([]);
const movLoading = ref(false);
const selectedProducto = ref(null);
const productoLoading = ref(false);
const productoOptions = ref([]);
const printingVentaTicket = ref(false);

const filters = reactive({
  ventaNumero: '',
  facturada: '',
  desde: '',
  hasta: ''
});

const facturacionOptions = [
  { label: 'Todas', value: '' },
  { label: 'Facturadas', value: 'true' },
  { label: 'No facturadas', value: 'false' }
];

const flash = (type, text) => {
  window.dispatchEvent(new CustomEvent('app-snackbar', { detail: { type, text } }));
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

const formatDate = (value) => {
  if (!value) return '-';
  try {
    return new Date(value).toLocaleString('es-AR');
  } catch {
    return value;
  }
};

const formatQuantity = (value) => {
  const amount = Number(value || 0);
  if (Number.isInteger(amount)) return amount;
  return amount.toFixed(2);
};

const mapProductoResults = (items) =>
  (items || [])
    .slice()
    .sort((a, b) => (a.name || '').localeCompare(b.name || '', 'es'))
    .map((item) => ({
      ...item,
      label: `${item.name} (${item.sku})`
    }));

const searchProductos = async (term) => {
  productoLoading.value = true;
  try {
    const params = new URLSearchParams();
    params.set('activo', 'true');
    if ((term || '').trim()) params.set('search', term.trim());
    const { response, data } = await getJson(`/api/v1/productos?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    productoOptions.value = mapProductoResults(data || []);
  } catch (err) {
    flash('error', err?.message || 'No se pudieron buscar productos.');
  } finally {
    productoLoading.value = false;
  }
};

const openDesktopTicketPreview = async (html) => {
  if (!html) return;
  if (typeof window.desktopBridge?.openTicketPreview !== 'function') {
    flash('error', 'La app de escritorio no tiene habilitada la vista previa de impresión.');
    return;
  }

  try {
    await window.desktopBridge.openTicketPreview({
      html,
      title: 'Vista previa de impresión'
    });
  } catch (err) {
    flash('error', err?.message || 'No se pudo abrir la vista previa de impresión.');
  }
};

const handleTicketPreviewMessage = (event) => {
  if (event?.data?.type !== 'stock-open-ticket-preview') return;
  openDesktopTicketPreview(event.data.html);
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
      throw new Error('No se encontró el detalle de la venta.');
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

    const pagosRows = pagosData
      .map((pago) => `<tr><td>${pago.medioPago}</td><td style="text-align:right">${formatMoney(pago.monto)}</td></tr>`)
      .join('');

    win.document.write(`
      <html>
        <head>
          <title>Ticket venta</title>
          <style>
            @page { margin: 8mm; }
            body { font-family: Arial, sans-serif; font-size: 12px; padding: 10px; }
            h1 { margin: 0 0 6px 0; font-size: 14px; }
            table { width: 100%; border-collapse: collapse; margin-top: 8px; }
            th, td { border-bottom: 1px dashed #ccc; padding: 4px 0; }
            th { text-align: left; font-weight: 600; }
            .ticket-actions { display: flex; gap: 8px; margin-bottom: 12px; }
            .ticket-actions button { border: 1px solid #d6d0c6; background: #fff; border-radius: 999px; padding: 6px 12px; cursor: pointer; }
            @media print {
              .ticket-actions { display: none; }
              body { padding: 0; }
            }
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
          <div>Venta N°: ${ventaData.numero ?? '-'}</div>
          <div>Fecha: ${formatDate(ventaData.createdAt)}</div>
          <table>
            <thead>
              <tr>
                <th>Producto</th>
                <th style="text-align:right">Cant.</th>
                <th style="text-align:right">Precio</th>
                <th style="text-align:right">Subtotal</th>
              </tr>
            </thead>
            <tbody>${itemsRows}</tbody>
          </table>
          <div style="margin-top:8px"><strong>Total: ${formatMoney(ventaData.totalNeto)}</strong></div>
          <div style="margin-top:8px">Pagos</div>
          <table>
            <tbody>${pagosRows || '<tr><td>Sin detalle</td><td></td></tr>'}</tbody>
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

const loadMovimientos = async () => {
  movLoading.value = true;
  try {
    const params = new URLSearchParams();
    if (selectedProducto.value?.id) params.set('productoId', selectedProducto.value.id);
    if (filters.ventaNumero.trim()) params.set('ventaNumero', filters.ventaNumero.trim());
    if (filters.facturada === 'true' || filters.facturada === 'false') params.set('facturada', filters.facturada);
    if (filters.desde) params.set('desde', filters.desde);
    if (filters.hasta) params.set('hasta', filters.hasta);

    const { response, data } = await getJson(`/api/v1/stock/movimientos?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    movimientos.value = (data || []).map((mov) => ({
      ...mov,
      items: (mov.items || [])
        .slice()
        .sort((a, b) => (a.nombre || '').localeCompare(b.nombre || '', 'es'))
    }));
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar movimientos.');
  } finally {
    movLoading.value = false;
  }
};

onMounted(() => {
  window.addEventListener('message', handleTicketPreviewMessage);
  searchProductos('');
  loadMovimientos();
});

onBeforeUnmount(() => {
  window.removeEventListener('message', handleTicketPreviewMessage);
});
</script>

<style scoped>
.stock-movements-card {
  min-height: calc(100vh - 180px);
}

.movement-list {
  display: grid;
  gap: 14px;
}

.movement-card {
  padding: 16px;
  border-radius: 18px;
  border-color: rgba(122, 90, 58, 0.18);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98) 0%, rgba(250, 247, 242, 0.96) 100%);
}

.movement-card__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.movement-card__title {
  font-size: 1rem;
  font-weight: 700;
  color: #3b0a12;
}

.movement-items {
  display: grid;
  gap: 10px;
}

.movement-item-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px 14px;
  border-radius: 14px;
  background: rgba(198, 164, 108, 0.1);
}

.movement-item-row__name {
  font-weight: 700;
}

.movement-item-row__meta {
  display: flex;
  align-items: center;
  gap: 10px;
  white-space: nowrap;
}

@media (max-width: 960px) {
  .movement-card__header,
  .movement-item-row {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
