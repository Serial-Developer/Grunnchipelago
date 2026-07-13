using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;

namespace Grunnchipelago
{
    /// <summary>
    /// Dumper de monde pour la conception de l'apworld Grunn.
    /// Parcourt les objets de scene porteurs de logique (pickups, interactions,
    /// hiders, portes, fantomes, pieces, polaroids, polygones de zones) et
    /// exporte le tout en JSON a la racine du jeu.
    /// Declenchement : automatique ~8s apres le chargement du monde, ou F8.
    /// </summary>
    [BepInPlugin("grunnchipelago.dumper", "Grunnchipelago World Dumper", "0.3.0")]
    public class DumperPlugin : BaseUnityPlugin
    {
        private bool dumped;
        private float readyTimer;

        private void Awake()
        {
            Logger.LogInfo("[Grunnchipelago] Dumper charge. Dump auto apres chargement du monde, ou touche F8.");
        }

        private void Update()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.F8))
                {
                    DoDump("manuel (F8)");
                    return;
                }
            }
            catch (Exception)
            {
                // Legacy input indisponible : on se repose sur le dump auto.
            }

            if (dumped) return;
            if (GameManager.allItemPickups == null || GameManager.allItemPickups.Count < 50)
            {
                readyTimer = 0f;
                return;
            }
            readyTimer += Time.deltaTime;
            if (readyTimer >= 8f) DoDump("auto");
        }

        private void DoDump(string trigger)
        {
            dumped = true;
            try
            {
                var root = BuildDump();
                string path = Path.Combine(Paths.GameRootPath, "grunnchipelago_dump.json");
                File.WriteAllText(path, JsonConvert.SerializeObject(root, Formatting.Indented));
                Logger.LogInfo($"[Grunnchipelago] Dump ({trigger}) ecrit : {path}");
            }
            catch (Exception e)
            {
                Logger.LogError("[Grunnchipelago] Echec du dump : " + e);
            }
        }

        private Dictionary<string, object> BuildDump()
        {
            var polys = SceneObjects<AmbienceAreaPolygon>();

            var root = new Dictionary<string, object>
            {
                ["meta"] = new Dictionary<string, object>
                {
                    ["dumper"] = "grunnchipelago.dumper 0.3.0",
                    ["date"] = DateTime.Now.ToString("s"),
                    ["scene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                },
                ["ambienceAreaPolygons"] = DumpPolygons(polys),
                ["itemPickups"] = DumpItemPickups(polys),
                ["interactions"] = DumpInteractions(polys),
                ["contentHiders"] = DumpContentHiders(polys),
                ["doors"] = DumpDoors(polys),
                ["ghosts"] = DumpGhosts(polys),
                ["collectibles"] = DumpCollectibles(polys),
                ["polaroids"] = DumpPolaroids(polys),
                ["portals"] = DumpPortals(polys)
            };
            return root;
        }

        // ---------- Helpers ----------

        private static List<T> SceneObjects<T>() where T : Component
        {
            var list = new List<T>();
            foreach (var o in Resources.FindObjectsOfTypeAll<T>())
            {
                if (o != null && o.gameObject.scene.IsValid())
                    list.Add(o);
            }
            return list;
        }

        private static string GetPath(Transform t)
        {
            var parts = new List<string>();
            while (t != null)
            {
                parts.Insert(0, t.name);
                t = t.parent;
            }
            return string.Join("/", parts);
        }

        private static Dictionary<string, object> Pos(Vector3 p)
        {
            return new Dictionary<string, object>
            {
                ["x"] = Math.Round(p.x, 2),
                ["y"] = Math.Round(p.y, 2),
                ["z"] = Math.Round(p.z, 2)
            };
        }

        private static List<string> AreasAt(List<AmbienceAreaPolygon> polys, Vector3 pos)
        {
            var areas = new List<string>();
            foreach (var p in polys)
            {
                try
                {
                    if (p.polygon != null && p.polygon.ContainsPoint(pos))
                        areas.Add(p.myAmbienceArea.ToString());
                }
                catch (Exception) { }
            }
            var gm = GameManager.instance;
            if (gm != null)
            {
                try
                {
                    if (gm.startGardenAreaBounds != null && gm.startGardenAreaBounds.ContainsPoint(pos)) areas.Add("MACRO:StartGarden");
                    if (gm.churchAreaBounds != null && gm.churchAreaBounds.ContainsPoint(pos)) areas.Add("MACRO:Church");
                    if (gm.roadAreaBounds != null && gm.roadAreaBounds.ContainsPoint(pos)) areas.Add("MACRO:Road");
                    if (gm.parkAreaBounds != null && gm.parkAreaBounds.ContainsPoint(pos)) areas.Add("MACRO:Park");
                }
                catch (Exception) { }
            }
            return areas;
        }

        private static List<string> EnumNames<T>(List<T> values)
        {
            var names = new List<string>();
            if (values != null)
                foreach (var v in values) names.Add(v.ToString());
            return names;
        }

        // ---------- Dumps par type ----------

        private List<object> DumpPolygons(List<AmbienceAreaPolygon> polys)
        {
            var list = new List<object>();
            foreach (var p in polys)
            {
                list.Add(new Dictionary<string, object>
                {
                    ["area"] = p.myAmbienceArea.ToString(),
                    ["path"] = GetPath(p.transform),
                    ["isVisitableLocation"] = p.isVisitableLocation,
                    ["pos"] = Pos(p.transform.position)
                });
            }
            return list;
        }

        private List<object> DumpItemPickups(List<AmbienceAreaPolygon> polys)
        {
            var list = new List<object>();
            foreach (var ip in SceneObjects<ItemPickup>())
            {
                var pos = ip.transform.position;
                list.Add(new Dictionary<string, object>
                {
                    ["path"] = GetPath(ip.transform),
                    ["pos"] = Pos(pos),
                    ["areas"] = AreasAt(polys, pos),
                    ["keyItems"] = EnumNames(ip.keyItemObtain),
                    ["specialItemTypes"] = EnumNames(ip.specialItemTypes),
                    ["isTool"] = ip.isTool,
                    ["toolType"] = ip.isTool ? ip.toolType.ToString() : null,
                    ["isGulden"] = ip.isGulden,
                    ["isGuldenOutsideGarden"] = ip.isGuldenOutsideGarden,
                    ["inShop"] = ip.inShop,
                    ["soldByKid"] = ip.soldByKid,
                    ["cost"] = ip.cost,
                    ["isRepeatablePickup"] = ip.isRepeatablePickup,
                    ["grabType"] = ip.myGrabType.ToString(),
                    ["startState"] = ip.startState.ToString(),
                    ["hideInDemo"] = ip.hideInDemo
                });
            }
            return list;
        }

        private List<object> DumpInteractions(List<AmbienceAreaPolygon> polys)
        {
            var list = new List<object>();
            foreach (var it in SceneObjects<Interaction>())
            {
                var pos = it.transform.position;
                var refs = new Dictionary<string, object>();
                if (it.doorReference != null)
                    refs["door"] = new Dictionary<string, object>
                    {
                        ["path"] = GetPath(it.doorReference.transform),
                        ["locked"] = it.doorReference.locked,
                        ["barred"] = it.doorReference.barred,
                        ["type"] = it.doorReference.type.ToString()
                    };
                if (it.itemReference != null) refs["itemPickup"] = GetPath(it.itemReference.transform);
                if (it.polaroidReference != null) refs["polaroid"] = it.polaroidReference.polaroidType.ToString();
                if (it.ghostReference != null) refs["ghost"] = GetPath(it.ghostReference.transform);
                if (it.busReference != null) refs["bus"] = true;
                if (it.bedReference != null) refs["bed"] = true;
                if (it.computerReference != null) refs["computer"] = true;
                if (it.carTrunkReference != null) refs["carTrunk"] = it.carTrunkReference.myTrunkType.ToString();
                if (it.leverReference != null) refs["lever"] = true;
                if (it.orbReference != null) refs["orb"] = true;
                if (it.ownerReference != null) refs["owner"] = true;
                if (it.ownerSavedReference != null) refs["ownerSaved"] = true;
                if (it.cardboardBoxReference != null) refs["cardboardBox"] = it.cardboardBoxReference.myBoxType.ToString();
                if (it.dogReference != null) refs["dog"] = true;
                if (it.fishbowlReference != null) refs["fishbowl"] = true;
                if (it.snailReference != null) refs["snail"] = true;
                if (it.humanReference != null) refs["human"] = GetPath(it.humanReference.transform);
                if (it.objectActiveRef != null) refs["objectActiveRef"] = it.objectActiveRef.name;

                list.Add(new Dictionary<string, object>
                {
                    ["path"] = GetPath(it.transform),
                    ["pos"] = Pos(pos),
                    ["areas"] = AreasAt(polys, pos),
                    ["type"] = it.interactionType.ToString(),
                    ["preventTypes"] = EnumNames(it.preventTypes),
                    ["preventAndCheck"] = it.preventAndCheck,
                    ["keyItemObtainedRef"] = it.keyItemObtainedRef.ToString(),
                    ["refs"] = refs
                });
            }
            return list;
        }

        private List<object> DumpContentHiders(List<AmbienceAreaPolygon> polys)
        {
            var list = new List<object>();
            foreach (var ch in SceneObjects<ContentHider>())
            {
                var pos = ch.transform.position;
                list.Add(new Dictionary<string, object>
                {
                    ["path"] = GetPath(ch.transform),
                    ["pos"] = Pos(pos),
                    ["areas"] = AreasAt(polys, pos),
                    ["hideConditions"] = EnumNames(ch.hideConditions),
                    ["hideConditionsAnd"] = ch.hideConditionsAnd,
                    ["keyItemRef"] = ch.keyItemRef.ToString(),
                    ["dayIndexCheck"] = ch.dayIndexCheck,
                    ["hourStart"] = ch.hourStart,
                    ["hourEnd"] = ch.hourEnd,
                    ["eventType"] = ch.myEventType.ToString(),
                    ["objectRef"] = ch.objectRef != null ? ch.objectRef.name : null,
                    ["itemPickupRef"] = ch.itemPickupRef != null ? GetPath(ch.itemPickupRef.transform) : null
                });
            }
            return list;
        }

        private List<object> DumpDoors(List<AmbienceAreaPolygon> polys)
        {
            var list = new List<object>();
            foreach (var d in SceneObjects<Door>())
            {
                var pos = d.transform.position;
                list.Add(new Dictionary<string, object>
                {
                    ["path"] = GetPath(d.transform),
                    ["pos"] = Pos(pos),
                    ["areas"] = AreasAt(polys, pos),
                    ["locked"] = d.locked,
                    ["barred"] = d.barred,
                    ["type"] = d.type.ToString(),
                    ["startState"] = d.startState.ToString(),
                    ["unlockItemNeeded"] = EnumNames(d.unlockItemNeeded)
                });
            }
            return list;
        }

        private List<object> DumpGhosts(List<AmbienceAreaPolygon> polys)
        {
            var list = new List<object>();
            foreach (var g in SceneObjects<Ghost>())
            {
                var pos = g.transform.position;
                list.Add(new Dictionary<string, object>
                {
                    ["path"] = GetPath(g.transform),
                    ["pos"] = Pos(pos),
                    ["areas"] = AreasAt(polys, pos)
                });
            }
            return list;
        }

        private List<object> DumpCollectibles(List<AmbienceAreaPolygon> polys)
        {
            var list = new List<object>();
            foreach (var c in SceneObjects<Collectible>())
            {
                var pos = c.transform.position;
                list.Add(new Dictionary<string, object>
                {
                    ["path"] = GetPath(c.transform),
                    ["pos"] = Pos(pos),
                    ["areas"] = AreasAt(polys, pos),
                    ["type"] = c.myType.ToString(),
                    ["value"] = c.value
                });
            }
            return list;
        }

        private List<object> DumpPolaroids(List<AmbienceAreaPolygon> polys)
        {
            var list = new List<object>();
            foreach (var p in SceneObjects<Polaroid>())
            {
                var pos = p.transform.position;
                list.Add(new Dictionary<string, object>
                {
                    ["path"] = GetPath(p.transform),
                    ["pos"] = Pos(pos),
                    ["areas"] = AreasAt(polys, pos),
                    ["polaroidType"] = p.polaroidType.ToString(),
                    ["hideInDemo"] = p.hideInDemo
                });
            }
            return list;
        }

        private List<object> DumpPortals(List<AmbienceAreaPolygon> polys)
        {
            var list = new List<object>();
            foreach (var p in SceneObjects<Portal>())
            {
                var pos = p.transform.position;
                list.Add(new Dictionary<string, object>
                {
                    ["path"] = GetPath(p.transform),
                    ["pos"] = Pos(pos),
                    ["areas"] = AreasAt(polys, pos),
                    ["linkedPortal"] = p.linkedPortal != null ? GetPath(p.linkedPortal.transform) : null,
                    ["teleportToNormalSpace"] = p.teleportToNormalSpace
                });
            }
            return list;
        }
    }
}
