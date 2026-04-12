<template>
  <div class="productos-page">
    <v-tabs v-model="tab" color="primary" class="mb-4">
      <v-tab value="productos">Productos</v-tab>
      <v-tab value="combos">Combo</v-tab>
      <v-tab value="proveedores">Proveedores</v-tab>
      <v-tab value="listas-precio">Categorías</v-tab>
    </v-tabs>

    <v-window v-model="tab">
      <v-window-item value="productos" class="productos-window-item">
        <div class="d-flex align-center flex-wrap gap-3 mb-4">
          <v-btn color="primary" size="large" variant="elevated" class="text-none product-primary-action" prepend-icon="mdi-plus" @click="startNewProduct">
            Nuevo producto
          </v-btn>
          <v-spacer />
          <v-btn
            color="primary"
            variant="text"
            class="text-none product-print-action"
            prepend-icon="mdi-printer-outline"
            :loading="barcodeLoading"
            :disabled="products.length === 0"
            @click="printBarcodes"
          >
            Imprimir codigos de barra
          </v-btn>
        </div>
        <v-row dense>
          <v-col cols="12">
            <v-card class="pos-card pa-4 productos-list-card">
              <div class="d-flex flex-wrap align-center gap-3">
                <div class="text-h6">Lista de productos</div>
                <v-spacer />
                <v-btn-toggle v-model="activoFilter" density="comfortable" mandatory>
                  <v-btn value="all" class="text-none">Todos</v-btn>
                  <v-btn value="true" class="text-none">Activos</v-btn>
                  <v-btn value="false" class="text-none">Inactivos</v-btn>
                </v-btn-toggle>
              </div>
              <div class="mt-4 d-flex flex-wrap align-center gap-3">
                <v-text-field
                  v-model="search"
                  label="Buscar por nombre o SKU"
                  prepend-inner-icon="mdi-magnify"
                  variant="outlined"
                  density="comfortable"
                  hide-details
                  style="min-width: 320px"
                  @keyup.enter="loadProducts"
                />
              </div>
              <div class="products-table-shell mt-3">
                <v-data-table
                  :headers="headers"
                  :items="products"
                  :items-per-page="10"
                  item-key="id"
                  class="products-table"
                  density="compact"
                >
                  <template v-slot:[`item.name`]="{ item }">
                    <div class="truncate-cell product-name-cell">{{ item.name || '-' }}</div>
                  </template>
                  <template v-slot:[`item.sku`]="{ item }">
                    <div class="truncate-cell product-sku-cell">{{ item.sku || '-' }}</div>
                  </template>
                  <template v-slot:[`item.precioBase`]="{ item }">
                    {{ formatMoney(item.precioBase) }}
                  </template>
                  <template v-slot:[`item.precioVenta`]="{ item }">
                    {{ formatMoney(item.precioVenta) }}
                  </template>
                  <template v-slot:[`item.isActive`]="{ item }">
                    <v-chip size="small" :color="item.isActive ? 'success' : 'error'" variant="tonal">
                      {{ item.isActive ? 'Activo' : 'Inactivo' }}
                    </v-chip>
                  </template>
                  <template v-slot:[`item.actions`]="{ item }">
                    <div class="table-actions">
                      <v-btn
                        size="small"
                        variant="tonal"
                        color="primary"
                        class="text-none"
                        @click="openEditProduct(item)"
                      >
                        Editar producto
                      </v-btn>
                      <v-btn
                        size="small"
                        icon="mdi-delete-outline"
                        variant="text"
                        color="error"
                        class="delete-row-action"
                        @click="promptDelete('producto', item)"
                      />
                    </div>
                  </template>
                </v-data-table>
              </div>
            </v-card>
          </v-col>
        </v-row>
      </v-window-item>

      <v-window-item value="combos" class="productos-window-item">
        <div class="d-flex align-center flex-wrap gap-3 mb-4">
          <v-btn color="primary" size="large" variant="elevated" class="text-none product-primary-action" prepend-icon="mdi-plus" @click="openNewComboDialog">
            Nuevo combo
          </v-btn>
        </div>
        <v-row dense>
          <v-col cols="12">
            <v-card class="pos-card pa-4 productos-list-card">
              <div class="d-flex flex-wrap align-center gap-3">
                <div class="text-h6">Combos</div>
              </div>
              <div class="products-table-shell mt-3">
                <v-data-table
                  :headers="comboHeaders"
                  :items="combos"
                  :loading="comboLoading"
                  :items-per-page="10"
                  item-key="id"
                  class="products-table"
                  density="compact"
                >
                  <template v-slot:[`item.precioVenta`]='{ item }'>
                    {{ formatMoney(item.precioVenta) }}
                  </template>
                  <template v-slot:[`item.actions`]='{ item }'>
                    <v-btn
                      size="small"
                      variant="tonal"
                      color="primary"
                      class="text-none"
                      @click="openEditCombo(item)"
                    >
                      Editar combo
                    </v-btn>
                  </template>
                </v-data-table>
              </div>
            </v-card>
          </v-col>
        </v-row>
      </v-window-item>

      <v-window-item value="proveedores" class="productos-window-item">
        <div class="d-flex align-center flex-wrap gap-3 mb-4">
          <v-btn color="primary" size="large" variant="elevated" class="text-none product-primary-action" prepend-icon="mdi-plus" @click="openProveedorDialogFromList">
            Nuevo proveedor
          </v-btn>
        </div>
        <v-row dense>
          <v-col cols="12">
            <v-card class="pos-card pa-4 productos-list-card">
              <div class="d-flex flex-wrap align-center gap-3">
                <div class="text-h6">Lista de proveedores</div>
                <v-spacer />
                <v-btn-toggle v-model="proveedorActivoFilter" density="comfortable" mandatory>
                  <v-btn value="all" class="text-none">Todos</v-btn>
                  <v-btn value="true" class="text-none">Activos</v-btn>
                  <v-btn value="false" class="text-none">Inactivos</v-btn>
                </v-btn-toggle>
              </div>
              <div class="mt-4 d-flex flex-wrap align-center gap-3">
                <v-text-field
                  v-model="proveedorSearch"
                  label="Buscar proveedor"
                  prepend-inner-icon="mdi-magnify"
                  variant="outlined"
                  density="comfortable"
                  hide-details
                  style="min-width: 320px"
                  @keyup.enter="loadProveedores"
                />
              </div>
              <div class="products-table-shell mt-3">
                <v-data-table
                  :headers="proveedorHeaders"
                  :items="proveedoresTable"
                  :items-per-page="10"
                  item-key="id"
                  class="products-table"
                  density="compact"
                >
                  <template v-slot:[`item.name`]="{ item }">
                    <div class="truncate-cell proveedor-name-cell">{{ item.name || '-' }}</div>
                  </template>
                  <template v-slot:[`item.direccion`]="{ item }">
                    <div class="truncate-cell proveedor-direccion-cell">{{ item.direccion || '-' }}</div>
                  </template>
                  <template v-slot:[`item.isActive`]="{ item }">
                    <v-chip size="small" :color="item.isActive ? 'success' : 'error'" variant="tonal">
                      {{ item.isActive ? 'Activo' : 'Inactivo' }}
                    </v-chip>
                  </template>
                  <template v-slot:[`item.actions`]="{ item }">
                    <div class="table-actions">
                      <v-btn
                        size="small"
                        variant="tonal"
                        color="primary"
                        class="text-none"
                        @click="openEditProveedor(item)"
                      >
                        Editar proveedor
                      </v-btn>
                      <v-btn
                        size="small"
                        icon="mdi-delete-outline"
                        variant="text"
                        color="error"
                        class="delete-row-action"
                        @click="promptDelete('proveedor', item)"
                      />
                    </div>
                  </template>
                </v-data-table>
              </div>
            </v-card>
          </v-col>
        </v-row>
      </v-window-item>

      <v-window-item value="listas-precio" class="productos-window-item">
        <div class="d-flex align-center flex-wrap gap-3 mb-4">
          <v-btn color="primary" size="large" variant="elevated" class="text-none product-primary-action" prepend-icon="mdi-plus" @click="openCategoriaDialogForCreate">
            Nueva categoría
          </v-btn>
        </div>
        <v-row dense>
          <v-col cols="12">
            <v-card class="pos-card pa-4 productos-list-card">
              <div class="d-flex flex-wrap align-center gap-3">
                <div class="text-h6">Categorías</div>
                <v-spacer />
                <v-btn-toggle v-model="categoriaActivoFilter" density="comfortable" mandatory>
                  <v-btn value="all" class="text-none">Todos</v-btn>
                  <v-btn value="true" class="text-none">Activos</v-btn>
                  <v-btn value="false" class="text-none">Inactivos</v-btn>
                </v-btn-toggle>
              </div>
              <div class="mt-4 d-flex flex-wrap align-center gap-3">
                <v-text-field
                  v-model="categoriaSearch"
                  label="Buscar categoría"
                  prepend-inner-icon="mdi-magnify"
                  variant="outlined"
                  density="comfortable"
                  hide-details
                  style="min-width: 320px"
                  @keyup.enter="loadCategorias"
                />
              </div>
              <div class="products-table-shell mt-3">
                <v-data-table
                  :headers="categoriaHeaders"
                  :items="categoriasTable"
                  :items-per-page="10"
                  item-key="id"
                  class="products-table"
                  density="compact"
                >
                  <template v-slot:[`item.name`]="{ item }">
                    <div class="truncate-cell categoria-name-cell">{{ item.name || '-' }}</div>
                  </template>
                  <template v-slot:[`item.margenGananciaPct`]="{ item }">
                    {{ Number(item.margenGananciaPct || 0).toFixed(2) }}%
                  </template>
                  <template v-slot:[`item.isActive`]="{ item }">
                    <v-chip size="small" :color="item.isActive ? 'success' : 'error'" variant="tonal">
                      {{ item.isActive ? 'Activo' : 'Inactivo' }}
                    </v-chip>
                  </template>
                  <template v-slot:[`item.actions`]="{ item }">
                    <div class="table-actions">
                      <v-btn
                        size="small"
                        variant="tonal"
                        color="primary"
                        class="text-none"
                        @click="openEditCategoria(item)"
                      >
                        Editar categoría
                      </v-btn>
                      <v-btn
                        size="small"
                        icon="mdi-delete-outline"
                        variant="text"
                        color="error"
                        class="delete-row-action"
                        @click="promptDelete('categoria', item)"
                      />
                    </div>
                  </template>
                </v-data-table>
              </div>
            </v-card>
          </v-col>
        </v-row>
      </v-window-item>
    </v-window>

    <v-dialog v-model="deleteDialog" persistent width="440">
      <v-card>
        <v-card-title>Confirmar eliminación</v-card-title>
        <v-card-text>
          {{ deleteDialogText }}
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" :disabled="deleteLoading" @click="closeDeleteDialog">Cancelar</v-btn>
          <v-btn color="error" variant="elevated" :loading="deleteLoading" @click="confirmDelete">
            Eliminar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="dialogDesactivar" width="420">
      <v-card>
        <v-card-title>Desactivar producto</v-card-title>
        <v-card-text>
          Esta accion desactiva el producto y lo oculta de operaciones. Continuar?
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" @click="cancelDeactivate">Cancelar</v-btn>
          <v-btn color="error" @click="confirmDeactivate">Desactivar</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog :model-value="!!productEditorMode" persistent width="760">
      <v-card v-if="productEditorMode" class="pa-4">
        <div>
          <div>
            <div class="text-h6">{{ productEditorMode === 'edit' ? 'Editar producto' : 'Nuevo producto' }}</div>
            <div class="text-caption text-medium-emphasis">{{ productEditorMode === 'edit' ? form.name : '' }}</div>
          </div>
        </div>

        <v-form class="mt-3">
          <v-row dense class="product-price-row">
            <v-col cols="12" md="7">
              <v-text-field
                v-model="form.name"
                variant="outlined"
                density="comfortable"
                maxlength="80"
                counter="80"
                :error-messages="errors.name"
                @blur="validateField('name')"
                required
              >
                <template #label>
                  Nombre <span class="required-asterisk">*</span>
                </template>
              </v-text-field>
            </v-col>
            <v-col cols="12" md="5">
              <v-text-field
                v-model="form.sku"
                variant="outlined"
                density="comfortable"
                maxlength="50"
                counter="50"
                :error-messages="errors.sku"
                inputmode="numeric"
                @update:model-value="onSkuInput"
                @blur="validateField('sku')"
                required
              >
                <template #label>
                  SKU <span class="required-asterisk">*</span>
                </template>
              </v-text-field>
            </v-col>
            <v-col cols="12" md="7">
              <v-autocomplete
                v-model="form.proveedorId"
                :items="proveedoresLookup"
                :loading="proveedorLoading"
                item-title="name"
                item-value="id"
                variant="outlined"
                density="comfortable"
                clearable
                :error-messages="errors.proveedorId"
                @update:search="searchProveedores"
                @focus="ensureProveedoresLookup"
                @blur="validateField('proveedorId')"
                required
              >
                <template #label>
                  Proveedor <span class="required-asterisk">*</span>
                </template>
                <template #no-data>
                  <v-list-item
                    prepend-icon="mdi-plus-circle-outline"
                    title="Nuevo proveedor"
                    @click="openProveedorDialogFromProduct"
                  />
                </template>
              </v-autocomplete>
            </v-col>
          </v-row>

          <div class="text-subtitle-2 product-section-title">Precio</div>
          <v-row dense>
            <v-col cols="12" md="4">
              <MoneyField
                v-model="form.precioBase"
                label="Precio base"
                variant="outlined"
                density="comfortable"
                :error-messages="errors.precioBase"
                :step="100"
                @blur="validateField('precioBase')"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-select
                v-model="form.pricingMode"
                :items="pricingModeItems"
                label="Modo de precio"
                variant="outlined"
                density="comfortable"
                item-title="title"
                item-value="value"
              />
            </v-col>
            <v-col v-if="form.pricingMode === 'CATEGORIA'" cols="12" md="4">
              <v-autocomplete
                v-model="form.categoriaId"
                :items="categoriasLookupDisplay"
                :loading="categoriaLoading"
                label="Categoría"
                item-title="displayTitle"
                item-value="id"
                variant="outlined"
                density="comfortable"
                clearable
                @update:search="searchCategorias"
                @focus="ensureCategoriasLookup"
              >
                <template #no-data>
                  <v-list-item
                    prepend-icon="mdi-plus-circle-outline"
                    title="Nueva categoría"
                    @click="openCategoriaDialogFromProduct"
                  />
                </template>
              </v-autocomplete>
            </v-col>
            <v-col v-else-if="form.pricingMode === 'FIJO_PCT'" cols="12" md="4">
              <v-text-field
                v-model="form.margenPct"
                label="% Ganancia fija"
                variant="outlined"
                density="comfortable"
                type="number"
                min="0"
                step="0.01"
                :error-messages="errors.margenPct"
                @focus="clearZeroOnFocus(form, 'margenPct')"
                @blur="validateField('margenPct')"
              />
            </v-col>
            <v-col v-else-if="form.pricingMode === 'MANUAL'" cols="12" md="4">
              <MoneyField
                v-model="form.precioVentaManual"
                label="Precio venta manual"
                variant="outlined"
                density="comfortable"
                :error-messages="errors.precioVentaManual"
                :step="100"
                @blur="validateField('precioVentaManual')"
              />
            </v-col>
            <v-col v-else cols="12" md="4" class="d-flex align-center">
              <div class="text-caption text-medium-emphasis">
                Precio de venta calculado: {{ formatMoney(precioVentaCalculado) }}
              </div>
            </v-col>
          </v-row>
          <div :class="['product-final-price', { 'product-final-price--night': isNightMode }]">
            <span class="product-final-price__label">Precio final:</span>
            <span class="product-final-price__value">{{ formatMoney(precioVentaCalculado) }}</span>
          </div>

          <div class="text-subtitle-2 product-section-title">Stock</div>
          <v-row dense>
            <v-col v-if="productEditorMode === 'new'" cols="12" md="4">
              <v-text-field
                v-model="form.stockInicial"
                label="Stock inicial"
                type="number"
                min="0"
                step="0.01"
                variant="outlined"
                density="comfortable"
                :error-messages="errors.stockInicial"
                @focus="clearZeroOnFocus(form, 'stockInicial')"
                @blur="validateField('stockInicial')"
              />
            </v-col>
            <v-col cols="12" :md="productEditorMode === 'new' ? 4 : 6">
              <v-text-field
                v-model="stockConfig.stockMinimo"
                label="Stock minimo"
                type="number"
                min="0"
                step="0.01"
                variant="outlined"
                density="comfortable"
                :error-messages="errors.stockMinimo"
                @focus="clearZeroOnFocus(stockConfig, 'stockMinimo')"
                @blur="validateField('stockMinimo')"
              />
            </v-col>
            <v-col cols="12" :md="productEditorMode === 'new' ? 4 : 6">
              <div :class="['product-status-row', { 'product-status-row--night': isNightMode }]">
                <span class="product-status-row__label">Estado:</span>
                <v-switch
                  :model-value="form.isActive"
                  color="primary"
                  inset
                  hide-details
                  @update:model-value="toggleActive"
                >
                  <template #label>
                    {{ form.isActive ? 'Activo' : 'Inactivo' }}
                  </template>
                </v-switch>
              </div>
            </v-col>
          </v-row>
        </v-form>
        <v-card-actions class="justify-end px-0 pt-5">
          <v-btn variant="outlined" color="secondary" class="text-none product-dialog-action" @click="closeProductEditor">Cancelar</v-btn>
          <v-btn color="primary" variant="elevated" class="text-none product-dialog-action" :loading="saving" @click="saveProduct">
            Guardar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="proveedorDialog" persistent width="560">
      <v-card>
        <v-card-title>{{ proveedorDialogMode === 'edit' ? 'Editar proveedor' : 'Nuevo proveedor' }}</v-card-title>
        <v-card-text class="pt-4">
          <v-form @submit.prevent="saveProveedor">
            <v-text-field
              v-model="proveedorForm.name"
              label="Nombre"
              variant="outlined"
              density="comfortable"
              maxlength="100"
              counter="100"
              :error-messages="proveedorErrors.name"
              @blur="validateProveedorField('name')"
              required
            />
            <v-text-field
              v-model="proveedorForm.telefono"
              label="Telefono"
              variant="outlined"
              density="comfortable"
              maxlength="20"
              counter="20"
              :error-messages="proveedorErrors.telefono"
              @blur="validateProveedorField('telefono')"
              required
            />
            <v-text-field
              v-model="proveedorForm.cuit"
              label="CUIT"
              variant="outlined"
              density="comfortable"
              maxlength="15"
              counter="15"
            />
            <v-text-field
              v-model="proveedorForm.direccion"
              label="Direccion"
              variant="outlined"
              density="comfortable"
              maxlength="100"
              counter="100"
            />
            <v-switch
              :model-value="proveedorForm.isActive"
              label="Activo"
              color="primary"
              inset
              @update:model-value="(value) => (proveedorForm.isActive = value)"
            />
            <button type="submit" class="d-none" aria-hidden="true" />
          </v-form>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" @click="closeProveedorDialog">Cancelar</v-btn>
          <v-btn color="primary" :loading="proveedorSaving" @click="saveProveedor">Guardar</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="categoriaDialog" persistent width="560">
      <v-card>
        <v-card-title>{{ categoriaDialogMode === 'edit' ? 'Editar categoría' : 'Nueva categoría' }}</v-card-title>
        <v-card-text class="pt-4">
          <v-form @submit.prevent="saveCategoria">
            <v-text-field
              v-model="categoriaForm.name"
              label="Nombre"
              variant="outlined"
              density="comfortable"
              maxlength="80"
              counter="80"
              :error-messages="categoriaErrors.name"
              @blur="validateCategoriaField('name')"
              required
            />
            <v-text-field
              v-model="categoriaForm.margenGananciaPct"
              label="% Ganancia"
              variant="outlined"
              density="comfortable"
              type="number"
              min="0"
              step="0.01"
              :error-messages="categoriaErrors.margenGananciaPct"
              @blur="validateCategoriaField('margenGananciaPct')"
              required
            />
            <v-checkbox
              v-model="categoriaForm.aplicarAProductos"
              label="Aplicar nuevo porcentaje a productos de la categoría"
              color="primary"
              hide-details
            />
            <v-switch
              :model-value="categoriaForm.isActive"
              label="Activo"
              color="primary"
              inset
              @update:model-value="(value) => (categoriaForm.isActive = value)"
            />
            <button type="submit" class="d-none" aria-hidden="true" />
          </v-form>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" @click="closeCategoriaDialog">Cancelar</v-btn>
          <v-btn color="primary" :loading="categoriaSaving" @click="saveCategoria">Guardar</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="comboDialog" persistent width="860">
      <v-card class="pa-4">
        <div class="text-h6">{{ comboEditorMode === 'edit' ? 'Editar combo' : 'Nuevo combo' }}</div>
        <div class="text-caption text-medium-emphasis">Definí los productos que lo componen y el precio final.</div>

        <v-row dense class="mt-3">
          <v-col cols="12" md="8">
            <v-text-field v-model="comboForm.name" label="Nombre" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12" md="4">
            <v-text-field
              v-model="comboForm.sku"
              label="SKU"
              variant="outlined"
              density="comfortable"
              inputmode="numeric"
              @update:model-value="onComboSkuInput"
            />
          </v-col>
        </v-row>

        <v-row dense>
          <v-col cols="12" md="8">
            <v-autocomplete
              v-model="comboSelectedProductId"
              :items="comboProductOptions"
              item-title="name"
              item-value="id"
              label="Agregar producto"
              variant="outlined"
              density="comfortable"
              clearable
            />
          </v-col>
          <v-col cols="12" md="2">
            <v-text-field
              v-model.number="comboSelectedCantidad"
              label="Cantidad"
              type="number"
              min="1"
              step="1"
              variant="outlined"
              density="comfortable"
            />
          </v-col>
          <v-col cols="12" md="2" class="d-flex align-center">
            <v-btn color="primary" class="text-none" @click="addComboItem">Agregar</v-btn>
          </v-col>
        </v-row>

        <v-data-table
          class="mt-2"
          :headers="comboItemHeaders"
          :items="comboForm.items"
          density="compact"
          item-key="productoId"
        >
          <template v-slot:[`item.subtotal`]='{ item }'>
            {{ formatMoney(Number(item.precioVenta || 0) * Number(item.cantidad || 0)) }}
          </template>
          <template v-slot:[`item.actions`]='{ item }'>
            <v-btn icon="mdi-delete-outline" size="x-small" variant="text" color="error" @click="removeComboItem(item.productoId)" />
          </template>
        </v-data-table>

        <v-row dense class="mt-2">
          <v-col cols="12" md="6">
            <div class="text-caption text-medium-emphasis">Total base (suma de productos)</div>
            <div class="text-h6">{{ formatMoney(comboBaseTotal) }}</div>
          </v-col>
          <v-col cols="12" md="6">
            <MoneyField v-model="comboForm.precioVenta" label="Precio final del combo" variant="outlined" density="comfortable" :step="100" />
          </v-col>
        </v-row>

        <v-card-actions class="justify-end px-0 pt-4">
          <v-btn variant="text" @click="closeComboDialog">Cancelar</v-btn>
          <v-btn color="primary" :loading="comboSaving" @click="saveCombo">{{ comboEditorMode === 'edit' ? 'Guardar cambios' : 'Guardar combo' }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" location="top end" timeout="1700">
      <div class="d-flex align-center gap-2">
        <v-icon>{{ snackbar.icon }}</v-icon>
        <span>{{ snackbar.text }}</span>
      </div>
    </v-snackbar>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useTheme } from 'vuetify';
