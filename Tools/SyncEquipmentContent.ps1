param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$recipesDir = Join-Path $ProjectRoot 'Assets/Crafting/Recipes'
$catalogPath = Join-Path $ProjectRoot 'Assets/Crafting/RecipeCatalog.asset'
$accidentalWeaponRecipeGuids = @()
Get-ChildItem $recipesDir -Filter 'Recipe_Weapon_Lvl*.asset.meta' -ErrorAction SilentlyContinue | ForEach-Object {
    $accidentalWeaponRecipeGuids += ((Get-Content $_.FullName | Select-String '^guid: ').Line -replace '^guid: ','')
    Remove-Item -LiteralPath ($_.FullName -replace '\.meta$','')
    Remove-Item -LiteralPath $_.FullName
}

function Equipment([string]$Folder, [int]$FileLevel, [string]$Id, [string]$Name, [int]$RequiredLevel, [string]$Description, [string]$Npc, [array]$Ingredients, [hashtable]$Stats) {
    [pscustomobject]@{ Folder=$Folder; FileLevel=$FileLevel; Id=$Id; Name=$Name; RequiredLevel=$RequiredLevel; Description=$Description; Npc=$Npc; Ingredients=$Ingredients; Stats=$Stats }
}
function Ingredient([string]$Id, [int]$Quantity) { [pscustomobject]@{ Id=$Id; Quantity=$Quantity } }

$A='npc.town.artificer'; $H='npc.town.hermetist'
$W='item.material.wood_log'; $L='item.material.leather'; $Cu='item.material.copper_bar'; $Fe='item.material.iron_bar'; $St='item.material.steel_bar'; $Au='item.material.gold_bar'; $Sn='item.material.tin_bar'; $Di='item.material.diamond_bar'
$Ta='item.material.tanzanite'; $Ci='item.material.citrine'; $Ch='item.material.charoite'; $Em='item.material.emerald'; $Op='item.material.opal'

