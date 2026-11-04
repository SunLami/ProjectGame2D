using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SheetContentEditModeTests
{
    private static ItemSO Item(string id) => AssetDatabase.FindAssets("t:ItemSO", new[]{"Assets/Resources/Items"})
        .Select(g=>AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(g))).Single(i=>i.itemId==id);

    [TestCase("carrot")][TestCase("grape")][TestCase("cucumber")][TestCase("chillie")]
    [TestCase("mushroom")][TestCase("lemon")][TestCase("apple")]
    public void CropMaturesAtSixtySecondsAndYieldsExactlyThree(string key)
    {
        var catalog=AssetDatabase.LoadAssetAtPath<FarmingCatalog>("Assets/Game/Farming/Definitions/FarmingCatalog.asset");
        Assert.IsTrue(catalog.TryResolve("crop."+key,out var crop));
        long planted=DateTime.UtcNow.Ticks;
        Assert.Less(crop.GetStageIndex(planted,planted+TimeSpan.FromSeconds(59.99).Ticks),crop.MatureStageIndex);
        Assert.AreEqual(crop.MatureStageIndex,crop.GetStageIndex(planted,planted+TimeSpan.FromSeconds(60).Ticks));
        Assert.AreEqual(3,crop.MinimumHarvestQuantity);Assert.AreEqual(3,crop.MaximumHarvestQuantity);
        Assert.AreSame(Item("item.crop."+key),crop.HarvestItem);
        Assert.IsNotNull(Item("item.seed."+key));
        foreach(var stage in crop.Stages)Assert.IsNotNull(stage.sprite);
    }
    [TestCase("small_health",ConsumableEffectType.HealPercent,.15f)]
    [TestCase("large_health",ConsumableEffectType.HealPercent,.35f)]
    [TestCase("small_mana",ConsumableEffectType.RestoreManaPercent,.15f)]
    [TestCase("large_mana",ConsumableEffectType.RestoreManaPercent,.35f)]
    public void PotionDefinitionsUsePercentagesWithoutInventingPrices(string key,ConsumableEffectType effect,float amount)
    {
        var item=Item("item.consumable."+key+"_potion");
        Assert.AreEqual(effect,item.consumableEffect);Assert.AreEqual(amount,item.effectAmount);
        Assert.AreEqual(0,item.MaxBuyPrice);Assert.AreEqual(0,item.MaxSellPrice);
        Assert.IsTrue(item.usedItems);Assert.AreEqual(QuickBarUseMode.Active,item.quickBarUseMode);
    }
    [Test]
    public void BuffRefreshDoesNotStackAndAllGroupsExpire()
    {
        var state=new ConsumableBuffState();
        var health=Item("item.food.harvest_stew");var mana=Item("item.drink.orchard_elixir");var stamina=Item("item.drink.trailrunner_tonic");
        state.Refresh(health);state.Refresh(mana);state.Refresh(stamina);state.Tick(45);
        state.Refresh(health);Assert.AreEqual(90,state.HealthRemaining);Assert.AreEqual(.2f,state.HealthCapBonus);
        state.Tick(45);Assert.AreEqual(0,state.ManaCapBonus);Assert.AreEqual(.25f,state.StaminaCapBonus);
        state.Tick(45);Assert.AreEqual(0,state.HealthCapBonus);Assert.AreEqual(0,state.StaminaCapBonus);
        Assert.AreEqual(90,health.buffDurationSeconds,"Runtime must not mutate definition.");
    }
    [Test]
    public void RecipesResolveFourDistinctIngredientsWithMeatFishAndFruitDrink()
    {
        foreach(string file in new[]{"HarvestStew","OrchardElixir","TrailrunnerTonic"})
        {
            var recipe=AssetDatabase.LoadAssetAtPath<RecipeDefinition>("Assets/Crafting/Recipes/SheetContent/Recipe_"+file+".asset");
            Assert.AreEqual(4,recipe.Ingredients.Count);Assert.AreEqual(4,recipe.Ingredients.Select(i=>i.ItemId).Distinct().Count());
            foreach(var ingredient in recipe.Ingredients)Assert.IsNotNull(Item(ingredient.ItemId));
            string protein=file=="HarvestStew"?"item.material.meat":file=="TrailrunnerTonic"?"item.material.fish_fillet":null;
            if(protein!=null)foreach(var ingredient in recipe.Ingredients)Assert.AreEqual(2,ingredient.Quantity);
            if(protein!=null)Assert.AreEqual(2,recipe.Ingredients.Single(i=>i.ItemId==protein).Quantity);
            else foreach(var ingredient in recipe.Ingredients){Assert.That(ingredient.ItemId,Does.StartWith("item.drink."));Assert.AreEqual(1,ingredient.Quantity);}
            if(file=="TrailrunnerTonic"){Assert.AreEqual("npc.town.gastronome",recipe.NpcId);Assert.IsTrue(recipe.Ingredients.Any(i=>i.ItemId=="item.crop.cucumber"));}
        }
        var ash=AssetDatabase.LoadAssetAtPath<RecipeDefinition>("Assets/Crafting/Recipes/SheetContent/Recipe_BoneAsh.asset");
        Assert.AreEqual(3,ash.OutputQuantity);Assert.AreEqual("item.material.bone",ash.Ingredients[0].ItemId);
        Assert.IsEmpty(ash.NpcId);Assert.IsEmpty(ash.RequiredStationTag);
    }
    [Test]
    public void MaterialAndConsumableRecipesHaveVisibleCategories()
    {
        Assert.AreEqual("MATERIAL",CraftingPresentationCategory.ForItem(Item("item.material.bone_ash")));
        Assert.AreEqual("CONSUMABLE",CraftingPresentationCategory.ForItem(Item("item.drink.orchard_elixir")));
        Assert.AreEqual("SWORD",CraftingPresentationCategory.ForItem(Item("sword_lvl1")));
    }
    [Test]
    public void ItemIdsAreUniqueAndEveryExpansionRecipeResolves()
    {
        var all=AssetDatabase.FindAssets("t:ItemSO",new[]{"Assets/Resources/Items"}).Select(g=>AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        Assert.AreEqual(all.Length,all.Select(i=>i.itemId).Distinct().Count());
        foreach(var item in all)Assert.IsNotEmpty(item.itemId);
        var recipes=AssetDatabase.FindAssets("t:RecipeDefinition",new[]{"Assets/Crafting/Recipes/SheetContent"});
        int fishCount=AssetDatabase.FindAssets("t:FishDefinitionSO",new[]{"Assets/Resources/Items"}).Length;
        Assert.AreEqual(12+fishCount,recipes.Length);
        foreach(var guid in recipes)
        {
            var recipe=AssetDatabase.LoadAssetAtPath<RecipeDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.IsNotNull(Item(recipe.OutputItemId));foreach(var ingredient in recipe.Ingredients)Assert.IsNotNull(Item(ingredient.ItemId));
        }
    }

    [Test]
    public void EveryFishHasAFilletRecipeAndJuicesUseTwoProduce()
    {
        var recipes=AssetDatabase.FindAssets("t:RecipeDefinition",new[]{"Assets/Crafting/Recipes/SheetContent"})
            .Select(g=>AssetDatabase.LoadAssetAtPath<RecipeDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        foreach(var guid in AssetDatabase.FindAssets("t:FishDefinitionSO",new[]{"Assets/Resources/Items"}))
        {
            var fish=AssetDatabase.LoadAssetAtPath<FishDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
            var recipe=recipes.Single(r=>r.OutputItemId=="item.material.fish_fillet"&&r.Ingredients.Single().ItemId==fish.itemId);
            Assert.AreEqual(1,recipe.Ingredients[0].Quantity);Assert.AreEqual(2,recipe.OutputQuantity);
            Assert.AreEqual("npc.town.gastronome",recipe.NpcId);
        }
        foreach(string name in new[]{"Lemon","Apple","Carrot","Grape"})
        {
            var recipe=AssetDatabase.LoadAssetAtPath<RecipeDefinition>("Assets/Crafting/Recipes/SheetContent/Recipe_"+name+"Juice.asset");
            Assert.AreEqual(2,recipe.Ingredients.Single().Quantity);Assert.AreEqual(1,recipe.OutputQuantity);
        }
        var shop=AssetDatabase.LoadAssetAtPath<ShopDefinition>("Assets/Shops/Definitions/Shop_TraderBread.asset");
        Assert.IsTrue(shop.BuyItems.Any(i=>i.itemId=="item.material.fish_fillet"));
        Assert.IsTrue(Item("item.material.fish_fillet").commerceConfigurationPending);
    }
}
