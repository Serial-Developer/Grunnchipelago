# Grunnchipelago — Document de design de l'APWorld

Synthèse des décisions de design (Jonath × Claude, juillet 2026).
Références : `regions_v3.md` (graphe/logique), `zone_logic.md` (données exhaustives),
`analysis/decompiled/` (code du jeu).

## 1. Vue d'ensemble

- **Jeu** : Grunn (Sokpop Collective / Tom van den Boogaart, 2024), Unity Mono x64
- **Architecture** : mod client BepInEx 5 (C#, Archipelago.MultiClient.Net) + apworld Python
- **Structure du jeu** : runs en boucle (semaine in-game), 11 fins, 3 macro-zones
  jardinables + zones spéciales, ~55 KeyItems, 6 outils
- **Principe d'identité des locations** : 1 location par item logique (KeyItem/outil),
  pas par emplacement — conséquence naturelle du hook central `ObtainKeyItem` qui
  déduplique par enum. Les items multi-emplacements (Lighter ×4, OfficeKey ×2,
  MagicSword ×2) = plusieurs routes vers la même location (règle OR).

## 2. Goals (option YAML `goal`)

| Valeur | Condition de victoire | Détection |
|---|---|---|
| `good_ending` | Atteindre la bonne fin | `TriggerEnding(GoodEnd)` |
| `true_ending` (défaut proposé) | Bonne fin après restauration de l'âme (3 SoulFragments) | `TriggerEnding(GoodEnd)` + `progressData.restoredOwnerSoul` |
| `all_endings` | Voir les 11 fins (DemoEnding exclu) | `SeenAllEndingTypes()` |

## 3. Pools de locations

| Pool | Taille | Option YAML | Défaut | Notes |
|---|---|---|---|---|
| Outils | 6 | `keep_shears` (sécateurs vanilla) | off | Trowel, Shears, WateringCan, Hammer, Trumpet, MagicSword |
| Objets (KeyItems) | ~45 | — (toujours actif) | — | tous les KeyItems hors clés/outils/exclusions |
| Clés | 9 | `exclude_bridge_key` | **on** | BridgeKey vanilla par défaut — LA première clé (spawn -> jardin) ; sans elle, 2 checks seulement en sphère 1 |
| Polaroids | 34 | `polaroid_checks` | on (à confirmer) | Ending* exclus (attribués par les fins, non randomisés) |
| Fantômes | 8 | `ghost_checks` | on (à confirmer) | event checks — aucun item vanilla à remplacer |
| Fins | 11 | toujours actif | — | chaque fin vue = 1 check ; sert aussi le goal |
| Pièces (gulden) | 15 posés | `coinsanity` | off | logique monétaire : coût boutique vs count("Gulden") ; tonte = revenu vanilla renouvelable |

KeyItems donnés par événement (GoldFishAlive, KidTriangle) : inclus — le hook central
les capte comme les autres.

## 4. Pool d'items

### Progression (impactent la logique)
Plank, OldPlank, Shears, MagicSword, WateringCan, Coin (pièce bleue), Lighter, Hammer,
Compass, Cd, OfficeKey, ChurchKey, Paddle, ToiletKey, ToiletPaper, Worm, StrangeKey,
Bone, GnomeIdol, TallIdol, ShortIdol, ShyIdol, FlowerGem, SoulFragment1/2/3, Trowel,
Blanket, Sandwich, GardenKey (si non exclue), Apple, SpecialSeed, ToyBoat, BridgeKey,
Doorknob, Corn/Butter (chaîne popcorn), Trumpet/Cymbals + KidTrumpet/KidCymbals/KidTriangle.
Classification progression/utile à affiner en écrivant rules.py.

### Utile (sans impact logique)
KeyItems restants (PizzaBox, SeveredHand, Medal, Eggball, PrettyFlower, GoldFish*,
Popcorn, PurifiedStone, AbandonedKey, AtticKey, OldKey…) — certains remonteront en
progression selon les règles finales.

### Buffs (items AP inédits, injectés par le mod)
- **Vitesse de déplacement** (progressif, ex. ×3 paliers) — patch PlayerControllerNew
- **Portée du sécateur** (progressif) — paramètres de CreateGrassCutter(Single, Int32)
- **Cadence de découpe** (progressif) — idem
- Extensible plus tard (portée d'interaction, etc.)

### Traps
- **Debuff vitesse / taille** — durée 1h in-game (TimeController.currentHour)
- **Contrôles inversés** — 1h in-game — patch MouseLookNew/InputManager
- **Regrow** — remet à zéro UN élément d'UNE zone (herbe, arrosage, haie, détritus,
  taupes) — vidage ciblé des listes de positions (grassCutPosition, etc.) +
  décrément AreaProgress ; comportement du rechargement à tester en pratique

### Fillers
- Gulden (si coinsanity) ; sinon petits bonus économiques ou buffs mineurs
- Quantités buffs/traps/fillers : options YAML (`trap_percentage` etc.) à calibrer
  quand le compte exact locations vs items sera généré

## 5. Persistance (décision arrêtée)

- **Inventaire AP** : réinjecté à chaque nouvelle run — postfix sur `TriggerNewRun()`
  après `ResetRunProgress()` ; le serveur AP rejoue de toute façon tous les items à la
  connexion (pattern Outer Wilds)
- **Checks** : acquis côté serveur, cache local anti-doublon
- **Flags de monde** : reset vanilla à chaque run (les fins exigent des états frais ;
  le graphe HideCondition contient beaucoup de conditions négatives)
- **Option `persistent_shortcuts`** (défaut : **off**) : restaure après reset la liste
  blanche de confort — unlockedBijkeukenShortcut, unlockedIntratuin, createdShortcut,
  parkUnlockedHooibaalGarden, parkUnlockedMaze, locksUnlocked
- Pas d'option de persistance totale (décision Jonath)

## 6. Logique (voir regions_v3.md pour le graphe complet)

### Équivalences
- Couper l'herbe : `Shears | MagicSword`
- Arroser : `WateringCan | (Coin + accès Église)` [pluie/BadWeather]
- Planche : `Plank | OldPlank`
- Os : casser un squelette (`Hammer | MagicSword | Trowel`) | accès Manoir via bureau

### États de monde dans les règles
- **Jours/heures** (`DayIndexIs`, `InTimeWindow`, `IsFinalDay`) : logiquement LIBRES —
  dormir fait avancer le temps sans prérequis
- **Scénarios** (`InRedWorld`, `InGoodEnding`, SnowWorld…) : event locations ou régions
  d'événement — à modéliser au cas par cas dans rules.py
- **Complétion de zones** (20% / 100% / MaintainedArea) : dépend de la capacité à
  jardiner la zone → herbe (équivalence ci-dessus) + arrosage + accès outils selon
  les tâches de la zone
- **Épée du parc** (post-bonne-fin) : ignorée par la logique — la source logique de
  MagicSword est le grenier du Manoir (chaîne du disque)

### Points d'attention logique
- L'Église a DEUX accès (portail jardin GardenKey — la clé de la buanderie —, ou
  Extérieur libre) ; ChurchKey (hall du Manoir) ouvre la porte INTÉRIEURE de l'église
  (crypte). BridgeKey = première clé (spawn -> jardin) -> ChurchKey/GardenKey non critiques
