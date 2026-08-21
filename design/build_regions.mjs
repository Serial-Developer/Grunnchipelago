// build_regions.mjs - Proposition AUTOMATIQUE du graphe de régions (sorties : design/regions_auto.*)
// Principe : les portails contrôlés par un ContentHider sont des
// arêtes CONDITIONNELLES. Elles sont exclues du calcul des composantes et
// émises comme connexions inter-régions avec leur règle d'accès dérivée par
// négation De Morgan des hideConditions (sémantique vérifiée dans
// ContentHider.Update : condition vraie => objet caché ; OU par défaut, ET si
// hideConditionsAnd).
import { fileURLToPath } from 'node:url';
import { dirname } from 'node:path';
import { readFileSync, writeFileSync } from 'node:fs';

const DIR = dirname(dirname(fileURLToPath(import.meta.url))); // racine du depot
const logic = JSON.parse(readFileSync(`${DIR}/dump/zone_logic.json`, 'utf8'));
const dump = JSON.parse(readFileSync(`${DIR}/dump/grunnchipelago_dump.json`, 'utf8'));

// ---------- Conditions des hiders, avec paramètres ----------
function hiderConds(ch) {
  return (ch.hideConditions || []).map(c => {
    if (c === 'KeyItemObtained' || c === 'KeyItemNotObtained') return `${c}(${ch.keyItemRef})`;
    if (c === 'DayIndexIs' || c === 'DayIndexIsNot') return `${c}(${ch.dayIndexCheck})`;
    if (c === 'InTimeWindow' || c === 'NotInTimeWindow') return `${c}(${ch.hourStart}h-${ch.hourEnd}h)`;
    return c;
  });
}

// Négation lisible d'une condition (pour la règle d'accès)
function negate(c) {
  if (c.startsWith('KeyItemNotObtained(')) return c.replace('KeyItemNotObtained', 'KeyItemObtained');
  if (c.startsWith('KeyItemObtained(')) return c.replace('KeyItemObtained', 'KeyItemNotObtained');
  if (c.startsWith('DayIndexIsNot(')) return c.replace('DayIndexIsNot', 'DayIndexIs');
  if (c.startsWith('DayIndexIs(')) return c.replace('DayIndexIs', 'DayIndexIsNot');
  if (c.startsWith('NotInTimeWindow(')) return c.replace('NotInTimeWindow', 'InTimeWindow');
  if (c.startsWith('InTimeWindow(')) return c.replace('InTimeWindow', 'NotInTimeWindow');
  if (c.startsWith('Not')) return c.slice(3);
  return `NOT(${c})`;
}

// Règle d'accès = négation De Morgan des conditions de masquage
function accessRule(conds, andMode) {
  const negs = conds.map(negate);
  // caché si (OU des conds) => accessible si (ET des négations) ; et inversement
  return andMode ? negs.join(' OU ') : negs.join(' ET ');
}

// ---------- Portails conditionnels (hider -> nom de portail) ----------
const portalNames = new Set();
for (const e of logic.portalEdgesDetailed) { portalNames.add(e.portalA); portalNames.add(e.portalB); }

const condsByPortal = new Map();
for (const ch of dump.contentHiders) {
  if (!ch.objectRef || !portalNames.has(ch.objectRef)) continue;
  if (!ch.hideConditions || ch.hideConditions.length === 0) continue;
  const prev = condsByPortal.get(ch.objectRef) || [];
  prev.push({ conds: hiderConds(ch), and: ch.hideConditionsAnd });
  condsByPortal.set(ch.objectRef, prev);
}

// ---------- Arêtes libres vs conditionnelles ----------
const freeEdges = [];
const conditionalEdges = [];
for (const e of logic.portalEdgesDetailed) {
  const hidersA = condsByPortal.get(e.portalA) || [];
  const hidersB = condsByPortal.get(e.portalB) || [];
  if (hidersA.length === 0 && hidersB.length === 0) {
    freeEdges.push(e);
  } else {
    const allHiders = [...hidersA, ...hidersB];
    conditionalEdges.push({
      ...e,
      hideConditions: allHiders.map(h => `[${h.and ? 'ET' : 'OU'}] ${h.conds.join(', ')}`),
      accessRule: allHiders.map(h => accessRule(h.conds, h.and)).join(' ET ')
    });
  }
}

