using DragoonMayCry.State;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;

namespace DragoonMayCry.Score
{
    public unsafe class ItemLevelCalculator
    {
        private readonly ExcelSheet<ParamGrow> growthSheet;
        private readonly InventoryManager* inventoryManager;
        private readonly ExcelSheet<Item> itemSheet;
        private readonly ExcelSheet<TerritoryType> territorySheet;

        public ItemLevelCalculator()
        {
            inventoryManager = InventoryManager.Instance();
            itemSheet = Service.DataManager.GetExcelSheet<Item>();
            territorySheet = Service.DataManager.GetExcelSheet<TerritoryType>();
            growthSheet = Service.DataManager.GetExcelSheet<ParamGrow>();
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

            // The iLvl sync for a specific instance ex: UCOB is 345, UWU is 375
            var iLvlSyncForInstance = territorySheet.GetRow(territory).ContentFinderCondition.Value.ItemLevelSync;

            // The iLvl sync in general for the content lvl. ex : at lvl 70 it 400, at lvl 90 it is 660
            ushort? iLvlSyncForContentLvl = 0;
            if (isLvlSync)
            {
                iLvlSyncForContentLvl = growthSheet.GetRow(playerCharacter.Level).ItemLevelSync;
            }

            var equippedItems = inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);
            if (equippedItems == null || equippedItems->Size == 0)
            {
                return 0;
            }

            uint totalILvl = 0;
            var itemCount = 0;
            for (uint i = 0; i < equippedItems->Size; ++i)
            {
                var rawItem = equippedItems->Items[i];
                var itemId = rawItem.ItemId;

                if (itemId == 0)
                {
                    continue;
                }

                var item = itemSheet.GetRow(itemId);
                if (item.RowId == 0)
                {
                    continue;
                }

                var itemEquipSlotCategory = item.EquipSlotCategory;
                if (itemEquipSlotCategory.Value.SoulCrystal == 1)
                {
                    continue;
                }

                var itemLevel = item.LevelItem.Value.RowId;
                var isItemSynced =
                    isLvlSync && iLvlSyncForInstance > 0 && itemLevel > iLvlSyncForInstance ||
                    iLvlSyncForContentLvl > 0 && itemLevel > iLvlSyncForContentLvl;
                if (isItemSynced)
                {
                    if (iLvlSyncForInstance > 0 && iLvlSyncForContentLvl > 0)
                    {
                        // the instance ilvl sync overrides the general content level ilvl sync
                        itemLevel = iLvlSyncForInstance;
                    }
                    else
                    {
                        itemLevel = Math.Max(iLvlSyncForInstance!,
                                             (uint)iLvlSyncForContentLvl!);
                    }
                }

                totalILvl += itemLevel;
                itemCount++;
                if (itemEquipSlotCategory.Value.Head == -1)
                {
                    totalILvl += itemLevel;
                    itemCount++;
                }

                if (itemEquipSlotCategory.Value.Legs == -1)
                {
                    totalILvl += itemLevel;
                    itemCount++;
                }

                if (itemEquipSlotCategory.Value.Feet == -1)
                {
                    totalILvl += itemLevel;
                    itemCount++;
                }

                if (itemEquipSlotCategory.Value.Gloves == -1)
                {
                    totalILvl += itemLevel;
                    itemCount++;
                }

                if (itemEquipSlotCategory.Value.OffHand == -1)
                {
                    totalILvl += itemLevel;
                    itemCount++;
                }
            }

            itemCount = Math.Max(itemCount, 12);
            var iLvl = (int)Math.Round(totalILvl / (float)itemCount, MidpointRounding.ToZero);
            return iLvl;
        }
    }
}
