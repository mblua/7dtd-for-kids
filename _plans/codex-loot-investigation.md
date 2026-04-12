# Zombie Death & Loot System Investigation

## 1. What happens when a zombie dies?

The entity does NOT get destroyed immediately. Flow:

1. `OnEntityDeath` fires
2. Ragdoll activates (`EnableRagdoll` / `ActivateDynamicRagdoll`) — same mesh, physics bones enabled
3. Entity stays as ragdoll for `ticksStayAfterDeath` (scaled by `cTimeStayAfterDeathScale`)
4. **Corpse block drops** at death position: `dropCorpseBlock` with `corpseBlockChance` probability
   - Creates a world block with `CorpseBlockId` (the gore blocks we already replace with rubble in blocks.xml)
   - Block has `TileEntityGoreBlock` as its tile entity — THIS is the loot container
5. Alternatively: `LootDropEntityClass` spawns an `EntityLootContainer` (loot bag entity)
   - Controlled by `PropLootDropEntityClass`, `PropLootDropProb`, `lootListOnDeath`
6. After timer expires: `MarkToUnload` -> `SendDestroyEntityToPlayers` -> entity destroyed

**TL;DR**: Zombie stays as ragdoll temporarily, then gets removed. A SEPARATE corpse block or loot bag entity holds the loot.

## 2. Loot raycast system

Loot is NOT on the zombie entity. It is on:

- **Corpse block** (`TileEntityGoreBlock`): Standard world block interaction (block raycast, same as any container)
- **Loot bag** (`EntityLootContainer`): Entity raycast via `FindHitEntity`
- UI: `XUiC_LootWindow` / `XUiC_LootContainer`

The player never "loots the zombie body" — they loot the corpse block or loot bag that was spawned at the death position. Our blocks.xml already replaces the gore block models with rubble, so the loot container will look like a rubble pile.

## 3. Disabling renderers — does loot break?

**NO. Loot is completely unaffected.** Loot lives on a separate object (corpse block or loot bag entity), not on the zombie entity itself. You can:
- `renderer.enabled = false` — loot works
- Delete all renderers — loot works
- Replace the entire mesh — loot works
- Destroy the entity entirely — loot ALREADY works (the entity gets destroyed after the corpse block/bag spawns)

The zombie's visual state is irrelevant to the loot system.

## 4. Ragdoll and model transform

**There is NO separate ragdoll transform.** Key symbols found:
- `GetModelTransform` / `GetModelTransformParent` — the only model transforms
- `_dynamicRagdoll`, `BlendRagdoll`, `EnableRagdoll` — ragdoll uses the SAME model transform
- No `GetRagdollTransform` exists

The ragdoll system activates physics on the existing bones of the model. It does NOT create a new mesh or swap the transform hierarchy.

**Impact on our MonoBehaviour**: A `ZombieColorScript` attached via `GetModelTransform().gameObject.GetOrAddComponent<>()` will:
- SURVIVE the ragdoll transition (same gameObject)
- Be destroyed when the entity is cleaned up after `ticksStayAfterDeath`
- This is fine — by that time the entity is gone anyway

## 5. Key methods found

| Method | Purpose |
|--------|---------|
| `OnEntityDeath` | Death event handler |
| `SetDead` / `_isDead` | Death state flag |
| `EnableRagdoll` / `ActivateDynamicRagdoll` | Start ragdoll physics |
| `dropCorpseBlock` | Spawn corpse block (gore block) at death pos |
| `corpseBlockChance` / `PropCorpseBlockChance` | Probability of corpse block |
| `CorpseBlockId` | Which block ID to place as corpse |
| `TileEntityGoreBlock` | Loot container on the corpse block |
| `LootDropEntityClass` / `PropLootDropEntityClass` | Alternative: spawn a loot bag entity |
| `lootListOnDeath` / `PropLootListOnDeath` | Which loot list to use |
| `lootDropProb` / `PropLootDropProb` | Loot drop probability |
| `dropItemOnDeath` | Drop equipped items on death |
| `ticksStayAfterDeath` | How long entity stays before cleanup |
| `MarkToUnload` | Queue entity for removal |
| `SendDestroyEntityToPlayers` | Network: remove entity from clients |
| `cLayerPhysicsDead` | Physics layer for dead entities |

## 6. Physics layers

Dead entities are moved to `cLayerPhysicsDead` — a separate physics layer from living entities (`layerEntity`). This means dead entity colliders are on a different layer.

## Summary for the team

- **Safe to replace zombie mesh/renderers with primitives** — loot is on corpse blocks, not on the entity
- **MonoBehaviour on model transform survives ragdoll** — ragdoll uses same transform
- **Our blocks.xml gore block replacement is already correct** — rubble models replace the corpse block visuals
- **No need to worry about renderer.enabled affecting anything** — it is purely visual
