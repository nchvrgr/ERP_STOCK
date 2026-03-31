import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import { createVuetify } from 'vuetify';
import * as components from 'vuetify/components';
import * as directives from 'vuetify/directives';
import { aliases, mdi } from 'vuetify/iconsets/mdi-svg';
import { es } from 'vuetify/locale';
import { h, watch } from 'vue';
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
  mdiMinus,
  mdiPackageVariant,
  mdiPlus,
  mdiPrinterOutline,
  mdiQrcodeScan,
  mdiWarehouse
} from '@mdi/js';
import 'vuetify/styles';
import './styles/main.css';
import router from './router';
import { useAuthStore } from './stores/auth';
import { setTokenProvider, setUnauthorizedHandler } from './services/apiClient';
import { applyColorMode, getStoredColorMode, getThemeName, POS_COLOR_MODE_DARK } from './utils/colorMode';

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
  mdiMinus,
  mdiPackageVariant,
  mdiPlus,
  mdiPrinterOutline,
  mdiQrcodeScan,
  mdiWarehouse
};

const mdiSvgSet = {
  component: (props) => {
    const icon = typeof props.icon === 'string' ? mdiPaths[toMdiExportName(props.icon)] || props.icon : props.icon;

    return h(mdi.component, { ...props, icon });
  }
};

const initialColorMode = getStoredColorMode();
const spanishLocale = {
  ...es,
  dataFooter: {
    ...es.dataFooter,
    pageText: '{0}-{1} de {2}'
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
    defaultTheme: getThemeName(initialColorMode),
    themes: {
      posTheme: {
        dark: false,
        colors: {
          primary: '#3B0A12',
          secondary: '#7A5A3A',
          accent: '#C6A46C',
          background: '#F4EFE6',
          surface: '#ffffff',
          info: '#A98A6F',
          success: '#16a34a',
          warning: '#C6A46C',
          error: '#dc2626'
        }
      },
      posNightTheme: {
        dark: true,
        colors: {
          primary: '#C58B94',
          secondary: '#C6A46C',
          accent: '#A7747D',
          background: '#171211',
          surface: '#241C1A',
          info: '#B89A7E',
          success: '#4ade80',
          warning: '#D6B574',
          error: '#f87171'
        }
      }
    }
  },
  locale: {
    locale: 'es',
    fallback: 'es',
    messages: {
      es: spanishLocale
    }
  }
});

applyColorMode(initialColorMode === POS_COLOR_MODE_DARK ? POS_COLOR_MODE_DARK : initialColorMode, vuetify.theme);

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

watch(
  () => auth.hasAppAccess,
  (hasAccess) => {
    if (!hasAccess && router.currentRoute.value.path !== '/login') {
      router.replace('/login');
    }
  }
);

auth.initializeFirebaseSession().finally(() => {
  app.mount('#app');
});
