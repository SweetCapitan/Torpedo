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

        //-------------------------------


        IMyRemoteControl RemCon;
        IMyTextPanel lcd1, lcd2;
        List<IMyGyro> gr;
        HomingHead HH;
        int Tick = 0;
        bool RadarActive;
        Vector3D TestVector = new Vector3D(13422,144388,-109465);

        public Program()
        {
            //gr = GridTerminalSystem.GetBlockWithName("TGyro") as IMyGyro;
            gr = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gr);
            RemCon = GridTerminalSystem.GetBlockWithName("RemCon") as IMyRemoteControl;
            lcd1 = GridTerminalSystem.GetBlockWithName("lcd1") as IMyTextPanel;
            lcd1.ContentType = ContentType.TEXT_AND_IMAGE;
            lcd1.FontSize = 2f;
            lcd2 = GridTerminalSystem.GetBlockWithName("lcd2") as IMyTextPanel;
            lcd1.ContentType = ContentType.TEXT_AND_IMAGE;
            lcd1.FontSize = 2f;
            HH = new HomingHead(this);
        }



        public void Main(string argument, UpdateType updateSource)
        {
            Tick++;
            // разбираем аргументы, с которыми скриптбыл запущен
            if (argument == "TryLock")
            {
                HH.Lock(true, 15000); if (HH.CurrentTarget.EntityId != 0)
                    RadarActive = true;
                else
                    RadarActive = false;
            }
            else
            {
                HH.Update();
            }
            if (argument == "Stop")
            {
                Runtime.UpdateFrequency = UpdateFrequency.None; HH.StopLock();
                RadarActive = false;
            }// если в захвате находится какой-то объект, то выполнение скрипта зацикливается
            if (RadarActive)
                Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public class HomingHead
        {
            Program ParentProgram;
            private static string Prefix = "Camera";
            private List<IMyTerminalBlock> CamArray;
            private int CamIndex;
            public MyDetectedEntityInfo CurrentTarget;
            public Vector3D MyPos;
            public Vector3D correctedTargetLocation;
            public double TargetDistance;
            public int LastLockTick;
            public int TicksPassed;

            public HomingHead (Program MyProg)
            {
                ParentProgram = MyProg;
                CamIndex = 0;
                CamArray = new List<IMyTerminalBlock>();
                ParentProgram.GridTerminalSystem.SearchBlocksOfName(Prefix, CamArray);
                ParentProgram.lcd1.WriteText("", false);

                foreach (IMyCameraBlock cam in CamArray)
                {
                    cam.EnableRaycast = true;

                }

            }

            public void Lock(bool TryLock = false, double InitialRange = 10000)
            {
                int initCamIndex = CamIndex;
            }
        }
    }
}
