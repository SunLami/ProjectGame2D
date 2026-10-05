"""UI additions, inventory / quest / commerce, world interactions, fishing, farming, footsteps and ambience loops."""
from sfxtypes import C, Mix, S

ENTRIES = {
    # ---- UI / inventory
    "sfx.ui.tab": S("UI", [C(759827, dur=0.25, peak=-8, note="tab"), C(836451, dur=0.25, peak=-8, note="tab 2")],
                    vol=0.6, jitter=0.04, cooldown=0.05, desc="Tab switch"),
    "sfx.ui.notification": S("UI", [C(740423, dur=0.9, peak=-6, note="notify"), C(554554, dur=0.9, peak=-6, note="notify 2")],
                             vol=0.6, cooldown=0.3, desc="Toast / notification"),
    "sfx.ui.save_success": S("UI", [C(538145, dur=1.0, peak=-8, note="soft chime")], vol=0.55, cooldown=0.5, desc="Game saved"),
    "sfx.ui.dialogue_blip": S("UI", [C(453037, dur=0.12, peak=-12, note="blip")], vol=0.35, jitter=0.12, cooldown=0.04, desc="Dialogue typewriter"),
    "sfx.inventory.open": S("UI", [C(568992, dur=0.5, peak=-6, note="open")], vol=0.7, jitter=0.03, cooldown=0.1, desc="Inventory opens"),
    "sfx.inventory.close": S("UI", [C(568991, dur=0.5, peak=-6, note="close")], vol=0.7, jitter=0.03, cooldown=0.1, desc="Inventory closes"),
    "sfx.inventory.pickup": S("UI", [C(678385, dur=0.4, peak=-6, note="pickup"), C(678384, dur=0.4, peak=-6, note="pickup 2")],
                              vol=0.7, jitter=0.08, cooldown=0.05, voices=3, desc="Item picked up"),
    "sfx.inventory.drop": S("UI", [C(335751, dur=0.4, peak=-6, note="put"), C(743237, dur=0.4, peak=-6, note="put 2")],
                            vol=0.65, jitter=0.08, cooldown=0.05, desc="Item dropped / moved"),
    "sfx.inventory.equip": S("UI", [C(396429, dur=0.5, peak=-6, note="equip"), C(494797, dur=0.5, peak=-6, note="cloth")],
                             vol=0.7, jitter=0.05, cooldown=0.08, desc="Equip item"),
    "sfx.inventory.unequip": S("UI", [C(494797, dur=0.4, peak=-7, pitch=0.9, note="cloth")], vol=0.6, jitter=0.05, cooldown=0.08, desc="Unequip item"),
    "sfx.inventory.use_potion": S("UI", [C(574077, dur=0.9, peak=-6, note="drink"), C(445970, dur=0.9, peak=-6, note="drink 2")],
                                  vol=0.7, jitter=0.04, cooldown=0.2, desc="Drink potion / consume"),

    # ---- quest
    "sfx.quest.accept": S("Quest", [C(523755, dur=0.8, peak=-6, note="quest")], vol=0.7, cooldown=0.3, desc="Quest accepted"),
    "sfx.quest.objective": S("Quest", [C(459694, dur=0.6, peak=-8, note="objective")], vol=0.6, cooldown=0.3, desc="Objective progress"),
    "sfx.quest.complete": S("Quest", [C(538145, dur=2.0, peak=-4, note="complete")], vol=0.8, cooldown=0.5, desc="Quest complete / turned in"),

    # ---- commerce
    "sfx.shop.buy": S("Commerce", [C(336583, dur=0.7, peak=-6, note="coins"), C(573374, dur=0.7, peak=-6, note="coins 2")],
                      vol=0.7, jitter=0.05, cooldown=0.1, desc="Item bought"),
    "sfx.shop.sell": S("Commerce", [C(223342, dur=0.7, peak=-6, note="coins sell")], vol=0.7, jitter=0.05, cooldown=0.1, desc="Item sold"),
    "sfx.craft.start": S("Commerce", [C(270588, dur=0.9, peak=-6, note="anvil")], vol=0.7, jitter=0.05, cooldown=0.2, desc="Crafting begins"),
    "sfx.craft.complete": S("Commerce", [C(270589, dur=1.2, peak=-5, note="anvil done")], vol=0.8, cooldown=0.3, desc="Crafting complete"),

    # ---- world interaction
    "sfx.world.chest_open": S("World", [C(573654, dur=1.2, peak=-5, note="chest"), C(771164, dur=1.2, peak=-5, note="chest 2")],
                              vol=0.8, jitter=0.04, cooldown=0.3, desc="Chest opens"),
    "sfx.world.unique_pickup": S("World", [C(422975, dur=1.2, peak=-4, note="treasure")], vol=0.8, cooldown=0.3, desc="Unique item pickup"),
    "sfx.world.harvest_wood": S("World", [C(452554, dur=0.6, peak=-5, note="chop"),
                                          C(536736, dur=0.6, peak=-5, note="chop 3")],
                                vol=0.8, jitter=0.07, cooldown=0.1, desc="Chop tree"),
    "sfx.world.harvest_stone": S("World", [C(431019, dur=0.6, peak=-5, note="mine"), C(760567, dur=0.6, peak=-5, note="mine 2"),
                                           C(233630, dur=0.6, peak=-5, note="mine 3")],
                                 vol=0.8, jitter=0.07, cooldown=0.1, desc="Mine rock"),
    "sfx.world.harvest_plant": S("World", [C(396012, dur=0.6, peak=-6, note="grass"), C(396014, dur=0.6, peak=-6, note="grass 2"),
                                           C(396016, dur=0.6, peak=-6, note="grass 3")],
                                 vol=0.7, jitter=0.08, cooldown=0.1, desc="Gather plant"),
    "sfx.world.resource_depleted": S("World", [C(590096, dur=0.8, peak=-6, note="thump")], vol=0.6, cooldown=0.2, desc="Resource node depleted"),
    "sfx.world.gate_exit": S("World", [C(441225, dur=1.2, peak=-6, note="transition")], vol=0.6, cooldown=0.5, desc="Scene transition"),

    # ---- fishing
    "sfx.fishing.cast": S("Fishing", [C(725426, dur=0.8, peak=-6, note="cast"), C(853287, dur=0.8, peak=-6, note="cast 2")],
                          vol=0.75, jitter=0.05, cooldown=0.3, desc="Rod cast"),
    "sfx.fishing.splash": S("Fishing", [C(507094, dur=0.8, peak=-6, note="splash"), C(321490, dur=0.8, peak=-6, note="splash 2")],
                            vol=0.7, jitter=0.06, cooldown=0.2, desc="Bobber lands / fish splash"),
    "sfx.fishing.bite": S("Fishing", [C(649003, dur=0.5, peak=-5, note="bite")], vol=0.8, cooldown=0.3, desc="Fish bites"),
    "sfx.fishing.reel": S("Fishing", [C(725424, dur=0.6, peak=-8, note="reel"), C(831928, dur=0.6, peak=-8, note="reel 2")],
                          vol=0.55, jitter=0.06, cooldown=0.15, desc="Reeling tick"),
    "sfx.fishing.catch": S("Fishing", [C(464701, dur=1.2, peak=-5, note="catch")], vol=0.8, cooldown=0.3, desc="Fish caught"),
    "sfx.fishing.fail": S("Fishing", [C(740423, dur=0.7, peak=-9, pitch=0.7, note="fail")], vol=0.6, cooldown=0.3, desc="Fish escaped"),

    # ---- farming
    "sfx.farm.plant": S("Farming", [C(387083, dur=0.7, peak=-6, note="plant")], vol=0.7, jitter=0.06, cooldown=0.1, desc="Plant seed"),
    "sfx.farm.harvest": S("Farming", [C(699491, dur=0.7, peak=-6, note="pull")], vol=0.75, jitter=0.06, cooldown=0.1, desc="Harvest crop"),
    "sfx.farm.water": S("Farming", [C(507094, dur=0.8, peak=-8, pitch=1.2, note="water")], vol=0.6, jitter=0.06, cooldown=0.2, desc="Water crop"),

    # ---- footsteps (fallback when the tile gives no clip)
    "sfx.step.stone": S("World", [C(517137, dur=0.3, peak=-8, note="stone"), C(390764, dur=0.3, peak=-8, note="stone 2"),
                                  C(197780, dur=0.3, peak=-8, note="stone 3")],
                        vol=0.7, jitter=0.07, cooldown=0.12, desc="Footstep on stone"),
    "sfx.step.sand": S("World", [C(464699, dur=0.4, peak=-8, note="sand"), C(778568, dur=0.4, peak=-8, note="sand 2")],
                       vol=0.7, jitter=0.07, cooldown=0.12, desc="Footstep on sand"),
    "sfx.step.grass": S("World", [C(421135, dur=0.4, peak=-8, note="grass"), C(151235, dur=0.4, peak=-8, note="grass 2"),
                                  C(390760, dur=0.4, peak=-8, note="grass 3")],
                        vol=0.7, jitter=0.07, cooldown=0.12, desc="Footstep on grass"),

    # ---- ambience loops
    "sfx.amb.forest": S("Ambience", [C(273538, start=2.0, dur=40.0, peak=-8, xfade=2.0, hp=120, note="forest birds")],
                        vol=0.5, loop=True, desc="Forest / village day"),
    "sfx.amb.village": S("Ambience", [C(432066, start=2.0, dur=40.0, peak=-8, xfade=2.0, hp=120, note="field ambience")],
                         vol=0.5, loop=True, desc="Village"),
    "sfx.amb.beach": S("Ambience", [C(852826, start=0.5, dur=20.0, peak=-8, xfade=1.5, note="ocean loop")],
                       vol=0.55, loop=True, desc="Beach / water arena"),
    "sfx.amb.campfire": S("Ambience", [C(637523, start=1.0, dur=30.0, peak=-9, xfade=2.0, note="campfire")],
                          vol=0.45, loop=True, desc="Campfire"),
    "sfx.amb.wind": S("Ambience", [C(527281, start=2.0, dur=40.0, peak=-9, xfade=2.0, note="wind")],
                      vol=0.5, loop=True, desc="Wind arena / plains"),
    "sfx.amb.cave": S("Ambience", [C(583474, start=2.0, dur=45.0, peak=-9, xfade=2.5, note="dark atmosphere")],
                      vol=0.5, loop=True, desc="Earth arena / ruins"),
    "sfx.amb.night": S("Ambience", [C(450573, start=1.0, dur=30.0, peak=-9, xfade=2.0, note="night crickets")],
                       vol=0.4, loop=True, desc="Night"),
}