// ---------- Composantes connexes sur les arêtes libres ----------
const zoneNames = Object.keys(logic.zones);
const adj = new Map(zoneNames.map(z => [z, new Set()]));
for (const e of freeEdges) {
  if (!adj.has(e.zoneA)) adj.set(e.zoneA, new Set());
  if (!adj.has(e.zoneB)) adj.set(e.zoneB, new Set());
  adj.get(e.zoneA).add(e.zoneB);
  adj.get(e.zoneB).add(e.zoneA);
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

// ---------- Voyage (bateau/bus/vélo) avec résolution de zone améliorée ----------
const aliases = {
  StartGardenArea: 'StartGarden (extérieur)', ChurchArea: 'Church (extérieur)',
  ParkArea: 'Park (extérieur)', RoadArea: 'Road'
};
function zoneOfDumpObj(obj) {
  const ambience = obj.areas.filter(a => !a.startsWith('MACRO:'));
  if (ambience.length) return ambience[0];
  const macro = obj.areas.find(a => a.startsWith('MACRO:'));
  if (macro) return macro.replace('MACRO:', '') + ' (extérieur)';
  const m = obj.path.match(/\/Areas\/([A-Za-z0-9]+)\//);
  if (m) return aliases[m[1]] ?? m[1];
  if (/bridge|bus|pylon|road/i.test(obj.path)) return 'Road';
  return `(à préciser, pos ${obj.pos.x},${obj.pos.z})`;
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
    blockers: fmtConds(it),
    accessRule: fmtConds(it).map(negate).join(' ET ') || 'libre'
  }));

// ---------- Régions ----------
const regions = comps.map((zones, i) => ({
  id: `R${String(i).padStart(2, '0')}`,
  name: zones.length === 1 ? zones[0] : `À NOMMER (${zones[0]}...)`,
  zones,
  checks: zones.reduce((s, z) => s + checkCount(z), 0)
}));
regions.sort((a, b) => b.zones.length - a.zones.length || b.checks - a.checks);
const regionIdOf = z => { const r = regions.find(r => r.zones.includes(z)); return r ? r.id : '?'; };

// ---------- Sorties ----------
const out = {
  meta: {
    generated: new Date().toISOString(),
    note: "PROPOSITION v2. Régions = composantes des portails LIBRES. connectionsAuto = portails conditionnels avec règle dérivée des données (accessRule). Reste à faire : nommer/fusionner les régions, remplir connectionsManuelles (adjacences à pied)."
  },
  regions,
  connectionsAuto: conditionalEdges.map(e => ({
    from: `${e.zoneA} (${regionIdOf(e.zoneA)})`,
    to: `${e.zoneB} (${regionIdOf(e.zoneB)})`,
    via: `${e.portalA} <-> ${e.portalB}`,
    hideConditions: e.hideConditions,
    accessRule: e.accessRule
  })),
  travelInteractionsAuto: travel,
  connectionsManuelles: [
    { from: 'EXEMPLE: StartGarden (extérieur)', to: 'EXEMPLE: Road', via: 'portail du jardin', rule: 'GardenKey', status: 'À VALIDER' }
  ]
};
writeFileSync(`${DIR}/design/regions_auto.json`, JSON.stringify(out, null, 2), 'utf8');

const L = [];
L.push('# Grunn - Graphe de régions AP v2');
L.push('');
L.push('Régions = composantes connexes des portails **libres**. Les portails conditionnés par ContentHider');
L.push('sont des connexions inter-régions avec règle d\'accès dérivée des données (négation De Morgan');
L.push('des hideConditions - sémantique vérifiée dans ContentHider.Update).');
L.push('');
L.push(`## ${regions.length} régions candidates`);
L.push('');
for (const r of regions) {
  L.push(`### ${r.id} - ${r.name} (${r.zones.length} zone(s), ${r.checks} checks)`);
  L.push(`Zones : ${r.zones.join(', ')}`);
  L.push('');
}
L.push(`## ${conditionalEdges.length} connexions conditionnelles (portails + règles auto)`);
L.push('');
for (const e of conditionalEdges) {
  L.push(`- **${e.zoneA}** (${regionIdOf(e.zoneA)}) <-> **${e.zoneB}** (${regionIdOf(e.zoneB)}) via \`${e.portalA}\``);
  L.push(`  - masqué si : ${e.hideConditions.join(' ; ')}`);
  L.push(`  - règle d'accès : **${e.accessRule}**`);
}
L.push('');
L.push('## Interactions de voyage (règles auto)');
L.push('');
for (const t of travel) {
  L.push(`- **${t.type}** depuis ${t.zone} - règle : **${t.accessRule}**`);
}
L.push('');
L.push('## À compléter manuellement : adjacences à pied entre régions');
L.push('');
L.push('Remplir `connectionsManuelles` dans regions_auto.json (jardin<->route, route<->station, etc.).');
writeFileSync(`${DIR}/design/regions_auto.md`, L.join('\n'), 'utf8');

console.log(`Régions : ${regions.length} (multi-zones : ${regions.filter(r => r.zones.length > 1).length})`);
console.log(`Arêtes libres : ${freeEdges.length} | conditionnelles : ${conditionalEdges.length}`);
console.log(`Voyages : ${travel.length}`);
console.log('-> design/regions_auto.md + design/regions_auto.json');
