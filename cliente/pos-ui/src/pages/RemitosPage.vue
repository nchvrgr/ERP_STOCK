<template>
  <div class="remitos-page">
    <v-card class="pos-card pa-4 remitos-main-card">
      <div class="d-flex align-center gap-3">
        <div>
          <div class="text-h6">Ingreso manual de remito</div>
        </div>
      </div>

      <v-alert v-if="error" type="error" variant="outlined" class="mt-3" density="compact">
        {{ error }}
      </v-alert>

      <div class="remitos-search-row mt-2">
        <div class="remitos-search-row__search">
          <v-autocomplete
            ref="productSearchFieldRef"
            :key="productoInputKey"
            v-model="selectedProductoId"
            v-model:search="productSearch"
            :items="productoOptions"
            item-title="label"
            item-value="id"
            label="Buscar producto o escanear SKU"
            variant="outlined"
            density="comfortable"
            clearable
            :loading="loadingProductos"
            @update:search="onProductSearch"
            @update:model-value="onProductoSeleccionado"
            @keydown.enter.prevent="handleProductInputEnter"
          >
            <template #no-data>
              <v-list-item
                v-if="productSearch.trim()"
                prepend-icon="mdi-plus-circle-outline"
                :title="buildNewProductSuggestion(productSearch.trim())"
                @click="openCreateProductDialog(productSearch.trim())"
              />
              <v-list-item
                v-else
                title="Sin resultados"
              />
            </template>
          </v-autocomplete>
        </div>
        <div class="remitos-search-row__provider">
          <v-autocomplete
            v-model="selectedProveedorId"
            :items="proveedores"
            item-title="name"
            item-value="id"
            label="Proveedor"
            variant="outlined"
            density="comfortable"
            clearable
            :loading="loadingProveedores"
            @update:model-value="onProveedorChanged"
          >
            <template #no-data>
              <v-list-item
                prepend-icon="mdi-plus-circle-outline"
                title="Crear proveedor"
                @click="openCreateProveedorDialog('')"
              />
            </template>
          </v-autocomplete>
        </div>
      </div>

      <div v-if="recentAddMessage" class="remitos-add-feedback">
        <span class="remitos-add-feedback__dot" />
        <span>{{ recentAddMessage }}</span>
      </div>

      <v-divider class="my-3" />

      <v-data-table
        :headers="headers"
        :items="items"
        item-key="productoId"
        density="compact"
        height="360"
        :items-per-page="-1"
        hide-default-footer
      >
        <template v-slot:[`header.feedback`]>
          <div class="remitos-marker-column" />
        </template>
        <template v-slot:[`header.cantidad`]>
          <div class="text-center w-100">Cantidad</div>
        </template>
        <template v-slot:[`item.feedback`]="{ item }">
          <div class="remitos-marker-column">
            <span
              v-if="recentlyAddedProductId === getTableItemData(item).productoId"
              class="recent-product-dot"
            />
          </div>
        </template>
        <template v-slot:[`item.name`]="{ item }">
          <span>{{ getTableItemData(item).name }}</span>
        </template>
        <template v-slot:[`item.precioVenta`]="{ item }">
          <div class="text-right">
            {{ formatMoney(getTableItemData(item).precioVenta) }}
          </div>
        </template>
        <template v-slot:[`item.cantidad`]="{ item }">
          <div class="quantity-stepper">
            <v-btn
              icon="mdi-minus"
              size="small"
              variant="text"
              color="primary"
              @click="decrementCantidad(getTableItemData(item).productoId)"
            />
            <v-text-field
              :model-value="getTableItemData(item).cantidad"
              type="number"
              min="1"
              step="1"
              variant="outlined"
              density="compact"
              hide-details
              class="quantity-stepper__input"
              inputmode="numeric"
              @update:model-value="(v) => updateCantidad(getTableItemData(item).productoId, v)"
            />
            <v-btn
              icon="mdi-plus"
              size="small"
              variant="text"
              color="primary"
              @click="incrementCantidad(getTableItemData(item).productoId)"
            />
          </div>
        </template>
        <template v-slot:[`item.actions`]="{ item }">
          <div class="d-flex align-center justify-end ga-1">
            <v-btn
              size="small"
              variant="tonal"
              color="primary"
              class="text-none"
              @click="openActualizarPrecioDialog(getTableItemData(item).productoId)"
            >
              Actualizar precio
            </v-btn>
            <v-btn icon="mdi-delete-outline" size="small" variant="text" color="error" @click="removeItem(getTableItemData(item).productoId)" />
          </div>
        </template>
      </v-data-table>

      <div class="d-flex justify-space-between align-center mt-4">
        <div class="text-caption text-medium-emphasis">
          Productos: {{ items.length }}
        </div>
        <div class="d-flex remitos-action-group">
          <v-btn variant="outlined" color="secondary" class="text-none remitos-action-btn" :disabled="saving" @click="clearForm">Limpiar</v-btn>
          <v-btn
            color="primary"
            class="text-none remitos-action-btn"
            :loading="saving"
            :disabled="!canSave"
            @click="confirmarIngreso"
          >
            Confirmar ingreso
          </v-btn>
        </div>
      </div>
    </v-card>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" location="top end" timeout="1700">
      <div class="d-flex align-center gap-2">
        <v-icon>{{ snackbar.icon }}</v-icon>
        <span>{{ snackbar.text }}</span>
      </div>
    </v-snackbar>

    <v-dialog v-model="comboPriceUpdateDialog" width="860">
      <v-card>
        <v-card-title>Actualizar precios de combos</v-card-title>
        <v-card-text>
          <div class="text-body-2 mb-3">
            Se detectaron combos que incluyen productos con precio modificado. Podes actualizar su precio final.
          </div>
          <v-data-table
            :headers="comboPriceUpdateHeaders"
            :items="comboPriceUpdateItems"
            item-key="id"
            density="compact"
          >
            <template v-slot:[`item.aplicar`]="{ item }">
              <v-checkbox
                :model-value="item.aplicar"
                hide-details
                color="primary"
                @update:model-value="(value) => (item.aplicar = value)"
              />
            </template>
            <template v-slot:[`item.precioVentaActual`]="{ item }">
              {{ formatMoney(item.precioVentaActual) }}
            </template>
            <template v-slot:[`item.precioVentaSugerido`]="{ item }">
              {{ formatMoney(item.precioVentaSugerido) }}
            </template>
            <template v-slot:[`item.precioVentaObjetivo`]="{ item }">
              <MoneyField
                v-model="item.precioVentaObjetivo"
                variant="outlined"
                density="compact"
                hide-details
                :step="100"
                :show-stepper="false"
              />
            </template>
          </v-data-table>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" :disabled="comboPriceUpdateLoading" @click="cancelComboPriceUpdates">Omitir</v-btn>
          <v-btn color="primary" :loading="comboPriceUpdateLoading" @click="applyComboPriceUpdates">Actualizar seleccionados</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="createDialog" persistent width="760">
      <v-card class="pa-4">
        <div>
          <div class="text-h6">Nuevo producto</div>
          <div class="text-caption text-medium-emphasis"></div>
        </div>
        <v-card-text class="pt-4 px-0">
          <v-form class="mt-3" @submit.prevent="createProductFromIngreso">
            <v-row dense class="product-price-row">
              <v-col cols="12" md="7">
                <v-text-field
                  v-model="createForm.name"
                  variant="outlined"
                  density="comfortable"
                  :error-messages="createErrors.name"
                  @blur="validateCreateField('name')"
                  required
                >
                  <template #label>
                    Nombre <span class="required-asterisk">*</span>
                  </template>
                </v-text-field>
              </v-col>
              <v-col cols="12" md="5">
                <v-text-field
                  v-model="createForm.sku"
                  variant="outlined"
                  density="comfortable"
                  :error-messages="createErrors.sku"
                  inputmode="numeric"
                  @update:model-value="onCreateSkuInput"
                  @blur="validateCreateField('sku')"
                  required
                >
                  <template #label>
                    SKU <span class="required-asterisk">*</span>
                  </template>
                </v-text-field>
              </v-col>
              <v-col cols="12" md="7">
                <v-autocomplete
                  v-model="createForm.proveedorId"
                  :items="proveedores"
                  item-title="name"
                  item-value="id"
                  variant="outlined"
                  density="comfortable"
                  clearable
                  :error-messages="createErrors.proveedorId"
                  @blur="validateCreateField('proveedorId')"
                  required
                >
                  <template #label>
                    Proveedor <span class="required-asterisk">*</span>
                  </template>
                  <template #no-data>
                    <v-list-item
                      prepend-icon="mdi-plus-circle-outline"
                      title="Crear proveedor"
                      @click="openCreateProveedorDialog('')"
                    />
                  </template>
                </v-autocomplete>
              </v-col>
            </v-row>

            <div class="text-subtitle-2 product-section-title">Precio</div>
            <v-row dense>
              <v-col cols="12" md="4">
                <MoneyField
                  v-model="createForm.precioBase"
                  label="Precio costo"
                  variant="outlined"
                  density="comfortable"
                  :error-messages="createErrors.precioBase"
                  :step="100"
                  :show-stepper="false"
                  @blur="validateCreateField('precioBase')"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="createForm.pricingMode"
                  :items="pricingModeItems"
                  label="Modo de precio"
                  variant="outlined"
                  density="comfortable"
                  item-title="title"
                  item-value="value"
                />
              </v-col>
              <v-col v-if="createForm.pricingMode === 'CATEGORIA'" cols="12" md="4">
                <v-autocomplete
                  v-model="createForm.categoriaId"
                  :items="categoriasLookupDisplay"
                  :loading="loadingCategorias"
                  label="Categoria"
                  item-title="displayTitle"
                  item-value="id"
                  variant="outlined"
                  density="comfortable"
                  clearable
                  no-data-text="No hay categorias."
                  @focus="ensureCategoriasLookup"
                />
              </v-col>
              <v-col v-else-if="createForm.pricingMode === 'FIJO_PCT'" cols="12" md="4">
                <v-text-field
                  v-model="createForm.margenPct"
                  label="% Ganancia fija"
                  variant="outlined"
                  density="comfortable"
                  type="number"
                  min="0"
                  step="0.01"
                  :error-messages="createErrors.margenPct"
                  @focus="clearZeroOnFocus(createForm, 'margenPct')"
                  @blur="validateCreateField('margenPct')"
                />
              </v-col>
              <v-col v-else-if="createForm.pricingMode === 'MANUAL'" cols="12" md="4">
                <MoneyField
                  v-model="createForm.precioVentaManual"
                  label="Precio venta manual"
                  variant="outlined"
                  density="comfortable"
                  :error-messages="createErrors.precioVentaManual"
                  :step="100"
                  :show-stepper="false"
                  @blur="validateCreateField('precioVentaManual')"
                />
              </v-col>
              <v-col v-else cols="12" md="4" class="d-flex align-center">
                <div class="text-caption text-medium-emphasis">
                  Precio de venta calculado: {{ formatMoney(createPrecioVentaCalculado) }}
                </div>
              </v-col>
            </v-row>
            <div :class="['product-final-price', { 'product-final-price--night': isNightMode }]">
              <span class="product-final-price__label">Precio final:</span>
              <span class="product-final-price__value">{{ formatMoney(createPrecioVentaCalculado) }}</span>
            </div>

            <div class="text-subtitle-2 product-section-title">Stock</div>
            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model="createForm.stockInicial"
                  label="Stock inicial"
                  type="number"
                  min="0"
                  step="0.01"
                  variant="outlined"
                  density="comfortable"
                  :error-messages="createErrors.stockInicial"
                  @focus="clearZeroOnFocus(createForm, 'stockInicial')"
                  @blur="validateCreateField('stockInicial')"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model="createStockConfig.stockMinimo"
                  label="Stock minimo"
                  type="number"
                  min="0"
                  step="0.01"
                  variant="outlined"
                  density="comfortable"
                  :error-messages="createErrors.stockMinimo"
                  @focus="clearZeroOnFocus(createStockConfig, 'stockMinimo')"
                  @blur="validateCreateField('stockMinimo')"
                />
              </v-col>
              <v-col cols="12" md="4">
                <div :class="['product-status-row', { 'product-status-row--night': isNightMode }]">
                  <span class="product-status-row__label">Estado:</span>
                  <v-switch
                    :model-value="createForm.isActive"
                    color="primary"
                    inset
                    hide-details
                    @update:model-value="(value) => (createForm.isActive = value)"
                  >
                    <template #label>
                      {{ createForm.isActive ? 'Activo' : 'Inactivo' }}
                    </template>
                  </v-switch>
                </div>
              </v-col>
            </v-row>
            <button type="submit" class="d-none" aria-hidden="true" />
          </v-form>
        </v-card-text>
        <v-card-actions class="justify-end px-0 pt-5">
          <v-btn variant="outlined" color="secondary" class="text-none product-dialog-action" @click="closeCreateDialog">Cancelar</v-btn>
          <v-btn color="primary" variant="elevated" class="text-none product-dialog-action" :loading="creatingProduct" @click="createProductFromIngreso">
            Guardar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="proveedorDialog" persistent width="520">
      <v-card>
        <v-card-title>Nuevo proveedor</v-card-title>
        <v-card-text class="pt-4">
          <v-text-field
            v-model="proveedorForm.name"
            label="Nombre"
            variant="outlined"
            density="comfortable"
            :error-messages="proveedorErrors.name"
          />
          <v-text-field
            v-model="proveedorForm.telefono"
            label="Teléfono"
            variant="outlined"
            density="comfortable"
            :error-messages="proveedorErrors.telefono"
          />
          <v-text-field
            v-model="proveedorForm.cuit"
            label="CUIT"
            variant="outlined"
            density="comfortable"
            :error-messages="proveedorErrors.cuit"
          />
          <v-text-field
            v-model="proveedorForm.direccion"
            label="Direccion"
            variant="outlined"
            density="comfortable"
            :error-messages="proveedorErrors.direccion"
          />
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" :disabled="creatingProveedor" @click="closeProveedorDialog">Cancelar</v-btn>
          <v-btn color="primary" :loading="creatingProveedor" @click="saveProveedor">Guardar</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="cantidadModalDialog" persistent width="400">
      <v-card>
        <v-card-title>Agregar producto</v-card-title>
        <v-card-text>
          <div v-if="cantidadModalProducto" class="py-3">
            <div class="text-h6">{{ cantidadModalProducto.name }}</div>
            <div class="text-caption text-medium-emphasis">SKU: {{ cantidadModalProducto.sku }}</div>
            <v-divider class="my-3" />
            <v-text-field
              v-model.number="cantidadModalQuantity"
              label="Cantidad a ingresar"
              type="number"
              min="1"
              step="1"
              variant="outlined"
              density="comfortable"
              autofocus
              @keyup.enter="confirmarAgregarProducto"
            />
          </div>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" @click="closeCantidadModal">Cancelar</v-btn>
          <v-btn color="primary" @click="confirmarAgregarProducto">Agregar producto</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="actualizarPrecioDialog" persistent width="760">
      <v-card>
        <v-card-title>Actualizar precio del producto</v-card-title>
        <v-card-text>
          <div class="price-dialog-meta mb-4">
            <div class="price-dialog-meta__item">
              <div class="price-dialog-meta__label">Producto</div>
              <div class="price-dialog-meta__value">{{ actualizarPrecioForm.nombre || '-' }}</div>
            </div>
            <div class="price-dialog-meta__item">
              <div class="price-dialog-meta__label">SKU</div>
              <div class="price-dialog-meta__value">{{ actualizarPrecioForm.sku || '-' }}</div>
            </div>
          </div>

          <div class="text-subtitle-2 product-section-title">Precio</div>
          <v-row dense>
            <v-col cols="12" md="4">
              <MoneyField
                v-model="actualizarPrecioForm.precioBase"
                label="Precio costo"
                variant="outlined"
                density="comfortable"
                :step="100"
                :show-stepper="false"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-select
                v-model="actualizarPrecioForm.pricingMode"
                :items="pricingModeItems"
                label="Modo de precio"
                variant="outlined"
                density="comfortable"
                item-title="title"
                item-value="value"
              />
            </v-col>
            <v-col v-if="actualizarPrecioForm.pricingMode === 'CATEGORIA'" cols="12" md="4">
              <v-autocomplete
                v-model="actualizarPrecioForm.categoriaId"
                :items="categoriasLookupDisplay"
                :loading="loadingCategorias"
                label="Categoría"
                item-title="displayTitle"
                item-value="id"
                variant="outlined"
                density="comfortable"
                clearable
              />
            </v-col>
            <v-col v-else-if="actualizarPrecioForm.pricingMode === 'FIJO_PCT'" cols="12" md="4">
              <v-text-field
                v-model="actualizarPrecioForm.margenPct"
                label="% Ganancia fija"
                variant="outlined"
                density="comfortable"
                type="number"
                min="0"
                step="0.01"
                @focus="clearZeroOnFocus(actualizarPrecioForm, 'margenPct')"
              />
            </v-col>
            <v-col v-else-if="actualizarPrecioForm.pricingMode === 'MANUAL'" cols="12" md="4">
              <MoneyField
                v-model="actualizarPrecioForm.precioVentaManual"
                label="Precio venta manual"
                variant="outlined"
                density="comfortable"
                :step="100"
                :show-stepper="false"
              />
            </v-col>
            <v-col v-else cols="12" md="4" class="d-flex align-center">
              <div class="text-caption text-medium-emphasis">
                Precio de venta calculado: {{ formatMoney(actualizarPrecioVentaCalculado) }}
              </div>
            </v-col>
          </v-row>
          <div :class="['product-final-price', { 'product-final-price--night': isNightMode }]">
            <span class="product-final-price__label">Precio final:</span>
            <span class="product-final-price__value">{{ formatMoney(actualizarPrecioVentaCalculado) }}</span>
          </div>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" :disabled="actualizarPrecioLoading" @click="closeActualizarPrecioDialog">Cancelar</v-btn>
          <v-btn color="primary" :loading="actualizarPrecioLoading" @click="saveActualizarPrecio">Guardar</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue';
