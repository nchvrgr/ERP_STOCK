import { defineStore } from 'pinia';
import { postJson } from '../services/apiClient';
import {
  doc,
  ensureFirebasePersistence,
  firebaseAuth,
  firestore,
  getDoc,
  onAuthStateChanged,
  onSnapshot,
  signInWithEmailAndPassword,
  signOut
} from '../services/firebase';

const STORAGE_KEY = 'pos-auth';
const LOGIN_HINTS_KEY = 'pos-login-hints';
const ACTIVE_SUBSCRIPTION_STATUS = 'active';

let firebaseBootstrapPromise = null;
let stopFirebaseAuthListener = null;
let stopSubscriptionListener = null;
let cashierSessionRefreshPromise = null;

const normalizeArray = (value) => {
  if (!value) return [];
  if (Array.isArray(value)) return value;
  if (typeof value === 'string') return [value];
  return [];
};

const parseJwt = (token) => {
  try {
    const parts = token.split('.');
    if (parts.length < 2) return {};
    const payload = parts[1];
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const binary = atob(base64);
    const json = decodeURIComponent(
      binary
        .split('')
        .map((char) => `%${char.charCodeAt(0).toString(16).padStart(2, '0')}`)
        .join('')
    );
    return JSON.parse(json);
  } catch {
    return {};
  }
};

const readExpiry = (token, fallback) => {
  if (fallback) return fallback;
  const payload = parseJwt(token);
  if (payload && payload.exp) {
    const ms = Number(payload.exp) * 1000;
    if (!Number.isNaN(ms)) {
      return new Date(ms).toISOString();
    }
  }
  return '';
};

const normalizeSubscriptionStatus = (value) => {
  return typeof value === 'string' && value.trim().toLowerCase() === ACTIVE_SUBSCRIPTION_STATUS
    ? ACTIVE_SUBSCRIPTION_STATUS
    : 'inactive';
};

const buildInactiveSubscriptionMessage = () => {
  return '';
};

const buildMissingSubscriptionMessage = () => {
  return '';
};

const buildSubscriptionConnectivityMessage = () => {
  return '';
};

