using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DragoonMayCry.Cache;
using DragoonMayCry.Data;
using DragoonMayCry.Util;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;

namespace DragoonMayCry.State
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

            ushort? iLvlSyncForInstance = territoryCache.GetRow(territory)?.ContentFinderCondition?.Value?.ItemLevelSync;
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

            int totalILvl = 0;
            int itemCount = 0;
            for (uint i = 0; i < equippedItems->Size; ++i)
            {
                var rawItem = equippedItems->Items[i];
                uint itemId = rawItem.ItemId;

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
                var finalIlvl = isItemSynced ? Math.Max((int)iLvlSyncForInstance, (int)iLvlSyncForContentLvl) : (int)itemIlvl;
#if DEBUG
                Service.Log.Debug($"Processing {item.Name} with iLvl {finalIlvl}");
#endif
                totalILvl += finalIlvl;
                itemCount++;
            }

            itemCount = Math.Max(itemCount, 11);
            int iLvl = (int)Math.Round((float)totalILvl / (float)itemCount);
            Service.Log.Debug($"Calculated ilvl {iLvl} with {itemCount} items");
            return iLvl;
        }
    }
}
