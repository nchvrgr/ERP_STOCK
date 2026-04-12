import { POS_PALETTES } from './palette';

const lightVars = POS_PALETTES.light.cssVars;

export const getTicketWindowStyles = () => `
  @page { margin: 8mm; }
  body {
    font-family: Arial, sans-serif;
    font-size: 12px;
    padding: 10px;
    color: ${lightVars['--pos-ink']};
    background: ${lightVars['--pos-card']};
  }
  h1 {
    margin: 0 0 6px 0;
    font-size: 14px;
    color: ${lightVars['--pos-accent-dark']};
  }
  table {
    width: 100%;
    border-collapse: collapse;
    margin-top: 8px;
  }
  th, td {
    border-bottom: 1px dashed ${lightVars['--pos-ticket-rule']};
    padding: 4px 0;
  }
  th {
    text-align: left;
    font-weight: 600;
  }
  .ticket-actions {
    display: flex;
    gap: 8px;
    margin-bottom: 12px;
  }
  .ticket-actions button {
    border: 1px solid ${lightVars['--pos-ticket-btn-border']};
    background: ${lightVars['--pos-ticket-btn-bg']};
    color: ${lightVars['--pos-ticket-btn-text']};
    border-radius: 999px;
    padding: 6px 12px;
    cursor: pointer;
  }
  @media print {
    .ticket-actions { display: none; }
    body { padding: 0; }
  }
`;