$items = @(
    (Equipment Weapon 1 sword_lvl1 'Training Sword' 1 'No need to sharpen it. Light and easy to swing. A first friend to every great legend... just a log shaped like a “sword”.' $A @((Ingredient $W 5)) @{attackDamage=2}),
    (Equipment Weapon 2 sword_lvl2 'Brasstan Edge' 5 'A heavy hand, a poor point, but a fine bite...wel, it is a sword after all. A rough thing, but the smith knew what he was doing.' $A @((Ingredient sword_lvl1 1),(Ingredient $Cu 10)) @{attackDamage=4}),
    (Equipment Weapon 3 sword_lvl3 "Bastard's Greetings" 10 'A sharp ringing challenge, a final farewell to those who dare answer.' $A @((Ingredient sword_lvl2 1),(Ingredient $Cu 5),(Ingredient $Fe 5)) @{attackDamage=6}),
    (Equipment Weapon 4 sword_lvl4 "Flame's Oath" 15 'An oath forged in honor, tempered by an unyielding will to prevail.' $A @((Ingredient sword_lvl3 1),(Ingredient $Fe 10)) @{attackDamage=9}),
    (Equipment Weapon 5 sword_lvl5 'Shardgleam' 20 'Its dark-grey gleam brings a quiet chill, the kind that makes a hand reach for the hilt.' $A @((Ingredient sword_lvl4 1),(Ingredient $St 10)) @{attackDamage=12}),
    (Equipment Weapon 6 sword_lvl6 'Paladin Wrath' 25 'From the shivering of death, a cleansing wrath is born.' $A @((Ingredient sword_lvl5 1),(Ingredient $Au 5),(Ingredient $St 10)) @{attackDamage=16}),
    (Equipment Weapon 7 sword_lvl7 'Artemis Compass' 30 'A silver edge pierces the night, cleaving the starry veil of chaos.' $A @((Ingredient sword_lvl6 1),(Ingredient $Sn 10)) @{attackDamage=21}),
    (Equipment Weapon 8 sword_lvl8 'Ishtar Vela' 35 'Beneath the stillness of dusk, a blessing falls like an arrow from the evening star, and the chaos fades into silence.' $A @((Ingredient sword_lvl7 1),(Ingredient $Sn 15),(Ingredient $Au 20)) @{attackDamage=27}),
    (Equipment Weapon 9 sword_lvl9 'Adamas' 40 'Unbreakable, unyielding, eternal— the arm of the God of Conquest. Chaos breaks beneath its wrath, while the battle-mad feast upon the glory of war.' $A @((Ingredient sword_lvl8 1),(Ingredient $Di 10),(Ingredient $Au 30)) @{attackDamage=35}),

    (Equipment Body 2 body_lvl2 "Hunter's Habit" 5 'A whole hide, rough and ragged as it is, yet there is something of the old forest in its embrace.' $A @((Ingredient $L 10)) @{maxHealth=5;defense=1}),
    (Equipment Body 3 body_lvl3 'Copper Armor' 10 "Rough, heavy, and far from pretty. But sturdy enough, and it gets the job done." $A @((Ingredient body_lvl2 1),(Ingredient $Cu 10)) @{maxHealth=8;defense=2}),
    (Equipment Body 4 body_lvl4 'Iron Armor' 15 'Heavy and plain, with little care for appearances. Solid enough to take the blow, and sturdy enough to give you another day.' $A @((Ingredient body_lvl3 1),(Ingredient $Fe 10)) @{maxHealth=12;defense=3}),
    (Equipment Body 5 body_lvl5 'Alloy Armor' 20 'Iron for strength, copper for balance. A little rough, a little heavy, but tougher than either metal alone.' $A @((Ingredient body_lvl4 1),(Ingredient $Fe 15),(Ingredient $Cu 5)) @{maxHealth=18;defense=5}),
    (Equipment Body 6 body_lvl6 'Ebony Baron' 25 'Dark, weighty, and finely wrought. No longer mere armor, but a mark of standing. Wear it, and let the silence speak for itself.' $A @((Ingredient body_lvl5 1),(Ingredient $St 10)) @{maxHealth=25;defense=7}),
    (Equipment Body 7 body_lvl7 'Ignitium Crucifix' 30 'Ash rode the wind, heavy with the stench of battle. Flames roared, gorging on blackened flesh. A black coffin dragged itself through the carnage, its every step sounding the drums of triumph.' $A @((Ingredient body_lvl6 1),(Ingredient $Au 5),(Ingredient $St 15)) @{maxHealth=35;defense=10}),
    (Equipment Body 8 body_lvl8 'Artemis Grace' 35 'Silver light pours down, cleansing blood and woe, watering the sacred garden into bloom. Life takes root, spreads without end, and where it reaches, ruin is no more.' $A @((Ingredient body_lvl7 1),(Ingredient $Sn 10)) @{maxHealth=48;defense=14}),
    (Equipment Body 9 body_lvl9 'Adamas Carne' 40 'When flesh is spent and kingdoms fall, when the last flame gutters in the dark, one thing remains — unyielding, unbroken, eternal.' $A @((Ingredient body_lvl8 1),(Ingredient $Di 10)) @{maxHealth=65;defense=19}),

    (Equipment Foot 1 foot_lvl1 "Hunter's Leap" 5 '' $A @((Ingredient $L 5)) @{moveSpeed=.1}),
    (Equipment Foot 2 foot_lvl2 'Copper Legging' 10 '' $A @((Ingredient foot_lvl1 1),(Ingredient $Cu 5)) @{moveSpeed=.12;dodgeChance=.005}),
    (Equipment Foot 3 foot_lvl3 'Iron Legging' 15 '' $A @((Ingredient foot_lvl2 1),(Ingredient $Fe 5)) @{moveSpeed=.14;dodgeChance=.01}),
    (Equipment Foot 4 foot_lvl4 'Alloy Legging' 20 '' $A @((Ingredient foot_lvl3 1),(Ingredient $Fe 8),(Ingredient $Cu 2)) @{moveSpeed=.16;dodgeChance=.015}),
    (Equipment Foot 5 foot_lvl5 'Ebony Territory' 25 '' $A @((Ingredient foot_lvl4 1),(Ingredient $St 5)) @{moveSpeed=.18;dodgeChance=.02}),
    (Equipment Foot 6 foot_lvl6 'Ignitium Crusade' 30 '' $A @((Ingredient foot_lvl5 1),(Ingredient $Au 2),(Ingredient $St 8)) @{moveSpeed=.2;dodgeChance=.025}),
    (Equipment Foot 7 foot_lvl7 'Artemis Hinds' 35 '' $A @((Ingredient foot_lvl6 1),(Ingredient $Sn 5)) @{moveSpeed=.23;dodgeChance=.03}),
    (Equipment Foot 8 foot_lvl8 'Adamas Ossa' 40 '' $A @((Ingredient foot_lvl7 1),(Ingredient $Di 5)) @{moveSpeed=.26;dodgeChance=.04}),

    (Equipment Head 4 head_lvl4 'Copper Helm' 15 '' $A @((Ingredient $Cu 7)) @{defense=3}),
    (Equipment Head 5 head_lvl5 'Iron Armet' 20 '' $A @((Ingredient head_lvl4 1),(Ingredient $Fe 7)) @{defense=5;criticalChance=.005}),
    (Equipment Head 6 head_lvl6 'Ebony Residence' 25 '' $A @((Ingredient head_lvl5 1),(Ingredient $St 7)) @{defense=7;criticalChance=.01}),
    (Equipment Head 7 head_lvl7 'Ignitium Precept' 30 '' $A @((Ingredient head_lvl6 1),(Ingredient $Au 4),(Ingredient $St 7)) @{defense=10;criticalChance=.015}),
    (Equipment Head 8 head_lvl8 'Artemis Veil' 35 '' $A @((Ingredient head_lvl7 1),(Ingredient $Sn 7)) @{defense=14;criticalChance=.02}),
    (Equipment Head 9 head_lvl9 'Adamas Voluntas' 40 '' $A @((Ingredient head_lvl8 1),(Ingredient $Di 7)) @{defense=19;criticalChance=.03}),

    (Equipment Shield 1 shield_lvl1 'Wooden Shield' 5 'A few planks, a bit of rope, and enough courage to stand behind it.' $A @((Ingredient $W 7)) @{defense=1.5}),
    (Equipment Shield 2 shield_lvl2 "Forest's Cogwheel" 10 'Made from what the forest gave, then taught to turn aside what it could not.' $A @((Ingredient shield_lvl1 1),(Ingredient $W 4),(Ingredient $Cu 5)) @{defense=3;damageReduction=.01}),
    (Equipment Shield 3 shield_lvl3 'Bordweall' 15 'A wall upon the arm, built to meet the blow and hold its ground.' $A @((Ingredient shield_lvl2 1),(Ingredient $W 4),(Ingredient $Fe 5)) @{defense=5;damageReduction=.02}),
    (Equipment Shield 4 shield_lvl4 'Ebony Bastion' 20 'A steadfast wall in the thick of battle, where every blow meets its end.' $A @((Ingredient shield_lvl3 1),(Ingredient $St 9)) @{defense=7;damageReduction=.03}),
    (Equipment Shield 5 shield_lvl5 'Ignitium Thrymgeat' 25 'When the battle rages and the heavens roar, one stands firm where none else would. Blow after blow, it holds the line, until the storm itself grows weary.' $A @((Ingredient shield_lvl4 1),(Ingredient $St 7),(Ingredient $Au 5)) @{defense=10;damageReduction=.04}),
    (Equipment Shield 6 shield_lvl6 'Artemis Mirror' 30 'Beneath the pale light, even the fiercest hand may see itself. The mirror does not judge, it merely shows what stands before it.' $A @((Ingredient shield_lvl5 1),(Ingredient $Sn 10)) @{defense=14;damageReduction=.05}),
    (Equipment Shield 7 shield_lvl7 'Adamas Hersian' 35 'The storm may break, the earth may tremble, yet the shield remains before its bearer. Where it stands, the world must find another way.' $A @((Ingredient shield_lvl6 1),(Ingredient $Sn 9),(Ingredient $Di 4)) @{defense=19;damageReduction=.065}),
    (Equipment Shield 8 shield_lvl8 'Gates of Janus' 40 'The gates were cast open with a single blow, and war poured forth upon the world. Fire followed iron, and the old peace was no more.' $A @((Ingredient shield_lvl7 1),(Ingredient $Di 12)) @{defense=25;damageReduction=.08}),

    (Equipment Ring 1 ring_lvl1 'Azure Spark Ring' 5 'A copper ring cradling a violet-blue Tanzanite, glimmering like a tiny spark against the night sky.' $H @((Ingredient $Cu 1),(Ingredient $Ta 1)) @{attackDamage=1;criticalChance=.005}),
    (Equipment Ring 2 ring_lvl2 'Sunshard Ring' 10 'A radiant Citrine set in steel, like a fragment of sunlight captured within cold metal.' $H @((Ingredient $St 1),(Ingredient $Ci 1)) @{attackDamage=2;criticalChance=.01}),
    (Equipment Ring 3 ring_lvl3 'Voidbloom Ring' 15 'A purple Charoite blooms upon a steel band, like a solitary flower in the silent void.' $H @((Ingredient $St 1),(Ingredient $Ch 1)) @{attackDamage=3;criticalChance=.015}),
    (Equipment Ring 4 ring_lvl4 'Verdant Oath Ring' 20 'A wooden ring inlaid with vivid Emerald, humble as a promise beneath ancient trees.' $H @((Ingredient $W 1),(Ingredient $Em 1)) @{attackDamage=4;criticalChance=.02}),
    (Equipment Ring 5 ring_lvl5 'Azurebound Ring' 25 'Twin Tanzanites rest upon a copper band, their violet-blue light entwined like an enduring bond.' $H @((Ingredient $Cu 1),(Ingredient $Ta 2)) @{attackDamage=6;criticalChance=.025}),
    (Equipment Ring 6 ring_lvl6 'Emerald Vanguard Ring' 30 'A sturdy steel band bears two deep-green Emeralds, carrying the solemn elegance of a forest guardian.' $H @((Ingredient $St 2),(Ingredient $Em 2)) @{attackDamage=8;criticalChance=.03}),
    (Equipment Ring 7 ring_lvl7 'Golden Verdure Ring' 35 'Three Emeralds adorn a golden band, like fresh leaves basking in the warmth of sunlight.' $H @((Ingredient $Au 3),(Ingredient $Em 3)) @{attackDamage=11;criticalChance=.035}),
    (Equipment Ring 8 ring_lvl8 'Prismatic Sovereign Ring' 40 "Five gemstones unite upon a gleaming gold band, blending Opal's shifting hues with Tanzanite, Citrine, Charoite, and Emerald." $H @((Ingredient $Au 5),(Ingredient $Ta 1),(Ingredient $Ci 1),(Ingredient $Ch 1),(Ingredient $Em 1),(Ingredient $Op 1)) @{attackDamage=15;criticalChance=.04}),

    (Equipment Necklace 1 necklace_lvl1 'Azure Gleam Necklace' 5 'A Tanzanite hangs from a copper chain, catching violet-blue light like a dewdrop beneath the morning sky.' $H @((Ingredient $Cu 2),(Ingredient $Ta 1)) @{maxHealth=5;healthRegeneration=.1}),
    (Equipment Necklace 2 necklace_lvl2 'Suncrest Necklace' 10 'A warm golden Citrine rests upon an iron chain, like the sun rising above the horizon.' $H @((Ingredient $Fe 2),(Ingredient $Ci 1)) @{maxHealth=8;healthRegeneration=.2}),
    (Equipment Necklace 3 necklace_lvl3 'Mystic Veil Necklace' 15 'Purple veins wind through a copper-set Charoite, like a veil of mist concealing an ancient tale.' $H @((Ingredient $Cu 2),(Ingredient $Ch 1)) @{maxHealth=12;healthRegeneration=.3}),
    (Equipment Necklace 4 necklace_lvl4 'Emerald Grove Necklace' 20 'A clear green Emerald rests within a simple iron setting, recalling the peaceful shade of a woodland grove.' $H @((Ingredient $Fe 2),(Ingredient $Em 1)) @{maxHealth=16;healthRegeneration=.45}),
    (Equipment Necklace 5 necklace_lvl5 'Opaline Dream Necklace' 25 'An Opal on a copper chain shifts color with the light, delicate and elusive as a half-remembered dream.' $H @((Ingredient $Cu 2),(Ingredient $Op 1)) @{maxHealth=21;healthRegeneration=.6}),
    (Equipment Necklace 6 necklace_lvl6 'Emerald Sentinel Necklace' 30 'Two Emeralds sit within a sturdy steel setting, like watchful eyes guarding a forest path.' $H @((Ingredient $St 2),(Ingredient $Em 2)) @{maxHealth=27;healthRegeneration=.75}),
    (Equipment Necklace 7 necklace_lvl7 'Royal Charoite Necklace' 35 'A deep-purple Charoite shines against warm gold, lending this necklace a quiet, regal elegance.' $H @((Ingredient $Au 2),(Ingredient $Ch 1)) @{maxHealth=33;healthRegeneration=.95}),
    (Equipment Necklace 8 necklace_lvl8 'Crown of the Verdant King' 40 'Five Emeralds form a crown-shaped golden pendant, evoking the majesty of a sovereign of the greenwood.' $H @((Ingredient $Au 5),(Ingredient $Em 5)) @{maxHealth=40;healthRegeneration=1.2})
)

