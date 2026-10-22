// ดึงข้อมูลปลา 30 ชนิดจากเวอร์ชันเว็บ (home-twin/fishing/js/fish.js) → Assets/Game/Resources/Data/species.json
//   node Tools/export-species.mjs ../home-redar/fishing/js/fish.js
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';

const src = readFileSync(process.argv[2] || '../home-redar/fishing/js/fish.js', 'utf8');
const a = src.indexOf('export const SPECIES = [');
const b = src.indexOf('\n];', a);
const species = Function(`return ${src.slice(a + 'export const SPECIES = '.length, b + 3)}`)();
const pm = src.match(/const PREF = (\{[\s\S]*?\});/);
const pref = pm ? Function(`return ${pm[1]}`)() : {};
const hex = n => '#' + n.toString(16).padStart(6, '0');
const out = species.map(s => ({
  id: s.id, stage: s.stage, name: s.name, en: s.en, stars: s.stars, chance: s.chance, price: s.price,
  kgMin: s.kg[0], kgMax: s.kg[1], cmMin: s.cm[0], cmMax: s.cm[1],
  shape: s.shape, top: hex(s.top), belly: hex(s.belly), fin: hex(s.fin), pattern: s.pattern,
  pull: s.pull, dash: s.dash, prog: s.prog, win: s.win, rare: !!s.rare, glow: !!s.glow, whiskers: !!s.whiskers,
  time: pref[s.id] || 'any', desc: s.desc,
}));
mkdirSync('Assets/Game/Resources/Data', { recursive: true });
writeFileSync('Assets/Game/Resources/Data/species.json', JSON.stringify({ species: out }, null, 1));
console.log('exported', out.length, 'species');
