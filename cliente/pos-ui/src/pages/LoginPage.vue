<template>
  <v-container class="fill-height">
    <v-row align="center" justify="center">
      <v-col cols="12" sm="8" md="4">
        <v-card class="pos-card pa-6">
          <div class="text-h5">Ingresar</div>
          <div class="text-caption text-medium-emphasis">Vinedos de la Villa</div>
          <div class="text-caption text-medium-emphasis mt-2">
            El acceso requiere una suscripcion activa validada en Firebase.
          </div>

          <v-alert
            v-if="displayMessage"
            :type="messageType"
            variant="tonal"
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
const displayMessage = computed(() => error.value || auth.accessMessage);
const messageType = computed(() => (error.value ? 'error' : 'warning'));

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

onMounted(() => {
  auth.loadLoginHints();
  email.value = auth.firebaseEmail || '';
  firebasePassword.value = '';
  focusUsername();
});
</script>