$materialValues = @{
    $W=@(2,3); $L=@(8,12); $Cu=@(22,27); $Fe=@(34,43); $St=@(125,150); $Au=@(61,78); $Sn=@(44,54); $Di=@(105,135)
    $Ta=@(40,55); $Ci=@(18,24); $Ch=@(30,40); $Em=@(55,75); $Op=@(24,32)
}
$equipmentCosts = @{}
$fields = @('maxHealth','attackDamage','defense','moveSpeed','sprintMultiplier','criticalChance','criticalMultiplier','damageReduction','healthRegeneration','dodgeChance')
$slotByFolder = @{Head=0;Body=1;Weapon=2;Ring=3;Necklace=4;Foot=5;Shield=6}
$filePrefixByFolder = @{Head='Head';Body='Body';Weapon='Sword';Ring='Ring';Necklace='Necklace';Foot='Foot';Shield='Shield'}
$categoryByFolder = @{Head='armor.head';Body='armor.body';Weapon='weapon.sword';Ring='jewelry.ring';Necklace='jewelry.necklace';Foot='armor.foot';Shield='armor.shield'}

$recipeGuids = @()
foreach ($item in $items) {
    $assetPath = Join-Path $ProjectRoot "Assets/Resources/Items/$($item.Folder)/$($filePrefixByFolder[$item.Folder])Lv$($item.FileLevel).asset"
    if (-not (Test-Path $assetPath)) { throw "Missing equipment asset: $assetPath" }

    $minCost=0; $maxCost=0
    foreach($ingredient in $item.Ingredients) {
        $value = if ($materialValues.ContainsKey($ingredient.Id)) { $materialValues[$ingredient.Id] } else { $equipmentCosts[$ingredient.Id] }
        if ($null -eq $value) { throw "No economy value for $($ingredient.Id)" }
        $minCost += $value[0] * $ingredient.Quantity; $maxCost += $value[1] * $ingredient.Quantity
    }
    $minSell=[Math]::Max(1,[Math]::Round($minCost*1.10)); $maxSell=[Math]::Max($minSell,[Math]::Round($maxCost*1.20))
    $equipmentCosts[$item.Id]=@($minCost,$maxCost)
    $minBuy=$minSell*2; $maxBuy=$maxSell*2

    $content = Get-Content -Raw $assetPath
    $content = [regex]::Replace($content, '(?m)^  itemName:.*$', '  itemName: ' + $item.Name)
    $descriptionLine = if ([string]::IsNullOrEmpty($item.Description)) { '  description:' } else { '  description: ' + ($item.Description | ConvertTo-Json -Compress) }
    $content = [regex]::Replace($content, '(?ms)^  description:.*?^  icon:', "$descriptionLine`n  icon:")
    $content = [regex]::Replace($content, '(?m)^  (_minBuyPrice|_maxBuyPrice|_minSellPrice|_maxSellPrice):.*\r?\n', '')
    $content = [regex]::Replace($content, '(?m)^  requiredLevel:.*\r?\n', '')
    $content = [regex]::Replace($content, '(?ms)^  statModifiers:.*\z', '')
    $content = [regex]::Replace($content, '(?m)^(  maxStackSize:.*)$', "`$1`n  _minBuyPrice: $minBuy`n  _maxBuyPrice: $maxBuy`n  _minSellPrice: $minSell`n  _maxSellPrice: $maxSell")
    $content = [regex]::Replace($content, '(?m)^(  slot:.*)$', "`$1`n  requiredLevel: $($item.RequiredLevel)")
    $stats = "  statModifiers:`n"
    foreach($field in $fields) { $value=if($item.Stats.ContainsKey($field)){$item.Stats[$field]}else{0}; $stats += "    ${field}: $value`n" }
    $content = $content.TrimEnd()+"`n"+$stats.TrimEnd()
    Set-Content -Path $assetPath -Value $content -Encoding utf8NoBOM

    $recipeName = "Recipe_$($filePrefixByFolder[$item.Folder])_Lvl$($item.FileLevel)"
    $recipePath = Join-Path $recipesDir "$recipeName.asset"
    $metaPath = "$recipePath.meta"
    if (Test-Path $metaPath) { $guid = ((Get-Content $metaPath | Select-String '^guid: ').Line -replace '^guid: ','') }
    else {
        $guid = [guid]::NewGuid().ToString('N')
        Set-Content -Path $metaPath -Value "fileFormatVersion: 2`nguid: $guid`nNativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 11400000`n  userData:`n  assetBundleName:`n  assetBundleVariant:" -Encoding utf8NoBOM
    }
    $recipeGuids += $guid
    $recipeIndex = [array]::IndexOf(($items | Where-Object Folder -eq $item.Folder), $item) + 1
    $yaml = "%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n--- !u!114 &11400000`nMonoBehaviour:`n  m_ObjectHideFlags: 0`n  m_CorrespondingSourceObject: {fileID: 0}`n  m_PrefabInstance: {fileID: 0}`n  m_PrefabAsset: {fileID: 0}`n  m_GameObject: {fileID: 0}`n  m_Enabled: 1`n  m_EditorHideFlags: 0`n  m_Script: {fileID: 11500000, guid: 5c4387d48b7747d4d94eec80e9c65e5d, type: 3}`n  m_Name: $recipeName`n  m_EditorClassIdentifier: ProjectGame2D.Runtime::RecipeDefinition`n  _recipeId: recipe.$($categoryByFolder[$item.Folder]).$($recipeIndex.ToString('000'))`n  _displayName: $($item.Name)`n  _ingredients:`n"
    foreach($ingredient in $item.Ingredients) { $yaml += "  - _itemId: $($ingredient.Id)`n    _quantity: $($ingredient.Quantity)`n" }
    $yaml += "  _outputItemId: $($item.Id)`n  _outputQuantity: 1`n  _requiredStationTag: station.forge`n  _npcId: $($item.Npc)`n"
    Set-Content -Path $recipePath -Value $yaml.TrimEnd() -Encoding utf8NoBOM
}

