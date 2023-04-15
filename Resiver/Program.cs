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
        static IMyGridTerminalSystem _gts;
        static IMyCockpit cockpit;
        static IMyGyro gyro;
        private static readonly Vector3D target = new Vector3D(56413.69, -22959.25, 7822.25);
        private Vector3D _prevPosition;

        private Vector3D _oldPosition;
        private List<IMyGyro> _gyroList;

        public Program()
        {
            _gts = GridTerminalSystem;
            cockpit = _gts.GetBlockWithName("Cockpit") as IMyCockpit;
            gyro = _gts.GetBlockWithName("Gyro") as IMyGyro;
            _prevPosition = new Vector3D();

            _gyroList = new List<IMyGyro>();
            _gts.GetBlocksOfType<IMyGyro>(_gyroList);
        }

        private void SpeedCompensation()
        {
            var currentPosition = cockpit.GetPosition();
            Vector3D shipSpeed = currentPosition - _oldPosition;
            var turnVector = Vector3D.Cross(Vector3D.Normalize(shipSpeed), cockpit.WorldMatrix.Down);
            
            foreach (var myGyro in _gyroList)
            {
                myGyro.GyroOverride = true;
                gyro.Yaw = (float)turnVector.Dot(gyro.WorldMatrix.Up);
                gyro.Pitch = (float)turnVector.Dot(gyro.WorldMatrix.Right);
                gyro.Roll = (float)turnVector.Dot(gyro.WorldMatrix.Backward);
                
            }
            _oldPosition = cockpit.GetPosition();
        }
        
        private void Horizon()
        {
            var vectorToTarget = cockpit.GetPosition() - target;
            var vectorToTargetNormalized = Vector3D.Normalize(vectorToTarget);
            Vector3D targetVector;
                                
            if (vectorToTarget.Length() < 1000)
            {
                var velocityVector = cockpit.GetPosition() - _prevPosition; 
                var targetVectorReflected = Vector3D.Reflect(velocityVector, vectorToTargetNormalized);
                targetVector = targetVectorReflected.Cross(cockpit.WorldMatrix.Backward);
                if (vectorToTarget.Dot(cockpit.WorldMatrix.Backward) < 0)
                {
                    targetVector = -Vector3D.Normalize(targetVector);
                }
            }
            else
            {
                targetVector = vectorToTargetNormalized.Cross(cockpit.WorldMatrix.Backward);
            }
            
            _prevPosition = cockpit.GetPosition();

            gyro.GyroOverride = true;
            gyro.Pitch = (float)targetVector.Dot(gyro.WorldMatrix.Right) * 2f;
            gyro.Yaw = (float)targetVector.Dot(gyro.WorldMatrix.Up) * 2f;
            gyro.Roll = (float)targetVector.Dot(gyro.WorldMatrix.Backward) * 2f;
            
        }
        
        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update1)
            {
                Horizon();
            }
            else
            {
                switch (argument)
                {
                    case "Start":
                        Runtime.UpdateFrequency = UpdateFrequency.Update1;
                        break;
                    case "Stop":
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        gyro.GyroOverride = false;
                        break;
                }
            }
        }
    }
}
