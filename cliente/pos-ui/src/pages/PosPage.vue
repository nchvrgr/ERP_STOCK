<template>
  <div class="pos-page">
    <div class="pos-sales-panel">
      <v-row dense class="pos-sales-shell" :class="{ 'is-locked': !cajaAbierta }">
        <v-col cols="12" md="8" class="pos-main-column">
          <v-card class="pos-card pa-4 mb-4 pos-session-card">
            <div class="d-flex flex-wrap align-center gap-3">
              <div>
                <div class="text-h6">Venta {{ ventaNumeroDisplay }}</div>
                <div v-if="!cajaAbierta" class="text-caption text-medium-emphasis">
                  Ventas bloqueadas hasta abrir la caja
                </div>
              </div>
              <v-chip
                class="status-chip"
                :color="cajaStatus === 'ABIERTA' ? 'success' : 'error'"
                variant="tonal"
              >
                Caja {{ cajaStatus }}
              </v-chip>
              <v-spacer />
              <div class="d-flex flex-wrap align-center gap-3 text-caption text-medium-emphasis pos-session-meta">
                <div class="d-flex align-center gap-1">
                  <v-icon size="16">mdi-cash-register</v-icon>
                  <span>Caja: {{ cajaDisplay }}</span>
                </div>
                <div class="d-flex align-center gap-1">
                  <v-icon size="16">mdi-account</v-icon>
                  <span>Cajero: {{ cajeroDisplay }}</span>
                </div>
                <div class="d-flex align-center gap-1">
                  <v-icon size="16">mdi-weather-sunset</v-icon>
                  <span>Turno: {{ turnoDisplay }}</span>
                </div>
                <div class="d-flex align-center gap-1">
                  <v-icon size="16">mdi-clock-outline</v-icon>
                  <span>Apertura: {{ aperturaDisplay }}</span>
                </div>
              </div>
            </div>

            <v-autocomplete
              ref="scanInputRef"
              v-model="productoSeleccionado"
              v-model:search="scanInput"
              :items="productosEncontrados"
              item-title="label"
              return-object
              class="scan-input pos-scan mt-4"
              :class="scanFlashClass"
              label="Buscar producto o escanear SKU"
              variant="outlined"
              density="comfortable"
              prepend-inner-icon="mdi-barcode-scan"
              clearable
              hide-no-data
              hide-details
              :loading="productoSearchLoading || scanLoading"
              @update:search="searchProductos"
              @update:modelValue="onProductoSeleccionado"
              @keydown.enter.prevent="handleScan"
              @blur="maybeRestoreScan"
              :disabled="!canScan || scanLoading"
            />
          </v-card>

          <v-card class="pos-card pa-4 pos-cart-card">
            <div>
              <div>
                <div class="text-h6">Carrito</div>
                <div class="text-caption text-medium-emphasis">
                  {{ items.length }} items - Lista {{ venta?.listaPrecio || 'Minorista' }}
                </div>
              </div>
            </div>

            <v-data-table
              :headers="headers"
              :items="items"
              item-key="id"
              no-data-text="Sin productos en el carrito"
              density="compact"
              class="mt-3"
              height="420"
            >
              <template v-slot:[`item.cantidad`]="{ item }">
                <v-text-field
                  v-model.number="qtyEdits[getRow(item).id]"
                  type="number"
                  min="1"
                  step="1"
                  density="compact"
                  hide-details
                  variant="outlined"
                  class="pos-qty-field"
                  style="max-width: 90px"
                  :disabled="!canEdit"
                  @blur="commitQty(getRow(item))"
                  @keyup.enter="commitQty(getRow(item))"
                />
              </template>
              <template v-slot:[`item.precioUnitario`]="{ item }">
                {{ formatMoney(getRow(item).precioUnitario) }}
              </template>
              <template v-slot:[`item.subtotal`]="{ item }">
                <strong>{{ formatMoney(getRow(item).subtotal) }}</strong>
              </template>
              <template v-slot:[`item.acciones`]="{ item }">
                <v-btn
                  icon="mdi-delete"
                  variant="text"
                  color="error"
                  :disabled="!canEdit"
                  @click="removeItem(getRow(item))"
                />
              </template>
            </v-data-table>

          </v-card>
        </v-col>

        <v-col cols="12" md="4" class="pos-side-column">
          <v-card class="pos-card pa-4 mb-4 pos-totals-card">
            <div class="text-h6">Totales</div>
            <div class="text-caption text-medium-emphasis">Resumen actual</div>
            <v-divider class="my-3" />

            <div class="d-flex justify-space-between mb-2 pos-total-row">
              <span>Total bruto</span>
              <strong>{{ formatMoney(totalBruto) }}</strong>
            </div>
            <div class="d-flex justify-space-between mb-2 pos-total-row">
              <span>Descuento</span>
              <strong>- {{ formatMoney(totalDescuento) }}</strong>
            </div>
            <div class="pos-total-net">
              <div class="text-body-2 text-medium-emphasis">Total neto</div>
              <strong>{{ formatMoney(totalNeto) }}</strong>
            </div>
            <div class="text-caption text-medium-emphasis mt-3">
              Items: {{ totalItems }}
            </div>

            <v-divider class="my-3" />

            <v-btn
              color="primary"
              size="large"
              class="text-none pos-primary-action"
              block
              :disabled="!canEdit || !items.length"
              @click="openPagos"
            >
              <v-icon start>mdi-cash-register</v-icon>
              Cobrar
            </v-btn>
            <v-btn
              color="error"
              variant="tonal"
              class="text-none mt-3 pos-secondary-action"
              block
              :disabled="!ventaId || !items.length || !canEdit"
              @click="clearCarrito"
            >
              <v-icon start>mdi-cart-off</v-icon>
              Cancelar / Vaciar carrito
            </v-btn>
          </v-card>

          <v-card class="pos-card pa-4 pos-actions-card">
            <div class="text-h6 pos-actions-title">Caja</div>
            <div class="text-caption text-medium-emphasis">Acciones auxiliares</div>

            <v-btn
              color="secondary"
              variant="tonal"
              prepend-icon="mdi-arrow-up-bold-box-outline"
              class="text-none mt-3 pos-utility-action"
              block
              :disabled="!cajaAbierta"
              @click="dialogMovimientoCaja = true"
            >
              Retiro / Ajuste de caja
            </v-btn>
            <v-btn
              color="secondary"
              variant="tonal"
              prepend-icon="mdi-chart-timeline-variant"
              class="text-none pos-utility-action"
              block
              :disabled="!cajaAbierta"
              @click="dialogResumenCaja = true"
            >
              Resumen en vivo
            </v-btn>
            <v-btn
              color="error"
              variant="tonal"
              prepend-icon="mdi-lock-off-outline"
              class="text-none pos-utility-action pos-utility-action-danger"
              block
              :disabled="!cajaAbierta"
              @click="openCerrarCajaDialog"
            >
              Cerrar caja
            </v-btn>
          </v-card>
        </v-col>
      </v-row>

      <div v-if="!cajaAbierta" class="pos-sales-overlay">
        <div class="pos-sales-overlay-card">
          <div class="text-h6">Caja cerrada</div>
          <div class="text-caption text-medium-emphasis">
            Abrí una caja para volver a habilitar las ventas.
          </div>
          <v-btn
            color="primary"
            class="text-none mt-4"
            :loading="openCajaLoading"
            @click="openAbrirCajaDialog"
          >
            Abrir caja
          </v-btn>
        </div>
      </div>
    </div>

    <v-dialog v-model="dialogPagos" width="640">
      <v-card>
        <v-card-title>Cobrar</v-card-title>
        <v-card-text>
          <div class="pos-payment-total mb-4">
            <span>Total a cobrar</span>
            <strong>{{ formatMoney(totalNeto) }}</strong>
          </div>

          <div
            v-for="(line, index) in pagos"
            :key="line.id"
            class="mb-2 pos-payment-row"
          >
            <div class="pos-payment-field-col">
              <v-select
                v-model="line.medioPago"
                :items="availableMediosPagoOptions(index)"
                item-title="title"
                item-value="value"
                label="Medio"
                density="compact"
                variant="outlined"
              />
            </div>
            <div class="pos-payment-field-col">
              <MoneyField
                v-if="!line.base || line.medioPago !== 'EFECTIVO'"
                v-model="line.monto"
                label="Monto"
                class="pos-payment-money-field"
                density="compact"
                variant="outlined"
                :step="10"
                :readonly="line.base"
                :show-stepper="!line.base"
                :clear-on-focus="!line.base"
                numeric
              />
              <MoneyField
                v-if="line.medioPago === 'EFECTIVO'"
                v-model="line.recibido"
                label="Recibido"
                class="pos-payment-money-field"
                :class="{ 'mt-2': !line.base }"
                density="compact"
                variant="outlined"
                :step="10"
                clear-on-focus
                numeric
              />
            </div>
            <div class="pos-payment-side-col">
              <div v-if="line.medioPago === 'EFECTIVO'" class="pos-payment-change">
                <span>Vuelto:</span>
                <strong>{{ formatMoney(vuelto(line)) }}</strong>
              </div>
              <div v-else class="pos-payment-no-change">Sin vuelto</div>
            </div>
            <div v-if="canRemovePago" class="pos-payment-action-col">
              <v-btn
                icon="mdi-delete-outline"
                variant="text"
                color="error"
                class="pos-payment-delete-btn"
                @click="removePago(index)"
              />
            </div>
          </div>

          <v-btn
            variant="tonal"
            color="primary"
            class="text-none"
            :disabled="!canAddPago"
            @click="addPago"
          >
            Agregar medio
          </v-btn>

          <v-divider class="my-3" />
          <div class="text-body-2 mb-2">Facturación</div>
          <v-btn-toggle
            v-model="facturacionSeleccion"
            mandatory
            divided
            class="mb-3 pos-billing-toggle"
            color="primary"
          >
            <v-btn :value="true" class="text-none pos-billing-option">Facturada</v-btn>
            <v-btn :value="false" class="text-none pos-billing-option">No facturada</v-btn>
          </v-btn-toggle>

          <v-checkbox
            v-model="imprimirRecibo"
            label="Imprimir recibo"
            density="comfortable"
            hide-details
            class="mb-4"
          />

          <div class="d-flex justify-space-between">
            <span>Total pagos</span>
            <strong>{{ formatMoney(totalPagos) }}</strong>
          </div>
        </v-card-text>
        <v-card-actions class="justify-end gap-2 pt-4">
          <v-btn
            variant="flat"
            size="large"
            class="text-none px-5 pos-payment-cancel-btn"
            @click="dialogPagos = false"
          >
            Cancelar
          </v-btn>
          <v-btn
            variant="flat"
            size="large"
            class="text-none px-6 pos-payment-confirm-btn"
            :loading="confirmLoading"
            :disabled="!canConfirm"
            @click="confirmarVenta"
          >
            Confirmar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="dialogMovimientoCaja" width="520">
      <v-card>
        <v-card-title>Subir movimiento</v-card-title>
        <v-card-text>
          <v-form @submit.prevent="registrarMovimientoCaja">
            <v-select
              v-model="movimientoCaja.tipo"
              :items="tiposMovimientoCaja"
              label="Tipo"
              variant="outlined"
              density="comfortable"
              :disabled="movimientoCajaLoading"
              required
            />
            <v-select
              v-model="movimientoCaja.medioPago"
              :items="mediosPago"
              label="Medio"
              variant="outlined"
              density="comfortable"
              :disabled="movimientoCajaLoading"
            />
            <v-text-field
              v-model="movimientoCaja.motivo"
              label="Motivo"
              variant="outlined"
              density="comfortable"
              :disabled="movimientoCajaLoading"
              required
            />
            <MoneyField
              v-model="movimientoCaja.monto"
              label="Monto"
              variant="outlined"
              density="comfortable"
              :disabled="movimientoCajaLoading"
              numeric
            />
          </v-form>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn
            variant="text"
            :disabled="movimientoCajaLoading"
            @click="dialogMovimientoCaja = false"
          >
            Cancelar
          </v-btn>
          <v-btn
            color="primary"
            :loading="movimientoCajaLoading"
            :disabled="!canSaveMovimientoCaja"
            @click="registrarMovimientoCaja"
          >
            Guardar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="dialogResumenCaja" width="560">
      <v-card>
        <v-card-title>Resumen en vivo</v-card-title>
        <v-card-text>
          <div v-if="resumenCaja">
            <div class="d-flex justify-space-between">
              <span>Monto inicial</span>
              <strong>{{ formatMoney(resumenCaja.montoInicial) }}</strong>
            </div>
            <div class="d-flex justify-space-between">
              <span>Ingresos</span>
              <strong>{{ formatMoney(resumenCaja.totalIngresos) }}</strong>
            </div>
            <div class="d-flex justify-space-between">
              <span>Egresos</span>
              <strong>{{ formatMoney(resumenCaja.totalEgresos) }}</strong>
            </div>
            <div class="d-flex justify-space-between text-h6 mt-2">
              <span>Saldo actual</span>
              <strong>{{ formatMoney(resumenCaja.saldoActual) }}</strong>
            </div>
            <div class="text-caption text-medium-emphasis">
              Movimientos: {{ resumenCaja.totalMovimientos }}
            </div>
          </div>
          <div v-else class="text-caption text-medium-emphasis">
            Sin resumen disponible.
          </div>

          <v-divider class="my-3" />
          <div class="text-subtitle-2">Por medio de pago</div>
          <v-list density="compact">
            <v-list-item v-for="medio in resumenCajaMedios" :key="medio.medio">
              <v-list-item-title>{{ medio.medio }}</v-list-item-title>
              <v-list-item-subtitle>Teorico: {{ formatMoney(medio.teorico) }}</v-list-item-subtitle>
              <template #append>
                <strong>{{ formatMoney(medio.total) }}</strong>
              </template>
            </v-list-item>
          </v-list>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn
            variant="text"
            :disabled="resumenCajaLoading"
            @click="dialogResumenCaja = false"
          >
            Cerrar
          </v-btn>
          <v-btn
            color="primary"
            variant="tonal"
            :loading="resumenCajaLoading"
            :disabled="!cajaSessionId"
            @click="loadResumenCaja"
          >
            Actualizar resumen
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="dialogAbrirCaja" width="640">
      <v-card>
        <v-card-title>Abrir caja</v-card-title>
        <v-card-text>
          <v-autocomplete
            v-model="cajaAperturaId"
            :items="cajaAperturaOptions"
            item-title="label"
            item-value="id"
            label="Caja (cajero)"
            variant="outlined"
            density="comfortable"
            :loading="cajasLoading"
            clearable
            required
          />
          <MoneyField
            v-model="montoInicialApertura"
            label="Monto inicial"
            variant="outlined"
            density="comfortable"
            :step="100"
            numeric
          />
          <v-select
            v-model="turnoApertura"
            :items="turnos"
            item-title="title"
            item-value="value"
            label="Turno"
            variant="outlined"
            density="comfortable"
            required
          />
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn
            color="secondary"
            variant="tonal"
            size="large"
            class="text-none px-6"
            :disabled="openCajaLoading"
            @click="dialogAbrirCaja = false"
          >
            Cancelar
          </v-btn>
          <v-btn
            color="primary"
            size="large"
            class="text-none px-6"
            :loading="openCajaLoading"
            :disabled="!canSaveAbrirCaja"
            @click="guardarAperturaCaja"
          >
            Confirmar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="dialogNuevaCaja" width="520">
      <v-card>
        <v-card-title>Nueva caja</v-card-title>
        <v-card-text>
          <v-text-field
            v-model="nuevaCajaNumero"
            label="Numero de caja"
            variant="outlined"
            density="comfortable"
            :error-messages="crearCajaErrors.numero"
            required
          />
          <v-text-field
            v-model="nuevaCajaNombre"
            label="Nombre de cajero"
            variant="outlined"
            density="comfortable"
            :error-messages="crearCajaErrors.nombre"
            required
          />
          <MoneyField
            v-model="defaultMontoInicialNuevaCaja"
            label="Monto inicial"
            variant="outlined"
            density="comfortable"
            :error-messages="crearCajaErrors.defaultMontoInicial"
            hint="Opcional"
            persistent-hint
            :step="100"
            :empty-value="null"
            numeric
          >
            <template #append-inner>
              <v-tooltip text="Es el monto inicial por defecto de la caja, pero puede cambiarse antes de iniciar cada sesion.">
                <template #activator="{ props }">
                  <button
                    type="button"
                    class="money-help-btn"
                    aria-label="Informacion sobre monto inicial"
                    v-bind="props"
                    @click.prevent
                  >
                    i
                  </button>
                </template>
              </v-tooltip>
            </template>
          </MoneyField>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn
            color="secondary"
            variant="tonal"
            size="large"
            class="text-none px-6"
            :disabled="crearCajaLoading"
            @click="dialogNuevaCaja = false"
          >
            Cancelar
          </v-btn>
          <v-btn
            color="primary"
            size="large"
            class="text-none px-6"
            :loading="crearCajaLoading"
            @click="crearNuevaCaja"
          >
            Confirmar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="dialogCerrarCaja" width="640">
      <v-card>
        <v-card-title>Cerrar caja</v-card-title>
        <v-card-text>
          <div class="d-flex justify-space-between text-body-2 mb-3">
            <span>Teorico total</span>
            <strong>{{ formatMoney(teoricoTotalCierre) }}</strong>
          </div>

          <MoneyField
            v-model="efectivoContado"
            label="Efectivo contado"
            variant="outlined"
            density="comfortable"
            :loading="cierreResumenLoading"
            :step="10"
            clear-zero-on-focus
            numeric
          />

          <v-row dense>
            <v-col cols="12" v-for="medio in mediosCierre" :key="medio.medio">
              <MoneyField
                v-model="medio.contado"
                :label="`Contado ${medio.medio}`"
                variant="outlined"
                density="comfortable"
                :loading="cierreResumenLoading"
                :step="10"
                clear-zero-on-focus
                numeric
              />
            </v-col>
          </v-row>

          <div class="d-flex justify-space-between mt-2">
            <span>Total contado</span>
            <strong>{{ formatMoney(totalContadoCierre) }}</strong>
          </div>
          <div class="d-flex justify-space-between text-caption">
            <span>Diferencia</span>
            <span :class="diferenciaCierre !== 0 ? 'text-error' : ''">
              {{ formatMoney(diferenciaCierre) }}
            </span>
          </div>

          <v-text-field
            v-model="motivoDiferencia"
            label="Motivo de la diferencia"
            variant="outlined"
            density="comfortable"
            :disabled="!needsMotivoCierre"
            class="mt-2"
          />
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn
            variant="text"
            :disabled="cierreResumenLoading || closeCajaLoading"
            @click="dialogCerrarCaja = false"
          >
            Cancelar
          </v-btn>
          <v-btn
            color="primary"
            :loading="closeCajaLoading"
            :disabled="!canSaveCerrarCaja"
            @click="guardarCierreCaja"
          >
            Guardar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="dialogStock" width="520">
      <v-card>
        <v-card-title>Stock insuficiente</v-card-title>
        <v-card-text>
          <div class="text-body-2 mb-2">Revisar los siguientes items:</div>
          <v-list density="compact">
            <v-list-item v-for="item in stockFaltantes" :key="item.id">
              <v-list-item-title>{{ item.nombre }}</v-list-item-title>
              <v-list-item-subtitle>
                Cantidad: {{ item.cantidad }}
              </v-list-item-subtitle>
            </v-list-item>
          </v-list>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn color="primary" variant="text" @click="dialogStock = false">Ok</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" location="top end" timeout="1600">
      <div class="d-flex align-center gap-2">
        <v-icon>{{ snackbar.icon }}</v-icon>
        <span>{{ snackbar.text }}</span>
      </div>
    </v-snackbar>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import MoneyField from '../components/MoneyField.vue';
