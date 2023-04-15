using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public IMyGridTerminalSystem GTS;
        private string _broadCastTag = "TC";
        public List<IMyCargoContainer> CargoList = new List<IMyCargoContainer>();
        public List<IMyReactor> ReactorList = new List<IMyReactor>();
        public List<IMyBatteryBlock> BatteryList = new List<IMyBatteryBlock>();
        public List<IMyRefinery> RefineryList = new List<IMyRefinery>();
        public List<IMyAssembler> AssemblerList = new List<IMyAssembler>();
        public IMyTextSurface LCD;
        public IMyBroadcastListener MyBroadcastListener;
        public IMyCargoContainer SorterCargo;
        public IMyCargoContainer OreCargo;
        public IMyCargoContainer DetailsCargo;
        public IMyCargoContainer DumpCargo;

        public string[] IngotWhitelist =
        {
            "Iron",
            "Nickel",
            "Cobalt",
            "Silver",
            "Gold",
            "Uranium",
            "Prometheus_Core",
            "Magnesium",
            "Copper",
            "Platinum"
        };

        public string[] ComponentWhitelist =
        {
            "Aluminium Plate",
            "Superconductor",
            "Prometeusingot",
            "ZoneChip",
            "PowerCell",
            "InteriorPlate",
            "SteelPlate",
            "Computer",
            "Girder",
            "LargeTube",
            "SteelBolt",
            "Motor",
            "Reactor",
            "RadioCommunication",
            "SolarCell",
            "BulletproofGlass",
            "Construction",
            "Thrust",
            "Medical",
            "Reactor",
            "MetalGrid",
            "SmallTube",
            "Display",
        };

        public Program()
        {
            GTS = GridTerminalSystem;
            GTS.GetBlocksOfType<IMyCargoContainer>(CargoList,
                (block => block.CustomName.Contains("Storage") && block.CubeGrid == Me.CubeGrid));
            GTS.GetBlocksOfType<IMyReactor>(ReactorList,
                (block => block.IsFunctional && block.CubeGrid == Me.CubeGrid));
            GTS.GetBlocksOfType<IMyBatteryBlock>(BatteryList, (block => block.CubeGrid == Me.CubeGrid));
            GTS.GetBlocksOfType<IMyRefinery>(RefineryList, (block => block.CubeGrid == Me.CubeGrid));
            SorterCargo = CargoList.Find(block => block.CustomName.Contains("Sorter")); //todo make its convigurable
            OreCargo = CargoList.Find(block => block.CustomName.Contains("Ore"));
            DetailsCargo = CargoList.Find(block => block.CustomName.Contains("Details"));
            DumpCargo = CargoList.Find(block => block.CustomName.Contains("Dump"));
            LCD = GTS.GetBlockWithName("Lcd_Info_Surface") as IMyTextSurface;
            var PB_LCD = Me.GetSurface(0);
            SetupTextSurface(LCD, ContentType.TEXT_AND_IMAGE, 0.6f);
            SetupTextSurface(PB_LCD, ContentType.TEXT_AND_IMAGE, 1f);
            PB_LCD.WriteText("KSB Base\nmainframe");
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void SetupTextSurface(IMyTextSurface surface, ContentType type, float fontSize)
        {
            surface.ContentType = type;
            surface.FontSize = fontSize;
            surface.Font = "DEBUG";
            surface.FontColor = new Color(255, 134, 0);
        }

        public List<MyInventoryItem> GetStatsOfAllStorageContainer()
        {
            var listItems = new List<MyInventoryItem>();
            foreach (IMyCargoContainer cargo in CargoList)
            {
                var storage = cargo.GetInventory();
                foreach (var ingot in IngotWhitelist)
                {
                    storage.GetItems(listItems,
                        (item => item.Type.TypeId == "MyObjectBuilder_Ingot" && item.Type.SubtypeId == ingot));
                }
            }

            return listItems;
        }

        public void SortOre(IMyInventory storage)
        {
            var oreStorage = OreCargo.GetInventory();
            if (storage == oreStorage)
            {
                return;
            }

            var listItems = new List<MyInventoryItem>();
            foreach (var ingot in IngotWhitelist)
            {
                storage.GetItems(listItems,
                    (item => item.Type.TypeId == "MyObjectBuilder_Ingot" && item.Type.SubtypeId == ingot));
                foreach (var item in listItems)
                {
                    if (!storage.TransferItemTo(oreStorage, item))
                    {
                        return;
                    }
                    // Echo("Transfered");
                }

                listItems.Clear();
            }
        }

        public string AccumulatorsPercentageBar()
        {
            var totalCharge = 0f;
            var maxCharge = 0f;
            foreach (var battery in BatteryList)
            {
                totalCharge += battery.CurrentStoredPower;
                maxCharge += battery.MaxStoredPower;
            }

            var chargePercents = totalCharge / maxCharge * 100;
            var state = chargePercents <= 50f;
            foreach (var reactor in ReactorList)
            {
                reactor.Enabled = state;
            }

            return ProgressBar(totalCharge, maxCharge);
        }

        public void SortDetails(IMyInventory storage)
        {
            var detailsStorage = DetailsCargo.GetInventory();
            if (storage == detailsStorage)
            {
                return;
            }

            var listItems = new List<MyInventoryItem>();
            foreach (var component in ComponentWhitelist)
            {
                storage.GetItems(listItems,
                    item => item.Type.TypeId == "MyObjectBuilder_Component" && item.Type.SubtypeId == component);
                foreach (var item in listItems)
                {
                    //TODO Better Sort to Groups
                    if (!storage.TransferItemTo(detailsStorage, item))
                    {
                        return;
                    }
                    // Echo("Transfered");
                }

                listItems.Clear();
            }
        }

        public void SortTrash(IMyInventory storage)
        {
            var dumpStorage = DumpCargo.GetInventory();
            if (storage == dumpStorage)
            {
                return;
            }

            var listItems = new List<MyInventoryItem>();
            storage.GetItems(listItems);
            foreach (var item in listItems)
            {
                // Echo($"{item.Type}: {item.Type.SubtypeId}");
                //TODO Better Sort to Groups
                if (!storage.TransferItemTo(dumpStorage, item))
                {
                    return;
                }
                // Echo("Transfered");
            }

            listItems.Clear();
        }

        public void ClearAllInventory()
        {
            foreach (var refinery in RefineryList)
            {
                ClearInventory(refinery.GetInventory());
            }

            foreach (var assembler in AssemblerList)
            {
                ClearInventory(assembler.GetInventory());
            }

            foreach (var cargo in CargoList)
            {
                ClearInventory(cargo.GetInventory());
            }
        }

        public List<IMyInventory> GetAllStorages()
        {
            return CargoList.Select(cargo => cargo.GetInventory()).ToList();
        }

        
        /// <summary>
        /// Passes through all storages and return string that's contains max and current volume of StorageContainers in
        /// current grid
        /// </summary>
        /// <returns></returns>
        public string TotalVolumesOfStorages()
        {
            var currentVolume = 0;
            var maxVolume = 0;
            foreach (var storage in GetAllStorages())
            {
                currentVolume += storage.CurrentVolume.ToIntSafe();
                maxVolume += storage.MaxVolume.ToIntSafe();
            }

            return $"Current using {currentVolume}k l of {maxVolume}k l\n";
        }

        public string ProgressBar(float currentValue, float maxValue, int barLength = 20, char barFullFiller = '=',
            char barEmptyFiller = ' ')
        {
            var progressBar = "";
            barLength = barLength - 2;
            var progress = (currentValue / maxValue * 100) / (100 / ((float) barLength));
            for (int i = 0; i < progress; i++)
            {
                progressBar += barFullFiller;
            }

            if (progressBar.Length < barLength)
            {
                for (int j = 0; j < barLength - progress; j++)
                {
                    progressBar += barEmptyFiller;
                }
            }

            return $"[{progressBar}]";
        }


        /// <summary>
        /// Sort Items in given storage. Sends each group of items to its own unique storage.
        /// </summary>
        /// <param name="storage"></param>
        public void ClearInventory(IMyInventory storage)
        {
            SortOre(storage);
            SortDetails(storage);
            SortTrash(storage);
        }


        /// <summary>
        /// Entry point for all script
        /// </summary>
        public void RunAll()
        {
            LCD.WriteText("");
            foreach (var item in GetStatsOfAllStorageContainer())
            {
                LCD.WriteText($"{item.Type.SubtypeId}: {item.Amount.ToIntSafe().ToString()}\n", true);
            }

            LCD.WriteText(TotalVolumesOfStorages(), true);
            LCD.WriteText(AccumulatorsPercentageBar(), true);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource >= (UpdateType) 32)
            {
                RunAll();
                // var arguments = argument.Split(';');
            }
            else
            {
                switch (argument)
                {
                    case "Sort":
                        // ClearInventory(SorterCargo.GetInventory());
                        ClearAllInventory();
                        break;
                }
            }
        }
        //TODO SORT IN ALL INVENTORYEs
    }
}