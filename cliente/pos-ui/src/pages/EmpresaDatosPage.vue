<template>
  <div class="empresa-page">
    <v-card class="pos-card pa-4">
      <div class="d-flex align-center justify-space-between">
        <div>
          <div class="text-h6">Datos de mi empresa</div>
          <div class="text-caption text-medium-emphasis">Datos del negocio que usa esta aplicacion</div>
        </div>
        <v-btn color="primary" class="text-none" :loading="saving" @click="save">
          Guardar
        </v-btn>
      </div>

      <v-form class="mt-4">
        <v-row dense>
          <v-col cols="12" md="6">
            <v-text-field
              v-model="form.razonSocial"
              label="Razon social"
              variant="outlined"
              density="comfortable"
              :error-messages="errors.razonSocial"
              @blur="validate('razonSocial')"
              required
            />
          </v-col>
          <v-col cols="12" md="6">
            <v-text-field v-model="form.cuit" label="CUIT" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12" md="6">
            <v-text-field v-model="form.telefono" label="Telefono" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12" md="6">
            <v-text-field v-model="form.email" label="Email" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12">
            <v-text-field v-model="form.direccion" label="Direccion" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12">
            <v-text-field v-model="form.web" label="Web" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12">
            <v-textarea
              v-model="form.observaciones"
              label="Otros datos"
              variant="outlined"
              density="comfortable"
              rows="3"
            />
          </v-col>
        </v-row>
      </v-form>

      <v-divider class="my-4" />

      <div>
        <div class="mb-2">
          <div class="text-subtitle-1 section-title">Configuracion</div>
          <div class="text-caption text-medium-emphasis">
            Define el medio de pago habitual y administra los medios disponibles para cobrar.
          </div>
        </div>
        <div v-if="configDirty" class="text-caption text-warning mb-3">
          Hay cambios en configuracion sin guardar.
        </div>

        <v-row dense>
          <v-col cols="12" md="6">
            <v-select
              v-model="form.medioPagoHabitual"
              :items="mediosPagoOptions"
              item-title="title"
              item-value="value"
              label="Medio de pago habitual"
              variant="outlined"
              density="comfortable"
              :error-messages="errors.medioPagoHabitual"
              @blur="validate('medioPagoHabitual')"
              @update:model-value="onMedioPagoHabitualChanged"
            />
          </v-col>
          <v-col cols="12" md="6">
            <div class="d-flex align-center gap-2">
              <v-text-field
                v-model="nuevoMedioPago"
                label="Nuevo medio de pago"
                variant="outlined"
                density="comfortable"
                maxlength="40"
                counter="40"
                @keyup.enter="addMedioPago"
              />
              <v-btn color="primary" class="text-none" @click="addMedioPago">Agregar</v-btn>
            </div>
          </v-col>
          <v-col cols="12">
            <div class="text-caption text-medium-emphasis mb-2">Medios disponibles</div>
            <div class="d-flex flex-wrap gap-2">
              <v-chip
                v-for="medio in form.mediosPago"
                :key="medio"
                size="small"
                color="primary"
                variant="tonal"
                :closable="medio !== 'EFECTIVO'"
                @click:close="removeMedioPago(medio)"
              >
                {{ medio }}
              </v-chip>
            </div>
          </v-col>
        </v-row>

        <v-divider class="my-4" />

        <div class="mb-2">
          <div class="text-subtitle-2 section-subtitle">Actualizar app</div>
          <div class="text-caption text-medium-emphasis">
            Busca e instala la ultima version disponible de la aplicacion.
          </div>
        </div>
        <div class="d-flex align-center gap-2">
          <v-btn
            color="primary"
            variant="tonal"
            class="text-none"
            :loading="checkingUpdate"
            :disabled="!canCheckUpdates"
            @click="checkForAppUpdate"
          >
            Actualizar app
          </v-btn>
        </div>
      </div>
    </v-card>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" location="top end" timeout="1600">
      <div class="d-flex align-center gap-2">
        <v-icon>{{ snackbar.icon }}</v-icon>
        <span>{{ snackbar.text }}</span>
      </div>
    </v-snackbar>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue';
import { getJson, requestJson } from '../services/apiClient';

const saving = ref(false);
const checkingUpdate = ref(false);
const loading = ref(false);
const configDirty = ref(false);
const nuevoMedioPago = ref('');
const desktopBridge = typeof window !== 'undefined' ? window.desktopBridge || null : null;
const form = reactive({
  razonSocial: '',
  cuit: '',
  telefono: '',
  direccion: '',
  email: '',
  web: '',
  observaciones: '',
  medioPagoHabitual: 'EFECTIVO',
  mediosPago: ['EFECTIVO', 'TARJETA', 'TRANSFERENCIA', 'APLICATIVO', 'OTRO']
});

const errors = reactive({
  razonSocial: '',
  medioPagoHabitual: ''
});

const snackbar = ref({
  show: false,
  text: '',
  color: 'success',
  icon: 'mdi-check-circle'
});

const knownLabels = {
  EFECTIVO: 'Efectivo',
  TARJETA: 'Tarjeta',
  TRANSFERENCIA: 'Transferencia',
  APLICATIVO: 'Aplicativo',
  OTRO: 'Otro'
};