import { useAuthStore } from '../stores/auth';
import { getJson, postJson, requestJson } from '../services/apiClient';
import { formatMoney } from '../utils/currency';

const auth = useAuthStore();
const POS_LAST_CAJA_KEY = 'pos-last-caja-id';
const CREATE_NEW_CAJA_OPTION_ID = '__create_new_caja__';

const venta = ref(null);
const items = ref([]);
const pricing = ref(null);
const cajaStatus = ref('CERRADA');
const cajaSessionId = ref('');
const scanInput = ref('');
const scanInputRef = ref(null);
const scanLoading = ref(false);
const creatingVenta = ref(false);
const confirmLoading = ref(false);
const anularLoading = ref(false);
const dialogPagos = ref(false);
const dialogMovimientoCaja = ref(false);
const dialogResumenCaja = ref(false);
const dialogAbrirCaja = ref(false);
const dialogNuevaCaja = ref(false);
const dialogCerrarCaja = ref(false);
const dialogStock = ref(false);
const qtyEdits = ref({});
const productoSearchLoading = ref(false);
const productosEncontrados = ref([]);
const productoSeleccionado = ref(null);
const POS_VENTA_KEY = 'pos-venta-id';
const POS_SESSION_META_KEY = 'pos-session-meta';
const displayVentaNumero = ref(null);
const cajaNumero = ref('');
const cajaNombre = ref('');
const cajaTurno = ref('');
const cajaAperturaAt = ref('');
let productoSearchTimer = null;