import { useTheme } from 'vuetify';
import MoneyField from '../components/MoneyField.vue';
import { getJson, postJson, requestJson } from '../services/apiClient';
import { formatMoney } from '../utils/currency';

const roundMoney = (amount) => Math.round((Number(amount) + Number.EPSILON) * 100) / 100;

const theme = useTheme();
const isNightMode = computed(() => theme.global.name.value === 'posNightTheme');
const loadingProveedores = ref(false);
const loadingProductos = ref(false);
const loadingCategorias = ref(false);
const addingItem = ref(false);
const saving = ref(false);

const error = ref('');
const proveedores = ref([]);
const productos = ref([]);
const categorias = ref([]);

const selectedProveedorId = ref(null);
const selectedProductoId = ref(null);
const productSearch = ref('');
const productSearchFieldRef = ref(null);
const productoInputKey = ref(0);
const suppressNextSearchClear = ref(false);
const ignoreAutocompleteSearchEvents = ref(false);
const cantidadModalDialog = ref(false);
const cantidadModalProducto = ref(null);
const cantidadModalQuantity = ref(1);
const actualizarPrecioDialog = ref(false);
const actualizarPrecioLoading = ref(false);
const comboPriceUpdateDialog = ref(false);
const comboPriceUpdateLoading = ref(false);
const comboPriceUpdateItems = ref([]);
const actualizarPrecioForm = reactive({
  productoId: '',
  nombre: '',
  sku: '',
  categoriaId: '',
  precioBase: '',
  pricingMode: 'FIJO_PCT',
  margenGananciaPct: null,
  margenPct: '30',
  margenCategoriaPct: '0',
  precioVentaManual: '',
  precioVentaOriginal: 0
});
const items = ref([]);
const remitoDraftStorageKey = 'viñedos-remito-ingreso-draft';
const legacyRemitoDraftStorageKey = 'vinedos-remito-ingreso-draft';
const createDialog = ref(false);
const creatingProduct = ref(false);
const recentlyAddedProductId = ref('');
const recentAddMessage = ref('');
const proveedorDialog = ref(false);
const creatingProveedor = ref(false);
const createForm = reactive({
  sku: '',
  name: '',
  proveedorId: '',
  categoriaId: '',
  precioBase: '',
  pricingMode: 'FIJO_PCT',
  margenPct: '30',
  margenCategoriaPct: '0',
  precioVentaManual: '',
  stockInicial: '',
  isActive: true
});
const createStockConfig = reactive({
  stockMinimo: '0'
});
const createErrors = reactive({
  name: '',
  sku: '',
  proveedorId: '',
  precioBase: '',
  margenPct: '',
  precioVentaManual: '',
  stockInicial: '',
  stockMinimo: ''
});
const proveedorForm = reactive({
  name: '',
  telefono: '',
  cuit: '',
  direccion: ''
});
const proveedorErrors = reactive({
  name: '',
  telefono: '',
  cuit: '',
  direccion: ''
});

