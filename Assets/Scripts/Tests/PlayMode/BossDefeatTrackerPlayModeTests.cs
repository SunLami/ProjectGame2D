using NUnit.Framework;
using UnityEngine;

public sealed class BossDefeatTrackerPlayModeTests
{
    private GameObject _bossGameObject;
    private GameObject _trackerRoot;
    private EnemyUniversal _boss;
    private BossDefeatTracker _tracker;

    [SetUp]
    public void SetUp()
    {
        _bossGameObject = new GameObject("Boss");
        _bossGameObject.AddComponent<Rigidbody2D>();
        _bossGameObject.AddComponent<Animator>();
        _boss = _bossGameObject.AddComponent<EnemyUniversal>();

        _trackerRoot = new GameObject("BossDefeatTrackerFixture");
        _tracker = _trackerRoot.AddComponent<BossDefeatTracker>();
        _tracker.ConfigureForTests("world.boss.forest.guardian.01", _boss);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_trackerRoot);
        if (_bossGameObject != null)
            Object.DestroyImmediate(_bossGameObject);
    }

    [Test]
    public void RealBossDeath_SetsDefeatedExactlyOnce()
    {
        int diedEvents = 0;
        _boss.Died += () => diedEvents++;

        Assert.IsFalse(_tracker.IsDefeated);

        _boss.TakeDamage(999999f);

        Assert.IsTrue(_tracker.IsDefeated);
        Assert.AreEqual(1, diedEvents);

        WorldObjectState captured = _tracker.CaptureState();
        Assert.IsTrue(captured.Flag);
    }

    [Test]
    public void RestoreState_Defeated_RemovesBossSilentlyWithoutDuplicateEvents()
    {
        int diedEvents = 0, healthChangedEvents = 0, enemyKilledEvents = 0;
        _boss.Died += () => diedEvents++;
        _boss.HealthChanged += (_, _) => healthChangedEvents++;
        void OnEnemyKilled(string enemyId, string areaId) => enemyKilledEvents++;
        QuestDomainEvents.EnemyKilled += OnEnemyKilled;

        try
        {
            _tracker.RestoreState(new WorldObjectState(true, 0));

            Assert.IsTrue(_tracker.IsDefeated);
            Assert.IsFalse(_bossGameObject.activeSelf, "A restored-defeated boss must be removed from play.");
            Assert.AreEqual(0, diedEvents, "Restore must not fire Died as if it just happened.");
            Assert.AreEqual(0, healthChangedEvents);
            Assert.AreEqual(0, enemyKilledEvents, "Restore must not grant duplicate quest kill credit.");
        }
        finally
        {
            QuestDomainEvents.EnemyKilled -= OnEnemyKilled;
        }
    }

    [Test]
    public void RestoreState_NotDefeated_LeavesBossAlive()
    {
        _tracker.RestoreState(new WorldObjectState(false, 0));

        Assert.IsFalse(_tracker.IsDefeated);
        Assert.IsTrue(_bossGameObject.activeSelf);
        Assert.IsFalse(_boss.IsDead);
    }

    [Test]
    public void OrdinaryEnemyWithoutTracker_NeverProducesAWorldSaveRecord()
    {
        // No BossDefeatTracker attached to this instance at all -- proving the acceptance rule
        // "regular enemies must not create a per-instance save record" by construction: only
        // objects explicitly wired into a WorldObjectRegistry (via a tracker like this one) are
        // ever captured into WorldSaveData.
        var plainEnemyGo = new GameObject("PlainSlime");
        plainEnemyGo.AddComponent<Rigidbody2D>();
        plainEnemyGo.AddComponent<Animator>();
        EnemyUniversal plainEnemy = plainEnemyGo.AddComponent<EnemyUniversal>();
        try
        {
            plainEnemy.TakeDamage(999999f);
            Assert.IsTrue(plainEnemy.IsDead);

            var registryRoot = new GameObject("WorldObjectRegistryFixture");
            var registry = registryRoot.AddComponent<WorldObjectRegistry>();
            registry.ConfigureForTests(new IPersistentWorldObject[] { _tracker });
            try
            {
                WorldSaveData data = registry.ToSaveData();
                Assert.AreEqual(1, data.objects.Count, "Only the explicitly registered boss tracker must appear, never the plain enemy.");
                Assert.AreEqual("world.boss.forest.guardian.01", data.objects[0].persistentId);
            }
            finally
            {
                Object.DestroyImmediate(registryRoot);
            }
        }
        finally
        {
            Object.DestroyImmediate(plainEnemyGo);
        }
    }
}
