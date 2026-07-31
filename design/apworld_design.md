# Grunnchipelago — Document de design de l'APWorld

Synthèse des décisions de design (Jonath, juillet 2026).
Références : `regions.md` (graphe/logique), `zone_logic.md` (données exhaustives),
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

### Traps (8, refonte 2026-07-27 — demande Jonath)
Temporisés — 2 h in-game (`TimeController.currentHour`), expirent au changement de jour :
- **Speed Trap** / **Size Trap** — debuff vitesse (×0.5) / taille (×0.45)
- **Inverted Controls Trap** — patch MouseLookNew/InputManager

One-shot (les 4 anciens « regrow un élément dans une zone au hasard » ont été refondus) :
- **Garden Reset Trap** — le jardin de départ retombe à **0 %** : herbe, taupinières,
  haie, fleurs à arroser et déchets tous restaurés
- **Church Reset Trap** — idem Église (herbe, taupinières, fleurs)
- **Park Reset Trap** — idem Parc (herbe, taupinières, fleurs, déchets)
- **Night Trap** — met directement l'heure à **03h00**
- **Sacred Flower Trap** — coupe **4 fleurs sacrées** (son compris). Conséquence voulue :
  ≥ 1 fleur déjà coupée ⇒ seuil de 5 atteint ⇒ **fin « fleurs sacrées » déclenchée** ;
  sinon la toute prochaine fleur coupée par le joueur la déclenche (seuil vanilla).

> **L'herbe est bien incluse dans les resets de zone.** Elle est rendue par le système
> DOTS `GrassSystem`, mais le jeu la reconstruit **à chaud** dans `GameManager.ResetWorld`
> (GameManager.cs:4064) : `GrassManager.ClearEntities()` + `Reset()` + `CornManager.Reset()`,
> puis les coupes du save sont rejouées via `performedLoadOperations` /
> `PerformLoadOperations` (GameManager.cs:874) et `GrassSystem` (GrassSystem.cs:483-572),
> sans ré-écriture ni son. Le client emprunte exactement ce chemin
> (`Effects.ResetGrassInArea`), d'où le retrait de `UNIMPLEMENTED_ITEMS`.

⚠️ Les **ids** (478660301..308) n'ont pas changé au renommage ; le client accepte aussi
les anciens noms pour qu'une seed antérieure au 2026-07-27 reste jouable (`GameIds`).

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
  blanche de confort — unlockedBijkeukenShortcut, createdShortcut,
  parkUnlockedHooibaalGarden, parkUnlockedMaze, locksUnlocked.
  ⚠ `unlockedIntratuin` EXCLU (session 2) : restaurer ce flag colore l'emblème de la
  porte aux fleurs mystérieuses mais empêche le ré-arrosage de l'ouvrir.
- Pas d'option de persistance totale (décision Jonath)
- **Profil de sauvegarde par seed** (session 2, 3.1 — `SaveProfile.cs`) : connecté, le
  mod redirige le PRÉFIXE `SaveManager.savePath` vers `grunn_ap_<seed>_<slot>`, au
  menu titre uniquement. La sauvegarde vanilla du joueur n'est jamais touchée et
  chaque seed a son propre monde (fins, polaroids, runs, raccourcis à zéro).
  Les réglages restent partagés (`settingsPath` distinct).

## 6. Logique (voir regions.md pour le graphe complet)

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
| `death_link` | toggle | off (convention AP) | V2 (décision Jonath, 2026-07) : les morts SONT les fins — envoi sur les 8 fins-morts (Mist, SacredFlowers, Drown, Darkness, LongHallway, HedgeMaze, WorldEnd, Dog), jamais Bus/Picnic/GoodEnd ; réception = reset de run SANS fin déclenchée ni check (anti-farm all_endings, anti-boucle) |

## 8. Hooks techniques (référence mod client)

| Hook | Type | Rôle |
|---|---|---|
| `GameManager.ObtainKeyItem(KeyItem, bool)` | Prefix | check à l'obtention + interception de l'octroi vanilla |
| `PlayerManager.AddTool(Item)` | Prefix | idem pour les 6 outils (chemin séparé) |
| `GameManager.TriggerEnding(EndingType)` | Postfix | checks de fins + détection du goal |
| `Ghost.Touch()` | Postfix | checks fantômes (ID par chemin de scène gelé depuis ids.json — V2, immune au déplacement) |
| `SaveManager.AddPolaroidCollected` | Postfix | checks polaroids (filtre Ending*) |
| `GameManager.TriggerNewRun()` | Postfix | réinjection inventaire AP + persistent_shortcuts |
| `GameManager.AddGulden(int)` / pickups isGulden | Prefix | coinsanity |
| `TriggerItemObtainPopup(KeyItem)` | appel direct | affichage des items reçus du multiworld |
| DeathLink V2 (via `TriggerEnding`) | Postfix | envoi sur les 8 fins-morts ; réception = reset de run sans fin ni check ; séquence de réception custom : noir 1 s + screamer 2 s (`nightmareFactorCur` forcé à 0.2 pendant le screamer) |

