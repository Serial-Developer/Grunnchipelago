// build_regions_v1.mjs — Proposition v1 du graphe de régions AP
// Méthode : composantes connexes du graphe de portails = candidats régions
// (fusion/scission à réviser par Jonath), + interactions de voyage extraites
// comme candidats de connexions inter-régions. Les adjacences "à pied" ne sont
// pas dans les données de scène : elles sont listées comme À COMPLÉTER.
import { readFileSync, writeFileSync } from 'node:fs';

const DIR = 'C:/Users/jonat/Desktop/Projets/Grunnchipelago';
const logic = JSON.parse(readFileSync(`${DIR}/dump/zone_logic.json`, 'utf8'));
const dump = JSON.parse(readFileSync(`${DIR}/dump/grunnchipelago_dump.json`, 'utf8'));

// ---------- 1. Composantes connexes (portails) ----------
const zoneNames = Object.keys(logic.zones);
const adj = new Map(zoneNames.map(z => [z, new Set()]));
for (const e of logic.portalConnections) {
  const [a, b] = e.split(' <-> ');
  if (!adj.has(a)) adj.set(a, new Set());
  if (!adj.has(b)) adj.set(b, new Set());
  adj.get(a).add(b);
  adj.get(b).add(a);
}

const compOf = new Map();
const comps = [];
for (const z of adj.keys()) {
  if (compOf.has(z)) continue;
  const comp = [];
  const queue = [z];
  compOf.set(z, comps.length);
  while (queue.length) {
    const c = queue.pop();
    comp.push(c);
    for (const n of adj.get(c) ?? []) {
      if (!compOf.has(n)) { compOf.set(n, comps.length); queue.push(n); }
    }
  }
  comps.push(comp.sort());
}

function checkCount(zn) {
  const v = logic.zones[zn];
  if (!v) return 0;
  return v.pickups.length + v.polaroids.length + v.ghosts.length + v.gulden.length;
}

// ---------- 2. Interactions de voyage (candidats connexions inter-régions) ----------
function zoneOfDumpObj(obj) {
  const ambience = obj.areas.filter(a => !a.startsWith('MACRO:'));
  if (ambience.length) return ambience[0];
  const macro = obj.areas.find(a => a.startsWith('MACRO:'));
  if (macro) return macro.replace('MACRO:', '') + ' (extérieur)';
  return '(zone à préciser)';
}

function fmtConds(it) {
  return (it.preventTypes || []).map(pt =>
    (pt === 'KeyItemObtained' || pt === 'KeyItemNotObtained') ? `${pt}(${it.keyItemObtainedRef})` : pt
  );
}

const travel = dump.interactions
  .filter(it => /Boat|Bus|Bike|Travel/i.test(it.type))
  .map(it => ({
    type: it.type,
    zone: zoneOfDumpObj(it),
    conditions: fmtConds(it),
    refs: Object.keys(it.refs || {})
  }));

// ---------- 3. Conditions internes par composante (candidates access rules) ----------
function regionConditions(zones) {
  const conds = new Set();
  for (const zn of zones) {
    const v = logic.zones[zn];
    if (!v) continue;
    for (const it of v.gatedInteractions) for (const c of it.conditions) conds.add(c);
  }
  return [...conds].sort();
}

// ---------- 4. Sorties ----------
const regions = comps.map((zones, i) => ({
  id: `R${String(i).padStart(2, '0')}`,
  name: zones.length === 1 ? zones[0] : `À NOMMER (${zones[0]}...)`,
  zones,
  checks: zones.reduce((s, z) => s + checkCount(z), 0),
  conditionsInternes: regionConditions(zones)
}));
regions.sort((a, b) => b.zones.length - a.zones.length || b.checks - a.checks);

const out = {
  meta: {
    generated: new Date().toISOString(),
    note: "PROPOSITION v1 à réviser : fusionner/scinder les régions, les nommer, puis remplir connectionsManuelles (adjacences à pied et transports) avec une règle par connexion (nom de KeyItem, 'libre', ou expression)."
  },
  regions,
  travelInteractionsAuto: travel,
  connectionsManuelles: [
    { from: 'EXEMPLE: StartGarden', to: 'EXEMPLE: Road', via: 'portail du jardin', rule: 'GardenKey', status: 'À VALIDER' }
  ]
};
writeFileSync(`${DIR}/design/regions_v1.json`, JSON.stringify(out, null, 2), 'utf8');

// ---------- 5. Markdown de revue ----------
const L = [];
L.push('# Grunn — Proposition v1 du graphe de régions AP');
L.push('');
L.push('Candidats régions = composantes connexes du graphe de portails (46 arêtes mesurées).');
L.push('Les liaisons À PIED entre régions ne sont pas dans les données de scène : à définir manuellement');
L.push('dans `regions_v1.json` -> `connectionsManuelles`, avec la règle d\'accès de chaque liaison.');
L.push('');
L.push(`## ${regions.length} régions candidates`);
L.push('');
for (const r of regions) {
  L.push(`### ${r.id} — ${r.name} (${r.zones.length} zone(s), ${r.checks} checks)`);
  L.push(`Zones : ${r.zones.join(', ')}`);
  if (r.conditionsInternes.length) {
    L.push(`Conditions rencontrées à l'intérieur : ${r.conditionsInternes.slice(0, 12).join(', ')}${r.conditionsInternes.length > 12 ? ` … (+${r.conditionsInternes.length - 12})` : ''}`);
  }
  L.push('');
}
L.push('## Interactions de voyage détectées (candidats connexions inter-régions)');
L.push('');
for (const t of travel) {
  L.push(`- **${t.type}** depuis ${t.zone}${t.conditions.length ? ` — conditions : ${t.conditions.join(', ')}` : ' — libre'}`);
}
L.push('');
L.push('## Travail de révision attendu');
L.push('');
L.push('1. Nommer les régions multi-zones, fusionner les singletons qui vont ensemble à pied.');
L.push('2. Remplir `connectionsManuelles` : chaque passage à pied/porte/voyage entre régions, avec sa règle.');
L.push('3. Signaler les zones de scénario (SnowWorld, Void, LongHallway...) : entrée par événement, pas par déplacement.');
writeFileSync(`${DIR}/design/regions_v1.md`, L.join('\n'), 'utf8');

console.log(`Régions candidates : ${regions.length}`);
console.log(`Multi-zones : ${regions.filter(r => r.zones.length > 1).length} | Singletons : ${regions.filter(r => r.zones.length === 1).length}`);
console.log(`Interactions de voyage : ${travel.length}`);
console.log('-> design/regions_v1.md + design/regions_v1.json');
