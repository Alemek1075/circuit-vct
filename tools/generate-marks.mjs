import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const marks = {
  FNC: '#E8A24B', DRX: '#86A9D4', PRX: '#D98F9D', G2: '#C8C9D0',
  TH: '#E7B160', MIBR: '#B2C7D1', NRG: '#93C9A0', GX: '#D4AD8A',
  LOUD: '#93BFA1', '100T': '#D98C87', VIT: '#E9B959', FUT: '#A8BCD2',
  TL: '#A7C1D2', KC: '#8CB3DA', NS: '#D78C80', GE: '#B9A9D9',
  T1: '#D5A0A0', JDG: '#D9A2A6', XLG: '#BA9ED1', TYL: '#DBA17E',
  EDG: '#B5C5D0'
};

const target = join('src', 'Circuit', 'wwwroot', 'img', 'marks');
mkdirSync(target, { recursive: true });
for (const [abbr, color] of Object.entries(marks)) {
  const count = 2 + (abbr.charCodeAt(0) % 2);
  const bars = Array.from({ length: count }, (_, i) => {
    const x = 14 + i * 12;
    return `<path d="M${x} 46 ${x + 12} 18h5L${x + 5} 46z" fill="${color}"/>`;
  }).join('');
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64"><rect width="64" height="64" fill="#20323b"/><path d="M0 0h5v64H0z" fill="${color}"/>${bars}</svg>`;
  writeFileSync(join(target, `${abbr.toLowerCase()}.svg`), svg);
}
