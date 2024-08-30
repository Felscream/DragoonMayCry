using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DragoonMayCry.Cache;
using DragoonMayCry.Data;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;

namespace DragoonMayCry.Score
{
    public unsafe class ItemLevelCalculator
    {
        private readonly InventoryManager* inventoryManager;
        private readonly LuminaCache<Item> itemCache;
        private readonly LuminaCache<TerritoryType> territoryCache;
        private readonly LuminaCache<ParamGrow> growthCache;

        public ItemLevelCalculator()
        {
            inventoryManager = InventoryManager.Instance();
            itemCache = LuminaCache<Item>.Instance;
            territoryCache = LuminaCache<TerritoryType>.Instance;
            growthCache = LuminaCache<ParamGrow>.Instance;
        }

        public int CalculateCurrentItemLevel()
        {
            var playerCharacter = PlayerState.GetInstance().Player;

            if (playerCharacter == null)
            {
                return 0;
            }

            var isLvlSync =
                FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState.Instance()->
                    IsLevelSynced != 0;

            var territory = Service.ClientState.TerritoryType;

            var iLvlSyncForInstance = territoryCache.GetRow(territory)?.ContentFinderCondition?.Value?.ItemLevelSync;
            ushort? iLvlSyncForContentLvl = 0;
            if (isLvlSync)
            {
                iLvlSyncForContentLvl = growthCache.GetRow(playerCharacter.Level)?.ItemLevelSync;
            }

            var equippedItems = inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);
            if (equippedItems == null || equippedItems->Size == 0)
            {
                return 0;
            }

            var totalILvl = 0;
            var itemCount = 0;
            for (uint i = 0; i < equippedItems->Size; ++i)
            {
                var rawItem = equippedItems->Items[i];
                var itemId = rawItem.ItemId;

                if (itemId == 0)
                {
                    continue;
                }

                var item = itemCache.GetRow(itemId);
                if (item == null || item.RowId == 0)
                {
                    continue;
                }

                var itemEquipSlotCategory = item.EquipSlotCategory.Value;
                if (itemEquipSlotCategory == null || itemEquipSlotCategory.SoulCrystal == 1)
                {
#if DEBUG
                    Service.Log.Debug($"Ignoring item {item.Name} equipped on slot {itemEquipSlotCategory} with ilvl {item.LevelItem?.Value!.RowId}");
#endif
                    continue;
                }

                var itemIlvl = item.LevelItem?.Value?.RowId;
                var isItemSynced =
                    iLvlSyncForInstance != 0 && itemIlvl > iLvlSyncForInstance || iLvlSyncForContentLvl != 0 && itemIlvl > iLvlSyncForContentLvl;
                
                var finalIlvl = isItemSynced ? Math.Max((int)iLvlSyncForInstance!, (int)iLvlSyncForContentLvl!) : (int)itemIlvl!;
#if DEBUG
                Service.Log.Debug($"Processing {item.Name} with iLvl {finalIlvl}");
#endif
                totalILvl += finalIlvl;
                itemCount++;
                if (itemEquipSlotCategory.Head == -1)
                {
                    totalILvl += finalIlvl;
                    itemCount++;
                }
                if (itemEquipSlotCategory.Legs == -1)
                {
                    totalILvl += finalIlvl;
                    itemCount++;
                }
                if (itemEquipSlotCategory.Feet == -1)
                {
                    totalILvl += finalIlvl;
                    itemCount++;
                }

                if (itemEquipSlotCategory.Gloves == -1)
                {
                    totalILvl += finalIlvl;
                    itemCount++;
                }
                if (itemEquipSlotCategory.OffHand == -1)
                {
                    totalILvl += finalIlvl;
                    itemCount++;
                }
            }

            itemCount = Math.Max(itemCount, 12);
            var iLvl = (int)Math.Round(totalILvl / (float)itemCount, MidpointRounding.ToZero);
            Service.Log.Debug($"Calculated ilvl {iLvl} with {itemCount} items");
            return iLvl;
        }
    }
}
