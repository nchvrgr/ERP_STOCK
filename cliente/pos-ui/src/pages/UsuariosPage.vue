<template>
  <div class="usuarios-page">
    <v-card v-if="!canManageUsers" class="pos-card pa-4">
      <div class="text-h6">Administrador</div>
      <div class="text-caption text-medium-emphasis">No tenes permisos para administrar la cuenta de administrador.</div>
    </v-card>

    <template v-else>
      <v-card class="pos-card pa-4 admin-card mb-4">
        <div class="text-h6">Contraseña de administrador</div>
        <div class="text-caption text-medium-emphasis">
          Cambiá la contraseña del acceso administrador. Se requiere la contraseña actual.
        </div>

        <v-row dense class="mt-2">
          <v-col cols="12" md="6">
            <v-text-field
              v-model="form.currentPassword"
              label="Contraseña actual"
              type="password"
              variant="outlined"
              density="comfortable"
              :error-messages="formErrors.currentPassword"
            />
          </v-col>
          <v-col cols="12" md="6">
            <v-text-field
              v-model="form.newPassword"
              label="Nueva contraseña"
              type="password"
              variant="outlined"
              density="comfortable"
              :error-messages="formErrors.newPassword"
              hint="Mínimo 4 caracteres."
              persistent-hint
            />
          </v-col>
          <v-col cols="12" md="6">
            <v-text-field
              v-model="form.confirmPassword"
              label="Repetir nueva contraseña"
              type="password"
              variant="outlined"
              density="comfortable"
              :error-messages="formErrors.confirmPassword"
              @keyup.enter="savePassword"
            />
          </v-col>
        </v-row>

        <div class="d-flex justify-end mt-4">
          <v-btn color="primary" class="text-none" :loading="saving" @click="savePassword">
            Cambiar contraseña
          </v-btn>
        </div>
      </v-card>

      <v-card class="pos-card pa-4 facturas-card">
        <div class="d-flex align-center justify-space-between gap-2">
          <div>
            <div class="text-h6">Facturas a realizar</div>
            <div class="text-caption text-medium-emphasis">
              Ventas Factura A pendientes de facturación administrativa.
            </div>
          </div>
          <v-btn
            color="primary"
            variant="tonal"
            class="text-none"
            :loading="facturasLoading"
            @click="loadFacturasPendientes"
          >
            Actualizar
          </v-btn>
        </div>

        <v-data-table
          class="mt-3"
          :headers="facturasHeaders"
          :items="facturasPendientes"
          :loading="facturasLoading"
          item-key="movimientoId"
          density="compact"
        >
          <template v-slot:[`item.fecha`]="{ item }">
            {{ formatDate(item.fecha) }}
          </template>
          <template v-slot:[`item.ventaNumero`]="{ item }">
            {{ item.ventaNumero || '-' }}
          </template>
          <template v-slot:[`item.tipoFactura`]="{ item }">
            {{ item.tipoFactura || '-' }}
          </template>
          <template v-slot:[`item.clienteNombre`]="{ item }">
            {{ item.clienteNombre || '-' }}
          </template>
          <template v-slot:[`item.clienteCuit`]="{ item }">
            {{ item.clienteCuit || '-' }}
          </template>
          <template v-slot:[`item.montoTotal`]="{ item }">
            {{ formatMoney(item.montoTotal || 0) }}
          </template>
          <template v-slot:[`item.actions`]="{ item }">
            <v-btn
              size="small"
              variant="tonal"
              color="success"
              class="text-none"
              :loading="resolvingMovimientoId === item.movimientoId"
              @click="resolverFactura(item)"
            >
              Ya facturada
            </v-btn>
          </template>
        </v-data-table>
      </v-card>
    </template>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" location="top end" timeout="2200">
      <div class="d-flex align-center gap-2">
        <v-icon>{{ snackbar.icon }}</v-icon>
        <span>{{ snackbar.text }}</span>
      </div>
    </v-snackbar>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue';
import { useAuthStore } from '../stores/auth';
import { getJson, requestJson } from '../services/apiClient';
import { formatMoney } from '../utils/currency';

