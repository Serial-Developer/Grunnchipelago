// build_ids.mjs — Génération des IDs items/locations de l'apworld Grunn
// Sources : enums.txt (ordres d'enum stables du jeu) + dump (positions fantômes/gulden).
// BASE_ID = 478660000 ("GRUNN" en T9 : 4-7-8-6-6, x10000) — arbitraire, modifiable
// tant que rien n'est publié.
import { fileURLToPath } from 'node:url';
import { dirname } from 'node:path';
import { readFileSync, writeFileSync } from 'node:fs';

const BASE = 478660000;
const DIR = dirname(dirname(fileURLToPath(import.meta.url))); // racine du depot
// enums.txt est produit hors depot par le workflow d'extraction (voir design/apworld_design.md).
// Surchargeable : GRUNN_ENUMS=/chemin/enums.txt node design/build_ids.mjs
const ENUMS = process.env.GRUNN_ENUMS ?? "";

// ---------- Parsing des enums ----------
const enumText = readFileSync(ENUMS, 'utf8');
function parseEnum(name) {
  const re = new RegExp(`=== ${name} ===\\r?\\n(.+)`, 'm');
  const m = enumText.match(re);
  if (!m) throw new Error(`Enum ${name} introuvable`);
  return m[1].split(',').map(s => s.trim()).filter(s => s && s !== 'Length');
}
const keyItems = parseEnum('KeyItem');
const endings = parseEnum('EndingType').filter(e => e !== 'DemoEnding');
const polaroids = parseEnum('PolaroidType').filter(p => !p.startsWith('Ending'));

// ---------- Dump : fantômes et gulden ----------
const dump = JSON.parse(readFileSync(`${DIR}/dump/grunnchipelago_dump.json`, 'utf8'));
const aliases = {
  StartGardenArea: 'StartGarden', ChurchArea: 'Church', ParkArea: 'Park', RoadArea: 'Road'
};
function zoneOf(obj) {
  const ambience = obj.areas.filter(a => !a.startsWith('MACRO:'));
  if (ambience.length) return ambience[0];
  const macro = obj.areas.find(a => a.startsWith('MACRO:'));
  if (macro) return macro.replace('MACRO:', '');
  const m = obj.path.match(/\/Areas\/([A-Za-z0-9]+)\//);
  if (m) return aliases[m[1]] ?? m[1];
  if (/bridge|bus|pylon|road|Car|car/i.test(obj.path)) return 'Road';
  return 'Unknown';
}
const byPos = (a, b) => (a.pos.x - b.pos.x) || (a.pos.z - b.pos.z);
const ghosts = [...dump.ghosts].sort(byPos);
const gulden = dump.itemPickups.filter(p => p.isGulden).sort(byPos);

// ---------- Items ----------
const items = {};
keyItems.forEach((k, i) => { items[k] = { id: BASE + 1 + i, category: 'keyitem' }; });

const buffs = ['Move Speed Boost', 'Cutter Range Boost', 'Cutting Rate Boost'];
buffs.forEach((b, i) => { items[b] = { id: BASE + 201 + i, category: 'buff' }; });

const traps = [
  'Speed Trap', 'Size Trap', 'Inverted Controls Trap',
  'Regrow Grass Trap', 'Rewater Flowers Trap', 'Regrow Hedge Trap',
  'Return Trash Trap', 'Regrow Molehills Trap'
];
traps.forEach((t, i) => { items[t] = { id: BASE + 301 + i, category: 'trap' }; });

items['Gulden'] = { id: BASE + 401, category: 'filler' };

// ---------- Locations ----------
const locations = {};
keyItems.forEach((k, i) => {
  locations[`Obtain ${k}`] = { id: BASE + 1001 + i, category: 'keyitem' };
});
endings.forEach((e, i) => {
  locations[`Ending: ${e}`] = { id: BASE + 1101 + i, category: 'ending' };
});
polaroids.forEach((p, i) => {
  locations[`Polaroid: ${p}`] = { id: BASE + 1201 + i, category: 'polaroid' };
});
ghosts.forEach((g, i) => {
  locations[`Calm Ghost #${i + 1} (${zoneOf(g)})`] = {
    id: BASE + 1301 + i, category: 'ghost',
    pos: { x: g.pos.x, z: g.pos.z }, path: g.path
  };
});
gulden.forEach((g, i) => {
  locations[`Gulden #${i + 1} (${zoneOf(g)})`] = {
    id: BASE + 1401 + i, category: 'gulden',
    pos: { x: g.pos.x, z: g.pos.z }, path: g.path
  };
});

// ---------- Sortie ----------
const catCount = (obj) => {
  const c = {};
  for (const v of Object.values(obj)) c[v.category] = (c[v.category] || 0) + 1;
  return c;
};
const out = {
  meta: {
    generated: new Date().toISOString(),
    baseId: BASE,
    note: 'IDs stables dérivés des ordres d\'enum du jeu + positions triées. Ne plus renuméroter après publication.'
  },
  itemCounts: catCount(items),
  locationCounts: catCount(locations),
  items, locations
};
writeFileSync(`${DIR}/design/ids.json`, JSON.stringify(out, null, 2), 'utf8');
console.log('Items :', JSON.stringify(out.itemCounts));
console.log('Locations :', JSON.stringify(out.locationCounts));
console.log(`Total items : ${Object.keys(items).length} | Total locations : ${Object.keys(locations).length}`);
console.log('-> design/ids.json');
