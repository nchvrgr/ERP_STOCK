<template>
  <div class="login-page">
    <v-container class="fill-height login-shell">
      <v-row align="center" justify="center">
        <v-col cols="12" sm="8" md="4">
          <v-card class="pos-card pa-6">
            <div class="text-h5">Ingresar</div>
            <div class="text-caption text-medium-emphasis">Vinedos de la Villa</div>

            <v-alert
              v-if="displayMessage"
              :type="messageType"
              variant="outlined"
              class="mt-4"
              density="compact"
            >
              {{ displayMessage }}
            </v-alert>

            <v-form class="mt-4" @submit.prevent="submit">
              <v-text-field
                ref="usernameRef"
                v-model="email"
                label="Usuario"
                type="email"
                variant="outlined"
                density="comfortable"
                autocomplete="email"
                required
              />
              <v-text-field
                v-model="firebasePassword"
                label="Contrasena"
                type="password"
                variant="outlined"
                density="comfortable"
                autocomplete="current-password"
                required
                @keyup.enter="submit"
              />

              <v-btn
                type="submit"
                color="primary"
                size="large"
                class="mt-4 text-none"
                block
                :loading="loading"
              >
                Login
              </v-btn>
            </v-form>
          </v-card>
        </v-col>
      </v-row>
    </v-container>

    <v-sheet
      v-if="showUpdatePanel"
      class="login-update-panel"
      rounded="xl"
      border
    >
      <div class="text-caption login-update-label">Version actual de la app</div>
      <div class="text-subtitle-2 login-update-version">{{ appVersionLabel }}</div>

      <v-btn
        color="primary"
        variant="elevated"
        class="text-none mt-3 login-update-check-btn"
        block
        :loading="checkingUpdate"
        @click="checkForAppUpdate"
      >
        Buscar actualizaciones
      </v-btn>

      <v-alert
        v-if="updateNotice"
        :type="updateNotice.type"
        density="compact"
        variant="tonal"
        class="mt-3 login-update-alert"
      >
        <div>{{ updateNotice.message }}</div>

        <div
          v-if="updateNotice.status === 'available'"
          class="login-update-actions"
        >
          <v-btn
            color="primary"
            size="small"
            class="text-none"
            :loading="installingUpdate"
            @click="installAppUpdate"
          >
            {{ updateNotice.isMandatory ? 'Actualizar Obligatoriamente' : 'Actualizar Ahora' }}
          </v-btn>
          <v-btn
            v-if="!updateNotice.isMandatory"
            variant="text"
            size="small"
            class="text-none"
            :disabled="installingUpdate"
            @click="dismissUpdateNotice"
          >
            Mas tarde
          </v-btn>
        </div>
      </v-alert>
    </v-sheet>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';

const auth = useAuthStore();
const router = useRouter();
const route = useRoute();

const email = ref('');
const firebasePassword = ref('');
const error = ref('');
const loading = ref(false);
const usernameRef = ref(null);
const desktopBridge = typeof window !== 'undefined' ? window.desktopBridge || null : null;
const appVersion = ref('');
const checkingUpdate = ref(false);
const installingUpdate = ref(false);
const updateNotice = ref(null);
const displayMessage = computed(() => error.value || auth.accessMessage);
const messageType = computed(() => (error.value ? 'error' : 'warning'));
const showUpdatePanel = computed(() =>
  typeof desktopBridge?.checkForAppUpdate === 'function' &&
  typeof desktopBridge?.installAppUpdate === 'function' &&
  typeof desktopBridge?.getAppVersion === 'function'
);
const appVersionLabel = computed(() => (appVersion.value ? `v${appVersion.value}` : 'Version desconocida'));

const focusUsername = () => {
  const input = usernameRef.value?.$el?.querySelector('input');
  if (input) input.focus();
};

const submit = async () => {
  if (loading.value) return;
  error.value = '';
  loading.value = true;

  try {
    await auth.login({
      email: email.value,
      firebasePassword: firebasePassword.value
    });

    const redirect =
      typeof route.query.redirect === 'string' ? route.query.redirect : '/caja';
    router.replace(redirect);
  } catch (err) {
    error.value = err?.message || 'Error de login.';
    focusUsername();
  } finally {
    loading.value = false;
  }
};

const dismissUpdateNotice = () => {
  updateNotice.value = null;
};

