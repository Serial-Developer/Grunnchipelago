# Analyse 1 : validation et statistiques du dump Grunn
$ErrorActionPreference = 'Stop'
$dst = 'C:\Users\jonat\Desktop\Projets\Grunnchipelago\dump\grunnchipelago_dump.json'
$d = Get-Content $dst -Raw | ConvertFrom-Json

"=== COMPTES ==="
"polygones zones : $($d.ambienceAreaPolygons.Count)"
"itemPickups     : $($d.itemPickups.Count)"
"interactions    : $($d.interactions.Count)"
"contentHiders   : $($d.contentHiders.Count)"
"doors           : $($d.doors.Count)"
"ghosts          : $($d.ghosts.Count)"
"collectibles    : $($d.collectibles.Count)"
"polaroids       : $($d.polaroids.Count)"
"portals         : $($d.portals.Count)"

"=== QUALITE RESOLUTION ZONES ==="
$noArea = @($d.itemPickups | Where-Object { $_.areas.Count -eq 0 }).Count
"pickups sans zone resolue : $noArea / $($d.itemPickups.Count)"

"=== PICKUPS ==="
$gulden = @($d.itemPickups | Where-Object { $_.isGulden })
"pickups gulden : $($gulden.Count)"
$shop = @($d.itemPickups | Where-Object { $_.inShop -or $_.soldByKid })
"pickups boutique : $($shop.Count)"
$withKey = @($d.itemPickups | Where-Object { $_.keyItems.Count -gt 0 })
"pickups avec keyItem : $($withKey.Count)"
$tools = @($d.itemPickups | Where-Object { $_.isTool })
"pickups outil : $($tools.Count) -> $(($tools | ForEach-Object { $_.toolType }) -join ', ')"

"=== INTERACTIONS AVEC CONDITIONS ==="
$gated = @($d.interactions | Where-Object { $_.preventTypes.Count -gt 0 })
"interactions gatees : $($gated.Count) / $($d.interactions.Count)"
"Top preventTypes :"
$gated | ForEach-Object { $_.preventTypes } | Group-Object | Sort-Object Count -Descending | Select-Object -First 20 | ForEach-Object { "  $($_.Count)x $($_.Name)" }

"=== HIDECONDITIONS ==="
"Top hideConditions :"
$d.contentHiders | ForEach-Object { $_.hideConditions } | Group-Object | Sort-Object Count -Descending | Select-Object -First 20 | ForEach-Object { "  $($_.Count)x $($_.Name)" }

"=== COLLECTIBLES PAR TYPE ==="
$d.collectibles | Group-Object type | ForEach-Object { "  $($_.Count)x $($_.Name)" }

"=== ZONES PRESENTES ==="
($d.ambienceAreaPolygons | ForEach-Object { $_.area } | Sort-Object -Unique) -join ', '