const snackbar = ref({
  show: false,
  text: '',
  color: 'success',
  icon: 'mdi-check-circle'
});

const pagos = ref([]);
const facturacionSeleccion = ref(true);
const imprimirRecibo = ref(false);
const MAX_MEDIOS_PAGO = 2;
const mediosPago = ['EFECTIVO', 'TARJETA', 'TRANSFERENCIA', 'APLICATIVO', 'OTRO'];
const mediosPagoOptions = [
  { title: 'Efectivo', value: 'EFECTIVO' },
  { title: 'Tarjeta', value: 'TARJETA' },
  { title: 'Transferencia', value: 'TRANSFERENCIA' },
  { title: 'Aplicación', value: 'APLICATIVO' },
  { title: 'Otro', value: 'OTRO' }
];
const tiposMovimientoCaja = ['Retiro', 'Gasto', 'Ajuste'];
const turnos = [
  { title: 'MAÑANA', value: 'MANANA' },
  { title: 'TARDE', value: 'TARDE' },
  { title: 'NOCHE', value: 'NOCHE' }
];
const createMovimientoCaja = () => ({
  tipo: 'Retiro',
  medioPago: 'EFECTIVO',
  motivo: '',
  monto: 0
});
const cajasDisponibles = ref([]);
const cajasLoading = ref(false);
const cajaAperturaId = ref('');
const montoInicialApertura = ref(0);
const turnoApertura = ref('MANANA');
const openCajaLoading = ref(false);
const nuevaCajaNumero = ref('');
const nuevaCajaNombre = ref('');
const defaultMontoInicialNuevaCaja = ref(null);
const crearCajaLoading = ref(false);
const crearCajaErrors = ref({
  numero: '',
  nombre: '',
  defaultMontoInicial: ''
});
const createMediosCierre = () => ([
  { medio: 'TARJETA', contado: 0 },
  { medio: 'TRANSFERENCIA', contado: 0 },
  { medio: 'APLICATIVO', contado: 0 },
  { medio: 'OTRO', contado: 0 }
]);
const cierreResumen = ref(null);
const cierreResumenLoading = ref(false);
const closeCajaLoading = ref(false);
const resumenCaja = ref(null);
const resumenCajaLoading = ref(false);
const efectivoContado = ref(0);
const mediosCierre = ref(createMediosCierre());
const motivoDiferencia = ref('');
const movimientoCaja = ref(createMovimientoCaja());
const movimientoCajaLoading = ref(false);

const headers = [
  { title: 'Producto', value: 'nombre' },
  { title: 'SKU', value: 'sku' },
  { title: 'Cantidad', value: 'cantidad', align: 'center' },
  { title: 'Precio', value: 'precioUnitario', align: 'center' },
  { title: 'Subtotal', value: 'subtotal', align: 'center' },
  { title: '', value: 'acciones', align: 'end' }
];

const createSessionMeta = (sessionId = '') => ({
  sessionId,
  nextNumber: 1
});

const sessionMeta = ref(createSessionMeta());

const ventaId = computed(() => venta.value?.id || '');
const ventaEstado = computed(() => venta.value?.estado || 'SIN_VENTA');
const canEdit = computed(() => ventaEstado.value === 'BORRADOR');
const cajaAbierta = computed(() => cajaStatus.value === 'ABIERTA');
const canScan = computed(() => cajaAbierta.value && (ventaEstado.value === 'BORRADOR' || ventaEstado.value === 'SIN_VENTA'));
const cajaDisplay = computed(() => cajaNumero.value || 'n/a');
const cajeroDisplay = computed(() => cajaNombre.value || 'n/a');
const turnoDisplay = computed(() => formatTurno(cajaTurno.value));
const aperturaDisplay = computed(() => formatDateTime(cajaAperturaAt.value));
const ventaNumeroDisplay = computed(() => (cajaAbierta.value ? displayVentaNumero.value || 1 : '-'));

const totalBruto = computed(() => {
  if (pricing.value?.totalBruto != null) return pricing.value.totalBruto;
  return items.value.reduce((acc, item) => acc + item.subtotal, 0);
});

const totalDescuento = computed(() => {
  if (pricing.value?.totalDescuento != null) return pricing.value.totalDescuento;
  return 0;
});

