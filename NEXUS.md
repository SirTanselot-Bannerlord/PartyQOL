# Party QOL

**Module id / GitHub:** `PartyQOL`  
**Source:** https://github.com/SirTanselot-Bannerlord/PartyQOL

Paste the **Summary** into Nexus “About this mod”, and everything under **Description** into the long description.

---

## Summary

Sort and filter troops on the party screen, and switch either side between your party, a nearby clan party, and your garrison without closing it. Absorb one of your clan's war parties from conversation. Bulk-upgrade ready troops, and remember a path when a unit branches.

## Description

Sort and filter the party screen, switch parties without leaving it, and fold a clan war party into yours.

Vanilla makes a large party slow to manage. Troops stay in recruit order, wounded men sit inside a mixed stack, and moving people between your party, a companion, and the garrison means closing one screen and opening another. Emptying a companion's party is one stack at a time. A troop with two upgrade paths has to be clicked through by hand.

This mod puts sort, filter, and party switching on the party screen, adds a conversation line to absorb a clan war party, and upgrades ready troops in one click.

### **Details**

**Sort**

- **Bar:** Type, Tier, Max Tier, and Culture ship with the mod. The gear opens the editor. Saving a preset does not select it; pick it on the bar to apply it. Up to 16 presets. Those four cannot be renamed or deleted.
- **Keys:** Type, tier, max tier, culture, level, name, and stack size. Each key sorts up or down. Type and culture have their own order.
- **Type:** Horse archers, cavalry, archers, then infantry, highest tier first, then culture, then name.
- **Max tier:** The highest tier that troop's upgrade line can reach, so a low-tier noble sorts with the tier 6 they can become.
- The screen opens on Type. One sort applies to both sides.

**Filter**

- **Bar:** The screen opens on No filter. Wounded and Healthy ship with the mod. The gear saves more, up to 16. Wounded and Healthy cannot be renamed or deleted.
- **Criteria:** Type, culture, tier, max tier, level, stack size, and wounded or healthy. Max tier is how far that line can upgrade.
- **Counts:** When the filter hides anyone, the troop line turns red: shown / full party / size limit. Wounded and Healthy also change each row, and the formation icons, to that count only.
- **Moves:** Transfer All and a stack click move only what the filter is showing. Wounded moves the wounded. Healthy leaves them behind.
- Heroes stay on the list.

**Parties**

- **Either side:** Your party, a clan war party in the same army, the same settlement, or close on the map, and the garrison of a settlement you own while you are standing in it. The left side can be Dismiss. The screen stays open.
- **Garrison:** In a town you own, Manage Garrison and Manage Prisoners open one screen: the garrison and the dungeon.
- **Absorb:** Talk to the leader of your own clan war party. Troops, prisoners, inventory, and ships fold into your party, and that party is removed. Not in an army, a battle, or a siege. Caravans, patrols, militia, villagers, and garrisons cannot be absorbed.

**Upgrades**

- **Upgrade all:** Upgrades every ready troop that has one path, or a path you marked. Branching troops with no mark are skipped.
- **Preferred path:** Ctrl + right-click an upgrade icon to set or clear it. Stored in the save. The marked icon stays highlighted.
- **Free only:** Shift-click the button to upgrade while the gold cost stays 0.

### **Requirements**

Tested with Bannerlord **v1.5.7**, Harmony **v2.4.2**, ButterLib **v2.12.0**, UIExtenderEx **v3.0.3**. No MCM.

### **Compatibility**

Safe to add to an existing save. Preferred upgrade paths are stored in the save. Sort and filter presets are stored in the Bannerlord config folder and are shared across saves. War Sails is optional; ships move when a party is absorbed. May conflict with mods that replace the party screen.

### **Other mods by me**

- [Same Rules for All Heroes](https://www.nexusmods.com/mountandblade2bannerlord/mods/13937) — Consistent attribute and focus point totals for every hero — player, companions, lords, and children.
- [Perk Fixes and Rebalance](https://www.nexusmods.com/mountandblade2bannerlord/mods/13951) — Fixes inconsistent perk effects and descriptions. Rebalances a handful of overtuned or misplaced effects.
- [Perk QoL - Respec and Highlighting](https://www.nexusmods.com/mountandblade2bannerlord/mods/13954) — Respec perks directly on the character screen. Highlight perks based on role or matching search text.

### Changelog

**v1.0.0**

- Sort troops by type, tier, culture, level, name, or stack size
- Filter by type, culture, tier, stack size, wounded, or healthy
- Switch either side among your party, clan parties, and your garrison
- Absorb a clan war party from conversation
- Bulk-upgrade ready troops, with a saved upgrade path