const snackbar = ref({
  show: false,
  text: '',
  color: 'success',
  icon: 'mdi-check-circle'
});

const headers = [
  { title: '', value: 'feedback', sortable: false, width: 28, align: 'center' },
  { title: 'Producto', value: 'name' },
  { title: 'Proveedor', value: 'proveedor' },
  { title: 'SKU', value: 'sku' },
  { title: 'Cantidad', value: 'cantidad', width: 180, align: 'center' },
  { title: 'Precio actual', value: 'precioVenta', align: 'end', width: 150 },
  { title: '', value: 'actions', sortable: false, align: 'end', width: 190 }
];

const comboPriceUpdateHeaders = [
  { title: 'Aplicar', value: 'aplicar', sortable: false, width: 90 },
  { title: 'Combo', value: 'name' },
  { title: 'SKU', value: 'sku' },
  { title: 'Precio actual', value: 'precioVentaActual', align: 'end' },
  { title: 'Precio sugerido', value: 'precioVentaSugerido', align: 'end' },
  { title: 'Precio objetivo', value: 'precioVentaObjetivo', sortable: false, align: 'end' }
];

const pricingModeItems = [
  { title: '% fijo', value: 'FIJO_PCT' },
  { title: 'Por categoria', value: 'CATEGORIA' },
  { title: 'Manual', value: 'MANUAL' }
];

const productoOptions = computed(() =>
  productos.value
    .slice()
    .sort((a, b) => (a.name || '').localeCompare(b.name || '', 'es'))
    .map((p) => ({
      ...p,
      label: `${p.name} (${p.sku})`
    }))
);

const hasBlockingDialog = computed(() =>
  cantidadModalDialog.value
  || actualizarPrecioDialog.value
  || comboPriceUpdateDialog.value
  || createDialog.value
  || proveedorDialog.value
);

const focusProductSearchField = async () => {
  await nextTick();
  productSearchFieldRef.value?.focus?.();
};

const isEditableTarget = (target) => {
  const element = target instanceof HTMLElement ? target : null;
  if (!element) return false;
  if (element.isContentEditable) return true;

  const editableContainer = element.closest('input, textarea, [contenteditable="true"], .v-field input, .v-field textarea');
  if (editableContainer) return true;

  const role = element.getAttribute('role');
  return role === 'textbox' || role === 'combobox' || role === 'spinbutton';
};