const mediosPagoOptions = computed(() =>
  form.mediosPago.map((medio) => ({
    title: knownLabels[medio] || medio,
    value: medio
  }))
);

const canCheckUpdates = computed(() => typeof desktopBridge?.checkForAppUpdate === 'function');

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
    if (firstKey && data.errors[firstKey]?.length) return `${firstKey}: ${data.errors[firstKey][0]}`;
  }
  return data.detail || data.title || 'Error inesperado.';
};

const normalizeMedioPago = (value) => (value || '')
  .trim()
  .toUpperCase()
  .replace(/\s+/g, '_');

const normalizeMediosPago = (values) => {
  const normalized = (values || [])
    .map((item) => normalizeMedioPago(item))
    .filter(Boolean)
    .filter((value, index, arr) => arr.indexOf(value) === index)
    .slice(0, 20);

  if (!normalized.includes('EFECTIVO')) {
    normalized.unshift('EFECTIVO');
  }

  return normalized;
};

const addMedioPago = () => {
  const newValue = normalizeMedioPago(nuevoMedioPago.value);
  if (!newValue) return;

  if (!form.mediosPago.includes(newValue)) {
    form.mediosPago = normalizeMediosPago([...form.mediosPago, newValue]);
  }

  nuevoMedioPago.value = '';
  configDirty.value = true;
};

const removeMedioPago = (medio) => {
  if (medio === 'EFECTIVO') return;

  form.mediosPago = normalizeMediosPago(form.mediosPago.filter((item) => item !== medio));
  if (!form.mediosPago.includes(form.medioPagoHabitual)) {
    form.medioPagoHabitual = 'EFECTIVO';
  }
  configDirty.value = true;
};

const onMedioPagoHabitualChanged = async () => {
  if (loading.value) return;
  configDirty.value = true;
  validate('medioPagoHabitual');
  if (errors.medioPagoHabitual) return;
  await save();
};

const validate = (field) => {
  if (field === 'razonSocial') {
    errors.razonSocial = form.razonSocial.trim() ? '' : 'La razon social es obligatoria.';
  }
  if (field === 'medioPagoHabitual') {
    errors.medioPagoHabitual = form.mediosPago.includes(form.medioPagoHabitual)
      ? ''
      : 'Selecciona un medio valido.';
  }
};

const load = async () => {
  loading.value = true;
  try {
    const { response, data } = await getJson('/api/v1/empresa/datos');
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    form.razonSocial = data?.razonSocial || '';
    form.cuit = data?.cuit || '';
    form.telefono = data?.telefono || '';
    form.direccion = data?.direccion || '';
    form.email = data?.email || '';
    form.web = data?.web || '';
    form.observaciones = data?.observaciones || '';
    form.mediosPago = normalizeMediosPago(data?.mediosPago || []);
    form.medioPagoHabitual = normalizeMedioPago(data?.medioPagoHabitual || 'EFECTIVO');

    if (!form.mediosPago.includes(form.medioPagoHabitual)) {
      form.medioPagoHabitual = 'EFECTIVO';
    }
    configDirty.value = false;
  } catch (err) {
    flash('error', err?.message || 'No se pudieron cargar los datos.');
  } finally {
    loading.value = false;
  }
};

const save = async () => {
  if (saving.value) return;
  validate('razonSocial');
  validate('medioPagoHabitual');
  if (errors.razonSocial || errors.medioPagoHabitual) return;

  saving.value = true;
  try {
    const payload = {
      razonSocial: form.razonSocial.trim(),
      cuit: form.cuit.trim() || null,
      telefono: form.telefono.trim() || null,
      direccion: form.direccion.trim() || null,
      email: form.email.trim() || null,
      web: form.web.trim() || null,
      observaciones: form.observaciones.trim() || null,
      medioPagoHabitual: form.medioPagoHabitual,
      mediosPago: normalizeMediosPago(form.mediosPago)
    };

    const { response, data } = await requestJson('/api/v1/empresa/datos', {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
    if (!response.ok) {
      throw new Error(extractProblemMessage(data));
    }

    form.mediosPago = normalizeMediosPago(form.mediosPago);
    form.medioPagoHabitual = normalizeMedioPago(form.medioPagoHabitual || 'EFECTIVO');
    configDirty.value = false;
    flash('success', 'Datos guardados');
  } catch (err) {
    flash('error', err?.message || 'No se pudieron guardar los datos.');
  } finally {
    saving.value = false;
  }
};

const checkForAppUpdate = async () => {
  if (!canCheckUpdates.value || checkingUpdate.value) {
    return;
  }

  checkingUpdate.value = true;

  try {
    const result = await desktopBridge.checkForAppUpdate();

    if (result?.status === 'up-to-date') {
      flash('success', result.message || 'La app ya esta actualizada.');
      return;
    }

    if (result?.status === 'installing') {
      flash('success', result.message || 'Se inicio la actualizacion.');
      return;
    }

    flash('error', result?.message || 'No se pudo verificar la actualizacion.');
  } catch (err) {
    flash('error', err?.message || 'No se pudo verificar la actualizacion.');
  } finally {
    checkingUpdate.value = false;
  }
};

onMounted(() => {
  load();
});
</script>

<style scoped>
.empresa-page {
  animation: fade-in 0.3s ease;
}

.section-title {
  font-weight: 700;
}

.section-subtitle {
  font-weight: 700;
}
</style>