const totalNeto = computed(() => {
  if (pricing.value?.totalNeto != null) return pricing.value.totalNeto;
  return items.value.reduce((acc, item) => acc + item.subtotal, 0);
});

const totalItems = computed(() => items.value.reduce((acc, item) => acc + item.cantidad, 0));

const totalPagos = computed(() => pagos.value.reduce((acc, line) => acc + (line.monto || 0), 0));
const diferenciaPagos = computed(() => totalPagos.value - totalNeto.value);
const canAddPago = computed(() => pagos.value.length < MAX_MEDIOS_PAGO);
const canRemovePago = computed(() => pagos.value.length > 1);
const resumenCajaMedios = computed(() => {
  if (!resumenCaja.value?.medios?.length) return [];
  return resumenCaja.value.medios.map((medio) => ({
    medio: medio.medio,
    total: medio.teorico || 0,
    teorico: medio.teorico || 0
  }));
});
const teoricoTotalCierre = computed(() => cierreResumen.value?.saldoActual || 0);
const totalContadoCierre = computed(() => {
  const otros = mediosCierre.value.reduce((acc, medio) => acc + Number(medio.contado || 0), 0);
  return Number(efectivoContado.value || 0) + otros;
});
const diferenciaCierre = computed(() => totalContadoCierre.value - teoricoTotalCierre.value);
const needsMotivoCierre = computed(() => Math.abs(diferenciaCierre.value) > 0.0001);

const canConfirm = computed(() => {
  if (!canEdit.value || !items.value.length) return false;
  if (totalNeto.value <= 0) return false;
  if (Math.abs(diferenciaPagos.value) > 0.0001) return false;
  if (facturacionSeleccion.value === null) return false;
  return true;
});
const canSaveAbrirCaja = computed(() => {
  if (cajaAbierta.value || openCajaLoading.value) return false;
  if (!cajaAperturaId.value) return false;
  if (cajaAperturaId.value === CREATE_NEW_CAJA_OPTION_ID) return false;
  return true;
});
const canSaveMovimientoCaja = computed(() => {
  if (!dialogMovimientoCaja.value || !cajaAbierta.value || !cajaSessionId.value) return false;
  if (movimientoCajaLoading.value) return false;
  if (!movimientoCaja.value.motivo.trim()) return false;
  if (Number(movimientoCaja.value.monto || 0) === 0) return false;
  return true;
});
const canSaveCerrarCaja = computed(() => {
  if (!dialogCerrarCaja.value || !cajaAbierta.value || !cajaSessionId.value) return false;
  if (cierreResumenLoading.value || closeCajaLoading.value) return false;
  if (!cierreResumen.value) return false;
  return true;
});
const cajaAperturaOptions = computed(() => [
  ...cajasDisponibles.value,
  {
    id: CREATE_NEW_CAJA_OPTION_ID,
    label: 'Crear una nueva caja'
  }
]);

const loadCajaSession = () => {
  const previousSessionId = cajaSessionId.value;
  const raw = localStorage.getItem('pos-caja-session');
  if (!raw) {
    cajaStatus.value = 'CERRADA';
    cajaSessionId.value = '';
    cajaNumero.value = '';
    cajaNombre.value = '';
    cajaTurno.value = '';
    cajaAperturaAt.value = '';
    resetVentaWorkspace();
    clearSessionMeta();
    return;
  }
  try {
    const session = JSON.parse(raw);
    if (session?.estado === 'ABIERTA') {
      cajaStatus.value = 'ABIERTA';
      cajaSessionId.value = session.id || '';
      cajaNumero.value = session.cajaNumero || shortId(session.cajaId);
      cajaNombre.value = session.cajaNombre || 'n/a';
      cajaTurno.value = session.turno || '';
      cajaAperturaAt.value = session.aperturaAt || '';
      if (session.cajaId) {
        localStorage.setItem(POS_LAST_CAJA_KEY, session.cajaId);
      }
      if (previousSessionId && previousSessionId !== cajaSessionId.value) {
        resetVentaWorkspace();
      }
      syncSessionMeta();
    } else {
      cajaStatus.value = 'CERRADA';
      cajaSessionId.value = '';
      cajaNumero.value = '';
      cajaNombre.value = '';
      cajaTurno.value = '';
      cajaAperturaAt.value = '';
      resetVentaWorkspace();
      clearSessionMeta();
    }
  } catch {
    cajaStatus.value = 'CERRADA';
    cajaSessionId.value = '';
    cajaNumero.value = '';
    cajaNombre.value = '';
    cajaTurno.value = '';
    cajaAperturaAt.value = '';
    resetVentaWorkspace();
    clearSessionMeta();
  }
};

const saveVentaId = (id) => {
  if (!id) return;
  localStorage.setItem(POS_VENTA_KEY, id);
};

const clearVentaId = () => {
  localStorage.removeItem(POS_VENTA_KEY);
};

const persistSessionMeta = () => {
  localStorage.setItem(POS_SESSION_META_KEY, JSON.stringify(sessionMeta.value));
};

const clearSessionMeta = () => {
  sessionMeta.value = createSessionMeta();
  localStorage.removeItem(POS_SESSION_META_KEY);
  displayVentaNumero.value = null;
};

const resolveNextVentaNumber = () => {
  const nextNumber = Number(sessionMeta.value.nextNumber);
  return nextNumber > 0 ? nextNumber : 1;
};

const refreshDisplayVentaNumero = () => {
  if (!cajaAbierta.value) {
    displayVentaNumero.value = null;
    return;
  }

  displayVentaNumero.value = resolveNextVentaNumber();
};

const syncSessionMeta = () => {
  if (!cajaSessionId.value) {
    clearSessionMeta();
    return;
  }

  try {
    const raw = localStorage.getItem(POS_SESSION_META_KEY);
    const parsed = raw ? JSON.parse(raw) : null;
    if (parsed?.sessionId === cajaSessionId.value) {
      sessionMeta.value = {
        ...createSessionMeta(cajaSessionId.value),
        ...parsed,
        sessionId: cajaSessionId.value
      };
      refreshDisplayVentaNumero();
      return;
    }
  } catch {
    // Si el estado local queda corrupto, se reinicia para la nueva sesion.
  }

  sessionMeta.value = createSessionMeta(cajaSessionId.value);
  refreshDisplayVentaNumero();
  persistSessionMeta();
};

const advanceVentaNumber = () => {
  if (!cajaAbierta.value) {
    displayVentaNumero.value = null;
    return;
  }

  if (sessionMeta.value.sessionId !== cajaSessionId.value) {
    sessionMeta.value = createSessionMeta(cajaSessionId.value);
  }

  const nextNumber = resolveNextVentaNumber();
  sessionMeta.value = {
    sessionId: cajaSessionId.value,
    nextNumber: nextNumber + 1
  };
  refreshDisplayVentaNumero();
  persistSessionMeta();
};

const resetVentaWorkspace = () => {
  venta.value = null;
  items.value = [];
  pricing.value = null;
  qtyEdits.value = {};
  pagos.value = [];
  facturacionSeleccion.value = true;
  imprimirRecibo.value = false;
  productoSeleccionado.value = null;
  productosEncontrados.value = [];
  scanInput.value = '';
  dialogPagos.value = false;
  dialogStock.value = false;
  clearVentaId();
  refreshDisplayVentaNumero();
};

const resetCierreCajaForm = () => {
  cierreResumen.value = null;
  efectivoContado.value = 0;
  mediosCierre.value = createMediosCierre();
  motivoDiferencia.value = '';
};

const resetMovimientoCajaForm = () => {
  movimientoCaja.value = createMovimientoCaja();
};

const resetResumenCaja = () => {
  resumenCaja.value = null;
};

const resetAbrirCajaForm = () => {
  cajaAperturaId.value = '';
  montoInicialApertura.value = 0;
  turnoApertura.value = 'MANANA';
};

const resetNuevaCajaForm = () => {
  nuevaCajaNumero.value = '';
  nuevaCajaNombre.value = '';
  defaultMontoInicialNuevaCaja.value = null;
  crearCajaErrors.value = {
    numero: '',
    nombre: '',
    defaultMontoInicial: ''
  };
};

const scanFlash = ref('');
const scanFlashClass = computed(() => {
  if (scanFlash.value === 'success') return 'scan-flash-success';
  if (scanFlash.value === 'error') return 'scan-flash-error';
  return '';
});

const stockFaltantes = computed(() => items.value.map((item) => ({
  id: item.id,
  nombre: item.nombre,
  cantidad: item.cantidad
})));

const shortId = (value) => {
  if (!value) return 'n/a';
  return value.length > 8 ? value.slice(0, 8) : value;
};