const handlePersistentProductSearchKeydown = (event) => {
  if (hasBlockingDialog.value) return;
  if (event.defaultPrevented) return;
  if (event.ctrlKey || event.metaKey || event.altKey) return;
  if (isEditableTarget(event.target)) return;

  if (event.key === 'Enter') {
    event.preventDefault();
    focusProductSearchField();
    handleProductInputEnter();
    return;
  }

  if (event.key === 'Backspace') {
    event.preventDefault();
    productSearch.value = String(productSearch.value || '').slice(0, -1);
    selectedProductoId.value = null;
    focusProductSearchField();
    onProductSearch(productSearch.value);
    return;
  }

  if (event.key === 'Delete') {
    event.preventDefault();
    productSearch.value = '';
    selectedProductoId.value = null;
    focusProductSearchField();
    onProductSearch('');
    return;
  }

  if (event.key.length !== 1) return;

  event.preventDefault();
  productSearch.value = `${String(productSearch.value || '')}${event.key}`;
  selectedProductoId.value = null;
  focusProductSearchField();
  onProductSearch(productSearch.value);
};

const categoriasLookupDisplay = computed(() =>
  (categorias.value || []).map((item) => ({
    ...item,
    displayTitle: `${item.name || 'Sin nombre'} (${Number(item.margenGananciaPct || 0).toFixed(2)}%)`
  }))
);

const createPrecioVentaCalculado = computed(() => {
  const base = Number(createForm.precioBase);
  if (Number.isNaN(base) || base <= 0) return 0;

  if (createForm.pricingMode === 'MANUAL') {
    const manual = Number(createForm.precioVentaManual);
    return Number.isNaN(manual) || manual < 0 ? 0 : manual;
  }

  const margen = createForm.pricingMode === 'CATEGORIA'
    ? Number(createForm.margenCategoriaPct || 0)
    : Number(createForm.margenPct);
  if (Number.isNaN(margen) || margen < 0) return base;
  return base * (1 + margen / 100);
});

const actualizarPrecioVentaCalculado = computed(() => {
  const base = Number(actualizarPrecioForm.precioBase);
  if (Number.isNaN(base) || base <= 0) return 0;

  if (actualizarPrecioForm.pricingMode === 'MANUAL') {
    const manual = Number(actualizarPrecioForm.precioVentaManual);
    return Number.isNaN(manual) || manual < 0 ? 0 : manual;
  }

  const margen = actualizarPrecioForm.pricingMode === 'CATEGORIA'
    ? Number(actualizarPrecioForm.margenCategoriaPct || 0)
    : Number(actualizarPrecioForm.margenPct);
  if (Number.isNaN(margen) || margen < 0) return base;
  return base * (1 + margen / 100);
});

const canSave = computed(() => items.value.length > 0);

const getTableItemData = (item) => item?.raw ?? item?.item?.raw ?? item?.item ?? item;

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

const cancelComboPriceUpdates = () => {
  comboPriceUpdateDialog.value = false;
  comboPriceUpdateItems.value = [];
};

const buildComboPriceUpdateCandidates = async (productoId, nuevoPrecioVenta) => {
  const productsResp = await getJson('/api/v1/productos?combo=false&activo=true');
  if (!productsResp.response.ok) {
    throw new Error(extractProblemMessage(productsResp.data));
  }

  const productPriceMap = new Map((productsResp.data || []).map((product) => [
    product.id,
    Number(product.precioVenta || 0)
  ]));
  productPriceMap.set(productoId, Number(nuevoPrecioVenta || 0));

  const combosResp = await getJson('/api/v1/productos?combo=true&activo=true');
  if (!combosResp.response.ok) {
    throw new Error(extractProblemMessage(combosResp.data));
  }

  const impacted = [];
  for (const combo of (combosResp.data || [])) {
    const detailResp = await getJson(`/api/v1/productos/${combo.id}`);
    if (!detailResp.response.ok) {
      continue;
    }

    const detail = detailResp.data || {};
    const itemsCombo = Array.isArray(detail.comboItems) ? detail.comboItems : [];
    if (!itemsCombo.some((entry) => entry.productoId === productoId)) {
      continue;
    }

    const precioBaseSugerido = roundMoney(itemsCombo.reduce((acc, entry) => {
      const price = Number(productPriceMap.get(entry.productoId) || 0);
      return acc + (price * Number(entry.cantidad || 0));
    }, 0));

    const precioVentaActual = Number(detail.precioVenta || 0);
    const precioVentaSugerido = Math.max(0, roundMoney(precioVentaActual + (precioBaseSugerido - Number(detail.precioBase || 0))));

    if (Math.abs(precioVentaSugerido - precioVentaActual) < 0.01) {
      continue;
    }

    impacted.push({
      id: detail.id,
      name: detail.name || combo.name || 'Combo',
      sku: detail.sku || combo.sku || '-',
      precioVentaActual,
      precioVentaSugerido,
      precioVentaObjetivo: precioVentaSugerido,
      precioBaseSugerido,
      comboItems: itemsCombo,
      aplicar: true
    });
  }

  return impacted;
};

const openComboPriceUpdateDialog = (candidates) => {
  if (!Array.isArray(candidates) || !candidates.length) return;

  const byId = new Map(comboPriceUpdateItems.value.map((item) => [item.id, item]));
  for (const candidate of candidates) {
    if (!byId.has(candidate.id)) {
      byId.set(candidate.id, candidate);
    }
  }

  comboPriceUpdateItems.value = Array.from(byId.values());
  comboPriceUpdateDialog.value = true;
};

const applyComboPriceUpdates = async () => {
  const selected = comboPriceUpdateItems.value.filter((item) => item.aplicar);
  if (!selected.length) {
    cancelComboPriceUpdates();
    return;
  }

  comboPriceUpdateLoading.value = true;
  try {
    for (const combo of selected) {
      const payload = {
        precioBase: combo.precioBaseSugerido,
        precioVenta: Math.max(0, Number(combo.precioVentaObjetivo ?? combo.precioVentaSugerido ?? 0)),
        pricingMode: 'MANUAL',
        isCombo: true,
        comboItems: combo.comboItems
      };

      const { response, data } = await requestJson(`/api/v1/productos/${combo.id}`, {
        method: 'PATCH',
        body: JSON.stringify(payload)
      });
      if (!response.ok) {
        throw new Error(extractProblemMessage(data));
      }
    }

    flash('success', 'Precios de combos actualizados');
    cancelComboPriceUpdates();
  } catch (err) {
    flash('error', err?.message || 'No se pudieron actualizar los combos.');
  } finally {
    comboPriceUpdateLoading.value = false;
  }
};

const loadProveedores = async () => {
  loadingProveedores.value = true;
  try {
    const { response, data } = await getJson('/api/v1/proveedores?activo=true');
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    proveedores.value = (data || [])
      .slice()
      .sort((a, b) => (a.name || '').localeCompare(b.name || '', 'es'));
  } catch (err) {
    error.value = err?.message || 'No se pudieron cargar proveedores.';
  } finally {
    loadingProveedores.value = false;
  }
};

