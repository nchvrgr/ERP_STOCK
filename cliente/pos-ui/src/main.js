import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import { createVuetify } from 'vuetify';
import * as components from 'vuetify/components';
import * as directives from 'vuetify/directives';
import { aliases, mdi } from 'vuetify/iconsets/mdi-svg';
import { h } from 'vue';
import {
  mdiAccount,
  mdiAlertCircle,
  mdiBarcodeScan,
  mdiCashMultiple,
  mdiCashRegister,
  mdiChartAreaspline,
  mdiCheckCircle,
  mdiClose,
  mdiDelete,
  mdiDeleteOutline,
  mdiDomain,
  mdiFileDocumentOutline,
  mdiMagnify,
  mdiMenu,
  mdiPackageVariant,
  mdiQrcodeScan,
  mdiStorefront,
  mdiWarehouse
} from '@mdi/js';
import 'vuetify/styles';
import './styles/main.css';
import router from './router';
import { useAuthStore } from './stores/auth';
import { setTokenProvider, setUnauthorizedHandler } from './services/apiClient';
import { registerSW } from 'virtual:pwa-register';

const toMdiExportName = (iconName) => {
  if (!iconName.startsWith('mdi-')) return null;

  const suffix = iconName
    .slice(4)
    .split('-')
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join('');

  return `mdi${suffix}`;
};

const mdiPaths = {
  mdiAccount,
  mdiAlertCircle,
  mdiBarcodeScan,
  mdiCashMultiple,
  mdiCashRegister,
  mdiChartAreaspline,
  mdiCheckCircle,
  mdiClose,
  mdiDelete,
  mdiDeleteOutline,
  mdiDomain,
  mdiFileDocumentOutline,
  mdiMagnify,
  mdiMenu,
  mdiPackageVariant,
  mdiQrcodeScan,
  mdiStorefront,
  mdiWarehouse
};

const mdiSvgSet = {
  component: (props) => {
    const icon = typeof props.icon === 'string' ? mdiPaths[toMdiExportName(props.icon)] || props.icon : props.icon;

    return h(mdi.component, { ...props, icon });
  }
};

const vuetify = createVuetify({
  components,
  directives,
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: {
      mdi: mdiSvgSet
    }
  },
  theme: {
    defaultTheme: 'posTheme',
    themes: {
      posTheme: {
        dark: false,
        colors: {
          primary: '#0f766e',
          secondary: '#ea580c',
          accent: '#0ea5a4',
          background: '#f5f3ef',
          surface: '#ffffff',
          info: '#0ea5a4',
          success: '#16a34a',
          warning: '#f59e0b',
          error: '#dc2626'
        }
      }
    }
  }
});

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);
app.use(vuetify);

const auth = useAuthStore(pinia);
auth.loadFromStorage();

setTokenProvider(() => auth.token);
setUnauthorizedHandler(() => {
  auth.logout();
  if (router.currentRoute.value.path !== '/login') {
    router.replace('/login');
  }
});

if (import.meta.env.PROD) {
  registerSW({ immediate: true });
}

app.mount('#app');
