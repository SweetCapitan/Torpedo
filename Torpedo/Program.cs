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
        IMyTimerBlock Timer;
        List<IMyGyro> gr;
        HomingHead HH;
        int Tick = 0;
        bool RadarActive;
        Vector3D TestVector = new Vector3D(13422,144388,-109465);

        public Program()
        {
            //gr = GridTerminalSystem.GetBlockWithName("TGyro") as IMyGyro;
            gr = new List<IMyGyro>();
            Timer = GridTerminalSystem.GetBlockWithName("Timer") as IMyTimerBlock;
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gr);
            RemCon = GridTerminalSystem.GetBlockWithName("RemCon") as IMyRemoteControl;
            lcd1 = GridTerminalSystem.GetBlockWithName("lcd1") as IMyTextPanel;
            lcd1.ContentType = ContentType.TEXT_AND_IMAGE;
            lcd1.FontSize = 1.2f;
            lcd2 = GridTerminalSystem.GetBlockWithName("lcd2") as IMyTextPanel;
            lcd2.ContentType = ContentType.TEXT_AND_IMAGE;
            lcd2.FontSize = 1.2f;
            HH = new HomingHead(this);
        }



        public void Main(string argument, UpdateType updateSource)
        {
            Tick++;
            // разбираем аргументы, с которыми скриптбыл запущен
            if (argument == "TryLock")
            {
                HH.Lock(true, 15000); 
                if (HH.CurrentTarget.EntityId != 0)
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
            private static string Prefix = "Камера";
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
                    //ParentProgram.lcd1.WriteText(" " + cam.CustomName + " - ", true);
                }
            }

            public void Lock(bool TryLock = false, double InitialRange = 10000)
            {
                int initCamIndex = CamIndex;
                MyDetectedEntityInfo lastDetectedInfo = CurrentTarget;
                bool CanScan = true;
                if (CurrentTarget.EntityId == 0) 
                    TargetDistance = InitialRange;

                while ((CamArray[CamIndex] as IMyCameraBlock).CanScan(TargetDistance) == false)
                {
                    CamIndex++;
                    if (CamIndex >= CamArray.Count) 
                        CamIndex = 0;

                    if (CamIndex == initCamIndex)
                    {
                        CanScan = false;
                        break;
                    }
                     
                }

                if (CanScan)
                {
                    if ((TryLock) && (CurrentTarget.EntityId == 0)) 
                        lastDetectedInfo = (CamArray[CamIndex] as IMyCameraBlock).Raycast(InitialRange, 0, 0);
                    else 
                        lastDetectedInfo = (CamArray[CamIndex] as IMyCameraBlock).Raycast(correctedTargetLocation);

                    if (lastDetectedInfo.EntityId != 0)
                    {
                        CurrentTarget = lastDetectedInfo;
                        LastLockTick = ParentProgram.Tick;
                        TicksPassed = 0;
                    }
                    else
                    {
                        ParentProgram.lcd1.WriteText("Target Lost" + "\n", false);
                        CurrentTarget = lastDetectedInfo;
                    }
                    CamIndex++;
                    if (CamIndex >= CamArray.Count) 
                        CamIndex = 0;
                }

            }
            public void StopLock()
            {
                CurrentTarget = (CamArray[0] as IMyCameraBlock).Raycast(0, 0, 0);
            }

            public void TargetInfoOutput()
            {
                if (CurrentTarget.EntityId != 0)
                {
                    ParentProgram.lcd2.WriteText("Target Info \n", false);
                    ParentProgram.lcd2.WriteText(CurrentTarget.EntityId + " \n", true);
                    ParentProgram.lcd2.WriteText(CurrentTarget.Type + " \n", true);
                    ParentProgram.lcd2.WriteText(CurrentTarget.Name + " \n", true);
                    ParentProgram.lcd2.WriteText("Position: \n", true);
                    ParentProgram.lcd2.WriteText("Size: " + CurrentTarget.BoundingBox.Size.ToString("0.0") + " \n", true);
                    ParentProgram.lcd2.WriteText("X: " + Math.Round(CurrentTarget.Position.GetDim(0), 2).ToString() + " \n", true);
                    ParentProgram.lcd2.WriteText("Y: " + Math.Round(CurrentTarget.Position.GetDim(1), 2).ToString() + " \n", true);
                    ParentProgram.lcd2.WriteText("Z: " + Math.Round(CurrentTarget.Position.GetDim(2), 2).ToString() + " \n", true);
                    ParentProgram.lcd2.WriteText("Velocity:" + Math.Round(CurrentTarget.Velocity.Length(), 2).ToString() + " \n", true);
                    ParentProgram.lcd2.WriteText("Distance: " + Math.Round(TargetDistance, 2).ToString() + " \n", true);
                }
                else
                    ParentProgram.lcd2.WriteText("NO TARGET \n", false);
            }

            public void Update()
            {
                MyPos = CamArray[0].GetPosition();
                if (CurrentTarget.EntityId != 0)
                {
                    TicksPassed = ParentProgram.Tick - LastLockTick;
                    correctedTargetLocation = CurrentTarget.Position + (CurrentTarget.Velocity * TicksPassed / 60);
                    TargetDistance = (correctedTargetLocation - MyPos).Length() + 10;

                    if (TicksPassed>TargetDistance*0.03/CamArray.Count)
                    {
                        ParentProgram.lcd1.WriteText("Cam array info: " + " \n", false);
                        ParentProgram.lcd1.WriteText("Cam quantity: " + CamArray.Count.ToString() +" \n", false) ;
                        ParentProgram.lcd1.WriteText("Cam: " + CamArray[CamIndex].CustomName + " \n", true);
                        ParentProgram.lcd1.WriteText("Distance: " + TargetDistance.ToString() + " \n", true);
                        ParentProgram.lcd1.WriteText("Delay: " + Math.Round(TargetDistance * 0.03 / CamArray.Count, 0).ToString() + " \n", true);

                        TargetInfoOutput();
                    }
                }
            }

        }
    }
}
