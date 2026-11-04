#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit editor authoring only. Never creates content from a runtime startup callback.
public static class SheetContentAuthoring
{
    private static readonly List<ItemSO> CreatedItems = new();
    private static readonly List<RecipeDefinition> Recipes = new();
    private static readonly List<GameObject> NodePrefabs = new();
    private static readonly List<GameObject> AnimalPrefabs = new();
    private static Sprite _fallback;

    [MenuItem("Tools/Project Game/Items List/Build Content Assets")]
    public static void BuildAssets()
    {
        CreatedItems.Clear(); Recipes.Clear(); NodePrefabs.Clear(); AnimalPrefabs.Clear();
        _fallback = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/Resources/Items/Farming/Crop.Carrot.asset").icon;
        BuildMaterials(); BuildFarming(); BuildProducts(); BuildNodes(); BuildAnimals();
        foreach (string guid in AssetDatabase.FindAssets("t:RecipeCatalog", new[]{"Assets/Crafting"}))
        {
            RecipeCatalog catalog = AssetDatabase.LoadAssetAtPath<RecipeCatalog>(AssetDatabase.GUIDToAssetPath(guid));
            SetArray(catalog, "_recipes", catalog.AllRecipes.Concat(Recipes).Distinct().Cast<UnityEngine.Object>().ToArray());
        }
        WireShops(); WireSlimePrefabs(); WireTreeVariants();
        AssetDatabase.SaveAssets();
        Debug.Log($"Items List: {CreatedItems.Count} item definitions configured; {Recipes.Count} recipes; {NodePrefabs.Count} node prefabs; {AnimalPrefabs.Count} wildlife prefabs. Missing prices/stations/NPCs left unset. Art is provisional.");
    }
    [MenuItem("Tools/Project Game/Items List/Install Demo Content")]
    public static void InstallDemoContent()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/DemoScene.unity") throw new InvalidOperationException("Open DemoScene first.");
        var player = UnityEngine.Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        var registry = UnityEngine.Object.FindAnyObjectByType<WorldObjectRegistry>(FindObjectsInactive.Include);
        if (player == null || registry == null) throw new InvalidOperationException("DemoScene player/registry missing.");
        var root = scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="SheetContentDemo");
        if (root == null) root = new GameObject("SheetContentDemo");
        var entries = registry.Entries?.Where(e=>e!=null).ToList() ?? new List<MonoBehaviour>();
        var paths = AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/Prefabs/SheetContent/Nodes"})
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).ToArray();
        for (int i=0;i<paths.Length;i++)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            var existing=root.transform.Find(prefab.name);
            GameObject obj=existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);
            if(existing==null)obj.transform.position=player.transform.position+new Vector3(3f+(i%4)*1.5f,-4f-(i/4)*1.5f,0);
            var node=obj.GetComponent<ResourceNodeInteractable>();
            Set(node,"_persistentId","world.resource.sheetdemo."+prefab.name.ToLowerInvariant()+".01");
            Set(node,"_areaId","area.demo");
            if(!entries.Contains(node))entries.Add(node);
        }
        SetArray(registry,"_entries",entries.Cast<UnityEngine.Object>().ToArray());
        var animals=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/SheetContent/Wildlife"})
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).ToArray();
        for(int i=0;i<animals.Length;i++)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(animals[i]);
            if(root.transform.Find(prefab.name)!=null)continue;
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);
            instance.transform.position=player.transform.position+new Vector3(8f+i*2f,-6f,0);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Items List DemoScene content installed. Production placement remains explicit.");
    }

    private static void BuildMaterials()
    {
        foreach (string name in new[]{"Meat", "FishFillet", "Bone", "BoneAsh", "Slime", "HealthHerb", "ManaHerb"})
        {
            string id = name switch { "FishFillet" => "fish_fillet", "BoneAsh" => "bone_ash", "HealthHerb" => "health_herb", "ManaHerb" => "mana_herb", _ => name.ToLowerInvariant() };
            Item("Materials/"+name, "item.material."+id, Split(name), ItemType.Material, InventoryCategory.Resources);
        }
        Recipe("BoneAsh", "item.material.bone_ash", 3, "", ("item.material.bone",1));
        foreach (string guid in AssetDatabase.FindAssets("t:FishDefinitionSO",new[]{"Assets/Resources/Items"}))
        {
            var fish=AssetDatabase.LoadAssetAtPath<FishDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
            var recipe=Recipe("Fillet_"+fish.name,"item.material.fish_fillet",2,"npc.town.gastronome",(fish.itemId,1));
            var data=new SerializedObject(recipe);
            data.FindProperty("_recipeId").stringValue="recipe.material.fish_fillet."+fish.itemId;
            data.FindProperty("_displayName").stringValue="Fillet "+fish.itemName;
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(recipe);
        }
    }
    private static void BuildFarming()
    {
        var crops = new List<CropDefinition>();
        // Reuse validated existing growth sprites provisionally, preserve original atlas/import IDs.
        var carrot = AssetDatabase.LoadAssetAtPath<CropDefinition>("Assets/Game/Farming/Definitions/Crop.Carrot.asset");
        foreach (string name in new[]{"Grape", "Cucumber", "Chillie", "Carrot", "Mushroom", "Lemon", "Apple"})
        {
            string key = name.ToLowerInvariant();
            ItemSO produce = Item("Farming/Crop."+name,"item.crop."+key,name,ItemType.Material,InventoryCategory.Resources);
            var crop = LoadOrCreate<CropDefinition>("Assets/Game/Farming/Definitions/Crop."+name+".asset");
            var so = new SerializedObject(crop);
            so.FindProperty("_cropId").stringValue = "crop."+key;
            so.FindProperty("_harvestItem").objectReferenceValue = produce;
            so.FindProperty("_minimumHarvestQuantity").intValue = 3;
            so.FindProperty("_maximumHarvestQuantity").intValue = 3;
            var stages = so.FindProperty("_stages"); stages.arraySize = 4;
            for (int i=0;i<4;i++)
            {
                var stage=stages.GetArrayElementAtIndex(i);
                stage.FindPropertyRelative("sprite").objectReferenceValue = carrot.GetStageSprite(i) ?? _fallback;
                stage.FindPropertyRelative("durationSeconds").floatValue = i==3 ? 0.1f : 20f;
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(crop); crops.Add(crop);
            var seed = LoadOrCreate<SeedItemSO>("Assets/Resources/Items/Farming/Seed."+name+".asset");
            ConfigureItem(seed,"item.seed."+key,name+" Seeds",ItemType.Material,InventoryCategory.Others);
            seed.usedItems=true; seed.quickBarUseMode=QuickBarUseMode.Inactive;
            Set(seed,"_crop",crop);
        }
        var catalog=AssetDatabase.LoadAssetAtPath<FarmingCatalog>("Assets/Game/Farming/Definitions/FarmingCatalog.asset");
        SetArray(catalog,"_crops",(catalog.Crops ?? Array.Empty<CropDefinition>()).Concat(crops).Distinct().Cast<UnityEngine.Object>().ToArray());
    }
    private static void BuildProducts()
    {
        foreach (bool mana in new[]{false,true})
        foreach (bool large in new[]{false,true})
        {
            string resource=mana?"Mana":"Health", size=large?"Large":"Small";
            var item=Item("Consumables/"+size+resource+"Potion","item.consumable."+(large?"large_":"small_")+resource.ToLowerInvariant()+"_potion",size+" "+resource+" Potion",ItemType.Consumable,InventoryCategory.Consumption);
            item.usedItems=true;item.quickBarUseMode=QuickBarUseMode.Active;
            item.consumableEffect=mana?ConsumableEffectType.RestoreManaPercent:ConsumableEffectType.HealPercent;
            item.effectAmount=large?0.35f:0.15f;
            item.description=$"Restores {(large?35:15)}% of maximum {resource.ToLowerInvariant()}. Shares a 10 second potion cooldown.";
            EditorUtility.SetDirty(item);
            Recipe(size+resource+"Potion",item.itemId,1,"",("item.material."+(mana?"mana_herb":"health_herb"),large?5:3),("item.drink."+(mana?"grape_juice":"carrot_juice"),large?2:1),("item.material.slime",large?5:3),("item.material.bone_ash",large?5:3));
        }
        foreach (string name in new[]{"Lemon","Apple","Carrot","Grape"})
        {
            ItemSO juice=Item("Drinks/"+name+"Juice","item.drink."+name.ToLowerInvariant()+"_juice",name+" Juice",ItemType.Consumable,InventoryCategory.Consumption);
            Recipe(name+"Juice",juice.itemId,1,"npc.town.mixologist",("item.crop."+name.ToLowerInvariant(),2));
        }
        Buff("HarvestStew","item.food.harvest_stew","Harvest Stew",ConsumableEffectType.HealthFood,0.2f,90f,0.01f,0f,
            "npc.town.gastronome",("item.material.meat",2),("item.crop.carrot",2),("item.crop.mushroom",2),("item.crop.chillie",2));
        Buff("OrchardElixir","item.drink.orchard_elixir","Orchard Elixir",ConsumableEffectType.ManaDrink,0.2f,90f,0.01f,0f,
            "npc.town.mixologist",("item.drink.lemon_juice",1),("item.drink.apple_juice",1),("item.drink.carrot_juice",1),("item.drink.grape_juice",1));
        Buff("TrailrunnerTonic","item.drink.trailrunner_tonic","Trailrunner Fish Broth",ConsumableEffectType.StaminaDrink,0.25f,120f,0f,0.25f,
            "npc.town.gastronome",("item.material.fish_fillet",2),("item.crop.lemon",2),("item.crop.carrot",2),("item.crop.cucumber",2));
    }
    private static void Buff(string file,string id,string name,ConsumableEffectType effect,float cap,float duration,float regen,float stamina,string npc,params (string,int)[] ingredients)
    {
        var item=Item("Consumables/"+file,id,name,ItemType.Consumable,InventoryCategory.Consumption);
        item.usedItems=true;item.quickBarUseMode=QuickBarUseMode.Active;item.consumableEffect=effect;item.effectAmount=cap;
        item.buffDurationSeconds=duration;item.buffRegenerationFraction=regen;item.buffStaminaRegenerationBonus=stamina;
        item.description=effect==ConsumableEffectType.StaminaDrink?"+25% maximum stamina and stamina regeneration for 120 seconds. Reuse refreshes duration.":$"+20% maximum {(effect==ConsumableEffectType.HealthFood?"health":"mana")} and 1% of maximum regenerated per second for 90 seconds. Reuse refreshes duration.";
        EditorUtility.SetDirty(item);
        var recipe=Recipe(file,id,1,npc,ingredients);
        var recipeData=new SerializedObject(recipe);recipeData.FindProperty("_displayName").stringValue=name;
        recipeData.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(recipe);
    }
    private static void BuildNodes()
    {
        foreach(string name in new[]{"Copper","Iron","Gold","Tin","Diamond"})
        {
            var def=AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>("Assets/Resources/World/ResourceNodes/"+name+"OreVein.asset");
            NodePrefabs.Add(CloneNode(name+"OreVein",def,false));
        }
        foreach(string name in new[]{"Health","Mana"})
        {
            var def=LoadOrCreate<ResourceNodeDefinition>("Assets/Resources/World/ResourceNodes/"+name+"Plant.asset");
            var so=new SerializedObject(def);
            so.FindProperty("_resourceId").stringValue="resource.herb."+name.ToLowerInvariant();
            so.FindProperty("_harvestType").intValue=(int)ResourceHarvestType.Gathering;
            so.FindProperty("_maximumHealth").floatValue=1f;
            so.FindProperty("_gatheringDuration").floatValue=1.2f;
            so.FindProperty("_respawnSeconds").floatValue=30f;
            WriteLoot(so.FindProperty("_lootTable"),(FindItem("item.material."+name.ToLowerInvariant()+"_herb"),3,3));
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(def);
            NodePrefabs.Add(CloneNode(name+"Plant",def,true));
        }
        var wood=AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>("Assets/Resources/World/ResourceNodes/WoodTree.asset");
        var data=new SerializedObject(wood);
        WriteLoot(data.FindProperty("_lootTable"),(FindItem("item.material.wood_log"),3,3));
        data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(wood);
    }
    private static GameObject CloneNode(string name,ResourceNodeDefinition def,bool herb)
    {
        string source=herb?"Assets/Prefabs/World/Resources/Collectable/MedicinalHerb.prefab":"Assets/Prefabs/World/Resources/Ore/CopperOreVein.prefab";
        string path="Assets/Prefabs/SheetContent/Nodes/"+name+".prefab";
        EnsureParent(path);
        GameObject root=PrefabUtility.LoadPrefabContents(source);
        try
        {
            root.name=name;var node=root.GetComponent<ResourceNodeInteractable>();
            Set(node,"_definition",def);Set(node,"_persistentId","");
            return PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    private static void BuildAnimals()
    {
        foreach (string name in new[]{"Rabbit","Deer","Boar"})
        {
            var def=LoadOrCreate<WildlifeDefinition>("Assets/Game/Wildlife/"+name+".asset");
            var so=new SerializedObject(def);
            so.FindProperty("_animalId").stringValue="animal."+name.ToLowerInvariant();
            so.FindProperty("_health").floatValue=name=="Rabbit"?15:name=="Deer"?30:50;
            so.FindProperty("_speed").floatValue=name=="Rabbit"?2.5f:2f;
            so.FindProperty("_aggressive").boolValue=name=="Boar";
            so.FindProperty("_contactDamage").floatValue=name=="Boar"?8f:0f;
            var loot=new List<(ItemSO,int,int)>{(FindItem("item.material.meat"),name=="Rabbit"?1:name=="Deer"?2:3,name=="Rabbit"?1:name=="Deer"?2:3),(FindItem("item.material.leather"),name=="Rabbit"?1:name=="Deer"?3:2,name=="Rabbit"?1:name=="Deer"?3:2)};
            if(name!="Rabbit")loot.Add((FindItem("item.material.bone"),1,1));
            WriteLoot(so.FindProperty("_loot"),loot.ToArray());so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(def);
            string path="Assets/Prefabs/SheetContent/Wildlife/"+name+".prefab";EnsureParent(path);
            var root=new GameObject(name);
            try
            {
                var body=root.AddComponent<Rigidbody2D>();body.gravityScale=0f;body.constraints=RigidbodyConstraints2D.FreezeRotation;
                root.AddComponent<CircleCollider2D>().radius=0.35f;
                root.AddComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Tiles/Tilesets/Top-Down Farm with Animals Pixel Art Asset Pack/Tiled_files/Pig_animation.png").OfType<Sprite>().FirstOrDefault()??_fallback;
                root.GetComponent<SpriteRenderer>().sortingOrder=20;
                var actor=root.AddComponent<WildlifeActor>();Set(actor,"_definition",def);root.AddComponent<ActorLootDrop>();
                AnimalPrefabs.Add(PrefabUtility.SaveAsPrefabAsset(root,path));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
    private static void WireTreeVariants()
    {
        string[] paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/World/Resources/Tree"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        Sprite[] sprites=paths.Select(p=>AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.sprite!=null)?.sprite).Where(s=>s!=null).Distinct().ToArray();
        foreach(string path in paths)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderer=root.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.sprite!=null);
                var variants=root.GetComponent<ResourceVisualVariants>()??root.AddComponent<ResourceVisualVariants>();
                Set(variants,"_renderer",renderer);SetArray(variants,"_variants",sprites.Cast<UnityEngine.Object>().ToArray());
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }

    private static void WireSlimePrefabs()
    {
        foreach(string path in new[]{"Assets/Prefabs/Enemies/Slime1.prefab","Assets/Prefabs/Enemies/Slime2.prefab"})
        {
            GameObject root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var drop=root.GetComponent<ActorLootDrop>()??root.AddComponent<ActorLootDrop>();
                var so=new SerializedObject(drop);WriteLoot(so.FindProperty("_loot"),(FindItem("item.material.slime"),1,3));so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
    private static void WireShops()
    {
        AppendShop("Shop_Leofrun",CreatedItems.Where(i=>i is SeedItemSO),CreatedItems.Where(i=>i.itemId.StartsWith("item.crop.")));
        var food=CreatedItems.Where(i=>i.itemId is "item.material.meat" or "item.material.fish_fillet" or "item.food.harvest_stew" or "item.drink.trailrunner_tonic");
        AppendShop("Shop_TraderBread",food,food);
        AppendShop("Shop_TraderDrinks",CreatedItems.Where(i=>i.itemId.StartsWith("item.drink.")&&i.itemId!="item.drink.trailrunner_tonic"),CreatedItems.Where(i=>i.itemId.StartsWith("item.drink.")&&i.itemId!="item.drink.trailrunner_tonic"));
        var drinks=AssetDatabase.LoadAssetAtPath<ShopDefinition>("Assets/Shops/Definitions/Shop_TraderDrinks.asset");
        SetArray(drinks,"_buyItems",drinks.BuyItems.Where(i=>i.itemId!="item.drink.trailrunner_tonic").Cast<UnityEngine.Object>().ToArray());
        SetArray(drinks,"_sellItems",drinks.SellItems.Where(i=>i.itemId!="item.drink.trailrunner_tonic").Cast<UnityEngine.Object>().ToArray());
    }
    private static void AppendShop(string file,IEnumerable<ItemSO> buy,IEnumerable<ItemSO> sell)
    {
        var shop=AssetDatabase.LoadAssetAtPath<ShopDefinition>("Assets/Shops/Definitions/"+file+".asset");
        SetArray(shop,"_buyItems",shop.BuyItems.Concat(shop.Stock.Select(e=>FindItem(e.ItemId))).Concat(buy).Distinct().Cast<UnityEngine.Object>().ToArray());
        SetArray(shop,"_sellItems",shop.SellItems.Concat(sell).Distinct().Cast<UnityEngine.Object>().ToArray());
    }
    private static ItemSO Item(string path,string id,string name,ItemType type,InventoryCategory category)
    {
        var item=LoadOrCreate<ItemSO>("Assets/Resources/Items/"+path+".asset");ConfigureItem(item,id,name,type,category);return item;
    }
    private static void ConfigureItem(ItemSO item,string id,string name,ItemType type,InventoryCategory category)
    {
        item.itemId=id;item.itemName=name;item.type=type;item.inventoryCategory=category;
        if(item.icon==null)item.icon=_fallback;
        item.isStackable=true;item.maxStackSize=99;
        item.commerceConfigurationPending = item.MaxBuyPrice == 0 || item.MaxSellPrice == 0;
        if(!CreatedItems.Contains(item))CreatedItems.Add(item);EditorUtility.SetDirty(item);
    }
    private static RecipeDefinition Recipe(string name,string output,int count,string npc,params (string,int)[] ingredients)
    {
        var recipe=LoadOrCreate<RecipeDefinition>("Assets/Crafting/Recipes/SheetContent/Recipe_"+name+".asset");
        var so=new SerializedObject(recipe);so.FindProperty("_recipeId").stringValue="recipe."+output.Substring(5);
        so.FindProperty("_displayName").stringValue=Split(name);so.FindProperty("_outputItemId").stringValue=output;
        so.FindProperty("_outputQuantity").intValue=count;so.FindProperty("_requiredStationTag").stringValue="";so.FindProperty("_npcId").stringValue=npc;
        var array=so.FindProperty("_ingredients");array.arraySize=ingredients.Length;
        for(int i=0;i<ingredients.Length;i++)
        {
            var entry=array.GetArrayElementAtIndex(i);entry.FindPropertyRelative("_itemId").stringValue=ingredients[i].Item1;entry.FindPropertyRelative("_quantity").intValue=ingredients[i].Item2;
        }
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(recipe);Recipes.Add(recipe);return recipe;
    }
    private static ItemSO FindItem(string id)
    {
        ItemSO result=CreatedItems.FirstOrDefault(i=>i.itemId==id)??AssetDatabase.FindAssets("t:ItemSO",new[]{"Assets/Resources/Items"}).Select(g=>AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(i=>i.itemId==id);
        return result??throw new InvalidOperationException("Missing item "+id);
    }
    private static void WriteLoot(SerializedProperty array,params (ItemSO,int,int)[] entries)
    {
        array.arraySize=entries.Length;
        for(int i=0;i<entries.Length;i++)
        {
            var e=array.GetArrayElementAtIndex(i);e.FindPropertyRelative("_item").objectReferenceValue=entries[i].Item1;e.FindPropertyRelative("_chance").floatValue=1f;e.FindPropertyRelative("_minimumQuantity").intValue=entries[i].Item2;e.FindPropertyRelative("_maximumQuantity").intValue=entries[i].Item3;
        }
    }
    private static T LoadOrCreate<T>(string path) where T:ScriptableObject
    {
        T result=AssetDatabase.LoadAssetAtPath<T>(path);
        if(result!=null)return result;EnsureParent(path);result=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(result,path);return result;
    }
    private static void EnsureParent(string path)
    {
        string folder=System.IO.Path.GetDirectoryName(path).Replace('\\','/');
        EnsureFolder(folder);
    }
    private static void EnsureFolder(string folder)
    {
        if(AssetDatabase.IsValidFolder(folder))return;
        string parent=System.IO.Path.GetDirectoryName(folder).Replace('\\','/');
        EnsureFolder(parent);AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(folder));
    }
    private static void Set(UnityEngine.Object target,string field,object value)
    {
        var so=new SerializedObject(target);var p=so.FindProperty(field);
        if(value is UnityEngine.Object obj)p.objectReferenceValue=obj;else if(value is string str)p.stringValue=str;
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);
    }
    private static void SetArray(UnityEngine.Object target,string field,UnityEngine.Object[] values)
    {
        var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;
        for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);
    }
    private static string Split(string name)=>System.Text.RegularExpressions.Regex.Replace(name,"([a-z])([A-Z])","$1 $2");
}
#endif
