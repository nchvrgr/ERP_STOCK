export const roundMoney = (value) => {
  const amount = Number(value || 0);
  if (!Number.isFinite(amount)) return 0;
  return Math.round(amount * 100) / 100;
};

export const formatMoney = (value) =>
  new Intl.NumberFormat('es-AR', {
    style: 'currency',
    currency: 'ARS',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(roundMoney(value));

const getDecimalSeparatorIndex = (value) => {
  const commaIndex = value.lastIndexOf(',');
  const dotIndex = value.lastIndexOf('.');
  const separatorIndex = Math.max(commaIndex, dotIndex);
  if (separatorIndex === -1) return -1;

  const digitsAfter = value.slice(separatorIndex + 1).replace(/\D/g, '');
  if (digitsAfter.length === 0) return -1;
  if (digitsAfter.length <= 2) return separatorIndex;
  return -1;
};

export const parseMoneyInput = (value) => {
  if (value == null) return null;

  const raw = String(value).trim();
  if (!raw) return null;

  const sanitized = raw.replace(/\s/g, '').replace(/\$/g, '');
  const separatorIndex = getDecimalSeparatorIndex(sanitized);
  const digitsOnly = sanitized.replace(/[^\d]/g, '');

  if (!digitsOnly) return null;

  if (separatorIndex === -1) {
    return roundMoney(Number(digitsOnly));
  }

  const integerPart = sanitized.slice(0, separatorIndex).replace(/[^\d]/g, '') || '0';
  const decimalPart = sanitized.slice(separatorIndex + 1).replace(/[^\d]/g, '').slice(0, 2);
  return roundMoney(Number(`${integerPart}.${decimalPart}`));
};

export const formatMoneyModelValue = (value) => {
  const rounded = roundMoney(value);
  return Number.isInteger(rounded) ? String(rounded) : rounded.toFixed(2);
};