# Remove obsolete SOs requested by the sheet and every typed catalog reference to their GUIDs.
$obsolete = @('Assets/Resources/Items/Shield/ShieldLv9.asset','Assets/Resources/Items/Ring/RingLv9.asset','Assets/Resources/Items/Ring/RingLv10.asset','Assets/Resources/Items/Necklace/NecklaceLv9.asset','Assets/Resources/Items/Necklace/NecklaceLv10.asset')
$obsoleteGuids = @()
foreach($relative in $obsolete) {
    $asset=Join-Path $ProjectRoot $relative; $meta="$asset.meta"
    if(Test-Path $meta){ $obsoleteGuids += ((Get-Content $meta|Select-String '^guid: ').Line -replace '^guid: ','') }
}
foreach($relativeCatalog in @('Assets/Resources/Items/EquipmentCatalog.asset','Assets/Resources/Items/ItemDatabase.asset')) {
    $path=Join-Path $ProjectRoot $relativeCatalog; $content=Get-Content -Raw $path
    foreach($guid in $obsoleteGuids){ $content=[regex]::Replace($content,"(?m)^  - (item: )?\{fileID: 11400000, guid: $guid, type: 2\}\r?\n(?:    amount: 1\r?\n)?",'') }
    Set-Content -Path $path -Value $content.TrimEnd() -Encoding utf8NoBOM
}
foreach($relative in $obsolete) { $asset=Join-Path $ProjectRoot $relative; Remove-Item -LiteralPath $asset -ErrorAction SilentlyContinue; Remove-Item -LiteralPath "$asset.meta" -ErrorAction SilentlyContinue }

# Preserve non-equipment recipes and rebuild the equipment portion exactly once.
$catalog = Get-Content -Raw $catalogPath
foreach($guid in $accidentalWeaponRecipeGuids){ $catalog=[regex]::Replace($catalog,"(?m)^  - \{fileID: 11400000, guid: $guid, type: 2\}\r?\n",'') }
foreach($guid in $recipeGuids){ $catalog=[regex]::Replace($catalog,"(?m)^  - \{fileID: 11400000, guid: $guid, type: 2\}\r?\n",'') }
$lines = ($recipeGuids | ForEach-Object { "  - {fileID: 11400000, guid: $_, type: 2}" }) -join "`n"
$catalog = $catalog.TrimEnd()+"`n"+$lines
Set-Content -Path $catalogPath -Value $catalog -Encoding utf8NoBOM

Write-Output "Synchronized $($items.Count) equipment items and recipes; removed $($obsolete.Count) obsolete equipment assets."
