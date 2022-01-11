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
        IMyCockpit cockpit;
        int Tick = 0;
        bool RadarActive;
        static bool DEBUG = false;
        Vector3D TestVector = new Vector3D(13422,144388,-109465);

        public Program()
        {
            //gr = GridTerminalSystem.GetBlockWithName("TGyro") as IMyGyro;
            gr = new List<IMyGyro>();
            Timer = GridTerminalSystem.GetBlockWithName("Timer") as IMyTimerBlock;
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gr);
            RemCon = GridTerminalSystem.GetBlockWithName("RemCon") as IMyRemoteControl;
            cockpit = GridTerminalSystem.GetBlockWithName("Cockpit") as IMyCockpit;
            cockpit.GetSurface(1).ContentType = ContentType.TEXT_AND_IMAGE;
            cockpit.GetSurface(2).ContentType = ContentType.TEXT_AND_IMAGE;
            cockpit.GetSurface(1).FontSize = 2f;
            cockpit.GetSurface(2).FontSize = 2f;
            if (DEBUG)
            {
                lcd1 = GridTerminalSystem.GetBlockWithName("lcd1") as IMyTextPanel;
                lcd1.ContentType = ContentType.TEXT_AND_IMAGE;
                lcd1.FontSize = 1.2f;
                lcd2 = GridTerminalSystem.GetBlockWithName("lcd2") as IMyTextPanel;
                lcd2.ContentType = ContentType.TEXT_AND_IMAGE;
                lcd2.FontSize = 1.2f;
            }
            HH = new HomingHead(this);
        }



        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update1)
            {
                Tick++;
                HH.TargetInfoOutput();
                HH.Update();
            }
            else
            {
                switch (argument)
                {
                    case "TryLock":
                        HH.Lock(true, 15000);
                        if (HH.CurrentTarget.EntityId != 0)
                            RadarActive = true;
                        else
                            RadarActive = false;
                        if (RadarActive)
                            Runtime.UpdateFrequency = UpdateFrequency.Update1;
                        break;
                    case "Stop":
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        HH.StopLock();
                        RadarActive = false;
                        HH.Locked = false;
                        HH.TargetInfoOutput();
                        break;
                }
            }
        }

        public static Vector3D WorldToGrid(Vector3 world_position, Vector3 GridPosition, MatrixD matrix)
        {
            Vector3D position = world_position - GridPosition;
            double num1 = (position.X * matrix.M11 + position.Y * matrix.M12 + position.Z * matrix.M13);
            double num2 = (position.X * matrix.M21 + position.Y * matrix.M22 + position.Z * matrix.M23);
            double num3 = (position.X * matrix.M31 + position.Y * matrix.M32 + position.Z * matrix.M33);
            return new Vector3D(num1, num2, num3);
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
            public bool Locked;
            public Vector3D O;//Координаты точки первого захвата лок

            public HomingHead (Program MyProg)
            {
                ParentProgram = MyProg;
                CamIndex = 0;
                Locked = false;
                CamArray = new List<IMyTerminalBlock>();
                ParentProgram.GridTerminalSystem.SearchBlocksOfName(Prefix, CamArray);

                foreach (IMyCameraBlock cam in CamArray)
                {
                    cam.EnableRaycast = true;
                    //ParentProgram.lcd1.WriteText(" " + cam.CustomName + " - ", true);
                }
            }

            public void Lock(bool TryLock = false, double InitialRange = 10000)
            {
                int initCamIndex = CamIndex++;
                if (CamIndex >= CamArray.Count)
                    CamIndex = 0;
                MyDetectedEntityInfo lastDetectedInfo;
                bool CanScan = true;
                // найдем первую после использованной в последний раз камеру, которая способна кастануть лучик на заданную дистанцию. 
                if (CurrentTarget.EntityId == 0)
                    TargetDistance = InitialRange;

                while ((CamArray[CamIndex] as IMyCameraBlock)?.CanScan(TargetDistance) == false)
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
                //если такая камера в массиве найдена - кастуем ей луч. 
                if (CanScan)
                {
                    //в случае, если мы осуществляем первоначальный захват цели, кастуем луч вперед 
                    if ((TryLock) && (CurrentTarget.IsEmpty()))
                    {
                        lastDetectedInfo = (CamArray[CamIndex] as IMyCameraBlock).Raycast(InitialRange, 0, 0);
                        if ((!lastDetectedInfo.IsEmpty()) && (lastDetectedInfo.Relationship != MyRelationsBetweenPlayerAndBlock.Owner))
                        {
                            Locked = true;
                            Vector3D deep_point = lastDetectedInfo.HitPosition.Value +
                                Vector3D.Normalize(lastDetectedInfo.HitPosition.Value - CamArray[CamIndex].GetPosition()) * 5;
                            O = WorldToGrid(lastDetectedInfo.HitPosition.Value, lastDetectedInfo.Position, lastDetectedInfo.Orientation);
                        }
                    }
                    else //иначе - до координат предполагаемого нахождения цели.	 
                        lastDetectedInfo = (CamArray[CamIndex] as IMyCameraBlock).Raycast(correctedTargetLocation);
                    //если что-то нашли лучем, то захват обновлен	 
                    if ((!lastDetectedInfo.IsEmpty()) && (lastDetectedInfo.Relationship != MyRelationsBetweenPlayerAndBlock.Owner))
                    {
                        Locked = true;
                        CurrentTarget = lastDetectedInfo;
                        LastLockTick = ParentProgram.Tick;
                        TicksPassed = 0;
                    }
                    else //иначе - захват потерян 
                    {
                        Locked = false;
                        //CurrentTarget = lastDetectedInfo;
                    }
                }
            }
                public void StopLock()
            {
                CurrentTarget = (CamArray[0] as IMyCameraBlock).Raycast(0, 0, 0);
            }

            public void TargetInfoOutput()
            {
                if (CurrentTarget.EntityId != 0 && DEBUG)
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
                    ParentProgram.lcd1.WriteText("Cam array info: " + " \n", false);
                    ParentProgram.lcd1.WriteText("Cam quantity: " + CamArray.Count.ToString() + " \n", false);
                    ParentProgram.lcd1.WriteText("Cam: " + CamArray[CamIndex].CustomName + " \n", true);
                    ParentProgram.lcd1.WriteText("Distance: " +  TargetDistance.ToString() + " \n", true);
                    ParentProgram.lcd1.WriteText("Delay: " + Math.Round(TargetDistance * 0.03 / CamArray.Count, 0).ToString() + " \n", true);
                    ParentProgram.lcd1.WriteText("Locked: " + Locked.ToString() + " \n", true);
                
                }
                else if (DEBUG)
                    ParentProgram.lcd2.WriteText("NO TARGET \n", false);
                    //ParentProgram.lcd1.WriteText("Locked: " + Locked.ToString() + " \n", false);
                else if (!DEBUG)
                {
                    ParentProgram.cockpit.GetSurface(1).WriteText("Name\n" + CurrentTarget.Name + " \n", false);
                    ParentProgram.cockpit.GetSurface(1).WriteText("Velocity:" + Math.Round(CurrentTarget.Velocity.Length(), 2).ToString() + " \n", true);
                    ParentProgram.cockpit.GetSurface(1).WriteText("Distance: " + Math.Round(TargetDistance, 2).ToString() + " \n", true);
                    ParentProgram.cockpit.GetSurface(2).WriteText("Locked: " + Locked.ToString() + " \n", false);
                    ParentProgram.cockpit.GetSurface(2).WriteText("Camera: " + CamArray[CamIndex].CustomName + " \n", true);
                }

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
                        Lock();
                    }
                }
            }

        }
    }
}