## 9. Étapes suivantes

1. Génération des IDs items/locations depuis les données (script -> items/locations apworld)
2. Scaffold du mod client (BepInEx + MultiClient.Net + hooks) 
3. Scaffold de l'apworld (regions.py généré depuis regions.md + zone_logic)
4. Test bout en bout solo, puis calibrage buffs/traps
5. Post de présentation Discord AP (#future-game-design)

## 10. Features Jonath — triage (2026-07-13)

> Statuts (CC, 2026-07-14) : #3 Bone spawn ✅ livré (d6c897f) ; #4 popup ✅ livré
> (d6c897f) ; #5 sync polaroids ✅ livré (d6c897f) ; #1 modèles concrets ✅ livré
> (mécanique complète, bibliothèque depuis les visualsObject de scène).
> ⚠️ Mise à jour 2026-07-21 — les « approximations » notées ici en 2026-07-14 sont
> PÉRIMÉES, les itérations suivantes ont récolté les vrais modèles :
> **GoldFishAlive** = visuel du poisson VIVANT de la scène
> (`MagicPond_FishAlive_Content` / `FishAliveContainer`), le poisson mort n'est plus
> qu'un *fallback* ; **KidTriangle** = triangle tenu en main par l'homme hirsute
> (`scruffyMan_triangleStick0`), donc plus « non couvert » ; **clés orphelines**
> (AbandonedKey / OldKey / AtticKey, sans pickup posé) = *fallback générique* vers le
> premier modèle de clé récolté, les clés de Grunn se ressemblant toutes.
> ; #2 modèles AP ✅ mécanique livrée (classification + traps déguisés
> déterministes + items d'autres joueurs), direction artistique PROVISOIRE
> (polaroid teinté rouge/bleu/gris) — Jonath tranche la DA finale.

### Pré-playtest (bloquants)
- **Sync des polaroids (feature #5)** : Grunn n'a qu'un seul fichier de sauvegarde ; sur
  une save terminée, `GlobalData.polaroidsCollected` contient déjà tout -> les 35 checks
  polaroids sont morts. Fix : à la connexion, retirer de la liste collectée (et solved si
  nécessaire) tout polaroid dont le check AP n'est pas encore envoyé, pour que l'objet
  réapparaisse en monde. + AUDIT général : tout autre check gaté par un état GlobalData
  déjà acquis (fantômes ? gulden ?) doit être resynchronisé de la même façon.
- **Items « cadeaux » près du spawn (feature #3)** : certains items reçus ne sont **jamais
  injectés en inventaire** ; le mod fait apparaître un pickup monde à côté du panneau des
  roses, au point de départ. Le joueur le prend seulement au besoin. Ces pickups spéciaux
  n'envoient **aucun check** et, la possession étant per-run, ils **réapparaissent à chaque
  nouvelle run**. Trois items, chacun pour la même raison — les posséder tue un contenu :
  | Item | Ce que la possession tuait |
  |---|---|
  | **Bone** | fin **Dog** (le chien ne tue plus) |
  | **Compass** | labyrinthe sans boussole et fin **HedgeMaze** |
  | **StrangeKey** | fin **LongHallway** : la porte de l'OrbRoom se déverrouille sur simple possession (`Door.PlayerHasUnlockItem`, Door.cs:910) et l'assaillant n'est armé que tant qu'elle est verrouillée (Door.cs:770) [J 2026-07-27] |
- **Texte du popup de ramassage (feature #4, partie légère)** : le popup vanilla affiche
  l'objet VU, pas l'objet reçu (ex. pagaie ramassée, triangle obtenu, message « pagaie »).
  Fix : supprimer le popup vanilla au ramassage intercepté et afficher le vrai contenu
  (item reçu, ou « Envoyé : X -> joueur » en multi).

### Backlog post-v1 (immersion)
- **Modèles physiques fidèles (feature #1)** : afficher à l'emplacement du pickup le
  modèle 3D de l'item réellement contenu (pour les items Grunn locaux).
- **Modèles « Archipelago » (feature #2)** : en multiworld, modèles distincts par
  classification (filler / useful / progression), et traps déguisés aléatoirement en
  useful ou progression — pattern classique des grands randomizers.
