// build_zone_logic.mjs - Génère la table de logique par zone depuis le dump Grunn
// Sorties : zone_logic.md (lisible) + zone_logic.json (machine)
import { fileURLToPath } from 'node:url';
import { dirname } from 'node:path';
import { readFileSync, writeFileSync } from 'node:fs';

const DIR = dirname(fileURLToPath(import.meta.url)); // dossier dump/
const dump = JSON.parse(readFileSync(`${DIR}/grunnchipelago_dump.json`, 'utf8'));

// ---------- Résolution de zone ----------
// Priorité : correction manuelle > zone d'ambiance > macro-zone
// > heuristique chemin > heuristique proximité (vote des 5 plus proches).
// La méthode de résolution est tracée par objet dans zoneConfidence.
const zoneConfidence = new Map(); // path -> mesure | chemin | proximité | manuel

// Corrections validées en jeu par Jonath (2026-07-11)
const manualZones = {
  'Main/Interactions/hammer0_car': 'StartGarden (extérieur)', // voiture garée dans le jardin, près de la cabane
  'Main/Interactions/item_plank0': 'Road' // planche contre un mur, à l'EXTÉRIEUR de la station
};

function baseZone(obj) {
  const ambience = obj.areas.filter(a => !a.startsWith('MACRO:'));
  if (ambience.length) return { zone: ambience[0], how: 'mesure' };
  const macro = obj.areas.find(a => a.startsWith('MACRO:'));
  if (macro) return { zone: macro.replace('MACRO:', '') + ' (extérieur)', how: 'mesure' };
  // Conteneur de scène Main/Areas/<Nom>/ : nommage du dev, ground truth
  const m = obj.path.match(/\/Areas\/([A-Za-z0-9]+)\//);
  if (m) {
    const aliases = {
      StartGardenArea: 'StartGarden (extérieur)',
      ChurchArea: 'Church (extérieur)',
      ParkArea: 'Park (extérieur)',
      RoadArea: 'Road'
    };
    return { zone: aliases[m[1]] ?? m[1], how: 'chemin' };
  }
  if (/bridge|bus|pylon|road/i.test(obj.path)) return { zone: 'Road', how: 'chemin' };
  return null;
}

// Points de référence : tous les objets résolus sans proximité
const refPoints = [];
for (const col of ['itemPickups', 'polaroids', 'ghosts', 'interactions', 'doors', 'contentHiders']) {
  for (const obj of dump[col]) {
    const r = baseZone(obj);
    if (r !== null) refPoints.push({ px: obj.pos.x, pz: obj.pos.z, zone: r.zone });
  }
}

function knnZone(obj) {
  const dists = refPoints.map(r => ({
    zone: r.zone,
    d: (r.px - obj.pos.x) ** 2 + (r.pz - obj.pos.z) ** 2
  }));
  dists.sort((a, b) => a.d - b.d);
  const counts = {};
  for (const t of dists.slice(0, 5)) counts[t.zone] = (counts[t.zone] || 0) + 1;
  const best = Object.entries(counts).sort((a, b) => b[1] - a[1])[0][0];
  return { zone: best, how: 'proximité' };
}

function primaryZone(obj) {
  if (manualZones[obj.path]) {
    zoneConfidence.set(obj.path, 'manuel');
    return manualZones[obj.path];
  }
  const r = baseZone(obj) ?? knnZone(obj);
  zoneConfidence.set(obj.path, r.how);
  return r.zone;
}

// Annotation de confiance pour l'affichage (vide si mesure directe)
function conf(path) {
  const c = zoneConfidence.get(path);
  return (c && c !== 'mesure') ? ` [zone: ${c}]` : '';
}

function fmtConditions(preventTypes, keyItemRef) {
  return (preventTypes || []).map(pt =>
    (pt === 'KeyItemObtained' || pt === 'KeyItemNotObtained')
      ? `${pt}(${keyItemRef})`
      : pt
  );
}

// ---------- Indexation par zone ----------
const zones = new Map();
function zoneOf(name) {
  if (!zones.has(name)) zones.set(name, {
    pickups: [], polaroids: [], ghosts: [], gulden: [],
    gatedInteractions: [], doors: [], hiders: [], portalEdges: new Set()
  });
  return zones.get(name);
}

for (const ip of dump.itemPickups) {
  const z = zoneOf(primaryZone(ip));
  const entry = {
    path: ip.path, keyItems: ip.keyItems, isTool: ip.isTool, toolType: ip.toolType,
    inShop: ip.inShop, soldByKid: ip.soldByKid, cost: ip.cost,
    repeatable: ip.isRepeatablePickup, startState: ip.startState, areas: ip.areas
  };
  if (ip.isGulden) z.gulden.push(entry); else z.pickups.push(entry);
}

for (const p of dump.polaroids) {
  zoneOf(primaryZone(p)).polaroids.push({ type: p.polaroidType, path: p.path, areas: p.areas });
}

for (const g of dump.ghosts) {
  zoneOf(primaryZone(g)).ghosts.push({ path: g.path, pos: g.pos, areas: g.areas });
}

for (const it of dump.interactions) {
  if (!it.preventTypes || it.preventTypes.length === 0) continue;
  zoneOf(primaryZone(it)).gatedInteractions.push({
    type: it.type,
    conditions: fmtConditions(it.preventTypes, it.keyItemObtainedRef),
    andCheck: it.preventAndCheck,
    refs: it.refs, path: it.path
  });
}

for (const d of dump.doors) {
  if (!d.locked && !d.barred) continue;
  zoneOf(primaryZone(d)).doors.push({ path: d.path, locked: d.locked, barred: d.barred, type: d.type });
}

for (const ch of dump.contentHiders) {
  if (!ch.hideConditions || ch.hideConditions.length === 0) continue;
  const conds = ch.hideConditions.map(c => {
    if (c === 'KeyItemObtained' || c === 'KeyItemNotObtained') return `${c}(${ch.keyItemRef})`;
    if (c === 'DayIndexIs' || c === 'DayIndexIsNot') return `${c}(${ch.dayIndexCheck})`;
    if (c === 'InTimeWindow' || c === 'NotInTimeWindow') return `${c}(${ch.hourStart}h-${ch.hourEnd}h)`;
    return c;
  });
  zoneOf(primaryZone(ch)).hiders.push({ conditions: conds, and: ch.hideConditionsAnd, objectRef: ch.objectRef, path: ch.path });
}

// ---------- Graphe des portails ----------
// Zone d'un portail : résolution standard, sinon déduction depuis le nom
// (convention dev : portal_SourceToDestination). Les portails "PortalTest"
// sont des restes de debug hors carte : exclus.
function portalZone(p) {
  if (/PortalTest/.test(p.path)) return null;
  const b = baseZone(p);
  if (b) return b.zone;
  // Convention dev : portal_SourceToDestination - ground truth avant la proximité
  const m = p.path.match(/portal_(.+?)To(.+?)\d*$/);
  if (m) return m[1];
  return knnZone(p).zone;
}

const portalByPath = new Map(dump.portals.map(p => [p.path, p]));
const edges = new Set();
const portalEdgesDetailed = [];
const seenPairs = new Set();
for (const p of dump.portals) {
  if (!p.linkedPortal) continue;
  const other = portalByPath.get(p.linkedPortal);
  if (!other) continue;
  const a = portalZone(p), b = portalZone(other);
  if (a === null || b === null) continue;
  if (a === b) continue;
  const key = [a, b].sort().join(' <-> ');
  edges.add(key);
  const pairKey = [p.path, other.path].sort().join('|');
  if (!seenPairs.has(pairKey)) {
    seenPairs.add(pairKey);
    portalEdgesDetailed.push({
      zoneA: a, zoneB: b,
      portalA: p.path.split('/').pop(),
      portalB: other.path.split('/').pop(),
      pathA: p.path, pathB: other.path
    });
  }
  zoneOf(a).portalEdges.add(`${a} -> ${b}`);
  zoneOf(b).portalEdges.add(`${b} -> ${a}`);
}

// ---------- KeyItems sans pickup (donnés par événement/PNJ) ----------
const placedKeyItems = new Set(dump.itemPickups.flatMap(ip => ip.keyItems));
// Réfs KeyItem vues dans les conditions = keyItems existants côté logique
const referencedKeyItems = new Set(
  dump.interactions
    .filter(it => (it.preventTypes || []).some(pt => pt.startsWith('KeyItem')))
    .map(it => it.keyItemObtainedRef)
);
const eventKeyItems = [...referencedKeyItems].filter(k => !placedKeyItems.has(k)).sort();

// ---------- Génération Markdown ----------
const L = [];
L.push('# Grunn - Table de logique par zone');
L.push('');
L.push(`Générée le ${new Date().toISOString()} depuis grunnchipelago_dump.json (${dump.meta.dumper}).`);
L.push('Source : données de scène extraites du jeu en runtime. Rien n\'est inféré hors des heuristiques signalées.');
L.push('');
L.push('## Résumé global');
L.push('');
L.push(`- Zones avec contenu : ${zones.size}`);
L.push(`- Connexions par portails (dédupliquées) : ${edges.size}`);
L.push(`- KeyItems posés dans le monde : ${placedKeyItems.size}`);
L.push(`- KeyItems référencés par la logique mais donnés par événement/PNJ : ${eventKeyItems.length}`);
L.push('');
L.push('### KeyItems donnés par événement/PNJ (pas de pickup posé)');
L.push('');
L.push(eventKeyItems.map(k => `- ${k}`).join('\n') || '- (aucun)');
L.push('');
L.push('### Connexions par portails');
L.push('');
for (const e of [...edges].sort()) L.push(`- ${e}`);
L.push('');

// ---------- Sections par zone ----------
const zoneNames = [...zones.keys()].sort();
for (const name of zoneNames) {
  const z = zones.get(name);
  const nChecks = z.pickups.length + z.polaroids.length + z.ghosts.length + z.gulden.length;
  L.push(`## Zone : ${name}`);
  L.push('');
  L.push(`Candidats checks : ${nChecks} (pickups ${z.pickups.length}, polaroids ${z.polaroids.length}, fantômes ${z.ghosts.length}, gulden ${z.gulden.length})`);
  L.push('');
  if (z.pickups.length) {
    L.push('### Pickups');
    for (const p of z.pickups) {
      const what = p.isTool ? `OUTIL ${p.toolType}` : p.keyItems.join(' + ') || '(sans keyItem)';
      const shop = (p.inShop || p.soldByKid) ? ` - BOUTIQUE ${p.cost} gulden${p.soldByKid ? ' (gamin)' : ''}` : '';
      const rep = p.repeatable ? ' - répétable' : '';
      L.push(`- ${what}${shop}${rep} - \`${p.path}\`${conf(p.path)}`);
    }
    L.push('');
  }
  if (z.gulden.length) {
    L.push('### Gulden posés');
    for (const g of z.gulden) L.push(`- \`${g.path}\`${conf(g.path)}`);
    L.push('');
  }
  if (z.polaroids.length) {
    L.push('### Polaroids');
    for (const p of z.polaroids) L.push(`- ${p.type} - \`${p.path}\`${conf(p.path)}`);
    L.push('');
  }
  if (z.ghosts.length) {
    L.push('### Fantômes');
    for (const g of z.ghosts) L.push(`- pos(${g.pos.x}, ${g.pos.z}) - \`${g.path}\`${conf(g.path)}`);
    L.push('');
  }
  if (z.doors.length) {
    L.push('### Portes verrouillées/barrées');
    for (const d of z.doors) L.push(`- ${d.type} - locked:${d.locked} barred:${d.barred} - \`${d.path}\``);
    L.push('');
  }
  if (z.gatedInteractions.length) {
    L.push('### Interactions conditionnées');
    for (const it of z.gatedInteractions) {
      const mode = it.andCheck ? 'ET' : 'OU';
      const refs = Object.keys(it.refs || {}).length ? ` - refs: ${JSON.stringify(it.refs)}` : '';
      L.push(`- ${it.type} [${mode}] ${it.conditions.join(', ')}${refs}`);
    }
    L.push('');
  }
  if (z.hiders.length) {
    L.push('### Visibilité conditionnée (ContentHiders)');
    for (const h of z.hiders) {
      const mode = h.and ? 'ET' : 'OU';
      L.push(`- [${mode}] ${h.conditions.join(', ')}${h.objectRef ? ` - objet: ${h.objectRef}` : ''}`);
    }
    L.push('');
  }
  if (z.portalEdges.size) {
    L.push('### Portails sortants');
    for (const e of [...z.portalEdges].sort()) L.push(`- ${e}`);
    L.push('');
  }
}

// ---------- Écriture des sorties ----------
writeFileSync(`${DIR}/zone_logic.md`, L.join('\n'), 'utf8');

const jsonOut = {};
for (const name of zoneNames) {
  const z = zones.get(name);
  jsonOut[name] = { ...z, portalEdges: [...z.portalEdges] };
}
writeFileSync(`${DIR}/zone_logic.json`, JSON.stringify({
  meta: { generated: new Date().toISOString(), source: dump.meta },
  portalConnections: [...edges].sort(),
  portalEdgesDetailed,
  eventKeyItems,
  zones: jsonOut
}, null, 2), 'utf8');

console.log(`Zones : ${zones.size}`);
console.log(`Connexions portails : ${edges.size}`);
console.log(`KeyItems posés : ${placedKeyItems.size} | par événement : ${eventKeyItems.length}`);
console.log('-> zone_logic.md + zone_logic.json');