const auth = useAuthStore();
const canManageUsers = computed(() => auth.erpUsername === 'admin' || auth.roles.includes('ADMIN') || auth.hasPermission('PERM_USUARIO_ADMIN'));
const saving = ref(false);
const facturasLoading = ref(false);
const resolvingMovimientoId = ref('');
const facturasPendientes = ref([]);

const facturasHeaders = [
  { title: 'Fecha', value: 'fecha' },
  { title: 'Venta N°', value: 'ventaNumero', align: 'end' },
  { title: 'Factura', value: 'tipoFactura' },
  { title: 'Cliente', value: 'clienteNombre' },
  { title: 'CUIT', value: 'clienteCuit' },
  { title: 'Total', value: 'montoTotal', align: 'end' },
  { title: '', value: 'actions', sortable: false, align: 'end' }
];

const snackbar = ref({
  show: false,
  text: '',
  color: 'success',
  icon: 'mdi-check-circle'
});

const form = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
});

const formErrors = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
});

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

const resetForm = () => {
  form.currentPassword = '';
  form.newPassword = '';
  form.confirmPassword = '';
  formErrors.currentPassword = '';
  formErrors.newPassword = '';
  formErrors.confirmPassword = '';
};

const formatDate = (value) => {
  if (!value) return '-';
  try {
    return new Date(value).toLocaleString('es-AR');
  } catch {
    return value;
  }
};

const loadFacturasPendientes = async () => {
  if (!canManageUsers.value) return;

  facturasLoading.value = true;
  try {
    const { response, data } = await getJson('/api/v1/stock/facturas-pendientes');
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }
    facturasPendientes.value = Array.isArray(data) ? data : [];
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar las facturas pendientes.');
  } finally {
    facturasLoading.value = false;
  }
};

const resolverFactura = async (item) => {
  const movimientoId = item?.movimientoId;
  if (!movimientoId || resolvingMovimientoId.value) return;

  resolvingMovimientoId.value = movimientoId;
  try {
    const { response, data } = await requestJson(`/api/v1/stock/facturas-pendientes/${movimientoId}`, {
      method: 'DELETE'
    });
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    facturasPendientes.value = facturasPendientes.value.filter((x) => x.movimientoId !== movimientoId);
    flash('success', 'Factura pendiente marcada como realizada.');
  } catch (err) {
    flash('error', err?.message || 'No se pudo actualizar el estado de la factura.');
  } finally {
    resolvingMovimientoId.value = '';
  }
};

const validateForm = () => {
  formErrors.currentPassword = form.currentPassword.trim() ? '' : 'Ingresá la contraseña actual.';
  formErrors.newPassword = form.newPassword.trim().length >= 4 ? '' : 'La nueva contraseña debe tener al menos 4 caracteres.';
  formErrors.confirmPassword = form.confirmPassword.trim() ? '' : 'Repetí la nueva contraseña.';

  if (!formErrors.confirmPassword && form.newPassword !== form.confirmPassword) {
    formErrors.confirmPassword = 'Las nuevas contraseñas no coinciden.';
  }

  return !formErrors.currentPassword && !formErrors.newPassword && !formErrors.confirmPassword;
};

const savePassword = async () => {
  if (saving.value || !validateForm()) return;

  saving.value = true;
  try {
    const { response, data } = await requestJson('/api/v1/users/admin/password', {
      method: 'POST',
      body: JSON.stringify({
        currentPassword: form.currentPassword.trim(),
        newPassword: form.newPassword.trim()
      })
    });

    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    flash('success', 'Contraseña de administrador actualizada.');
    resetForm();
  } catch (err) {
    flash('error', err?.message || 'No se pudo actualizar la contraseña.');
  } finally {
    saving.value = false;
  }
};

onMounted(() => {
  loadFacturasPendientes();
});
</script>

<style scoped>
.usuarios-page {
  animation: fade-in 0.3s ease;
}

.admin-card {
  max-width: 780px;
}

.facturas-card {
  width: 100%;
}
</style>
