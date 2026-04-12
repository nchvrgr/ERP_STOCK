<template>
  <v-text-field
    v-bind="$attrs"
    :model-value="displayValue"
    :label="label"
    :variant="variant"
    :density="density"
    :disabled="disabled"
    :loading="loading"
    :error-messages="errorMessages"
    :hint="hint"
    :persistent-hint="persistentHint"
    :hide-details="hideDetails"
    class="money-field"
    inputmode="decimal"
    @update:model-value="handleInput"
    @blur="handleBlur"
    @focus="handleFocus"
    @keydown.up.prevent="adjustValue(stepValue)"
    @keydown.down.prevent="adjustValue(-stepValue)"
  >
    <template v-if="hasAppendInner" #append-inner>
      <div class="money-field__append-inner">
        <slot name="append-inner" />
        <div v-if="showStepper" class="money-field__steppers">
          <button
            type="button"
            class="money-field__step-btn"
            :disabled="disabled"
            aria-label="Subir monto"
            @mousedown.prevent
            @click.stop="adjustValue(stepValue)"
          >
            ▲
          </button>
          <button
            type="button"
            class="money-field__step-btn"
            :disabled="disabled"
            aria-label="Bajar monto"
            @mousedown.prevent
            @click.stop="adjustValue(-stepValue)"
          >
            ▼
          </button>
        </div>
      </div>
    </template>
  </v-text-field>
</template>

<script setup>
import { computed, ref, useSlots, watch } from 'vue';
import { formatMoney, formatMoneyModelValue, parseMoneyInput, roundMoney } from '../utils/currency';

const props = defineProps({
  modelValue: {
    type: [Number, String],
    default: ''
  },
  label: {
    type: String,
    default: ''
  },
  variant: {
    type: String,
    default: 'outlined'
  },
  density: {
    type: String,
    default: 'comfortable'
  },
  disabled: {
    type: Boolean,
    default: false
  },
  loading: {
    type: Boolean,
    default: false
  },
  errorMessages: {
    type: [Array, String],
    default: () => []
  },
  hint: {
    type: String,
    default: ''
  },
  persistentHint: {
    type: Boolean,
    default: false
  },
  hideDetails: {
    type: [Boolean, String],
    default: false
  },
  step: {
    type: Number,
    default: 1
  },
  min: {
    type: Number,
    default: 0
  },
  numeric: {
    type: Boolean,
    default: false
  },
  emptyValue: {
    type: [Number, String],
    default: ''
  },
  showStepper: {
    type: Boolean,
    default: true
  },
  clearOnFocus: {
    type: Boolean,
    default: false
  },
  clearZeroOnFocus: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['update:modelValue', 'blur', 'focus']);

const slots = useSlots();
const isFocused = ref(false);
const editingValue = ref('');
const autoClearedZero = ref(false);
const valueBeforeFocus = ref(null);

const isBlank = (value) => value === '' || value == null;
const toEditableValue = (value) => {
  if (isBlank(value)) return '';

  const parsed = parseMoneyInput(value);
  if (parsed == null) return '';

  return formatMoneyModelValue(parsed);
};

const hasAppendInner = computed(() => Boolean(slots['append-inner']) || props.showStepper);
const stepValue = computed(() => {
  const parsed = Number(props.step);
  return parsed > 0 ? parsed : 1;
});
const displayValue = computed(() => {
  if (isFocused.value) return editingValue.value;
  return isBlank(props.modelValue) ? '' : formatMoney(props.modelValue);
});

const emitParsedValue = (value) => {
  const rounded = roundMoney(value);
  emit('update:modelValue', props.numeric ? rounded : formatMoneyModelValue(rounded));
};

watch(
  () => props.modelValue,
  (value) => {
    if (isFocused.value) return;
    editingValue.value = toEditableValue(value);
    autoClearedZero.value = false;
  },
  { immediate: true }
);

const handleInput = (value) => {
  const rawValue = value == null ? '' : String(value);
  editingValue.value = rawValue;

  if (rawValue.trim() === '') {
    emit('update:modelValue', props.emptyValue);
    return;
  }

  const parsed = parseMoneyInput(rawValue);
  if (parsed == null) return;

  emitParsedValue(parsed);
};

const adjustValue = (delta) => {
  const current = isBlank(props.modelValue) ? 0 : Number(props.modelValue);
  const safeCurrent = Number.isFinite(current) ? current : 0;
  const nextValue = Math.max(Number(props.min || 0), roundMoney(safeCurrent + delta));
  editingValue.value = formatMoneyModelValue(nextValue);
  emitParsedValue(nextValue);
};

const handleBlur = (event) => {
  const rawValue = editingValue.value.trim();
  if (!rawValue) {
    if (props.clearOnFocus) {
      if (valueBeforeFocus.value == null || valueBeforeFocus.value === '') {
        emit('update:modelValue', props.emptyValue);
      } else {
        emitParsedValue(valueBeforeFocus.value);
      }
    } else if (autoClearedZero.value) {
      emitParsedValue(0);
    } else {
      emit('update:modelValue', props.emptyValue);
    }
  } else {
    const parsed = parseMoneyInput(rawValue);
    if (parsed != null) {
      emitParsedValue(parsed);
    }
  }

  autoClearedZero.value = false;
  valueBeforeFocus.value = null;
  isFocused.value = false;
  emit('blur', event);
};

const handleFocus = (event) => {
  isFocused.value = true;
  const editableValue = toEditableValue(props.modelValue);
  const parsedValue = parseMoneyInput(props.modelValue);
  valueBeforeFocus.value = parsedValue;
  autoClearedZero.value = props.clearZeroOnFocus && parsedValue === 0;
  editingValue.value = props.clearOnFocus || autoClearedZero.value ? '' : editableValue;
  emit('focus', event);
};
</script>

<style scoped>
.money-field :deep(input) {
  text-align: left;
}

.money-field__append-inner {
  display: flex;
  align-items: center;
  gap: 6px;
  opacity: 1;
}

.money-field__steppers {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.money-field__step-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 24px;
  width: 24px;
  height: 20px;
  padding: 0;
  border: none;
  border-radius: 0;
  background: transparent;
  color: var(--pos-accent-strong);
  font-size: 11px;
  font-weight: 700;
  line-height: 1;
  cursor: pointer;
}

.money-field__step-btn:disabled {
  opacity: 0.45;
  cursor: default;
}
</style>
