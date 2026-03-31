import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '../stores/auth';

const LoginPage = () => import('../pages/LoginPage.vue');
const MainLayout = () => import('../layouts/MainLayout.vue');
const ProductosPage = () => import('../pages/ProductosPage.vue');
const StockPage = () => import('../pages/StockPage.vue');
const ReportesPage = () => import('../pages/ReportesPage.vue');
const CajaPage = () => import('../pages/CajaPage.vue');
const RemitosPage = () => import('../pages/RemitosPage.vue');
const PreRecepcionPage = () => import('../pages/PreRecepcionPage.vue');
const RecepcionPage = () => import('../pages/RecepcionPage.vue');
const EmpresaDatosPage = () => import('../pages/EmpresaDatosPage.vue');

const routes = [
  {
    path: '/login',
    name: 'login',
    component: LoginPage,
    meta: { public: true }
  },
  {
    path: '/',
    component: MainLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        redirect: '/caja'
      },
      {
        path: 'pos',
        redirect: '/caja'
      },
      {
        path: 'productos',
        name: 'productos',
        component: ProductosPage,
        meta: { permission: 'PERM_PRODUCTO_VER' }
      },
      {
        path: 'remitos',
        name: 'remitos',
        component: RemitosPage,
        meta: { permission: 'PERM_COMPRAS_REGISTRAR' }
      },
      {
        path: 'remitos/pre-recepcion/:id',
        name: 'pre-recepcion',
        component: PreRecepcionPage,
        meta: { permission: 'PERM_COMPRAS_REGISTRAR' }
      },
      {
        path: 'recepciones/:id',
        name: 'recepcion',
        component: RecepcionPage,
        meta: { permission: 'PERM_COMPRAS_REGISTRAR' }
      },
      {
        path: 'stock',
        name: 'stock',
        component: StockPage,
        meta: { permission: 'PERM_STOCK_AJUSTAR' }
      },
      {
        path: 'reportes',
        name: 'reportes',
        component: ReportesPage,
        meta: { permission: 'PERM_REPORTES_VER' }
      },
      {
        path: 'caja',
        name: 'caja',
        component: CajaPage,
        meta: { permission: 'PERM_VENTA_CREAR' }
      },
      {
        path: 'empresa',
        name: 'empresa',
        component: EmpresaDatosPage,
        meta: { permission: 'PERM_PRODUCTO_VER' }
      }
    ]
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/caja'
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

router.beforeEach(async (to) => {
  const auth = useAuthStore();
  await auth.initializeFirebaseSession();

  if (to.meta.public) {
    if (auth.hasAppAccess && to.path === '/login') {
      return '/caja';
    }
    return true;
  }

  if (!auth.hasAppAccess) {
    return { path: '/login', query: { redirect: to.fullPath } };
  }

  return true;
});

export default router;
