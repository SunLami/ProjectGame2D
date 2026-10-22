using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SheetContentPlayModeTests
{
    private PlayerStat _stat;
    private InventoryManager _inventory;
    private ConsumableUseHandler _handler;
    private InventorySaveData _savedInventory;
    private GameObject _statRoot,_inventoryRoot;
    private float _health,_mana,_stamina;
    private List<ItemSO> _items=new();
    private static void Field(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    [SetUp]
    public void Setup()
    {
        GameStateManager.Instance.ResetToPlaying();
        _stat=PlayerStat.Instance;
        if(_stat==null){_statRoot=new GameObject("ConsumableTestPlayer");_stat=_statRoot.AddComponent<PlayerStat>();}
        _health=_stat.Health;_mana=_stat.Mana;_stamina=_stat.Stamina;
        _stat.ConsumableBuffs.Tick(10000);_stat.NotifyConsumableStatsChanged();
        _inventory=InventoryManager.Instance;
        if(_inventory==null){_inventoryRoot=new GameObject("ConsumableTestInventory");_inventory=_inventoryRoot.AddComponent<InventoryManager>();}
        _savedInventory=_inventory.ToSaveData();
        _inventory.LoadFromSaveData(new InventorySaveData(),new Dictionary<string,ItemSO>());
        if(QuickBarManager.Instance==null)new GameObject("TestQuickBar").AddComponent<QuickBarManager>();
        _handler=QuickBarManager.Instance.GetComponent<ConsumableUseHandler>();
        Field(_handler,"_potionReadyAt",0f);
        _stat.RestoreHealth(-1);_stat.RestoreMana(_stat.MaxMana);
    }
    [TearDown]
    public void Teardown()
    {
        _stat.ConsumableBuffs.Tick(10000);_stat.NotifyConsumableStatsChanged();
        _stat.RestoreHealth(_health);Field(_stat,"_mana",_mana);Field(_stat,"_stamina",_stamina);
        Field(_handler,"_potionReadyAt",0f);
        _inventory.LoadFromSaveData(_savedInventory,new ResourcesItemResolver(),null);
        if(_statRoot!=null)UnityEngine.Object.DestroyImmediate(_statRoot);
        if(_inventoryRoot!=null)UnityEngine.Object.DestroyImmediate(_inventoryRoot);
        foreach(var item in _items)UnityEngine.Object.DestroyImmediate(item);_items.Clear();
        GameStateManager.Instance.ResetToPlaying();
    }
    private ItemSO Make(ConsumableEffectType effect,float amount)
    {
        var item=ScriptableObject.CreateInstance<ItemSO>();item.itemId="item.test."+Guid.NewGuid().ToString("N");
        item.usedItems=true;item.quickBarUseMode=QuickBarUseMode.Active;item.consumableEffect=effect;item.effectAmount=amount;
        _items.Add(item);_inventory.AddItem(item,4);return item;
    }
    [TestCase(.15f)][TestCase(.35f)]
    public void PercentHealthAndManaShareCooldown(float fraction)
    {
        var hp=Make(ConsumableEffectType.HealPercent,fraction);var mp=Make(ConsumableEffectType.RestoreManaPercent,fraction);
        _stat.RestoreHealth(1);_stat.TryConsumeMana(_stat.MaxMana);
        Assert.IsTrue(_handler.TryUse(hp,100));Assert.That(_stat.Health,Is.EqualTo(1+_stat.MaxHealth*fraction).Within(.001));
        Assert.IsFalse(_handler.TryUse(mp,109.99f));Assert.AreEqual(4,_inventory.GetTotalQuantity(mp.itemId));
        Assert.IsTrue(_handler.TryUse(mp,110));Assert.That(_stat.Mana,Is.EqualTo(_stat.MaxMana*fraction).Within(.001));
        Assert.AreEqual(3,_inventory.GetTotalQuantity(hp.itemId));Assert.AreEqual(3,_inventory.GetTotalQuantity(mp.itemId));
    }
    [Test]
    public void FullResourceMissingStockAndDeadPlayerNeverConsumeOrStartCooldown()
    {
        var item=Make(ConsumableEffectType.HealPercent,.15f);
        Assert.IsFalse(_handler.TryUse(item,10));Assert.AreEqual(4,_inventory.GetTotalQuantity(item.itemId));
        _stat.RestoreHealth(1);_inventory.RemoveItem(item,4);Assert.IsFalse(_handler.TryUse(item,10));
        _inventory.AddItem(item,1);_stat.RestoreHealth(0);Assert.IsFalse(_handler.TryUse(item,10));
        _stat.RestoreHealth(1);Assert.IsTrue(_handler.TryUse(item,10));
    }
    [Test]
    public void BuffRefreshAndExpiryChangeCapsWithoutInstantHealing()
    {
        var food=Make(ConsumableEffectType.HealthFood,.2f);food.buffDurationSeconds=90;food.buffRegenerationFraction=.01f;
        float max=_stat.MaxHealth;_stat.RestoreHealth(10);
        Assert.IsTrue(_handler.TryUse(food,10));Assert.AreEqual(10,_stat.Health);Assert.That(_stat.MaxHealth,Is.EqualTo(max*1.2f).Within(.001));
        _stat.TickRegeneration(1);Assert.That(_stat.Health,Is.EqualTo(10+max*1.2f*.01f).Within(.001));
        Assert.IsTrue(_handler.TryUse(food,11));Assert.AreEqual(90,_stat.ConsumableBuffs.HealthRemaining);
        _stat.RestoreHealth(-1);_stat.TickRegeneration(90);Assert.That(_stat.MaxHealth,Is.EqualTo(max).Within(.001));Assert.That(_stat.Health,Is.LessThanOrEqualTo(max));
    }
    [Test]
    public void ManaAndStaminaBuffsApplyTheirOwnCapsAndRegeneration()
    {
        var mana=Make(ConsumableEffectType.ManaDrink,.2f);mana.buffDurationSeconds=90;mana.buffRegenerationFraction=.01f;
        var stamina=Make(ConsumableEffectType.StaminaDrink,.25f);stamina.buffDurationSeconds=120;stamina.buffStaminaRegenerationBonus=.25f;
        float mp=_stat.MaxMana,sp=_stat.MaxStamina;
        _stat.TryConsumeMana(_stat.MaxMana);Field(_stat,"_stamina",0f);
        Assert.IsTrue(_handler.TryUse(mana,10));Assert.IsTrue(_handler.TryUse(stamina,10));
        Assert.That(_stat.MaxMana,Is.EqualTo(mp*1.2f).Within(.001));Assert.That(_stat.MaxStamina,Is.EqualTo(sp*1.25f).Within(.001));
        float baseRegen=(float)typeof(PlayerStat).GetField("_staminaRegenerationPerSecond",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(_stat);
        _stat.TickRegeneration(1);_stat.TickStamina(false,1);
        Assert.That(_stat.Mana,Is.EqualTo(mp*1.2f*.01f).Within(.001));Assert.That(_stat.Stamina,Is.EqualTo(baseRegen*1.25f).Within(.001));
    }
    [Test]
    public void WildlifeDeathDropsOnceAndRetainsLootUntilCapacityIsAvailable()
    {
        var meat=Make(ConsumableEffectType.None,0);_inventory.RemoveItem(meat,4);
        var entry=new ResourceLootEntry();entry.ConfigureForTests(meat,1,2,2);
        var def=ScriptableObject.CreateInstance<WildlifeDefinition>();Field(def,"_health",20f);Field(def,"_animalId","animal.test");Field(def,"_loot",new[]{entry});
        var root=new GameObject("TestWildlife");root.SetActive(false);root.transform.position=_stat.transform.position;
        var body=root.AddComponent<Rigidbody2D>();body.gravityScale=0;
        var actor=root.AddComponent<WildlifeActor>();Field(actor,"_definition",def);root.AddComponent<ActorLootDrop>();
        DroppedLootPickup drop=null;
        try
        {
            root.SetActive(true);actor.TakeDamage(100,Vector2.zero,0);actor.TakeDamage(100,Vector2.zero,0);
            drop=UnityEngine.Object.FindObjectsByType<DroppedLootPickup>().Single();
            foreach(var slot in _inventory.Slots){slot.item=meat;slot.quantity=99;}
            var update=typeof(DroppedLootPickup).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance);
            update.Invoke(drop,null);Assert.IsNotNull(drop);
            foreach(var slot in _inventory.Slots)slot.Clear();Field(drop,"_nextAttempt",0f);update.Invoke(drop,null);
            Assert.AreEqual(2,_inventory.GetTotalQuantity(meat.itemId));
        }
        finally
        {
            if(drop!=null)UnityEngine.Object.DestroyImmediate(drop.gameObject);
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(def);
        }
    }

    [Test]
    public void ConsumptionIsNotReenteredThroughInventoryCallbacks()
    {
        var item=Make(ConsumableEffectType.HealPercent,.15f);_stat.RestoreHealth(1);
        bool nested=false;void Reenter(){nested|=_handler.TryUse(item,100);}
        _inventory.OnInventoryChanged+=Reenter;
        try{Assert.IsTrue(_handler.TryUse(item,100));Assert.IsFalse(nested);Assert.AreEqual(3,_inventory.GetTotalQuantity(item.itemId));}
        finally{_inventory.OnInventoryChanged-=Reenter;}
    }
    [Test]
    public void FishPurchaseCommitsGoldAndInstanceBeforeNotificationAndRoundTrips()
    {
        var fish=ScriptableObject.CreateInstance<FishDefinitionSO>();fish.itemId="fish.test.transaction";fish.isStackable=false;fish.maxStackSize=1;_items.Add(fish);
        _inventory.AddGold(100);int notifications=0;
        void Observe(){notifications++;Assert.AreEqual(90,_inventory.Gold);Assert.AreEqual(1,_inventory.GetTotalQuantity(fish.itemId));}
        _inventory.OnInventoryChanged+=Observe;
        try{Assert.IsTrue(_inventory.TryPurchaseItem(fish,1,10,500));}finally{_inventory.OnInventoryChanged-=Observe;}
        Assert.AreEqual(1,notifications);var saved=_inventory.ToSaveData();
        var slot=saved.slots.Single(s=>s.itemId==fish.itemId);Assert.AreEqual(500,slot.fish.weightGrams);Assert.IsNotEmpty(slot.fish.instanceId);
        _inventory.LoadFromSaveData(saved,new Dictionary<string,ItemSO>{{fish.itemId,fish}});
        Assert.AreEqual(slot.fish.instanceId,_inventory.ToSaveData().slots.Single(s=>s.itemId==fish.itemId).fish.instanceId);
    }
    [Test]
    public void FullInventoryPurchaseDoesNotSpendOrGrant()
    {
        var item=Make(ConsumableEffectType.Heal,1);item.isStackable=false;item.maxStackSize=1;
        foreach(var slot in _inventory.Slots){slot.item=item;slot.quantity=1;}
        _inventory.AddGold(100);Assert.IsFalse(_inventory.TryPurchaseItem(item,1,10));Assert.AreEqual(100,_inventory.Gold);
    }
}