const getRow = (row) => row?.raw ?? row;

const focusScan = () => {
  nextTick(() => {
    const el = scanInputRef.value?.$el?.querySelector('input');
    if (el && !el.disabled) el.focus();
  });
};

const maybeRestoreScan = () => {
  setTimeout(() => {
    const activeTag = document.activeElement?.tagName || '';
    if (activeTag === 'INPUT' || activeTag === 'TEXTAREA') return;
    focusScan();
  }, 0);
};

const flash = (type, text) => {
  snackbar.value = {
    show: true,
    text,
    color: type === 'success' ? 'success' : 'error',
    icon: type === 'success' ? 'mdi-check-circle' : 'mdi-alert-circle'
  };
  scanFlash.value = type;
  setTimeout(() => {
    scanFlash.value = '';
  }, 250);
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

const mapCajas = (items) =>
  (items || []).map((item) => {
    const numero = item.numero || shortId(item.id);
    return {
      ...item,
      label: `${numero} - ${item.nombre}`
    };
  });

const getPreferredCajaId = (items) => {
  if (!items.length) return '';

  const lastCajaId = localStorage.getItem(POS_LAST_CAJA_KEY);
  if (lastCajaId && items.some((item) => item.id === lastCajaId)) {
    return lastCajaId;
  }

  return items[0].id;
};

const validarCrearCaja = () => {
  crearCajaErrors.value = { numero: '', nombre: '', defaultMontoInicial: '' };
  if (!nuevaCajaNumero.value.trim()) {
    crearCajaErrors.value.numero = 'El numero es obligatorio.';
  } else if (!/^\d+$/.test(nuevaCajaNumero.value.trim())) {
    crearCajaErrors.value.numero = 'El numero debe ser solo digitos.';
  }

  if (!nuevaCajaNombre.value.trim()) {
    crearCajaErrors.value.nombre = 'El nombre es obligatorio.';
  }

  if (defaultMontoInicialNuevaCaja.value != null && Number(defaultMontoInicialNuevaCaja.value) < 0) {
    crearCajaErrors.value.defaultMontoInicial = 'El monto inicial debe ser mayor o igual a 0.';
  }

  return !crearCajaErrors.value.numero && !crearCajaErrors.value.nombre && !crearCajaErrors.value.defaultMontoInicial;
};

const loadCajasDisponibles = async () => {
  cajasLoading.value = true;
  try {
    const { response, data } = await getJson('/api/v1/caja?activo=true');
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    cajasDisponibles.value = mapCajas(data || []);
    if (!cajaAbierta.value) {
      cajaAperturaId.value = getPreferredCajaId(cajasDisponibles.value);
    }
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar cajas.');
  } finally {
    cajasLoading.value = false;
  }
};

const ensureVenta = async () => {
  if (ventaId.value) return ventaId.value;
  await createVenta();
  return ventaId.value;
};

const applyItemDto = (dto) => {
  const index = items.value.findIndex((item) => item.id === dto.id);
  const item = {
    id: dto.id,
    productoId: dto.productoId,
    nombre: dto.nombre,
    sku: dto.sku,
    cantidad: dto.cantidad,
    precioUnitario: dto.precioUnitario,
    subtotal: dto.subtotal ?? dto.cantidad * dto.precioUnitario
  };

  if (index >= 0) {
    items.value.splice(index, 1, item);
  } else {
    items.value.unshift(item);
  }
  qtyEdits.value[item.id] = item.cantidad;
  pricing.value = null;
};

const resetProductInput = () => {
  productoSeleccionado.value = null;
  scanInput.value = '';
  productosEncontrados.value = [];
};

const createVenta = async () => {
  if (!cajaAbierta.value) {
    flash('error', 'Caja cerrada');
    return;
  }
  if (creatingVenta.value) return;
  creatingVenta.value = true;
  try {
    const { response, data } = await postJson('/api/v1/ventas', {});
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    venta.value = data;
    saveVentaId(venta.value?.id);
    items.value = data.items?.map((item) => ({
      ...item,
      subtotal: item.subtotal ?? item.cantidad * item.precioUnitario
    })) || [];
    qtyEdits.value = {};
    items.value.forEach((item) => {
      qtyEdits.value[item.id] = item.cantidad;
    });
    pricing.value = null;
    pagos.value = [];
    facturacionSeleccion.value = true;
    imprimirRecibo.value = false;
    flash('success', 'Venta creada');
  } catch (err) {
    flash('error', err?.message || 'No se pudo crear la venta.');
  } finally {
    creatingVenta.value = false;
    focusScan();
  }
};

const restoreVenta = async () => {
  if (!cajaSessionId.value) {
    resetVentaWorkspace();
    return;
  }
  const savedId = localStorage.getItem(POS_VENTA_KEY);
  if (!savedId) return;

  try {
    const { response, data } = await getJson(`/api/v1/ventas/${savedId}`);
    if (!response.ok) {
      clearVentaId();
      return;
    }
    venta.value = data;
    items.value = data.items?.map((item) => ({
      ...item,
      subtotal: item.subtotal ?? item.cantidad * item.precioUnitario
    })) || [];
    qtyEdits.value = {};
    items.value.forEach((item) => {
      qtyEdits.value[item.id] = item.cantidad;
    });
    pricing.value = null;
    pagos.value = [];
    refreshDisplayVentaNumero();
  } catch {
    clearVentaId();
    refreshDisplayVentaNumero();
  }
};

const handleScan = async () => {
  const code = scanInput.value.trim();
  if (!code || scanLoading.value || !canScan.value) return;
  if (!cajaAbierta.value) {
    flash('error', 'Caja cerrada');
    return;
  }

  scanLoading.value = true;

  try {
    if (productoSeleccionado.value?.id) {
      await onProductoSeleccionado(productoSeleccionado.value);
      return;
    }

    const id = await ensureVenta();
    if (!id) return;

    const { response, data } = await postJson(`/api/v1/ventas/${id}/items/scan`, { code });
    if (!response.ok) {
      const message = extractProblemMessage(data);
      if (response.status === 404) {
        flash('error', 'SKU no encontrado');
      } else {
        flash('error', message);
      }
      return;
    }

    applyItemDto(data);
    resetProductInput();
    flash('success', `Agregado: ${data.nombre}`);
  } catch (err) {
    flash('error', err?.message || 'Error al escanear.');
  } finally {
    scanLoading.value = false;
    focusScan();
  }
};

const mapProductoResults = (items) =>
  (items || []).map((item) => ({
    ...item,
    label: `${item.name} (${item.sku})`
  }));

const searchProductos = (term) => {
  if (!cajaAbierta.value) {
    productosEncontrados.value = [];
    return;
  }
  if (productoSearchTimer) {
    clearTimeout(productoSearchTimer);
  }
  if (!term || term.trim().length < 2) {
    productosEncontrados.value = [];
    return;
  }
  productoSearchTimer = setTimeout(async () => {
    productoSearchLoading.value = true;
    try {
      const params = new URLSearchParams();
      params.set('search', term.trim());
      params.set('activo', 'true');
      const { response, data } = await getJson(`/api/v1/productos?${params.toString()}`);
      if (!response.ok) {
        throw new Error(extractProblemMessage(data));
      }
      productosEncontrados.value = mapProductoResults(data || []);
    } catch (err) {
      flash('error', err?.message || 'No se pudieron buscar productos.');
    } finally {
      productoSearchLoading.value = false;
    }
  }, 250);
};

const onProductoSeleccionado = async (producto) => {
  if (!producto?.id) return;
  if (!cajaAbierta.value) {
    flash('error', 'Caja cerrada');
    return;
  }

  const id = await ensureVenta();
  if (!id) return;

  try {
    const { response, data } = await postJson(`/api/v1/ventas/${id}/items`, {
      productId: producto.id
    });
    if (!response.ok) {
      const message = extractProblemMessage(data);
      flash('error', message);
      return;
    }
    applyItemDto(data);
    flash('success', `Agregado: ${data.nombre}`);
  } catch (err) {
    flash('error', err?.message || 'No se pudo agregar el producto.');
  } finally {
    resetProductInput();
    focusScan();
  }
};

const removeItem = async (item) => {
  if (!canEdit.value) return;
  if (!ventaId.value) return;
  try {
    const { response, data } = await requestJson(`/api/v1/ventas/${ventaId.value}/items/${item.id}`, {
      method: 'DELETE'
    });
    if (!response.ok) {
      if (response.status === 404 || response.status === 405) {
        const fallback = await requestJson(`/api/v1/ventas/${ventaId.value}/items/${item.id}`, {
          method: 'PATCH',
          body: JSON.stringify({ cantidad: 0 })
        });
        if (!fallback.response.ok) {
          const message = extractProblemMessage(fallback.data);
          flash('error', message);
          return;
        }
      } else {
        const message = extractProblemMessage(data);
        flash('error', message);
        return;
      }
    }
    items.value = items.value.filter((i) => i.id !== item.id);
    delete qtyEdits.value[item.id];
    flash('success', 'Item eliminado');
  } catch (err) {
    flash('error', err?.message || 'No se pudo eliminar el item.');
  }
};

const clearCarrito = async () => {
  if (!canEdit.value || !items.value.length || anularLoading.value) return;
  if (!ventaId.value) return;

  anularLoading.value = true;
  try {
    const currentItems = [...items.value];
    for (const item of currentItems) {
      const { response } = await requestJson(`/api/v1/ventas/${ventaId.value}/items/${item.id}`, {
        method: 'DELETE'
      });

      if (!response.ok) {
        const fallback = await requestJson(`/api/v1/ventas/${ventaId.value}/items/${item.id}`, {
          method: 'PATCH',
          body: JSON.stringify({ cantidad: 0 })
        });
        if (!fallback.response.ok) {
          throw new Error(extractProblemMessage(fallback.data));
        }
      }
    }

    items.value = [];
    qtyEdits.value = {};
    pricing.value = null;
    flash('success', 'Carrito vaciado');
    focusScan();
  } catch (err) {
    flash('error', err?.message || 'No se pudo vaciar el carrito.');
  } finally {
    anularLoading.value = false;
  }
};

const commitQty = async (item) => {
  const cantidad = Number(qtyEdits.value[item.id]);
  if (!canEdit.value) return;
  if (!cantidad || cantidad <= 0 || Number.isNaN(cantidad)) {
    qtyEdits.value[item.id] = item.cantidad;
    flash('error', 'Cantidad invalida');
    return;
  }
  if (cantidad === item.cantidad) return;
  if (!ventaId.value) return;

  try {
    const { response, data } = await requestJson(`/api/v1/ventas/${ventaId.value}/items/${item.id}`, {
      method: 'PATCH',
      body: JSON.stringify({ cantidad })
    });

    if (!response.ok) {
      const message = extractProblemMessage(data);
      qtyEdits.value[item.id] = item.cantidad;
      flash('error', message);
      return;
    }

    applyItemDto(data);
  } catch (err) {
    qtyEdits.value[item.id] = item.cantidad;
    flash('error', err?.message || 'Error al actualizar item.');
  }
};

const createPagoLine = (medioPago = 'EFECTIVO') => ({
  id: `${Date.now()}-${Math.random()}`,
  medioPago,
  monto: 0,
  recibido: 0,
  base: false
});

const nextAvailableMedioPago = () => (
  mediosPagoOptions.find((option) => !pagos.value.some((line) => line.medioPago === option.value))?.value
  || mediosPagoOptions[0].value
);

const availableMediosPagoOptions = (index) => {
  const actual = pagos.value[index]?.medioPago;
  const usados = new Set(
    pagos.value
      .filter((_, lineIndex) => lineIndex !== index)
      .map((line) => line.medioPago)
  );

  return mediosPagoOptions.filter((option) => option.value === actual || !usados.has(option.value));
};

const ensurePagoBase = () => {
  if (!pagos.value.length) {
    const created = createPagoLine('EFECTIVO');
    created.base = true;
    created.monto = totalNeto.value;
    created.recibido = totalNeto.value;
    pagos.value = [created];
    return created;
  }

  pagos.value.forEach((line, index) => {
    line.base = index === 0;
  });

  return pagos.value[0];
};

const syncPagosBase = () => {
  if (!pagos.value.length) return;

  const baseLine = ensurePagoBase();
  baseLine.base = true;

  const previousMonto = Number(baseLine.monto || 0);
  const previousRecibido = Number(baseLine.recibido || 0);
  const otrosTotal = pagos.value
    .filter((line) => !line.base)
    .reduce((acc, line) => acc + Number(line.monto || 0), 0);
  const nuevoMonto = Math.max(Number(totalNeto.value || 0) - otrosTotal, 0);

  if (previousMonto !== nuevoMonto) {
    baseLine.monto = nuevoMonto;
  }

  if (baseLine.medioPago === 'EFECTIVO' && (previousRecibido === previousMonto || previousRecibido === 0)) {
    baseLine.recibido = nuevoMonto;
  }

  if (baseLine.medioPago !== 'EFECTIVO') {
    baseLine.recibido = 0;
  }
};

watch(
  [pagos, totalNeto],
  () => {
    syncPagosBase();
  },
  { deep: true }
);

const openPagos = () => {
  dialogPagos.value = true;
  facturacionSeleccion.value = true;
  imprimirRecibo.value = false;
  ensurePagoBase();
  syncPagosBase();
};

const openAbrirCajaDialog = async () => {
  if (cajaAbierta.value || openCajaLoading.value) return;

  resetAbrirCajaForm();
  resetNuevaCajaForm();
  dialogAbrirCaja.value = true;
  await loadCajasDisponibles();
};

const openCerrarCajaDialog = async () => {
  if (!cajaAbierta.value || !cajaSessionId.value || cierreResumenLoading.value) return;

  dialogCerrarCaja.value = true;
  resetCierreCajaForm();
  cierreResumenLoading.value = true;

  try {
    const { response, data } = await getJson(`/api/v1/caja/sesiones/${cajaSessionId.value}/resumen`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    cierreResumen.value = data;
  } catch (err) {
    dialogCerrarCaja.value = false;
    flash('error', err?.message || 'No se pudo cargar el resumen de caja.');
  } finally {
    cierreResumenLoading.value = false;
  }
};

const addPago = () => {
  if (!canAddPago.value) return;

  pagos.value.push(createPagoLine(nextAvailableMedioPago()));
  syncPagosBase();
};

const removePago = (index) => {
  if (!canRemovePago.value) return;
  pagos.value.splice(index, 1);
  syncPagosBase();
};

const vuelto = (line) => {
  if (line.medioPago !== 'EFECTIVO') return 0;
  const recibido = Number(line.recibido || 0);
  const monto = Number(line.monto || 0);
  return recibido > monto ? recibido - monto : 0;
};

const registrarMovimientoCaja = async () => {
  if (!canSaveMovimientoCaja.value || !cajaSessionId.value) return;

  movimientoCajaLoading.value = true;
  try {
    const payload = {
      tipo: movimientoCaja.value.tipo,
      motivo: movimientoCaja.value.motivo.trim(),
      monto: Number(movimientoCaja.value.monto || 0),
      medioPago: movimientoCaja.value.medioPago
    };

    const { response, data } = await postJson(`/api/v1/caja/sesiones/${cajaSessionId.value}/movimientos`, payload);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    dialogMovimientoCaja.value = false;
    window.dispatchEvent(new CustomEvent('pos-caja-movimiento-changed'));
    flash('success', 'Movimiento registrado');
  } catch (err) {
    flash('error', err?.message || 'No se pudo registrar el movimiento.');
  } finally {
    movimientoCajaLoading.value = false;
  }
};

const loadResumenCaja = async () => {
  if (!cajaSessionId.value) return;

  resumenCajaLoading.value = true;
  try {
    const { response, data } = await getJson(`/api/v1/caja/sesiones/${cajaSessionId.value}/resumen`);
    if (!response.ok) {
      if (response.status === 404) {
        dialogResumenCaja.value = false;
        localStorage.removeItem('pos-caja-session');
        window.dispatchEvent(new CustomEvent('pos-caja-session-changed'));
        flash('error', 'La sesion de caja guardada ya no existe. Abri una nueva sesion.');
        return;
      }
      throw new Error(extractProblemMessage(data));
    }

    resumenCaja.value = data;
  } catch (err) {
    flash('error', err?.message || 'No se pudo cargar el resumen.');
  } finally {
    resumenCajaLoading.value = false;
  }
};

const crearNuevaCaja = async () => {
  if (crearCajaLoading.value) return;
  if (!validarCrearCaja()) return;

  crearCajaLoading.value = true;
  try {
    const payload = {
      numero: nuevaCajaNumero.value.trim(),
      nombre: nuevaCajaNombre.value.trim(),
      defaultMontoInicial: Number(defaultMontoInicialNuevaCaja.value || 0),
      isActive: true
    };

    const { response, data } = await postJson('/api/v1/caja', payload);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    await loadCajasDisponibles();
    cajaAperturaId.value = data.id;
    dialogNuevaCaja.value = false;
    flash('success', 'Caja creada');
  } catch (err) {
    flash('error', err?.message || 'No se pudo crear la caja.');
  } finally {
    crearCajaLoading.value = false;
  }
};

const guardarAperturaCaja = async () => {
  if (!canSaveAbrirCaja.value) return;

  openCajaLoading.value = true;
  try {
    const payload = {
      cajaId: cajaAperturaId.value,
      montoInicial: Number(montoInicialApertura.value || 0),
      turno: turnoApertura.value
    };

    const { response, data } = await postJson('/api/v1/caja/sesiones/abrir', payload);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    const cajaSeleccionada = cajasDisponibles.value.find((item) => item.id === cajaAperturaId.value);
    localStorage.setItem(POS_LAST_CAJA_KEY, cajaAperturaId.value);
    localStorage.setItem('pos-caja-session', JSON.stringify({
      ...data,
      cajaNumero: cajaSeleccionada?.numero || shortId(data.cajaId),
      cajaNombre: cajaSeleccionada?.nombre || 'n/a'
    }));
    dialogAbrirCaja.value = false;
    window.dispatchEvent(new CustomEvent('pos-caja-session-changed'));
    flash('success', 'Caja abierta');
  } catch (err) {
    flash('error', err?.message || 'No se pudo abrir la caja.');
  } finally {
    openCajaLoading.value = false;
  }
};

const confirmarVenta = async () => {
  if (!ventaId.value || confirmLoading.value) return;
  confirmLoading.value = true;
  const ventaIdActual = ventaId.value;

  try {
    if (facturacionSeleccion.value === null) {
      flash('error', 'Selecciona si la venta es facturada o no facturada.');
      return;
    }
    loadCajaSession();
    const payload = {
      pagos: pagos.value
        .filter((line) => line.medioPago && line.monto > 0)
        .map((line) => ({ medioPago: line.medioPago, monto: line.monto })),
      cajaSesionId: cajaSessionId.value || null,
      facturada: facturacionSeleccion.value
    };

    const { response, data } = await postJson(`/api/v1/ventas/${ventaId.value}/confirmar`, payload);
    if (!response.ok) {
      const message = extractProblemMessage(data);
      if (message.toLowerCase().includes('stock insuficiente')) {
        dialogStock.value = true;
      } else if (message.toLowerCase().includes('caja')) {
        cajaStatus.value = 'CERRADA';
      }
      throw new Error(message);
    }

    let ventaConfirmada = data.venta || data.Venta || data;
    const pagosConfirmados = data.pagos || data.Pagos || [];

    if ((!ventaConfirmada?.numero && !ventaConfirmada?.Numero) && ventaIdActual) {
      const { response: ventaResponse, data: ventaData } = await getJson(`/api/v1/ventas/${ventaIdActual}`);
      if (ventaResponse.ok && ventaData) {
        ventaConfirmada = ventaData;
      }
    }
    venta.value = ventaConfirmada;
    clearVentaId();
    if (imprimirRecibo.value) {
      printTicket(ventaConfirmada, pagosConfirmados);
    }
    flash('success', 'Venta confirmada');
    advanceVentaNumber();
    // limpiar para nueva venta
    venta.value = null;
    items.value = [];
    qtyEdits.value = {};
    pricing.value = null;
    cajaStatus.value = 'ABIERTA';
    dialogPagos.value = false;
    pagos.value = [];
    facturacionSeleccion.value = true;
    imprimirRecibo.value = false;
    resetProductInput();
    focusScan();
  } catch (err) {
    flash('error', err?.message || 'Error al confirmar venta.');
  } finally {
    confirmLoading.value = false;
  }
};

const guardarCierreCaja = async () => {
  if (!canSaveCerrarCaja.value || !cajaSessionId.value) return;
  closeCajaLoading.value = true;

  try {
    const medios = mediosCierre.value
      .filter((medio) => Number(medio.contado) > 0)
      .map((medio) => ({ medio: medio.medio, contado: Number(medio.contado || 0) }));

    const payload = {
      efectivoContado: Number(efectivoContado.value || 0),
      medios,
      motivoDiferencia: motivoDiferencia.value.trim() || null
    };

    const { response, data } = await postJson(`/api/v1/caja/sesiones/${cajaSessionId.value}/cerrar`, payload);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    dialogCerrarCaja.value = false;
    localStorage.removeItem('pos-caja-session');
    window.dispatchEvent(new CustomEvent('pos-caja-session-changed'));
    flash('success', 'Caja cerrada');
  } catch (err) {
    flash('error', err?.message || 'No se pudo cerrar la caja.');
  } finally {
    closeCajaLoading.value = false;
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

const formatTurno = (value) => {
  if (!value) return '-';
  if (value === 'MANANA') return 'MAÑANA';
  return value;
};

const printTicket = (ventaData, pagosData) => {
  if (!ventaData) return;
  const win = window.open('', '_blank', 'width=380,height=600');
  if (!win) return;
  const esFacturada = (ventaData.facturada ?? ventaData.Facturada) === true;

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
          @page { margin: 8mm; }
          body { font-family: Arial, sans-serif; font-size: 12px; padding: 10px; }
          h1 { margin: 0 0 6px 0; font-size: 14px; }
          table { width: 100%; border-collapse: collapse; margin-top: 8px; }
          th, td { border-bottom: 1px dashed #ccc; padding: 4px 0; }
          th { text-align: left; font-weight: 600; }
          .ticket-actions { display: flex; gap: 8px; margin-bottom: 12px; }
          .ticket-actions button {
            border: 1px solid #d6d0c6;
            background: #fff;
            border-radius: 999px;
            padding: 6px 12px;
            cursor: pointer;
          }
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
              type: 'pos-open-ticket-preview',
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
        <div>Venta Nï¿½: ${ventaData.numero ?? ventaData.Numero ?? '-'}</div>
        <div>Fecha: ${formatDateTime(ventaData.createdAt)}</div>
        <div>Facturacion: ${esFacturada ? 'FACTURADA' : 'NO FACTURADA'}</div>
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
        <div style="margin-top:8px"><strong>Total: ${formatMoney(ventaData.totalNeto ?? totalNeto.value)}</strong></div>
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
  if (event?.data?.type !== 'pos-open-ticket-preview') return;
  openDesktopTicketPreview(event.data.html);
};

watch(dialogPagos, (open) => {
  if (!open) {
    focusScan();
  }
});

watch(dialogMovimientoCaja, (open) => {
  if (!open) {
    resetMovimientoCajaForm();
    focusScan();
  }
});

watch(dialogResumenCaja, (open) => {
  if (open) {
    loadResumenCaja();
    return;
  }

  resetResumenCaja();
  focusScan();
});

watch(dialogAbrirCaja, (open) => {
  if (!open) {
    resetAbrirCajaForm();
    dialogNuevaCaja.value = false;
    focusScan();
  }
});

watch(dialogNuevaCaja, (open) => {
  if (!open) {
    resetNuevaCajaForm();
  }
});

watch(cajaAperturaId, (value, previousValue) => {
  if (value === CREATE_NEW_CAJA_OPTION_ID) {
    dialogNuevaCaja.value = true;
    cajaAperturaId.value = previousValue && previousValue !== CREATE_NEW_CAJA_OPTION_ID
      ? previousValue
      : getPreferredCajaId(cajasDisponibles.value);
    return;
  }

  const cajaSeleccionada = cajasDisponibles.value.find((item) => item.id === value);
  if (!cajaSeleccionada) {
    montoInicialApertura.value = 0;
    return;
  }

  montoInicialApertura.value = Number(cajaSeleccionada.defaultMontoInicial || 0);
});

watch(dialogCerrarCaja, (open) => {
  if (!open) {
    resetCierreCajaForm();
    focusScan();
  }
});

const handleCajaSessionChanged = () => {
  loadCajaSession();
  if (!cajaSessionId.value) {
    dialogResumenCaja.value = false;
    resetResumenCaja();
  } else if (dialogResumenCaja.value) {
    loadResumenCaja();
  }
  if (cajaSessionId.value) {
    restoreVenta();
    focusScan();
  }
};

onMounted(() => {
  loadCajaSession();
  restoreVenta();
  focusScan();
  window.addEventListener('pos-caja-session-changed', handleCajaSessionChanged);
  window.addEventListener('message', handleTicketPreviewMessage);
});

onBeforeUnmount(() => {
  window.removeEventListener('pos-caja-session-changed', handleCajaSessionChanged);
  window.removeEventListener('message', handleTicketPreviewMessage);
});
</script>

<style scoped>
.pos-page {
  animation: fade-in 0.3s ease;
}

.pos-scan {
  width: 100%;
  min-width: 0;
}

.pos-search {
  min-width: 280px;
}

.pos-main-column,
.pos-side-column {
  display: flex;
  flex-direction: column;
}

.pos-sales-shell {
  align-items: stretch;
}

.pos-cart-card {
  flex: 1;
}

.pos-qty-field :deep(input) {
  text-align: center;
}

.pos-payment-total {
  display: grid;
  gap: 4px;
  padding: 14px 16px;
  border-radius: 16px;
  border: 1px solid color-mix(in srgb, var(--pos-accent-strong) 16%, var(--pos-border));
  background: linear-gradient(
    135deg,
    color-mix(in srgb, var(--pos-accent-strong) 10%, var(--pos-card)) 0%,
    color-mix(in srgb, var(--pos-accent-strong) 4%, var(--pos-card)) 100%
  );
}

.pos-payment-total span {
  font-size: 0.9rem;
  color: var(--pos-ink-muted);
}

.pos-payment-total strong {
  font-size: 1.4rem;
  line-height: 1.1;
  color: var(--pos-accent-dark);
}

.pos-payment-row {
  display: grid;
  gap: 10px;
  align-items: center;
  grid-template-columns: minmax(170px, 210px) minmax(0, 1fr) max-content auto;
}

.pos-payment-field-col {
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
}

.pos-payment-auto-note {
  min-height: 40px;
  padding: 0 12px;
  border-radius: 14px;
  border: 1px dashed color-mix(in srgb, var(--pos-accent-strong) 16%, var(--pos-border));
  color: var(--pos-ink-muted);
  display: flex;
  align-items: center;
  font-size: 0.76rem;
  line-height: 1.1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.pos-payment-side-col {
  display: flex;
  align-self: start;
  min-height: 40px;
  align-items: center;
  justify-content: center;
}

.pos-payment-action-col {
  display: flex;
  align-self: start;
  min-height: 40px;
  align-items: center;
  justify-content: center;
}

.pos-payment-delete-btn {
  width: 40px;
  min-width: 40px;
  height: 40px;
}

.pos-payment-change {
  min-height: 40px;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  gap: 6px;
  white-space: nowrap;
  color: var(--pos-ink-muted);
  font-size: 0.88rem;
}

.pos-payment-change strong {
  color: var(--pos-accent-dark);
  font-size: 0.94rem;
}

.pos-payment-no-change {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 40px;
  color: var(--pos-ink-muted);
  font-size: 0.88rem;
  white-space: nowrap;
}

.pos-payment-money-field :deep(.v-field__append-inner) {
  gap: 1px;
  padding-inline-start: 0;
}

.pos-payment-money-field :deep(.money-field__append-inner) {
  gap: 1px;
}

.pos-payment-money-field :deep(.money-field__steppers) {
  gap: 0;
}

.pos-payment-money-field :deep(.money-field__step-btn) {
  min-width: 12px;
  width: 12px;
  height: 14px;
  font-size: 10px;
  line-height: 1;
}

@media (max-width: 960px) {
  .pos-payment-row {
    grid-template-columns: 1fr;
  }

  .pos-payment-side-col,
  .pos-payment-action-col {
    justify-content: flex-start;
  }
}

.pos-billing-toggle {
  border: 1px solid color-mix(in srgb, var(--pos-accent-strong) 18%, var(--pos-border));
  border-radius: 14px;
  overflow: hidden;
  background: color-mix(in srgb, var(--pos-accent-strong) 4%, var(--pos-card));
}

.pos-billing-option {
  min-height: 48px;
  min-width: 124px;
  font-weight: 700;
}

.pos-payment-cancel-btn {
  min-height: 46px;
  min-width: 140px;
  font-weight: 700;
  background: var(--pos-cancel-btn-bg) !important;
  color: var(--pos-cancel-btn-text) !important;
  border: 1px solid var(--pos-cancel-btn-border) !important;
  box-shadow: 0 12px 24px rgba(201, 150, 147, 0.22);
}

.pos-payment-confirm-btn {
  min-height: 46px;
  min-width: 152px;
  font-weight: 800;
  background: var(--pos-primary-btn-bg) !important;
  color: var(--pos-primary-btn-text) !important;
  box-shadow: var(--pos-primary-btn-shadow);
}

.pos-session-meta {
  justify-content: flex-end;
}

.pos-totals-card {
  padding: 20px 20px 18px;
  border: 1px solid rgba(153, 122, 126, 0.16);
  box-shadow: 0 16px 34px rgba(113, 76, 84, 0.08);
}

.pos-total-row {
  font-size: 15px;
}

.pos-total-net {
  display: grid;
  gap: 4px;
  margin-top: 16px;
  padding: 16px 18px;
  border-radius: 16px;
  background: linear-gradient(
    135deg,
    color-mix(in srgb, var(--pos-accent-strong) 12%, var(--pos-card)) 0%,
    color-mix(in srgb, var(--pos-accent-strong) 4%, var(--pos-card)) 100%
  );
  border: 1px solid color-mix(in srgb, var(--pos-accent-strong) 18%, var(--pos-border));
}

.pos-total-net strong {
  font-size: 2rem;
  line-height: 1;
  color: var(--pos-accent-dark);
}

.pos-primary-action {
  min-height: 52px;
  font-size: 1.05rem;
  font-weight: 800;
  letter-spacing: 0.01em;
  background: var(--pos-primary-btn-bg) !important;
  color: var(--pos-primary-btn-text) !important;
  box-shadow: var(--pos-primary-btn-shadow);
}

.pos-primary-action.v-btn--disabled {
  opacity: 1 !important;
  background: var(--pos-primary-btn-bg) !important;
  color: var(--pos-primary-btn-text) !important;
  box-shadow: var(--pos-primary-btn-shadow);
}

.pos-primary-action.v-btn--disabled :deep(.v-btn__overlay) {
  opacity: 0;
}

.pos-secondary-action {
  min-height: 46px;
  font-weight: 700;
  background: var(--pos-danger-btn-bg) !important;
  border-color: var(--pos-danger-btn-border) !important;
  color: var(--pos-danger-btn-text) !important;
}

.pos-secondary-action.v-btn--disabled {
  opacity: 1 !important;
  background: var(--pos-danger-btn-bg) !important;
  border-color: var(--pos-danger-btn-border) !important;
  color: var(--pos-danger-btn-text) !important;
}

.pos-secondary-action.v-btn--disabled :deep(.v-btn__overlay) {
  opacity: 0;
}

.pos-actions-card {
  margin-top: auto;
  border: 1px solid var(--pos-border);
  background: linear-gradient(180deg, var(--pos-card-top) 0%, var(--pos-card-bottom) 100%);
  box-shadow: var(--pos-shadow);
}

.pos-actions-title {
  color: var(--pos-accent-dark);
}

.pos-utility-action {
  min-height: 42px;
  justify-content: flex-start;
  margin-top: 10px;
  padding-inline: 10px;
  border: 1px solid color-mix(in srgb, var(--pos-accent-strong) 16%, var(--pos-border));
  color: var(--pos-accent-dark);
  font-weight: 600;
}

.pos-utility-action-danger {
  border-color: var(--pos-danger-btn-border);
  color: var(--pos-danger-btn-text);
  font-weight: 700;
  box-shadow: 0 10px 18px rgba(191, 54, 46, 0.08);
}

.pos-utility-action :deep(.v-btn__content) {
  width: 100%;
  justify-content: flex-start;
  gap: 8px;
}

.pos-utility-action :deep(.v-btn__prepend) {
  margin-inline-end: 8px;
}

.pos-sales-panel {
  position: relative;
}

.pos-sales-shell.is-locked {
  filter: grayscale(0.2) blur(1px);
  opacity: 0.35;
  pointer-events: none;
  user-select: none;
}

.pos-sales-overlay {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
}

.pos-sales-overlay-card {
  max-width: 420px;
  padding: 20px 24px;
  border: 1px solid var(--pos-border);
  border-radius: 18px;
  background: var(--pos-card-soft);
  box-shadow: 0 18px 40px rgba(59, 10, 18, 0.12);
  text-align: center;
}

.money-help-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  padding: 0;
  border: 1px solid color-mix(in srgb, var(--pos-accent-strong) 35%, transparent);
  border-radius: 999px;
  background: color-mix(in srgb, var(--pos-gold) 18%, var(--pos-card));
  color: var(--pos-accent-strong);
  font-size: 12px;
  font-weight: 800;
  line-height: 1;
  cursor: pointer;
}

.gap-1 {
  gap: 4px;
}

.gap-2 {
  gap: 8px;
}

.gap-3 {
  gap: 12px;
}
</style>