const loadProductos = async (search = '') => {
  loadingProductos.value = true;
  try {
    const params = new URLSearchParams();
    params.set('activo', 'true');
    params.set('combo', 'false');
    if (search.trim()) {
      params.set('search', search.trim());
    }

    const { response, data } = await getJson(`/api/v1/productos?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    const allProducts = (data || [])
      .slice()
      .sort((a, b) => (a.name || '').localeCompare(b.name || '', 'es'));
    productos.value = selectedProveedorId.value
      ? allProducts.filter((p) => p.proveedorId === selectedProveedorId.value)
      : allProducts;
  } catch (err) {
    error.value = err?.message || 'No se pudieron cargar productos.';
  } finally {
    loadingProductos.value = false;
  }
};

const loadCategorias = async () => {
  loadingCategorias.value = true;
  try {
    const params = new URLSearchParams();
    params.set('activo', 'true');
    const { response, data } = await getJson(`/api/v1/categorias-precio?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    categorias.value = (data || [])
      .slice()
      .sort((a, b) => (a.name || '').localeCompare(b.name || '', 'es'));
  } catch (err) {
    error.value = err?.message || 'No se pudieron cargar categorias.';
  } finally {
    loadingCategorias.value = false;
  }
};

const onProveedorChanged = async () => {
  clearRecentAddFeedback();
  selectedProductoId.value = null;
  await loadProductos('');
  focusProductSearchField();
};

const onProductSearch = async (value) => {
  if (ignoreAutocompleteSearchEvents.value) {
    if (!(value || '').trim()) {
      ignoreAutocompleteSearchEvents.value = false;
    }
    return;
  }

  if (suppressNextSearchClear.value && !(value || '').trim()) {
    suppressNextSearchClear.value = false;
    await loadProductos('');
    return;
  }

  clearRecentAddFeedback();
  await loadProductos(value || '');
};

const clearRecentAddFeedback = () => {
  recentlyAddedProductId.value = '';
  recentAddMessage.value = '';
};

const clearZeroOnFocus = (target, field) => {
  if (!target || !(field in target)) return;
  const currentValue = String(target[field] ?? '').trim();
  if (currentValue === '0' || currentValue === '0.00' || currentValue === '0,00') {
    target[field] = '';
  }
};

const resetProductInput = async () => {
  ignoreAutocompleteSearchEvents.value = true;
  suppressNextSearchClear.value = true;
  selectedProductoId.value = null;
  productSearch.value = '';
  await nextTick();
  productoInputKey.value += 1;
};

const buildNewProductSuggestion = (term) => {
  const normalized = (term || '').trim();
  if (!normalized) return 'Agregar nuevo producto';

  return /^\d+$/.test(normalized)
    ? `Agregar nuevo producto: SKU ${normalized}`
    : `Agregar nuevo producto: '${normalized}'`;
};

const openCantidadModal = (producto) => {
  if (!producto) return;
  cantidadModalProducto.value = producto;
  cantidadModalQuantity.value = 1;
  cantidadModalDialog.value = true;
};

const closeCantidadModal = () => {
  cantidadModalDialog.value = false;
  cantidadModalProducto.value = null;
  cantidadModalQuantity.value = 1;
};

const closeActualizarPrecioDialog = () => {
  actualizarPrecioDialog.value = false;
  actualizarPrecioForm.productoId = '';
  actualizarPrecioForm.nombre = '';
  actualizarPrecioForm.sku = '';
  actualizarPrecioForm.categoriaId = '';
  actualizarPrecioForm.precioBase = '';
  actualizarPrecioForm.pricingMode = 'FIJO_PCT';
  actualizarPrecioForm.margenGananciaPct = null;
  actualizarPrecioForm.margenPct = '30';
  actualizarPrecioForm.margenCategoriaPct = '0';
  actualizarPrecioForm.precioVentaManual = '';
  actualizarPrecioForm.precioVentaOriginal = 0;
};

const applyActualizarCategoriaMargin = () => {
  const categoria = categorias.value.find((item) => item.id === actualizarPrecioForm.categoriaId);
  actualizarPrecioForm.margenCategoriaPct = categoria ? Number(categoria.margenGananciaPct || 0).toString() : '0';
};

const openActualizarPrecioDialog = async (productoId) => {
  if (!productoId) return;

  actualizarPrecioLoading.value = true;
  try {
    const { response, data } = await getJson(`/api/v1/productos/${productoId}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    actualizarPrecioForm.productoId = data.id;
    actualizarPrecioForm.nombre = data.name || '';
    actualizarPrecioForm.sku = data.sku || '';
    actualizarPrecioForm.categoriaId = data.categoriaId || '';
    const base = Number(data.precioBase ?? 0);
    const venta = Number(data.precioVenta ?? base);
    const margen = base > 0 ? ((venta / base) - 1) * 100 : 0;
    actualizarPrecioForm.precioBase = base ? base.toString() : '';
    actualizarPrecioForm.pricingMode = data.pricingMode || 'FIJO_PCT';
    actualizarPrecioForm.margenPct = Number.isFinite(margen) ? margen.toFixed(2) : '0';
    actualizarPrecioForm.margenCategoriaPct = data.margenGananciaPct != null
      ? Number(data.margenGananciaPct).toString()
      : '0';
    actualizarPrecioForm.precioVentaManual = venta ? venta.toString() : '';
    actualizarPrecioForm.precioVentaOriginal = venta;
    actualizarPrecioForm.margenGananciaPct = data.margenGananciaPct;
    await ensureCategoriasLookup();
    applyActualizarCategoriaMargin();
    actualizarPrecioDialog.value = true;
  } catch (err) {
    flash('error', err?.message || 'No se pudo cargar el producto.');
  } finally {
    actualizarPrecioLoading.value = false;
  }
};

const saveActualizarPrecio = async () => {
  if (!actualizarPrecioForm.productoId || actualizarPrecioLoading.value) return;

  const precioBase = Number(actualizarPrecioForm.precioBase);
  if (Number.isNaN(precioBase) || precioBase < 0) {
    flash('error', 'El precio costo debe ser valido.');
    return;
  }

  actualizarPrecioLoading.value = true;
  try {
    const precioVenta = Number(actualizarPrecioVentaCalculado.value);
    if (Number.isNaN(precioVenta) || precioVenta < 0) {
      flash('error', 'El precio venta debe ser valido.');
      return;
    }

    const payload = {
      precioBase,
      precioVenta,
      categoriaId: actualizarPrecioForm.categoriaId || null,
      pricingMode: actualizarPrecioForm.pricingMode,
      margenGananciaPct: actualizarPrecioForm.pricingMode === 'FIJO_PCT'
        ? Number(actualizarPrecioForm.margenPct || 0)
        : null
    };

    const { response, data } = await requestJson(`/api/v1/productos/${actualizarPrecioForm.productoId}`, {
      method: 'PATCH',
      body: JSON.stringify(payload)
    });
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    await loadProductos(productSearch.value || '');
    items.value = items.value.map((item) => item.productoId === actualizarPrecioForm.productoId
      ? {
        ...item,
        precioBase,
        precioVenta
      }
      : item);

    if (Math.abs(precioVenta - Number(actualizarPrecioForm.precioVentaOriginal || 0)) >= 0.01) {
      const candidates = await buildComboPriceUpdateCandidates(actualizarPrecioForm.productoId, precioVenta);
      openComboPriceUpdateDialog(candidates);
    }

    flash('success', 'Precio actualizado');
    closeActualizarPrecioDialog();
  } catch (err) {
    flash('error', err?.message || 'No se pudo actualizar el precio.');
  } finally {
    actualizarPrecioLoading.value = false;
  }
};

const addProducto = async (producto, qty = 1) => {
  if (!producto) return;
  addingItem.value = true;
  try {
    const normalizedQty = Math.max(1, Number.parseInt(qty, 10) || 1);
    const existing = items.value.find((i) => i.productoId === producto.id);
    if (existing) {
      existing.cantidad = Math.max(1, Number.parseInt(existing.cantidad, 10) || 1) + normalizedQty;
      existing.precioBase = Number(producto.precioBase || existing.precioBase || 0);
      existing.precioVenta = Number(producto.precioVenta || existing.precioVenta || 0);
    } else {
      items.value.push({
        productoId: producto.id,
        name: producto.name,
        proveedor: producto.proveedor || '',
        sku: producto.sku,
        precioBase: Number(producto.precioBase || 0),
        precioVenta: Number(producto.precioVenta || 0),
        cantidad: normalizedQty
      });
    }

    recentlyAddedProductId.value = producto.id;
    recentAddMessage.value = `${producto.name} añadido a la lista`;
    await resetProductInput();
  } finally {
    addingItem.value = false;
  }
};

const onProductoSeleccionado = async (productId) => {
  if (!productId || addingItem.value) return;

  const producto = productos.value.find((p) => p.id === productId);
  if (!producto) {
    flash('error', 'Producto no encontrado.');
    return;
  }

  openCantidadModal(producto);
};

const confirmarAgregarProducto = async () => {
  if (!cantidadModalProducto.value) return;
  const cantidad = Number(cantidadModalQuantity.value);
  if (Number.isNaN(cantidad) || cantidad <= 0) {
    flash('error', 'La cantidad debe ser mayor a 0.');
    return;
  }
  const producto = cantidadModalProducto.value;
  closeCantidadModal();
  await addProducto(producto, cantidad);
};

const removeItem = (productId) => {
  clearRecentAddFeedback();
  items.value = items.value.filter((i) => i.productoId !== productId);
};

const updatePrecioItem = (productId, field, value) => {
  const item = items.value.find((i) => i.productoId === productId);
  if (!item) return;

  const nextValue = Number(value);
  item[field] = Number.isFinite(nextValue) && nextValue >= 0 ? nextValue : 0;
};

const incrementCantidad = (productId) => {
  clearRecentAddFeedback();
  const item = items.value.find((i) => i.productoId === productId);
  if (!item) return;
  item.cantidad = Math.max(1, Number.parseInt(item.cantidad, 10) || 1) + 1;
};

const decrementCantidad = (productId) => {
  clearRecentAddFeedback();
  const item = items.value.find((i) => i.productoId === productId);
  if (!item) return;
  const currentQty = Math.max(1, Number.parseInt(item.cantidad, 10) || 1);
  item.cantidad = Math.max(1, currentQty - 1);
};

const updateCantidad = (productId, value) => {
  clearRecentAddFeedback();
  const qty = Number.parseInt(value, 10);
  const item = items.value.find((i) => i.productoId === productId);
  if (!item) return;
  const previousQty = Math.max(1, Number.parseInt(item.cantidad, 10) || 1);

  if (Number.isNaN(qty) || qty <= 0) {
    item.cantidad = 1;
    return;
  }
  item.cantidad = qty;
};

const clearForm = () => {
  clearRecentAddFeedback();
  selectedProveedorId.value = null;
  resetProductInput();
  items.value = [];
  error.value = '';
  localStorage.removeItem(remitoDraftStorageKey);
  loadProductos('');
  focusProductSearchField();
};

const resetCreateForm = () => {
  createForm.sku = '';
  createForm.name = '';
  createForm.proveedorId = selectedProveedorId.value || '';
  createForm.categoriaId = '';
  createForm.precioBase = '';
  createForm.pricingMode = 'FIJO_PCT';
  createForm.margenPct = '30';
  createForm.margenCategoriaPct = '0';
  createForm.precioVentaManual = '';
  createForm.stockInicial = '';
  createForm.isActive = true;
  createStockConfig.stockMinimo = '0';
  Object.keys(createErrors).forEach((key) => {
    createErrors[key] = '';
  });
};

const resetProveedorForm = () => {
  proveedorForm.name = '';
  proveedorForm.telefono = '';
  proveedorForm.cuit = '';
  proveedorForm.direccion = '';
  proveedorErrors.name = '';
  proveedorErrors.telefono = '';
  proveedorErrors.cuit = '';
  proveedorErrors.direccion = '';
};

const openCreateProveedorDialog = (initialName = '') => {
  resetProveedorForm();
  proveedorForm.name = (initialName || '').trim();
  proveedorDialog.value = true;
};

const closeProveedorDialog = () => {
  proveedorDialog.value = false;
  resetProveedorForm();
};

const saveProveedor = async () => {
  proveedorErrors.name = proveedorForm.name.trim() ? '' : 'El nombre es obligatorio.';
  proveedorErrors.telefono = proveedorForm.telefono.trim() ? '' : 'El teléfono es obligatorio.';
  proveedorErrors.cuit = proveedorForm.cuit.trim() ? '' : 'El CUIT es obligatorio.';
  proveedorErrors.direccion = proveedorForm.direccion.trim() ? '' : 'La direccion es obligatoria.';
  if (proveedorErrors.name || proveedorErrors.telefono || proveedorErrors.cuit || proveedorErrors.direccion || creatingProveedor.value) return;

  creatingProveedor.value = true;
  try {
    const { response, data } = await postJson('/api/v1/proveedores', {
      name: proveedorForm.name.trim(),
      telefono: proveedorForm.telefono.trim() || null,
      cuit: proveedorForm.cuit.trim() || null,
      direccion: proveedorForm.direccion.trim() || null,
      isActive: true
    });
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    await loadProveedores();
    createForm.proveedorId = data.id;
    if (!selectedProveedorId.value) {
      selectedProveedorId.value = data.id;
    }
    closeProveedorDialog();
    flash('success', 'Proveedor creado');
  } catch (err) {
    flash('error', err?.message || 'No se pudo crear el proveedor.');
  } finally {
    creatingProveedor.value = false;
  }
};

const validateCreateField = (field) => {
  if (field === 'name') {
    createErrors.name = createForm.name.trim() ? '' : '* El nombre es obligatorio.';
  }
  if (field === 'sku') {
    if (!createForm.sku.trim()) {
      createErrors.sku = '* El SKU es obligatorio.';
    } else if (!/^\d+$/.test(createForm.sku.trim())) {
      createErrors.sku = '* El SKU debe contener solo numeros.';
    } else {
      createErrors.sku = '';
    }
  }
  if (field === 'proveedorId') {
    createErrors.proveedorId = createForm.proveedorId ? '' : '* El proveedor es obligatorio.';
  }
  if (field === 'precioBase') {
    if (createForm.precioBase === '') {
      createErrors.precioBase = '';
    } else if (Number(createForm.precioBase) < 0 || Number.isNaN(Number(createForm.precioBase))) {
      createErrors.precioBase = 'Precio costo invalido.';
    } else {
      createErrors.precioBase = '';
    }
  }
  if (field === 'margenPct') {
    if (createForm.pricingMode !== 'FIJO_PCT') {
      createErrors.margenPct = '';
    } else if (createForm.margenPct === '') {
      createErrors.margenPct = '';
    } else if (Number(createForm.margenPct) < 0 || Number.isNaN(Number(createForm.margenPct))) {
      createErrors.margenPct = 'Margen invalido.';
    } else {
      createErrors.margenPct = '';
    }
  }
  if (field === 'precioVentaManual') {
    if (createForm.pricingMode !== 'MANUAL') {
      createErrors.precioVentaManual = '';
    } else if (createForm.precioVentaManual === '') {
      createErrors.precioVentaManual = 'Precio venta obligatorio.';
    } else if (Number(createForm.precioVentaManual) < 0 || Number.isNaN(Number(createForm.precioVentaManual))) {
      createErrors.precioVentaManual = 'Precio venta invalido.';
    } else {
      createErrors.precioVentaManual = '';
    }
  }
  if (field === 'stockInicial') {
    if (createForm.stockInicial === '') {
      createErrors.stockInicial = '';
    } else if (Number(createForm.stockInicial) < 0 || Number.isNaN(Number(createForm.stockInicial))) {
      createErrors.stockInicial = 'Stock inicial invalido.';
    } else {
      createErrors.stockInicial = '';
    }
  }
  if (field === 'stockMinimo') {
    if (createStockConfig.stockMinimo === '') {
      createErrors.stockMinimo = '';
    } else if (Number(createStockConfig.stockMinimo) < 0 || Number.isNaN(Number(createStockConfig.stockMinimo))) {
      createErrors.stockMinimo = 'Stock minimo invalido.';
    } else {
      createErrors.stockMinimo = '';
    }
  }
};

const validateCreateForm = () => {
  validateCreateField('name');
  validateCreateField('sku');
  validateCreateField('proveedorId');
  validateCreateField('precioBase');
  validateCreateField('margenPct');
  validateCreateField('precioVentaManual');
  validateCreateField('stockInicial');
  validateCreateField('stockMinimo');

  return Object.values(createErrors).every((value) => !value);
};

const applyCategoriaMargin = () => {
  const categoria = categorias.value.find((item) => item.id === createForm.categoriaId);
  createForm.margenCategoriaPct = categoria ? Number(categoria.margenGananciaPct || 0).toString() : '0';
};

const ensureCategoriasLookup = async () => {
  if (categorias.value.length > 0 || loadingCategorias.value) return;
  await loadCategorias();
};

const openCreateProductDialog = async (term = '') => {
  clearRecentAddFeedback();
  resetCreateForm();
  const normalized = (term || '').trim();
  if (/^\d+$/.test(normalized)) {
    createForm.sku = normalized;
    createForm.name = '';
  } else {
    createForm.name = normalized;
    createForm.sku = '';
  }
  createDialog.value = true;
  await ensureCategoriasLookup();
};

const onCreateSkuInput = (value) => {
  createForm.sku = (value || '').replace(/\D+/g, '');
};

const closeCreateDialog = () => {
  createDialog.value = false;
  resetCreateForm();
};

const handleProductInputEnter = async () => {
  await nextTick();

  const search = (productSearch.value || '').trim();
  if (!search || addingItem.value) return;

  const selectedProduct = productos.value.find((p) => p.id === selectedProductoId.value);
  if (selectedProduct) {
    openCantidadModal(selectedProduct);
    return;
  }

  if (productoOptions.value.length > 0) {
    const firstMatch = productoOptions.value[0];
    openCantidadModal(firstMatch);
    return;
  }

  const params = new URLSearchParams();
  params.set('activo', 'true');
  params.set('search', search);
  const { response, data } = await getJson(`/api/v1/productos?${params.toString()}`);
  if (!response.ok) {
    flash('error', extractProblemMessage(data));
    return;
  }

  const all = data || [];
  const exact = all.find((p) => (p.sku || '').trim().toLowerCase() === search.toLowerCase());
  if (exact) {
    openCantidadModal(exact);
    return;
  }

  const exactName = all.find((p) => (p.name || '').trim().toLowerCase() === search.toLowerCase());
  if (exactName) {
    openCantidadModal(exactName);
    return;
  }

  if (all.length > 0) {
    openCantidadModal(all[0]);
    return;
  }

  await openCreateProductDialog(search);
};

const createProductFromIngreso = async () => {
  if (creatingProduct.value) return;
  if (!validateCreateForm()) return;

  const sku = createForm.sku.trim();
  const name = createForm.name.trim();
  const precioBase = createForm.precioBase === '' ? null : Number(createForm.precioBase);
  const precioVenta = createForm.pricingMode === 'MANUAL'
    ? (createForm.precioVentaManual === '' ? null : Number(createForm.precioVentaManual))
    : (precioBase == null ? null : Number(createPrecioVentaCalculado.value));
  const margenGananciaPct = createForm.pricingMode === 'FIJO_PCT'
    ? (createForm.margenPct === '' ? null : Number(createForm.margenPct))
    : null;

  creatingProduct.value = true;
  try {
    const payload = {
      name,
      sku,
      proveedorId: createForm.proveedorId,
      categoriaId: createForm.categoriaId || null,
      isActive: createForm.isActive,
      precioBase,
      precioVenta,
      pricingMode: createForm.pricingMode,
      margenGananciaPct
    };

    const { response, data } = await postJson('/api/v1/productos', payload);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    const stockPayload = {
      stockMinimo: createStockConfig.stockMinimo === '' ? 0 : Number(createStockConfig.stockMinimo)
    };
    const stockConfigResponse = await requestJson(`/api/v1/productos/${data.id}/stock-config`, {
      method: 'PATCH',
      body: JSON.stringify(stockPayload)
    });
    if (!stockConfigResponse.response.ok) {
      throw new Error(extractProblemMessage(stockConfigResponse.data));
    }

    const stockInicial = Number(createForm.stockInicial);
    if (!Number.isNaN(stockInicial) && stockInicial > 0) {
      const stockResp = await postJson('/api/v1/stock/ajustes', {
        tipo: 'AJUSTE',
        motivo: 'Stock inicial',
        items: [
          {
            productoId: data.id,
            cantidad: stockInicial,
            esIngreso: true
          }
        ]
      });
      if (!stockResp.response.ok) {
        throw new Error(extractProblemMessage(stockResp.data));
      }
    }

    await loadProductos(sku);
    const created = productos.value.find((p) => p.id === data.id) || productos.value.find((p) => p.sku === sku);
    if (created) {
      await addProducto(created);
      recentAddMessage.value = `Nuevo producto "${created.name}" agregado al inventario`;
    }

    closeCreateDialog();
    flash('success', 'Producto creado y agregado al ingreso.');
  } catch (err) {
    flash('error', err?.message || 'No se pudo crear el producto.');
  } finally {
    creatingProduct.value = false;
  }
};

const confirmarIngreso = async () => {
  clearRecentAddFeedback();
  if (saving.value || !canSave.value) return;

  const invalid = items.value.some((i) => Number(i.cantidad) <= 0);
  if (invalid) {
    flash('error', 'Todas las cantidades deben ser mayores a 0.');
    return;
  }

  const proveedor = proveedores.value.find((p) => p.id === selectedProveedorId.value);
  const motivo = proveedor
    ? `Ingreso manual remito - filtro proveedor ${proveedor.name}`
    : 'Ingreso manual remito - varios proveedores';

  saving.value = true;
  try {
    const productosConPrecio = items.value
      .map((item) => ({
        productoId: item.productoId,
        precioBase: Number(item.precioBase),
        precioVenta: Number(item.precioVenta)
      }))
      .filter((item) => item.productoId);

    const precioVentaActualByProducto = new Map(
      (productos.value || []).map((p) => [p.id, Number(p.precioVenta || 0)])
    );
    const comboCandidatesById = new Map();

    for (const producto of productosConPrecio) {
      if (Number.isNaN(producto.precioBase) || producto.precioBase < 0) {
        throw new Error('El precio costo debe ser valido.');
      }

      if (Number.isNaN(producto.precioVenta) || producto.precioVenta < 0) {
        throw new Error('El precio venta debe ser valido.');
      }

      const { response: precioResp, data: precioData } = await requestJson(`/api/v1/productos/${producto.productoId}`, {
        method: 'PATCH',
        body: JSON.stringify({
          precioBase: producto.precioBase,
          precioVenta: producto.precioVenta,
          pricingMode: 'MANUAL',
          margenGananciaPct: null
        })
      });

      if (!precioResp.ok) {
        throw new Error(extractProblemMessage(precioData));
      }

      const precioVentaActual = precioVentaActualByProducto.get(producto.productoId);
      if (Number.isFinite(precioVentaActual) && Math.abs(producto.precioVenta - Number(precioVentaActual)) >= 0.01) {
        const candidates = await buildComboPriceUpdateCandidates(producto.productoId, producto.precioVenta);
        for (const candidate of candidates) {
          if (!comboCandidatesById.has(candidate.id)) {
            comboCandidatesById.set(candidate.id, candidate);
          }
        }
      }
    }

    const payload = {
      tipo: 'AJUSTE',
      motivo,
      items: items.value.map((i) => ({
        productoId: i.productoId,
        cantidad: Number(i.cantidad),
        esIngreso: true
      }))
    };

    const { response, data } = await postJson('/api/v1/stock/ajustes', payload);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    flash('success', 'Ingreso de productos registrado.');
    openComboPriceUpdateDialog(Array.from(comboCandidatesById.values()));
    clearForm();
    await loadProveedores();
  } catch (err) {
    flash('error', err?.message || 'No se pudo registrar el ingreso.');
  } finally {
    saving.value = false;
  }
};

const saveDraft = () => {
  const payload = {
    selectedProveedorId: selectedProveedorId.value,
    items: items.value.map((item) => ({
      productoId: item.productoId,
      name: item.name,
      proveedor: item.proveedor || '',
      sku: item.sku,
      precioBase: Number(item.precioBase || 0),
      precioVenta: Number(item.precioVenta || 0),
      cantidad: Number(item.cantidad)
    }))
  };
  localStorage.setItem(remitoDraftStorageKey, JSON.stringify(payload));
};

const restoreDraft = () => {
  const raw = localStorage.getItem(remitoDraftStorageKey)
    || localStorage.getItem(legacyRemitoDraftStorageKey);
  if (!raw) return;

  try {
    const parsed = JSON.parse(raw);
    selectedProveedorId.value = parsed.selectedProveedorId || null;
    items.value = Array.isArray(parsed.items)
      ? parsed.items.map((item) => ({
          productoId: item.productoId,
          name: item.name,
          proveedor: item.proveedor || '',
          sku: item.sku,
          precioBase: Number(item.precioBase || 0),
          precioVenta: Number(item.precioVenta || 0),
          cantidad: Number(item.cantidad) > 0 ? Number(item.cantidad) : 1
        }))
      : [];

    if (localStorage.getItem(legacyRemitoDraftStorageKey)) {
      localStorage.removeItem(legacyRemitoDraftStorageKey);
      saveDraft();
    }
  } catch {
    localStorage.removeItem(remitoDraftStorageKey);
    localStorage.removeItem(legacyRemitoDraftStorageKey);
  }
};

onMounted(async () => {
  restoreDraft();
  await loadProveedores();
  await loadProductos('');
  await loadCategorias();
  resetCreateForm();
  window.addEventListener('keydown', handlePersistentProductSearchKeydown, true);
  focusProductSearchField();
});

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handlePersistentProductSearchKeydown, true);
});

watch(
  () => createForm.pricingMode,
  async (value) => {
    if (value === 'MANUAL' && !createForm.precioVentaManual) {
      createForm.precioVentaManual = createPrecioVentaCalculado.value.toFixed(2);
    }
    if (value === 'CATEGORIA') {
      await ensureCategoriasLookup();
      applyCategoriaMargin();
    }
    validateCreateField('margenPct');
    validateCreateField('precioVentaManual');
  }
);

watch(
  () => createForm.categoriaId,
  () => {
    applyCategoriaMargin();
  }
);

watch(
  () => actualizarPrecioForm.pricingMode,
  async (value) => {
    if (value === 'MANUAL' && !actualizarPrecioForm.precioVentaManual) {
      actualizarPrecioForm.precioVentaManual = actualizarPrecioVentaCalculado.value.toFixed(2);
    }
    if (value === 'CATEGORIA') {
      await ensureCategoriasLookup();
      applyActualizarCategoriaMargin();
    }
  }
);

watch(
  () => actualizarPrecioForm.categoriaId,
  () => {
    applyActualizarCategoriaMargin();
  }
);

watch(
  () => hasBlockingDialog.value,
  (isOpen) => {
    if (!isOpen) {
      focusProductSearchField();
    }
  }
);

watch(
  [selectedProveedorId, items],
  () => {
    saveDraft();
  },
  { deep: true }
);
</script>

<style scoped>
.remitos-page {
  animation: fade-in 0.3s ease;
}

.remitos-main-card {
  margin-top: 10px;
}

.remitos-search-row {
  display: grid;
  grid-template-columns: minmax(0, 3fr) minmax(0, 2fr);
  gap: 12px;
  align-items: start;
}

.remitos-search-row__search,
.remitos-search-row__provider {
  min-width: 0;
}

.remitos-action-btn {
  min-width: 150px;
  min-height: 46px;
  padding-inline: 20px;
  font-weight: 700;
}

.remitos-action-group {
  gap: 14px;
}

.remitos-add-feedback {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  margin-top: 8px;
  color: var(--pos-success-strong);
  font-weight: 700;
}

.remitos-add-feedback__dot,
.recent-product-dot {
  width: 10px;
  height: 10px;
  border-radius: 999px;
  background: var(--pos-success-surface);
  box-shadow: var(--pos-success-ring);
}

.required-asterisk {
  color: var(--pos-error);
  font-weight: 800;
}

.product-section-title {
  margin: 16px 0 12px;
}

.price-dialog-meta {
  display: grid;
  gap: 12px;
}

.price-dialog-meta__item {
  display: grid;
  gap: 4px;
}

.price-dialog-meta__label {
  font-size: 0.8rem;
  font-weight: 700;
  color: rgba(255, 255, 255, 0.62);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.price-dialog-meta__value {
  font-size: 1rem;
  font-weight: 700;
  color: rgba(255, 255, 255, 0.92);
}

.product-final-price {
  display: flex;
  align-items: baseline;
  gap: 10px;
  padding: 12px 16px;
  margin: 0;
  border: 1px solid var(--pos-price-panel-border);
  border-radius: 14px;
  background: var(--pos-price-panel-bg);
  box-shadow: var(--pos-price-panel-sheen);
}

.product-price-row {
  margin-bottom: -6px;
}

.product-final-price--night {
  border-color: var(--pos-price-panel-night-border);
  background: var(--pos-price-panel-night-bg);
  box-shadow: var(--pos-price-panel-night-sheen);
}

.product-final-price__label {
  font-size: 0.95rem;
  font-weight: 700;
  color: var(--pos-accent-strong);
}

.product-final-price__value {
  font-size: 1.15rem;
  font-weight: 800;
  color: var(--pos-accent-dark);
}

.product-final-price--night .product-final-price__label {
  color: var(--pos-inverse-soft);
}

.product-final-price--night .product-final-price__value {
  color: var(--pos-ink);
}

.product-status-row {
  min-height: 56px;
  display: flex;
  align-items: center;
  gap: 12px;
  color: var(--pos-ink);
}

.product-status-row__label {
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--pos-ink);
}

.product-status-row--night {
  color: var(--pos-inverse);
}

.product-status-row--night .product-status-row__label {
  color: var(--pos-inverse);
}

.product-status-row--night :deep(.v-label) {
  color: var(--pos-inverse);
}

.product-dialog-action {
  min-width: 132px;
  min-height: 44px;
  padding-inline: 20px;
  font-weight: 700;
}

.quantity-stepper {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  min-height: 40px;
}

.remitos-marker-column,
.recent-product-dot-slot,
.recent-quantity-dot-slot {
  width: 14px;
  min-width: 14px;
  display: inline-flex;
  justify-content: center;
  align-items: center;
}

.quantity-stepper__input {
  width: 70px;
}

.quantity-stepper__input :deep(.v-field) {
  min-height: 34px;
  align-items: center;
}

.quantity-stepper__input :deep(.v-field__input) {
  justify-content: center;
  padding-inline: 0;
}

.quantity-stepper__input :deep(input) {
  text-align: center;
  -moz-appearance: textfield;
}

.quantity-stepper__input :deep(input::-webkit-outer-spin-button),
.quantity-stepper__input :deep(input::-webkit-inner-spin-button) {
  -webkit-appearance: none;
  margin: 0;
}

@media (max-width: 960px) {
  .remitos-search-row {
    grid-template-columns: 1fr;
  }
}
</style>