const checkForAppUpdate = async () => {
  if (!showUpdatePanel.value || checkingUpdate.value) return;

  checkingUpdate.value = true;
  updateNotice.value = null;

  try {
    const result = await desktopBridge.checkForAppUpdate();

    if (result?.status === 'available') {
      updateNotice.value = {
        status: 'available',
        isMandatory: Boolean(result.isMandatory),
        type: result.isMandatory ? 'warning' : 'info',
        message: result.isMandatory
          ? 'Esta actualizacion es obligatoria para seguir usando ERP_STOCK. Por favor, actualiza ahora.'
          : `Se encontro una nueva actualizacion: v${result.latestVersion}.`
      };
      return;
    }

    if (result?.status === 'up-to-date') {
      updateNotice.value = {
        status: 'up-to-date',
        type: 'success',
        message: result.message || 'No se encontraron nuevas actualizaciones.'
      };
      return;
    }

    updateNotice.value = {
      status: 'error',
      type: 'error',
      message: result?.message || 'No se pudo verificar la actualizacion.'
    };
  } catch (err) {
    updateNotice.value = {
      status: 'error',
      type: 'error',
      message: err?.message || 'No se pudo verificar la actualizacion.'
    };
  } finally {
    checkingUpdate.value = false;
  }
};

const installAppUpdate = async () => {
  if (!showUpdatePanel.value || installingUpdate.value) return;

  installingUpdate.value = true;

  try {
    const result = await desktopBridge.installAppUpdate();

    if (result?.status === 'installing') {
      updateNotice.value = {
        status: 'installing',
        type: 'success',
        message: result.message || `Instalador ejecutado para instalar la version ${result?.latestVersion || ''}.`
      };
      return;
    }

    if (result?.status === 'cancelled') {
      updateNotice.value = {
        status: 'available',
        isMandatory: Boolean(result?.isMandatory),
        type: 'info',
        message: result?.isMandatory
          ? 'Esta actualizacion es obligatoria para seguir usando ERP_STOCK. Por favor, actualiza ahora.'
          : `Se encontro una nueva actualizacion: v${result.latestVersion}.`
      };
      return;
    }

    if (result?.status === 'up-to-date') {
      updateNotice.value = {
        status: 'up-to-date',
        type: 'success',
        message: result.message || 'No se encontraron nuevas actualizaciones.'
      };
      return;
    }

    updateNotice.value = {
      status: 'error',
      type: 'error',
      message: result?.message || 'No se pudo iniciar la actualizacion.'
    };
  } catch (err) {
    updateNotice.value = {
      status: 'error',
      type: 'error',
      message: err?.message || 'No se pudo iniciar la actualizacion.'
    };
  } finally {
    installingUpdate.value = false;
  }
};

onMounted(() => {
  auth.loadLoginHints();
  email.value = auth.firebaseEmail || '';
  firebasePassword.value = '';
  focusUsername();

  if (showUpdatePanel.value) {
    desktopBridge.getAppVersion()
      .then((value) => {
        appVersion.value = typeof value === 'string' ? value : '';
      })
      .catch(() => {
        appVersion.value = '';
      });
  }
});
</script>

<style scoped>
.login-page {
  position: relative;
  min-height: 100vh;
  padding-bottom: 132px;
}

.login-shell {
  min-height: 100vh;
}

.login-update-panel {
  position: absolute;
  right: 24px;
  bottom: 24px;
  width: min(320px, calc(100vw - 32px));
  padding: 16px;
  color: var(--pos-ink);
  border: 1px solid var(--pos-border) !important;
  background:
    linear-gradient(180deg, var(--pos-card-top) 0%, var(--pos-card-bottom) 100%);
  box-shadow: var(--pos-shadow);
}

.login-update-label {
  color: var(--pos-ink-muted);
}

.login-update-version {
  color: var(--pos-accent-dark);
  font-weight: 700;
}

.login-update-check-btn {
  color: var(--pos-primary-btn-text) !important;
  background: var(--pos-primary-btn-bg) !important;
  box-shadow: var(--pos-primary-btn-shadow) !important;
}

.login-update-alert {
  border-radius: 14px;
}

.login-update-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 10px;
}

@media (max-width: 640px) {
  .login-page {
    padding-bottom: 200px;
  }

  .login-update-panel {
    left: 16px;
    right: 16px;
    bottom: 16px;
    width: auto;
  }
}
</style>
