<template>
  <v-card class="pos-card pa-4">
    <div class="d-flex align-center justify-space-between">
      <div>
        <div class="text-h6">Alertas de stock</div>
        <div class="text-caption text-medium-emphasis">Vista rapida</div>
      </div>
      <v-chip color="warning" variant="tonal">{{ alerts.length }}</v-chip>
    </div>

    <v-list density="compact" class="mt-3 stock-list-no-bg">
      <v-list-item v-for="alert in alerts" :key="alert.id" class="stock-alert-item">
        <v-list-item-title>{{ alert.nombre }}</v-list-item-title>
        <v-list-item-subtitle>
          Stock actual: {{ alert.stock }} / Min: {{ alert.minimo }}
        </v-list-item-subtitle>
        <template #append>
          <v-chip
            size="small"
            class="status-chip"
            :color="alert.estado === 'CRITICO' ? 'error' : 'warning'"
            variant="outlined"
          >
            {{ alert.estado }}
          </v-chip>
        </template>
      </v-list-item>
    </v-list>

    <style scoped>
      :deep(.stock-list-no-bg) {
        background-color: transparent;
      }

      :deep(.stock-alert-item) {
        background-color: transparent;
      }
    </style>

    <v-btn class="mt-3 text-none" variant="outlined" color="primary">
      Ver todas
    </v-btn>
  </v-card>
</template>

<script setup>
const alerts = [
  { id: 1, nombre: 'Yerba 1kg', stock: 1, minimo: 4, estado: 'CRITICO' },
  { id: 2, nombre: 'Azucar 1kg', stock: 3, minimo: 5, estado: 'BAJO' },
  { id: 3, nombre: 'Cafe 250g', stock: 2, minimo: 3, estado: 'BAJO' }
];
</script>
