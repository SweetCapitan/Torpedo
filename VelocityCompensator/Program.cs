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
        private Vector3D _oldPosition;
        private List<IMyGyro> _gyroList;

        public Program()
        {
            _gts = GridTerminalSystem;
            cockpit = _gts.GetBlockWithName("Cockpit") as IMyCockpit;
            _gyroList = new List<IMyGyro>();
            _gts.GetBlocksOfType<IMyGyro>(_gyroList);
        }

        private void SpeedCompensation()
        {
            var currentPosition = cockpit.GetPosition();
            var shipSpeed = currentPosition - _oldPosition;
            var turnVector = Vector3D.Cross(Vector3D.Normalize(shipSpeed), cockpit.WorldMatrix.Down);

            if (cockpit.WorldMatrix.Down.Dot(Vector3.Normalize(shipSpeed)) < 0)
            {
                turnVector = Vector3D.Normalize(turnVector);
            }
            
            foreach (var myGyro in _gyroList)
            {
                myGyro.GyroOverride = true;
                myGyro.Yaw = (float)turnVector.Dot(myGyro.WorldMatrix.Up);
                myGyro.Pitch = (float)turnVector.Dot(myGyro.WorldMatrix.Right);
                myGyro.Roll = (float)turnVector.Dot(myGyro.WorldMatrix.Backward);
            }
            _oldPosition = cockpit.GetPosition();
        }
        
        private Vector3D VectorTransform(Vector3D vec, MatrixD Orientation)
        {
            return new Vector3D(vec.Dot(Orientation.Right), vec.Dot(Orientation.Up), vec.Dot(Orientation.Backward));
        }

        private float RotateAngle(float angle)
        {
            if (float.IsNaN(angle))
            {
                angle = 0;
            }
            if (angle < -Math.PI)
            {
                angle += 2 * (float) Math.PI;
            }
            else if (angle > Math.PI)
            {
                angle -= 2 * (float)Math.PI;
            }

            return angle;
        }
        
        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update1)
            {
                SpeedCompensation();
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
                        foreach (var myGyro in _gyroList)
                        {
                            myGyro.GyroOverride = false;
                        }
                        break;
                }
            }
        }
    }
}
