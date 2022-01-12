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

        //------------------------------


        IMyRemoteControl RemCon;
        IMyTextPanel lcd;
        List<IMyGyro> gr;
        int Tick = 0;

        Vector3D Target = new Vector3D(13422,144388,-109465);

        public Program()
        {
            //gr = GridTerminalSystem.GetBlockWithName("TGyro") as IMyGyro;
            gr = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gr);
            RemCon = GridTerminalSystem.GetBlockWithName("RemCon") as IMyRemoteControl;
            lcd = GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }



        public void Main(string argument, UpdateType updateSource)
        {
            Tick++;

            (GridTerminalSystem.GetBlockWithName("TorpedoConnector") as IMyTerminalBlock).ApplyAction("OnOff_Off");
            (GridTerminalSystem.GetBlockWithName("FThrust") as IMyThrust).ThrustOverride = 60000;
            (GridTerminalSystem.GetBlockWithName("FThrust") as IMyTerminalBlock).ApplyAction("OnOff_On");

            lcd.WriteText("Current: " + Tick.ToString());

            if (Tick >= 250)
            {
                if (Tick >= 320) (GridTerminalSystem.GetBlockWithName("FThrust") as IMyThrust).ThrustOverride = 172800;
                Vector3D TargetVector = Target - RemCon.GetPosition();
                Vector3D V3Dup = RemCon.WorldMatrix.Up;
                Vector3D V3Dfow = RemCon.WorldMatrix.Forward;
                Vector3D V3Dleft = RemCon.WorldMatrix.Left;

                Vector3D TargetVector_YZ = Vector3D.Reject(TargetVector, V3Dleft);
                double Alpha = Math.Acos(Vector3D.Dot(Vector3D.Normalize(TargetVector_YZ), V3Dup));
                double Pitch = Alpha - (Math.PI / 2);

                //double TargetPitch = Math.Acos(Vector3D.Dot(V3Dup, Vector3D.Normalize(Vector3D.Reject(TargetVector, V3Dleft)))) - (Math.PI / 2);

                Vector3D TargetVector_XZ = Vector3D.Reject(TargetVector, V3Dup);
                double Beta = Math.Acos(Vector3D.Dot(Vector3D.Normalize(TargetVector_XZ), V3Dleft));
                double Yaw = Beta - (Math.PI / 2);

                Vector3D Grav_XY = Vector3D.Normalize(Vector3D.Reject(-RemCon.GetNaturalGravity(), V3Dfow));
                double a = Math.Acos(Vector3D.Dot(Grav_XY, V3Dleft));
                double Roll = a - (Math.PI / 2);

                Vector3 NavAngles = new Vector3D(Yaw, Pitch, Roll);

                lcd.WriteText("Yaw: " + Math.Round(Yaw, 5) +"\nPitch: " + Math.Round(Pitch, 5) + "\nRoll: " + Math.Round(Roll, 5));
                
                float Power = 3;
                foreach (IMyGyro gyro in gr)
                {
                    gyro.GyroOverride = true;

                    gyro.Yaw = NavAngles.GetDim(0) * Power;
                    gyro.Pitch = NavAngles.GetDim(1) * Power;
                    gyro.Roll = NavAngles.GetDim(2) * Power;
                }

                
                
            }


        }
    }
}