- L'Extérieur a DEUX accès depuis le jardin (pont Plank|OldPlank, haie Shears|MagicSword)
- Économie sans coinsanity : la tonte suffit (sécateurs → gulden infinis) pour bus (10),
  Cd (5), Compass (4), OfficeKey (2), Medal (10)

## 7. Options YAML — récapitulatif

| Option | Type | Défaut | Effet |
|---|---|---|---|
| `goal` | choix | true_ending (proposé) | good_ending / true_ending / all_endings |
| `keep_shears` | toggle | off | sécateurs vanilla (accessibilité early) |
| `exclude_bridge_key` | toggle | **on** | BridgeKey vanilla (première clé, spawn -> jardin) |
| `polaroid_checks` | toggle | on | 34 polaroids de monde en checks |
| `ghost_checks` | toggle | on | 8 fantômes en checks |
| `coinsanity` | toggle | off | 15 gulden posés en checks + logique monétaire |
| `persistent_shortcuts` | toggle | off | restaure les raccourcis entre les runs |
| `trap_percentage` etc. | range | à calibrer | proportion buffs/traps/fillers |
| `death_link` | toggle | off (convention AP) | STRICT : toute mort envoie un DeathLink, tout DeathLink reçu tue — aucun filtrage (décision Jonath) |

## 8. Hooks techniques (référence mod client)

| Hook | Type | Rôle |
|---|---|---|
| `GameManager.ObtainKeyItem(KeyItem, bool)` | Prefix | check à l'obtention + interception de l'octroi vanilla |
| `PlayerManager.AddTool(Item)` | Prefix | idem pour les 6 outils (chemin séparé) |
| `GameManager.TriggerEnding(EndingType)` | Postfix | checks de fins + détection du goal |
| `Ghost.Touch()` | Postfix | checks fantômes (ID par position triée) |
| `SaveManager.AddPolaroidCollected` | Postfix | checks polaroids (filtre Ending*) |
| `GameManager.TriggerNewRun()` | Postfix | réinjection inventaire AP + persistent_shortcuts |
| `GameManager.AddGulden(int)` / pickups isGulden | Prefix | coinsanity |
| `TriggerItemObtainPopup(KeyItem)` | appel direct | affichage des items reçus du multiworld |
| `GameManager.SetNightmareState(NightmareState)` | Postfix | DeathLink : déclenchement du cauchemar = mort (états exacts à affiner au dev) ; réception DeathLink = déclencher le cauchemar |

## 9. Étapes suivantes

1. Génération des IDs items/locations depuis les données (script -> items/locations apworld)
2. Prompt Claude Code : scaffold mod client (BepInEx + MultiClient.Net + hooks) 
3. Prompt Claude Code : scaffold apworld (regions.py généré depuis regions_v3 + zone_logic)
4. Test bout en bout solo, puis calibrage buffs/traps
5. Post de présentation Discord AP (#future-game-design)