const toFirebaseErrorMessage = (error) => {
  const code = error?.code || '';

  if (code === 'auth/invalid-credential' || code === 'auth/wrong-password') {
    return 'Email o contraseña de suscripción inválidos.';
  }

  if (code === 'auth/user-not-found') {
    return 'No existe un usuario de suscripción con ese email.';
  }

  if (code === 'auth/invalid-email') {
    return 'El email de suscripción no es válido.';
  }

  if (code === 'auth/too-many-requests') {
    return 'Demasiados intentos fallidos. Espera un momento e intenta de nuevo.';
  }

  return 'No se pudo validar la suscripción en Firebase.';
};

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: '',
    expiresAt: '',
    tenantId: '',
    sucursalId: '',
    userId: '',
    firebaseUid: '',
    firebaseEmail: '',
    erpUsername: '',
    subscriptionStatus: '',
    subscriptionChecked: false,
    accessMessage: '',
    roles: [],
    permissions: []
  }),
  getters: {
    isAuthenticated: (state) => {
      if (!state.token) return false;
      if (!state.expiresAt) return true;
      return new Date(state.expiresAt).getTime() > Date.now();
    },
    hasActiveSubscription: (state) => state.subscriptionStatus === ACTIVE_SUBSCRIPTION_STATUS,
    hasAppAccess() {
      return this.isAuthenticated && this.hasActiveSubscription;
    }
  },
  actions: {
    hasPermission(permission) {
      if (this.roles.includes('ADMIN')) {
        return true;
      }
      if (!permission) return false;
      const normalized = permission.startsWith('PERM_')
        ? permission.slice(5)
        : permission;
      return this.permissions.includes(normalized) || this.permissions.includes(permission);
    },
    loadFromStorage() {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return;
      try {
        const data = JSON.parse(raw);
        this.token = data.token || '';
        this.expiresAt = data.expiresAt || '';
        this.tenantId = data.tenantId || '';
        this.sucursalId = data.sucursalId || '';
        this.userId = data.userId || '';
        this.firebaseUid = data.firebaseUid || '';
        this.firebaseEmail = data.firebaseEmail || '';
        this.erpUsername = data.erpUsername || '';
        this.subscriptionStatus = data.subscriptionStatus || '';
        this.subscriptionChecked = Boolean(data.subscriptionChecked);
        this.accessMessage = data.accessMessage || '';
        this.roles = Array.isArray(data.roles) ? data.roles : [];
        this.permissions = Array.isArray(data.permissions) ? data.permissions : [];
        if (this.expiresAt && new Date(this.expiresAt).getTime() <= Date.now()) {
          this.clearBackendSession();
        }
      } catch {
        this.clearSession();
      }
    },
    saveToStorage() {
      const payload = {
        token: this.token,
        expiresAt: this.expiresAt,
        tenantId: this.tenantId,
        sucursalId: this.sucursalId,
        userId: this.userId,
        firebaseUid: this.firebaseUid,
        firebaseEmail: this.firebaseEmail,
        erpUsername: this.erpUsername,
        subscriptionStatus: this.subscriptionStatus,
        subscriptionChecked: this.subscriptionChecked,
        accessMessage: this.accessMessage,
        roles: this.roles,
        permissions: this.permissions
      };
      localStorage.setItem(STORAGE_KEY, JSON.stringify(payload));
    },
    clearBackendSession() {
      this.token = '';
      this.expiresAt = '';
      this.tenantId = '';
      this.sucursalId = '';
      this.userId = '';
      this.roles = [];
      this.permissions = [];
      this.saveToStorage();
    },
    clearSubscriptionState() {
      this.firebaseUid = '';
      this.firebaseEmail = '';
      this.erpUsername = '';
      this.subscriptionStatus = '';
      this.subscriptionChecked = false;
      this.accessMessage = '';
      if (stopSubscriptionListener) {
        stopSubscriptionListener();
        stopSubscriptionListener = null;
      }
      this.saveToStorage();
    },
    clearSession() {
      this.token = '';
      this.expiresAt = '';
      this.tenantId = '';
      this.sucursalId = '';
      this.userId = '';
      this.firebaseUid = '';
      this.firebaseEmail = '';
      this.erpUsername = '';
      this.subscriptionStatus = '';
      this.subscriptionChecked = false;
      this.accessMessage = '';
      this.roles = [];
      this.permissions = [];
      if (stopSubscriptionListener) {
        stopSubscriptionListener();
        stopSubscriptionListener = null;
      }
      localStorage.removeItem(STORAGE_KEY);
    },
    setAccessMessage(message) {
      this.accessMessage = message || '';
      this.saveToStorage();
    },
    loadLoginHints() {
      const raw = localStorage.getItem(LOGIN_HINTS_KEY);
      if (!raw) return;
      try {
        const data = JSON.parse(raw);
        this.firebaseEmail = data.firebaseEmail || this.firebaseEmail;
      } catch {
        localStorage.removeItem(LOGIN_HINTS_KEY);
      }
    },
    saveLoginHints({ firebaseEmail }) {
      if (firebaseEmail) {
        this.firebaseEmail = firebaseEmail.trim();
      }

      localStorage.setItem(
        LOGIN_HINTS_KEY,
        JSON.stringify({
          firebaseEmail: this.firebaseEmail
        })
      );
      this.saveToStorage();
    },
    applySubscriptionStatus(status, message = '') {
      this.subscriptionStatus = normalizeSubscriptionStatus(status);
      this.subscriptionChecked = true;
      this.accessMessage = this.subscriptionStatus === ACTIVE_SUBSCRIPTION_STATUS ? '' : message;

      if (this.subscriptionStatus !== ACTIVE_SUBSCRIPTION_STATUS) {
        this.clearBackendSession();
      } else {
        this.saveToStorage();
      }
    },
    async refreshSubscriptionStatus(uid) {
      const subscriptionRef = doc(firestore, 'users', uid);
      const snapshot = await getDoc(subscriptionRef);

      if (!snapshot.exists()) {
        this.applySubscriptionStatus('inactive', buildMissingSubscriptionMessage());
        return this.subscriptionStatus;
      }

      const data = snapshot.data() || {};
      const status = normalizeSubscriptionStatus(data.subscriptionStatus);
      const message = status === ACTIVE_SUBSCRIPTION_STATUS ? '' : buildInactiveSubscriptionMessage();

      this.saveLoginHints({ firebaseEmail: this.firebaseEmail });

      this.applySubscriptionStatus(status, message);
      return status;
    },
    startSubscriptionListener(uid) {
      if (stopSubscriptionListener) {
        stopSubscriptionListener();
      }

      stopSubscriptionListener = onSnapshot(
        doc(firestore, 'users', uid),
        (snapshot) => {
          if (!snapshot.exists()) {
            this.applySubscriptionStatus('inactive', buildMissingSubscriptionMessage());
            return;
          }

          const data = snapshot.data() || {};
          const status = normalizeSubscriptionStatus(data.subscriptionStatus);
          const message = status === ACTIVE_SUBSCRIPTION_STATUS ? '' : buildInactiveSubscriptionMessage();

          this.saveLoginHints({ firebaseEmail: this.firebaseEmail });

          this.applySubscriptionStatus(status, message);
        },
        () => {
          this.applySubscriptionStatus('inactive', buildSubscriptionConnectivityMessage());
        }
      );
    },
    async signInWithFirebase(email, password) {
      try {
        await ensureFirebasePersistence();
        const credential = await signInWithEmailAndPassword(firebaseAuth, email.trim(), password);

        this.firebaseUid = credential.user.uid;
        this.firebaseEmail = credential.user.email || email.trim();
        this.saveLoginHints({ firebaseEmail: this.firebaseEmail });

        const status = await this.refreshSubscriptionStatus(credential.user.uid);
        this.startSubscriptionListener(credential.user.uid);
        this.saveToStorage();

        if (status !== ACTIVE_SUBSCRIPTION_STATUS) {
          throw new Error(this.accessMessage || buildInactiveSubscriptionMessage());
        }
      } catch (error) {
        if (!this.accessMessage) {
          this.setAccessMessage(toFirebaseErrorMessage(error));
        }
        throw new Error(this.accessMessage);
      }
    },
    async refreshCashierBackendSession() {
      if (!this.firebaseEmail) {
        return;
      }

      if (!cashierSessionRefreshPromise) {
        cashierSessionRefreshPromise = (async () => {
          const request = {
            firebaseEmail: this.firebaseEmail.trim(),
            enterAsAdmin: false,
            erpPassword: null,
            tenantId: null,
            sucursalId: null
          };

          const { response, data } = await postJson('/api/v1/auth/login', request);
          if (!response.ok || !data?.token) {
            this.clearBackendSession();
            throw new Error(data?.detail || data?.title || 'No se pudo renovar la sesion de cajero.');
          }

          this.erpUsername = 'cajero';
          this.setSession(data.token, data.expiresAt || '');
        })();
      }

      try {
        await cashierSessionRefreshPromise;
      } finally {
        cashierSessionRefreshPromise = null;
      }
    },
    async initializeFirebaseSession() {
      if (!firebaseBootstrapPromise) {
        firebaseBootstrapPromise = (async () => {
          await ensureFirebasePersistence();

          if (!stopFirebaseAuthListener) {
            stopFirebaseAuthListener = onAuthStateChanged(firebaseAuth, async (user) => {
              if (!user) {
                this.clearSubscriptionState();
                this.clearBackendSession();
                return;
              }

              this.firebaseUid = user.uid;
              this.firebaseEmail = user.email || '';

              try {
                await this.refreshSubscriptionStatus(user.uid);
                this.startSubscriptionListener(user.uid);
                if (this.subscriptionStatus === ACTIVE_SUBSCRIPTION_STATUS && this.erpUsername === 'cajero') {
                  await this.refreshCashierBackendSession();
                }
              } catch {
                this.applySubscriptionStatus('inactive', buildSubscriptionConnectivityMessage());
              }

              this.saveToStorage();
            });
          }

          await new Promise((resolve) => {
            const unsubscribe = onAuthStateChanged(firebaseAuth, () => {
              unsubscribe();
              resolve();
            });
          });
        })();
      }

      if (firebaseAuth.currentUser && this.subscriptionStatus === ACTIVE_SUBSCRIPTION_STATUS && this.erpUsername === 'cajero') {
        await this.refreshCashierBackendSession();
      }

      return firebaseBootstrapPromise;
    },
    setSession(token, expiresAt) {
      this.token = token;
      this.expiresAt = readExpiry(token, expiresAt);

      const payload = parseJwt(token);
      this.tenantId = payload.tenant_id || '';
      this.sucursalId = payload.sucursal_id || '';
      this.userId = payload.user_id || '';
      this.roles = normalizeArray(payload.roles);
      this.permissions = normalizeArray(payload.permissions);

      this.saveToStorage();
    },
    async login({ email, firebasePassword, loginAsAdmin, erpPassword }) {
      this.setAccessMessage('');
      this.saveLoginHints({ firebaseEmail: email || '' });
      this.erpUsername = loginAsAdmin ? 'admin' : 'cajero';
      this.saveToStorage();
      try {
        await this.signInWithFirebase(email || '', firebasePassword || '');
      } catch (error) {
        this.erpUsername = '';
        this.saveToStorage();
        throw error;
      }

      const request = {
        firebaseEmail: (this.firebaseEmail || email || '').trim(),
        enterAsAdmin: Boolean(loginAsAdmin),
        erpPassword: loginAsAdmin ? (erpPassword || null) : null,
        tenantId: null,
        sucursalId: null
      };

      const { response, data } = await postJson('/api/v1/auth/login', request);

      if (!response.ok) {
        if (response.status === 401) {
          await this.logout();
          throw new Error('Credenciales invalidas.');
        }
        await this.logout();
        const message = data?.detail || data?.title || 'Error de login.';
        throw new Error(message);
      }

      if (!data || !data.token) {
        await this.logout();
        throw new Error('Respuesta de login invalida.');
      }

      this.setSession(data.token, data.expiresAt || '');
    },
    async logout() {
      if (stopSubscriptionListener) {
        stopSubscriptionListener();
        stopSubscriptionListener = null;
      }

      try {
        await signOut(firebaseAuth);
      } catch {
        // No bloquea el logout local.
      }

      this.clearSession();
    }
  }
});