import MoneyField from '../components/MoneyField.vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { buildApiUrl, getJson, postJson, requestJson } from '../services/apiClient';
import { formatMoney } from '../utils/currency';

const tab = ref('productos');
const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const theme = useTheme();
const isNightMode = computed(() => theme.global.name.value === 'posNightTheme');

const products = ref([]);
const combos = ref([]);
const loading = ref(false);
const comboLoading = ref(false);
const saving = ref(false);
const comboSaving = ref(false);
const barcodeLoading = ref(false);

const search = ref('');
const activoFilter = ref('all');
let loadProductsDebounceId = null;
let loadProveedoresDebounceId = null;
let loadCategoriasDebounceId = null;

const proveedoresLookup = ref([]);
const proveedorLoading = ref(false);
const categoriaLoading = ref(false);
const categoriasLookup = ref([]);
const proveedorLookupSearch = ref('');
const productEditorMode = ref('');
const proveedorDialogMode = ref('');
const categoriaDialogMode = ref('');
const deleteDialog = ref(false);
const deleteLoading = ref(false);
const deleteTarget = ref(null);

const form = reactive({
  id: '',
  name: '',
  sku: '',
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

const stockConfig = reactive({
  stockMinimo: ''
});

const comboDialog = ref(false);
const comboEditorMode = ref('new');
const comboSelectedProductId = ref('');
const comboSelectedCantidad = ref(1);
const comboProductOptions = ref([]);
const comboForm = reactive({
  id: '',
  name: '',
  sku: '',
  precioVenta: '',
  items: []
});

const errors = reactive({
  name: '',
  sku: '',
  proveedorId: '',
  precioBase: '',
  margenPct: '',
  precioVentaManual: '',
  stockInicial: '',
  stockMinimo: ''
});


const dialogDesactivar = ref(false);
const proveedorDialog = ref(false);
const categoriaDialog = ref(false);
const seleccionarProveedorDespuesDeGuardar = ref(false);
const seleccionarCategoriaDespuesDeGuardar = ref(false);
const pendingActive = ref(true);

const snackbar = ref({
  show: false,
  text: '',
  color: 'success',
  icon: 'mdi-check-circle'
});

const deleteEntityLabels = {
  producto: 'el producto',
  proveedor: 'el proveedor',
  categoria: 'la categoría'
};

const headers = [
  { title: 'Nombre', value: 'name' },
  { title: 'SKU', value: 'sku' },
  { title: 'Categoria', value: 'categoria' },
  { title: 'Proveedor', value: 'proveedor' },
  { title: 'Precio base', value: 'precioBase' },
  { title: 'Precio venta', value: 'precioVenta' },
  { title: 'Estado', value: 'isActive' },
  { title: '', value: 'actions', sortable: false, align: 'end' }
];

const comboHeaders = [
  { title: 'Nombre', value: 'name' },
  { title: 'SKU', value: 'sku' },
  { title: 'Precio', value: 'precioVenta', align: 'end' },
  { title: '', value: 'actions', sortable: false, align: 'end' }
];

const comboItemHeaders = [
  { title: 'Producto', value: 'name' },
  { title: 'SKU', value: 'sku' },
  { title: 'Cantidad', value: 'cantidad', align: 'end' },
  { title: 'Precio', value: 'precioVenta', align: 'end' },
  { title: 'Subtotal', value: 'subtotal', align: 'end' },
  { title: '', value: 'actions', sortable: false, align: 'end' }
];

const pricingModeItems = [
  { title: '% fijo', value: 'FIJO_PCT' },
  { title: 'Por categoria', value: 'CATEGORIA' },
  { title: 'Manual', value: 'MANUAL' }
];

const proveedorHeaders = [
  { title: 'Nombre', value: 'name' },
  { title: 'Telefono', value: 'telefono' },
  { title: 'CUIT', value: 'cuit' },
  { title: 'Direccion', value: 'direccion' },
  { title: 'Estado', value: 'isActive' },
  { title: '', value: 'actions', sortable: false, align: 'end' }
];

const categoriaHeaders = [
  { title: 'Nombre', value: 'name' },
  { title: '% Ganancia', value: 'margenGananciaPct' },
  { title: 'Estado', value: 'isActive' },
  { title: '', value: 'actions', sortable: false, align: 'end' }
];

const proveedoresTable = ref([]);
const proveedorSearch = ref('');
const proveedorActivoFilter = ref('all');
const proveedorLoadingTable = ref(false);
const proveedorSaving = ref(false);

const proveedorForm = reactive({
  id: '',
  name: '',
  telefono: '',
  cuit: '',
  direccion: '',
  isActive: true
});

const proveedorErrors = reactive({
  name: '',
  telefono: ''
});

const categoriasTable = ref([]);
const categoriaSearch = ref('');
const categoriaActivoFilter = ref('all');
const categoriaLoadingTable = ref(false);
const categoriaSaving = ref(false);

const categoriaForm = reactive({
  id: '',
  name: '',
  margenGananciaPct: '30',
  aplicarAProductos: true,
  isActive: true
});

const categoriaErrors = reactive({
  name: '',
  margenGananciaPct: ''
});

const deleteDialogText = computed(() => {
  if (!deleteTarget.value) return '';
  return `Vas a eliminar ${deleteEntityLabels[deleteTarget.value.type]} "${deleteTarget.value.name}". Esta acción no se puede deshacer.`;
});

const categoriasLookupDisplay = computed(() =>
  (categoriasLookup.value || []).map((item) => ({
    ...item,
    displayTitle: `${item.name || 'Sin nombre'} (${Number(item.margenGananciaPct || 0).toFixed(2)}%)`
  }))
);

const categoriaSeleccionadaLabel = computed(() => {
  if (!form.categoriaId) return 'Sin categoria';
  const categoria = (categoriasLookup.value || []).find((item) => item.id === form.categoriaId);
  if (!categoria) return 'Sin categoria';
  return `${categoria.name || 'Sin nombre'} (${Number(categoria.margenGananciaPct || 0).toFixed(2)}%)`;
});

const precioVentaCalculado = computed(() => {
  const base = Number(form.precioBase);
  if (Number.isNaN(base) || base <= 0) return 0;

  if (form.pricingMode === 'MANUAL') {
    const manual = Number(form.precioVentaManual);
    return Number.isNaN(manual) || manual < 0 ? 0 : manual;
  }

  const margen = form.pricingMode === 'CATEGORIA'
    ? Number(form.margenCategoriaPct || 0)
    : Number(form.margenPct);
  if (Number.isNaN(margen) || margen < 0) return base;
  return base * (1 + margen / 100);
});

const comboBaseTotal = computed(() =>
  comboForm.items.reduce((acc, item) => acc + (Number(item.precioVenta || 0) * Number(item.cantidad || 0)), 0)
);

const shortId = (value) => {
  if (!value) return 'n/a';
  return value.length > 8 ? value.slice(0, 8) : value;
};

const resolveTableRow = (row) => row?.raw ?? row?.item?.raw ?? row?.item ?? row;

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

const clearErrors = () => {
  Object.keys(errors).forEach((key) => {
    errors[key] = '';
  });
};

const validateField = (field) => {
  if (field === 'name') {
    errors.name = form.name.trim() ? '' : '* El nombre es obligatorio.';
  }
  if (field === 'sku') {
    if (!form.sku.trim()) {
      errors.sku = '* El SKU es obligatorio.';
    } else if (!/^\d+$/.test(form.sku.trim())) {
      errors.sku = '* El SKU debe contener solo numeros.';
    } else {
      errors.sku = '';
    }
  }
  if (field === 'proveedorId') {
    errors.proveedorId = form.proveedorId ? '' : '* El proveedor es obligatorio.';
  }
  if (field === 'precioBase') {
    if (form.precioBase === '') {
      errors.precioBase = '';
    } else if (Number(form.precioBase) < 0 || Number.isNaN(Number(form.precioBase))) {
      errors.precioBase = 'Precio base invalido.';
    } else {
      errors.precioBase = '';
    }
  }
  if (field === 'margenPct') {
    if (form.pricingMode !== 'FIJO_PCT') {
      errors.margenPct = '';
    } else if (form.margenPct === '') {
      errors.margenPct = '';
    } else if (Number(form.margenPct) < 0 || Number.isNaN(Number(form.margenPct))) {
      errors.margenPct = 'Margen invalido.';
    } else {
      errors.margenPct = '';
    }
  }
  if (field === 'precioVentaManual') {
    if (form.pricingMode !== 'MANUAL') {
      errors.precioVentaManual = '';
    } else if (form.precioVentaManual === '') {
      errors.precioVentaManual = 'Precio venta obligatorio.';
    } else if (Number(form.precioVentaManual) < 0 || Number.isNaN(Number(form.precioVentaManual))) {
      errors.precioVentaManual = 'Precio venta invalido.';
    } else {
      errors.precioVentaManual = '';
    }
  }
  if (field === 'stockMinimo') {
    if (stockConfig.stockMinimo === '') {
      errors.stockMinimo = '';
    } else if (Number(stockConfig.stockMinimo) < 0 || Number.isNaN(Number(stockConfig.stockMinimo))) {
      errors.stockMinimo = 'Stock minimo invalido.';
    } else {
      errors.stockMinimo = '';
    }
  }
  if (field === 'stockInicial') {
    if (productEditorMode.value !== 'new') {
      errors.stockInicial = '';
    } else if (form.stockInicial === '') {
      errors.stockInicial = '';
    } else if (Number(form.stockInicial) < 0 || Number.isNaN(Number(form.stockInicial))) {
      errors.stockInicial = 'Stock inicial invalido.';
    } else {
      errors.stockInicial = '';
    }
  }
};

const validateForm = () => {
  validateField('name');
  validateField('sku');
  validateField('proveedorId');
  validateField('precioBase');
  validateField('margenPct');
  validateField('precioVentaManual');
  if (productEditorMode.value === 'new') {
    validateField('stockInicial');
  } else {
    errors.stockInicial = '';
  }
  validateField('stockMinimo');
  return (
    !errors.name &&
    !errors.sku &&
    !errors.proveedorId &&
    !errors.precioBase &&
    !errors.margenPct &&
    !errors.precioVentaManual &&
    !errors.stockInicial &&
    !errors.stockMinimo
  );
};

const loadProducts = async () => {
  loading.value = true;
  try {
    const params = new URLSearchParams();
    if (search.value.trim()) params.set('search', search.value.trim());
    if (activoFilter.value !== 'all') params.set('activo', activoFilter.value);
    params.set('combo', 'false');

    const { response, data } = await getJson(`/api/v1/productos?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    products.value = (data || [])
      .slice()
      .sort((a, b) => (a.name || '').localeCompare(b.name || '', 'es'));
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar los productos.');
  } finally {
    loading.value = false;
  }
};

const loadCombos = async () => {
  comboLoading.value = true;
  try {
    const params = new URLSearchParams();
    params.set('combo', 'true');
    params.set('activo', 'true');
    const { response, data } = await getJson(`/api/v1/productos?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    combos.value = (data || []).slice().sort((a, b) => (a.name || '').localeCompare(b.name || '', 'es'));
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar los combos.');
  } finally {
    comboLoading.value = false;
  }
};

const loadComboProducts = async () => {
  try {
    const params = new URLSearchParams();
    params.set('combo', 'false');
    params.set('activo', 'true');
    const { response, data } = await getJson(`/api/v1/productos?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    comboProductOptions.value = data || [];
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar productos para combo.');
  }
};

const resetComboForm = () => {
  comboForm.id = '';
  comboForm.name = '';
  comboForm.sku = '';
  comboForm.precioVenta = '';
  comboForm.items = [];
  comboSelectedProductId.value = '';
  comboSelectedCantidad.value = 1;
  comboEditorMode.value = 'new';
};

const onComboSkuInput = (value) => {
  comboForm.sku = String(value || '').replace(/\D+/g, '');
};

const openNewComboDialog = async () => {
  resetComboForm();
  await loadComboProducts();
  comboDialog.value = true;
};

const openEditCombo = async (row) => {
  const combo = resolveTableRow(row);
  if (!combo?.id) return;

  comboSaving.value = true;
  try {
    await loadComboProducts();
    const { response, data } = await getJson(`/api/v1/productos/${combo.id}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    comboForm.id = data.id;
    comboForm.name = data.name || '';
    comboForm.sku = data.sku || '';
    comboForm.precioVenta = data.precioVenta ?? '';

    const items = Array.isArray(data.comboItems) ? data.comboItems : [];
    comboForm.items = items.map((item) => {
      const product = comboProductOptions.value.find((p) => p.id === item.productoId);
      return {
        productoId: item.productoId,
        name: product?.name || item.name || 'Producto',
        sku: product?.sku || item.sku || '',
        precioVenta: Number(product?.precioVenta || 0),
        cantidad: Number(item.cantidad || 0)
      };
    }).filter((item) => item.productoId && item.cantidad > 0);

    comboEditorMode.value = 'edit';
    comboDialog.value = true;
  } catch (err) {
    flash('error', err?.message || 'No se pudo cargar el combo.');
  } finally {
    comboSaving.value = false;
  }
};

const closeComboDialog = () => {
  comboDialog.value = false;
  resetComboForm();
};

const addComboItem = () => {
  const product = comboProductOptions.value.find((p) => p.id === comboSelectedProductId.value);
  const cantidad = Number(comboSelectedCantidad.value || 0);
  if (!product || cantidad <= 0) return;

  const existing = comboForm.items.find((x) => x.productoId === product.id);
  if (existing) {
    existing.cantidad = Number(existing.cantidad) + cantidad;
  } else {
    comboForm.items.push({
      productoId: product.id,
      name: product.name,
      sku: product.sku,
      precioVenta: Number(product.precioVenta || 0),
      cantidad
    });
  }

  comboForm.precioVenta = Number(comboForm.precioVenta || 0) > 0
    ? comboForm.precioVenta
    : comboBaseTotal.value.toFixed(2);
  comboSelectedProductId.value = '';
  comboSelectedCantidad.value = 1;
};

const removeComboItem = (productoId) => {
  comboForm.items = comboForm.items.filter((x) => x.productoId !== productoId);
};

const saveCombo = async () => {
  if (comboSaving.value) return;
  if (!comboForm.name.trim()) {
    flash('error', 'El nombre del combo es obligatorio.');
    return;
  }
  if (!/^\d+$/.test(comboForm.sku.trim())) {
    flash('error', 'El SKU del combo debe ser numerico.');
    return;
  }
  if (!comboForm.items.length) {
    flash('error', 'Agregá productos al combo.');
    return;
  }

  comboSaving.value = true;
  try {
    const baseTotal = Number(comboBaseTotal.value || 0);
    const precioFinal = Number(comboForm.precioVenta || baseTotal);
    const payload = {
      name: comboForm.name.trim(),
      sku: comboForm.sku.trim(),
      proveedorId: null,
      categoriaId: null,
      isActive: true,
      precioBase: baseTotal,
      precioVenta: precioFinal,
      pricingMode: 'MANUAL',
      margenGananciaPct: null,
      isCombo: true,
      comboItems: comboForm.items.map((x) => ({ productoId: x.productoId, cantidad: Number(x.cantidad) }))
    };

    const isEdit = comboEditorMode.value === 'edit' && comboForm.id;
    const result = isEdit
      ? await requestJson(`/api/v1/productos/${comboForm.id}`, {
        method: 'PATCH',
        body: JSON.stringify(payload)
      })
      : await postJson('/api/v1/productos', payload);

    const { response, data } = result;
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    flash('success', isEdit ? 'Combo actualizado' : 'Combo creado');
    closeComboDialog();
    await loadCombos();
  } catch (err) {
    flash('error', err?.message || 'No se pudo guardar el combo.');
  } finally {
    comboSaving.value = false;
  }
};

const searchProveedores = async (term) => {
  proveedorLookupSearch.value = term || '';
  proveedorLoading.value = true;
  try {
    const params = new URLSearchParams();
    if (term && term.trim()) params.set('busqueda', term.trim());
    params.set('activo', 'true');
    const { response, data } = await getJson(`/api/v1/proveedores?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    proveedoresLookup.value = (data || [])
      .slice()
      .sort((a, b) => (a.name || '').localeCompare(b.name || '', 'es'));
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar proveedores.');
  } finally {
    proveedorLoading.value = false;
  }
};

const ensureProveedoresLookup = async () => {
  if (proveedoresLookup.value.length > 0 || proveedorLoading.value) return;
  await searchProveedores(proveedorLookupSearch.value);
};

const applyCategoriaMargin = () => {
  const categoria = categoriasLookup.value.find((item) => item.id === form.categoriaId);
  form.margenCategoriaPct = categoria ? Number(categoria.margenGananciaPct || 0).toString() : '0';
};

const searchCategorias = async (term) => {
  categoriaLoading.value = true;
  try {
    const params = new URLSearchParams();
    if (term && term.trim()) params.set('busqueda', term.trim());
    params.set('activo', 'true');
    const { response, data } = await getJson(`/api/v1/categorias-precio?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    categoriasLookup.value = (data || [])
      .slice()
      .sort((a, b) => (a.name || '').localeCompare(b.name || '', 'es'));
    applyCategoriaMargin();
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar categorias.');
  } finally {
    categoriaLoading.value = false;
  }
};

const ensureCategoriasLookup = async () => {
  if (categoriasLookup.value.length > 0 || categoriaLoading.value) return;
  await searchCategorias('');
};

const openEditProduct = async (row) => {
  const product = resolveTableRow(row);
  if (!product?.id) return;
  try {
    const { response, data } = await getJson(`/api/v1/productos/${product.id}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    form.id = data.id;
    form.name = data.name || '';
    form.sku = data.sku || '';
    form.proveedorId = data.proveedorId || '';
    form.categoriaId = data.categoriaId || '';
    form.isActive = data.isActive ?? true;
    const base = Number(data.precioBase ?? 0);
    const venta = Number(data.precioVenta ?? base);
    const margen = base > 0 ? ((venta / base) - 1) * 100 : 0;
    form.precioBase = base ? base.toString() : '';
    form.pricingMode = data.pricingMode || 'FIJO_PCT';
    form.margenPct = Number.isFinite(margen) ? margen.toFixed(2) : '0';
    form.margenCategoriaPct = data.margenGananciaPct != null
      ? Number(data.margenGananciaPct).toString()
      : form.margenCategoriaPct;
    form.precioVentaManual = venta ? venta.toString() : '';
    form.stockInicial = '';

    const configResp = await getJson(`/api/v1/productos/${product.id}/stock-config`);
    if (configResp.response.ok) {
      stockConfig.stockMinimo = configResp.data.stockMinimo?.toString?.() ?? '0';
    } else {
      stockConfig.stockMinimo = '0';
    }

    clearErrors();
    applyCategoriaMargin();
    productEditorMode.value = 'edit';
  } catch (err) {
    flash('error', err?.message || 'No se pudo cargar el producto.');
  }
};

const resetForm = () => {
  form.id = '';
  form.name = '';
  form.sku = '';
  form.proveedorId = '';
  form.categoriaId = '';
  form.precioBase = '';
  form.pricingMode = 'FIJO_PCT';
  form.margenPct = '30';
  form.margenCategoriaPct = '0';
  form.precioVentaManual = '';
  form.stockInicial = '';
  form.isActive = true;
  stockConfig.stockMinimo = '0';
  clearErrors();
};

const onSkuInput = (value) => {
  form.sku = (value || '').replace(/\D+/g, '');
};

const clearZeroOnFocus = (target, field) => {
  if (!target || !(field in target)) return;
  const currentValue = String(target[field] ?? '').trim();
  if (currentValue === '0' || currentValue === '0.00' || currentValue === '0,00') {
    target[field] = '';
  }
};

const startNewProduct = () => {
  resetForm();
  productEditorMode.value = 'new';
};

const closeProductEditor = () => {
  resetForm();
  productEditorMode.value = '';
};

const saveStockConfig = async (productId) => {
  const payload = {
    stockMinimo: stockConfig.stockMinimo === '' ? 0 : Number(stockConfig.stockMinimo)
  };

  const { response, data } = await requestJson(`/api/v1/productos/${productId}/stock-config`, {
    method: 'PATCH',
    body: JSON.stringify(payload)
  });
  if (!response.ok) {
    throw new Error(extractProblemMessage(data));
  }
};

const applyStockInicial = async (productId) => {
  if (form.stockInicial === '') return;
  const qty = Number(form.stockInicial);
  if (Number.isNaN(qty) || qty <= 0) return;

  const payload = {
    tipo: 'AJUSTE',
    motivo: 'Stock inicial',
    items: [
      {
        productoId: productId,
        cantidad: qty,
        esIngreso: true
      }
    ]
  };

  const { response, data } = await postJson('/api/v1/stock/ajustes', payload);
  if (!response.ok) {
    throw new Error(extractProblemMessage(data));
  }
  form.stockInicial = '';
};

const saveProduct = async () => {
  if (saving.value) return;
  if (!validateForm()) return;

  saving.value = true;
  try {
    const precioBase = form.precioBase === '' ? null : Number(form.precioBase);
    const precioVenta = form.pricingMode === 'MANUAL'
      ? (form.precioVentaManual === '' ? null : Number(form.precioVentaManual))
      : (precioBase == null ? null : Number(precioVentaCalculado.value));
    const margenGananciaPct = form.pricingMode === 'FIJO_PCT'
      ? (form.margenPct === '' ? null : Number(form.margenPct))
      : null;

    if (!form.id) {
      const payload = {
        name: form.name.trim(),
        sku: form.sku.trim(),
        proveedorId: form.proveedorId,
        categoriaId: form.categoriaId || null,
        isActive: form.isActive,
        precioBase,
        precioVenta,
        pricingMode: form.pricingMode,
        margenGananciaPct
      };

      const { response, data } = await postJson('/api/v1/productos', payload);
      if (!response.ok) {
        throw new Error(extractProblemMessage(data));
      }
      const productId = data.id;
      await saveStockConfig(productId);
      await applyStockInicial(productId);
      flash('success', 'Producto creado');
      await loadProducts();
      closeProductEditor();
      return;
    }

    const payload = {
      name: form.name.trim() || null,
      sku: form.sku.trim() || null,
      proveedorId: form.proveedorId,
      categoriaId: form.categoriaId || null,
      isActive: form.isActive,
      precioBase,
      precioVenta,
      pricingMode: form.pricingMode,
      margenGananciaPct
    };

    const { response, data } = await requestJson(`/api/v1/productos/${form.id}`, {
      method: 'PATCH',
      body: JSON.stringify(payload)
    });
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    await saveStockConfig(form.id);
    flash('success', 'Producto actualizado');
    await loadProducts();
    closeProductEditor();
  } catch (err) {
    flash('error', err?.message || 'No se pudo guardar el producto.');
  } finally {
    saving.value = false;
  }
};

const printBarcodes = async () => {
  if (barcodeLoading.value) return;

  const productoIds = products.value
    .map((p) => p.id)
    .filter((id) => typeof id === 'string' && id.length > 0);

  if (!productoIds.length) {
    flash('error', 'No hay productos para imprimir.');
    return;
  }

  barcodeLoading.value = true;
  try {
    const response = await fetch(buildApiUrl('/api/v1/etiquetas/codigos-barra/pdf'), {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(auth.token ? { Authorization: `Bearer ${auth.token}` } : {})
      },
      body: JSON.stringify({ productoIds })
    });

    if (!response.ok) {
      let message = 'No se pudo generar el PDF.';
      try {
        const data = await response.json();
        message = extractProblemMessage(data);
      } catch {
        // ignore parse errors and keep generic message
      }

      throw new Error(message);
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'codigos-barra-productos.pdf';
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
    flash('success', 'PDF generado');
  } catch (err) {
    flash('error', err?.message || 'No se pudo generar el PDF.');
  } finally {
    barcodeLoading.value = false;
  }
};

const toggleActive = (value) => {
  if (form.isActive && !value) {
    pendingActive.value = false;
    dialogDesactivar.value = true;
    return;
  }
  form.isActive = value;
};

const cancelDeactivate = () => {
  dialogDesactivar.value = false;
  form.isActive = true;
};

const confirmDeactivate = () => {
  dialogDesactivar.value = false;
  form.isActive = pendingActive.value;
};

const validateProveedorField = (field) => {
  if (field === 'name') {
    proveedorErrors.name = proveedorForm.name.trim() ? '' : 'El nombre es obligatorio.';
  }
  if (field === 'telefono') {
    proveedorErrors.telefono = proveedorForm.telefono.trim() ? '' : 'El telefono es obligatorio.';
  }
};

const resetProveedorForm = () => {
  proveedorForm.id = '';
  proveedorForm.name = '';
  proveedorForm.telefono = '';
  proveedorForm.cuit = '';
  proveedorForm.direccion = '';
  proveedorForm.isActive = true;
  proveedorErrors.name = '';
  proveedorErrors.telefono = '';
};

const openProveedorDialogFromProduct = () => {
  seleccionarProveedorDespuesDeGuardar.value = true;
  resetProveedorForm();
  proveedorForm.name = proveedorLookupSearch.value.trim();
  proveedorDialogMode.value = 'new';
  proveedorDialog.value = true;
};

const openProveedorDialogFromList = () => {
  seleccionarProveedorDespuesDeGuardar.value = false;
  resetProveedorForm();
  proveedorDialogMode.value = 'new';
  proveedorDialog.value = true;
};

const closeProveedorDialog = () => {
  proveedorDialog.value = false;
  proveedorDialogMode.value = '';
  seleccionarProveedorDespuesDeGuardar.value = false;
  resetProveedorForm();
};

const loadProveedores = async () => {
  proveedorLoadingTable.value = true;
  try {
    const params = new URLSearchParams();
    if (proveedorSearch.value.trim()) params.set('busqueda', proveedorSearch.value.trim());
    if (proveedorActivoFilter.value !== 'all') params.set('activo', proveedorActivoFilter.value);
    const { response, data } = await getJson(`/api/v1/proveedores?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    proveedoresTable.value = (data || [])
      .slice()
      .sort((a, b) => (a.name || '').localeCompare(b.name || '', 'es'));
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar proveedores.');
  } finally {
    proveedorLoadingTable.value = false;
  }
};

const openEditProveedor = (row) => {
  const proveedor = resolveTableRow(row);
  if (!proveedor?.id) return;
  seleccionarProveedorDespuesDeGuardar.value = false;
  proveedorForm.id = proveedor.id;
  proveedorForm.name = proveedor.name || '';
  proveedorForm.telefono = proveedor.telefono || '';
  proveedorForm.cuit = proveedor.cuit || '';
  proveedorForm.direccion = proveedor.direccion || '';
  proveedorForm.isActive = proveedor.isActive ?? true;
  proveedorErrors.name = '';
  proveedorErrors.telefono = '';
  proveedorDialogMode.value = 'edit';
  proveedorDialog.value = true;
};

const saveProveedor = async () => {
  if (proveedorSaving.value) return;
  validateProveedorField('name');
  validateProveedorField('telefono');
  if (proveedorErrors.name || proveedorErrors.telefono) return;

  proveedorSaving.value = true;
  try {
    let savedProveedor = null;
    const payload = {
      name: proveedorForm.name.trim(),
      telefono: proveedorForm.telefono.trim(),
      cuit: proveedorForm.cuit.trim() || null,
      direccion: proveedorForm.direccion.trim() || null,
      isActive: proveedorForm.isActive
    };

    if (!proveedorForm.id) {
      const { response, data } = await postJson('/api/v1/proveedores', payload);
      if (!response.ok) {
        throw new Error(extractProblemMessage(data));
      }
      savedProveedor = data;
      flash('success', 'Proveedor creado');
    } else {
      const { response, data } = await requestJson(`/api/v1/proveedores/${proveedorForm.id}`, {
        method: 'PATCH',
        body: JSON.stringify(payload)
      });
      if (!response.ok) {
        throw new Error(extractProblemMessage(data));
      }
      savedProveedor = data ?? { id: proveedorForm.id, ...payload };
      flash('success', 'Proveedor actualizado');
    }

    await loadProveedores();
    await searchProveedores('');

    if (seleccionarProveedorDespuesDeGuardar.value && savedProveedor?.id) {
      form.proveedorId = savedProveedor.id;
      validateField('proveedorId');
      closeProveedorDialog();
      return;
    }

    closeProveedorDialog();
  } catch (err) {
    flash('error', err?.message || 'No se pudo guardar el proveedor.');
  } finally {
    proveedorSaving.value = false;
  }
};

const validateCategoriaField = (field) => {
  if (field === 'name') {
    categoriaErrors.name = categoriaForm.name.trim() ? '' : 'El nombre es obligatorio.';
  }
  if (field === 'margenGananciaPct') {
    const value = Number(categoriaForm.margenGananciaPct);
    categoriaErrors.margenGananciaPct = Number.isNaN(value) || value < 0
      ? 'Margen invalido.'
      : '';
  }
};

const resetCategoriaForm = () => {
  categoriaForm.id = '';
  categoriaForm.name = '';
  categoriaForm.margenGananciaPct = '30';
  categoriaForm.aplicarAProductos = true;
  categoriaForm.isActive = true;
  categoriaErrors.name = '';
  categoriaErrors.margenGananciaPct = '';
};

const openCategoriaDialogForCreate = () => {
  seleccionarCategoriaDespuesDeGuardar.value = false;
  resetCategoriaForm();
  categoriaDialogMode.value = 'new';
  categoriaDialog.value = true;
};

const openCategoriaDialogFromProduct = () => {
  seleccionarCategoriaDespuesDeGuardar.value = true;
  resetCategoriaForm();
  categoriaForm.name = categoriaSearch.value.trim();
  categoriaDialogMode.value = 'new';
  categoriaDialog.value = true;
};

const closeCategoriaDialog = () => {
  categoriaDialog.value = false;
  categoriaDialogMode.value = '';
  seleccionarCategoriaDespuesDeGuardar.value = false;
  resetCategoriaForm();
};

const loadCategorias = async () => {
  categoriaLoadingTable.value = true;
  try {
    const params = new URLSearchParams();
    if (categoriaSearch.value.trim()) params.set('busqueda', categoriaSearch.value.trim());
    if (categoriaActivoFilter.value !== 'all') params.set('activo', categoriaActivoFilter.value);

    const { response, data } = await getJson(`/api/v1/categorias-precio?${params.toString()}`);
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    categoriasTable.value = (data || [])
      .slice()
      .sort((a, b) => (a.name || '').localeCompare(b.name || '', 'es'));
    categoriasLookup.value = categoriasTable.value
      .filter((item) => item.isActive)
      .map((item) => ({ ...item }));
    applyCategoriaMargin();
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar categorias.');
  } finally {
    categoriaLoadingTable.value = false;
  }
};

const openEditCategoria = (row) => {
  const categoria = resolveTableRow(row);
  if (!categoria?.id) return;
  categoriaForm.id = categoria.id;
  categoriaForm.name = categoria.name || '';
  categoriaForm.margenGananciaPct = Number(categoria.margenGananciaPct || 0).toString();
  categoriaForm.aplicarAProductos = true;
  categoriaForm.isActive = categoria.isActive ?? true;
  categoriaErrors.name = '';
  categoriaErrors.margenGananciaPct = '';
  categoriaDialogMode.value = 'edit';
  categoriaDialog.value = true;
};

const saveCategoria = async () => {
  if (categoriaSaving.value) return;
  validateCategoriaField('name');
  validateCategoriaField('margenGananciaPct');
  if (categoriaErrors.name || categoriaErrors.margenGananciaPct) return;

  categoriaSaving.value = true;
  try {
    let savedCategoria = null;
    const payload = {
      name: categoriaForm.name.trim(),
      margenGananciaPct: Number(categoriaForm.margenGananciaPct),
      isActive: categoriaForm.isActive,
      aplicarAProductos: categoriaForm.aplicarAProductos
    };

    if (!categoriaForm.id) {
      const { response, data } = await postJson('/api/v1/categorias-precio', payload);
      if (!response.ok) throw new Error(extractProblemMessage(data));
      savedCategoria = data;
      flash('success', 'Categoria creada');
    } else {
      const { response, data } = await requestJson(`/api/v1/categorias-precio/${categoriaForm.id}`, {
        method: 'PATCH',
        body: JSON.stringify(payload)
      });
      if (!response.ok) throw new Error(extractProblemMessage(data));
      savedCategoria = data;
      flash('success', 'Categoria actualizada');
    }

    await loadCategorias();
    await loadProducts();
    if (seleccionarCategoriaDespuesDeGuardar.value && savedCategoria?.id) {
      form.categoriaId = savedCategoria.id;
      applyCategoriaMargin();
    }
    closeCategoriaDialog();
  } catch (err) {
    flash('error', err?.message || 'No se pudo guardar la categoria.');
  } finally {
    categoriaSaving.value = false;
  }
};

const promptDelete = (type, row) => {
  const entity = resolveTableRow(row);
  if (!entity?.id) return;

  deleteTarget.value = {
    id: entity.id,
    type,
    name: entity.name || 'Sin nombre'
  };
  deleteDialog.value = true;
};

const closeDeleteDialog = () => {
  deleteDialog.value = false;
  deleteTarget.value = null;
};

const confirmDelete = async () => {
  if (!deleteTarget.value || deleteLoading.value) return;

  const target = deleteTarget.value;
  const endpointMap = {
    producto: `/api/v1/productos/${target.id}`,
    proveedor: `/api/v1/proveedores/${target.id}`,
    categoria: `/api/v1/categorias-precio/${target.id}`
  };
  const successMap = {
    producto: 'Producto eliminado',
    proveedor: 'Proveedor eliminado',
    categoria: 'Categoría eliminada'
  };

  deleteLoading.value = true;
  try {
    const { response, data } = await requestJson(endpointMap[target.type], {
      method: 'DELETE'
    });
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    if (target.type === 'producto') {
      await loadProducts();
    } else if (target.type === 'proveedor') {
      await loadProveedores();
      await searchProveedores(proveedorLookupSearch.value);
      if (form.proveedorId === target.id) {
        form.proveedorId = '';
        validateField('proveedorId');
      }
    } else if (target.type === 'categoria') {
      await loadCategorias();
      await searchCategorias('');
      await loadProducts();
      if (form.categoriaId === target.id) {
        form.categoriaId = '';
      }
    }

    flash('success', successMap[target.type]);
    closeDeleteDialog();
  } catch (err) {
    flash('error', err?.message || 'No se pudo eliminar el elemento.');
  } finally {
    deleteLoading.value = false;
  }
};

const syncTabFromRoute = (value) => {
  if (value === 'combos') {
    tab.value = 'combos';
    return;
  }
  if (value === 'proveedores') {
    tab.value = 'proveedores';
    return;
  }
  if (value === 'listas-precio') {
    tab.value = 'listas-precio';
    return;
  }
  tab.value = 'productos';
};

watch(
  [() => search.value, () => activoFilter.value],
  () => {
    if (tab.value !== 'productos') return;
    if (loadProductsDebounceId) {
      clearTimeout(loadProductsDebounceId);
    }
    loadProductsDebounceId = setTimeout(() => {
      loadProducts();
    }, 250);
  }
);

watch(
  [() => proveedorSearch.value, () => proveedorActivoFilter.value],
  () => {
    if (tab.value !== 'proveedores') return;
    if (loadProveedoresDebounceId) {
      clearTimeout(loadProveedoresDebounceId);
    }
    loadProveedoresDebounceId = setTimeout(() => {
      loadProveedores();
    }, 250);
  }
);

watch(
  [() => categoriaSearch.value, () => categoriaActivoFilter.value],
  () => {
    if (tab.value !== 'listas-precio') return;
    if (loadCategoriasDebounceId) {
      clearTimeout(loadCategoriasDebounceId);
    }
    loadCategoriasDebounceId = setTimeout(() => {
      loadCategorias();
    }, 250);
  }
);

watch(
  () => tab.value,
  (value) => {
    if (value === 'combos') {
      loadCombos();
    }
    const nextQuery = { ...route.query, tab: value };
    router.replace({ path: '/productos', query: nextQuery });
  }
);

watch(
  () => route.query.tab,
  (value) => {
    syncTabFromRoute(value);
  }
);

watch(
  () => form.pricingMode,
  (value) => {
    if (value === 'MANUAL' && !form.precioVentaManual) {
      form.precioVentaManual = precioVentaCalculado.value.toFixed(2);
    }
    if (value === 'CATEGORIA') {
      applyCategoriaMargin();
      ensureCategoriasLookup();
    }
    validateField('margenPct');
    validateField('precioVentaManual');
  }
);

watch(
  () => form.categoriaId,
  () => {
    applyCategoriaMargin();
  }
);

onMounted(() => {
  syncTabFromRoute(route.query.tab);
  loadProducts();
  loadCombos();
  loadProveedores();
  loadCategorias();
  searchProveedores('');
  searchCategorias('');
});
</script>

<style scoped>
.productos-page {
  animation: fade-in 0.3s ease;
}

.productos-window-item {
  height: 100%;
}

.productos-list-card {
  min-height: calc(100vh - 180px);
  display: flex;
  flex-direction: column;
}

.products-table-shell {
  flex: 1;
  min-height: 0;
  display: flex;
}

.products-table {
  flex: 1;
  min-height: 0;
}

.products-table :deep(.v-table) {
  height: 100%;
}

.products-table :deep(.v-table__wrapper) {
  flex: 1;
  min-height: 0;
  overflow: auto;
}

.products-table :deep(.v-data-table__wrapper) {
  flex: 1;
  min-height: 0;
}

.products-table :deep(.v-data-table-footer) {
  margin-top: auto;
}

.product-primary-action {
  min-width: 180px;
  min-height: 46px;
  font-weight: 800;
  box-shadow: var(--pos-primary-btn-shadow);
}

.product-print-action {
  min-height: 46px;
  padding-inline: 0;
  font-weight: 700;
}

.table-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
}

.truncate-cell {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.product-name-cell {
  max-width: 280px;
}

.product-sku-cell {
  max-width: 150px;
}

.proveedor-name-cell {
  max-width: 280px;
}

.proveedor-direccion-cell {
  max-width: 280px;
}

.categoria-name-cell {
  max-width: 280px;
}

.delete-row-action {
  opacity: 0.9;
}

.required-asterisk {
  color: var(--pos-error);
  font-weight: 800;
}

.product-section-title {
  margin: 16px 0 12px;
}

.product-final-price {
  display: flex;
  align-items: baseline;
  gap: 10px;
  padding: 12px 16px;
  margin: 0 0 0;
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

</style>

