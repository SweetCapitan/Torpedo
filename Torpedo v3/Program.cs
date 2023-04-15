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
        #region ProgrammBody

        #region ScriptParams
        // Parameters of Script
        // You may change this for better result
        private const string CockpitName = "Cockpit";
        private const string RadarName = "Radar";
        private const string TorpedoName = "Torpedo";
        private const bool CenterShot = true;
        private const float LockPointDepth = 5;
        private const float InterceptCourse = 1.0f;
        private const float MaxVelocity = 100;
        private const int WarheadArmDist = 100;
        private const float TorpedoReflectK = 2f;
        private const float AccelDet = 1.0f;
        private const int WarheadTimer = 300;
        private const int NumberOfCharges = 7;
        private const string BroadCastTag = "TC";
        private const float GyroMultiply = 6f; 
        private const int DelayAfterLaunch = 50;
        #endregion

        static IMyGridTerminalSystem _gts;
        static int Tick = 0;
        private List<Torpedo> _torpedoList;
        private static IMyBroadcastListener _myBroadcastListener;
        private static IMyTextPanel _lcd;
        private static int torpedoIndex;
        private enum LaunchMode
        {
            Direct,
            Ballistic,
            Swarm
        }
        private static LaunchMode _launchMode = LaunchMode.Ballistic;
        private Vector3D _target;

        Program()
        {
            _gts = GridTerminalSystem;
            _lcd = _gts.GetBlockWithName("LCD") as IMyTextPanel;
            _lcd.ContentType = ContentType.TEXT_AND_IMAGE;
            _lcd.FontSize = 1.5f;
            _torpedoList = new List<Torpedo>();
            InitializeTorpedos();
            _myBroadcastListener = IGC.RegisterBroadcastListener(BroadCastTag);
            _myBroadcastListener.SetMessageCallback(BroadCastTag);

            _target = new Vector3D();
            torpedoIndex = -1;
        }

        void InitializeTorpedos()
        {
            Echo("Initializing torpedo's: \n");
            string reportOfChecking = null;
            string errors = String.Empty;
            for (var x = 0; x <= NumberOfCharges; x++)
            {
                try
                {
                    if (Torpedo.CheckBlocksStatus(TorpedoName + x, out reportOfChecking))
                    {
                        _torpedoList.Add(new Torpedo(x));
                        reportOfChecking += reportOfChecking + "\n";
                    }
                }
                catch (Exception e)
                {
                    errors += e.ToString() + "\n";
                }
            }

            Echo($"Report: {reportOfChecking}");
            Echo($"Errors: {errors}");
            Echo("\n" + _torpedoList.FindAll((b) => ((b.Status == Torpedo.StatusEnum.Ready))).Count + " torpedo's ready for launch");
            Echo("\n" + _torpedoList.FindAll((b) => ((b.Status == Torpedo.StatusEnum.Launched))).Count + " torpedo's on the way");
            Echo("\n" + _torpedoList.Count + "torpedo's in list");
        }

        void CleanGarbage()
        {
            Echo("Cleaning: ");

            var killList = (from t in _torpedoList where !t.CheckHealth() select _torpedoList.IndexOf(t)).ToList();

            _torpedoList.RemoveIndices(killList);
            Echo("" + killList.Count + " torpedos trashed\n");
        }


        void Main(string arg, UpdateType updateSource)
        {
            //todo Translate the work of scripts to the state machine
            if (updateSource == UpdateType.IGC)
            {
                while (_myBroadcastListener.HasPendingMessage)
                {
                    var myIgcMessage = _myBroadcastListener.AcceptMessage();
                    if (myIgcMessage.Tag == BroadCastTag)
                    {
                        if (myIgcMessage.Data.ToString() == "Launch")
                        {
                            _lcd.WriteText("LAUNCH");
                            foreach (var torpedo in _torpedoList)
                            {
                                torpedo.Launch();
                            }
                            Runtime.UpdateFrequency = UpdateFrequency.Update1;
                            break;
                        }
                        else if (myIgcMessage.Data.ToString() == "Once") //TODO Not Workking
                        {
                            torpedoIndex++;
                            _torpedoList[torpedoIndex].Launch();
                            if (torpedoIndex > _torpedoList.Count)
                            {
                                torpedoIndex = 0;
                            }
                            Runtime.UpdateFrequency = UpdateFrequency.Update1;
                            break;
                        }
                        Vector3D vector;
                        if (Vector3D.TryParse(myIgcMessage.Data.ToString(), out vector))
                        {
                            // cockpit.GetSurface(0).WriteText("Target: " + vector.ToString());
                            _lcd.WriteText("Target: " + vector);
                            _target = vector;
                        }
                    }
                }
            }
            else if (updateSource == UpdateType.Update1)
            {
                var launched = 0;
                foreach (var torpedo in _torpedoList)
                {
                    torpedo.Update(_target);
                    if (torpedo.Status == Torpedo.StatusEnum.Launched)
                    {
                        launched += 1;
                    }
                }
                _lcd.WriteText($"Launched: {launched} | {_torpedoList.Count}");
            }
            // if (uType == UpdateType.Update1)
            // {
            //     Tick++;
            //     radar.Update();
            //     cockpit.GetSurface(0).WriteText("LOCKED: " + radar.Locked, false);
            //     cockpit.GetSurface(0)
            //         .WriteText("\nTarget: " + radar.CurrentTarget.Name + ", tick: " + radar.LastLockTick, true);
            //     cockpit.GetSurface(0).WriteText("\nDistance: " + Math.Round(radar.TargetDistance), true);
            //     cockpit.GetSurface(0)
            //         .WriteText("\nVelocity: " + Math.Round(radar.CurrentTarget.Velocity.Length()), true);
            //     foreach (var t in _torpedoList.Where(t => t.Status == Torpedo.StatusEnum.Launched))
            //     {
            //         t.Update(radar.CurrentTarget, CenterShot ? radar.CurrentTarget.Position : radar.T);
            //     }
            // }
            // else
            // {
            //     switch (arg)
            //     {
            //         case "Lock":
            //             radar.Lock(true, 10000);
            //             if (radar.Locked)
            //             {
            //                 Runtime.UpdateFrequency = UpdateFrequency.Update1;
            //             }
            //             else
            //             {
            //                 cockpit.GetSurface(0).WriteText("NO TARGET", false);
            //                 Runtime.UpdateFrequency = UpdateFrequency.None;
            //             }
            //             break;
            //         case "Init":
            //             CleanGarbage();
            //             InitializeTorpedos();
            //             break;
            //         case "Stop":
            //             radar.StopLock();
            //             Runtime.UpdateFrequency = UpdateFrequency.None;
            //             break;
            //         case "Launch":
            //             if (radar.Locked)
            //                 foreach (var t in _torpedoList)
            //                 {
            //                     Echo("\nTry Launch: ");
            //                     if (t.Status == Torpedo.StatusEnum.Ready)
            //                     {
            //                         Echo("1 go");
            //                         t.Launch();
            //                         break;
            //                     }
            //                 }
            //             else
            //                 Echo("No Target Lock");
            //
            //             break;
            //         case "Test":
            //             Echo("\n Test:" + VerticalDelay(5000.0f, 1700.0f, 300));
            //             break;
            //     }
            // }
        }

        public class Torpedo
        {
            private static int _tickPassed = 0;
            private double _myVelocity = 0;
            public readonly string Name;
            private Vector3D _prevPosition;
            public enum StatusEnum
            {
                Idle,
                Ready,
                Launched
            }
            public StatusEnum Status;
            private List<IMyTerminalBlock> _rotorList;
            private List<IMyTerminalBlock> _mergeList;
            private IMyRemoteControl _remoteControl;
            private List<IMyTerminalBlock> _batteryList;
            private List<IMyTerminalBlock> _thrusterList;
            private List<IMyTerminalBlock> _gyroList;
            private List<IMyTerminalBlock> _warheadList;
            private static IMyBlockGroup _group;

            public Torpedo(int torpedoIndex)
            {
                Name = TorpedoName + torpedoIndex;
                _group = _gts.GetBlockGroupWithName(Name);
                _rotorList = GetListBlocksOfType<IMyMotorStator>();
                _mergeList = GetListBlocksOfType<IMyShipMergeBlock>();
                _remoteControl = GetListBlocksOfType<IMyRemoteControl>()[0] as IMyRemoteControl;
                _batteryList = GetListBlocksOfType<IMyBatteryBlock>();
                _thrusterList = GetListBlocksOfType<IMyThrust>();
                _gyroList = GetListBlocksOfType<IMyGyro>();
                _warheadList = GetListBlocksOfType<IMyWarhead>();
                Status = StatusEnum.Ready;
                _prevPosition = new Vector3D();
            }

            private static List<IMyTerminalBlock> GetListBlocksOfType<T>() where T : class
            {
                var blocks = new List<IMyTerminalBlock>();
                _group.GetBlocksOfType<T>(blocks, ((block) => block.CustomName.Contains(TorpedoName)));
                return blocks;
            }

            /// <summary>
            /// Function for align to gravity vector
            /// </summary>
            private void HorizonAlignment()
            {
                var gravityVector = Vector3D.Normalize(_remoteControl.GetNaturalGravity());
                var turnVector = gravityVector.Cross(_remoteControl.WorldMatrix.Down);
                // var pitch = (float)gravityVector.Dot(_remoteControl.WorldMatrix.Backward);
                // var roll = (float)gravityVector.Dot(_remoteControl.WorldMatrix.Left);

                if (_remoteControl.WorldMatrix.Down.Dot(gravityVector) < 0)
                {
                    turnVector = Vector3D.Normalize(turnVector);
                }
                
                foreach (var myTerminalBlock in _gyroList)
                {
                    var gyro = (IMyGyro)myTerminalBlock;
                    gyro.GyroOverride = true;
                    gyro.Yaw = (float)turnVector.Dot(gyro.WorldMatrix.Up) * GyroMultiply;
                    gyro.Pitch = (float)turnVector.Dot(gyro.WorldMatrix.Right) * GyroMultiply;
                    gyro.Roll = (float)turnVector.Dot(gyro.WorldMatrix.Backward) * GyroMultiply;
                }
            }

            /// <summary>
            /// Get blocks from Grid that match forwarded interface
            /// </summary>
            /// <param name="outList">List in which returned result</param>
            /// <typeparam name="T">Interface of block that need to get</typeparam>
            private static void GetBlocksOfType<T>(string torpedoName, out List<IMyTerminalBlock> outList) where T : class
            {
                outList = new List<IMyTerminalBlock>();
                _group.GetBlocksOfType<T>(outList, (block) => block.CustomName.Contains(torpedoName));
            }

            /// <summary>
            /// Checking that the list of block is not null and return string with block count report
            /// </summary>
            /// <param name="blockName">Name of block which currently initialized</param>
            private static void CheckTempListLength(List<IMyTerminalBlock> list, string blockName, out string report)
            {
                if (list.Count == 0)
                {
                    throw new Exception($"\n{blockName} Blocks not found");
                    // return false;
                }
                report = $"\n{blockName} Block's: " + list.Count;
                list.Clear();
            }
            
            /// <summary>
            /// Check all block of missile or torpedo during initialization
            /// </summary>Name of group that using for allocate missile block from all grid
            /// <param name="GroupName">Name of group that using for allocate missile block from all grid</param>
            /// <param name="reportOfCheckingBlocks">String of result of checking</param>
            /// <returns></returns>
            public static bool CheckBlocksStatus(string GroupName, out string reportOfCheckingBlocks)
            {
                List<IMyTerminalBlock> tempList;
                reportOfCheckingBlocks = GroupName;

                try
                {
                    var fullReport = String.Empty;
                    //---------- MERGE ------------
                    GetBlocksOfType<IMyShipMergeBlock>(TorpedoName, out tempList);
                    CheckTempListLength(tempList, "Merge", out reportOfCheckingBlocks);
                    fullReport += reportOfCheckingBlocks + "\n";

                    _gts.GetBlocksOfType<IMyShipMergeBlock>(tempList,
                        (block) => (block.CustomName.Contains(TorpedoName) && ((IMyShipMergeBlock)block).IsConnected));
                    fullReport += " of which has connected: " + tempList.Count;
                    tempList.Clear();

                    //---------- REM CON ------------
                    GetBlocksOfType<IMyRemoteControl>(TorpedoName, out tempList);
                    CheckTempListLength(tempList, "Remote Control", out reportOfCheckingBlocks);
                    fullReport += reportOfCheckingBlocks;

                    _gts.GetBlocksOfType<IMyRemoteControl>(tempList,
                        (b) => (b.CustomName.Contains(TorpedoName) && b.IsFunctional)); //OPTIMIZE wrap this to function 
                    fullReport += "   functional: " + tempList.Count;

                    //---------- BATTERY ------------
                    GetBlocksOfType<IMyBatteryBlock>(TorpedoName, out tempList);
                    CheckTempListLength(tempList, "Battery", out reportOfCheckingBlocks);
                    fullReport += reportOfCheckingBlocks;

                    _gts.GetBlocksOfType<IMyBatteryBlock>(tempList,
                        (b) => (b.CustomName.Contains(TorpedoName) && b.IsFunctional));
                    fullReport += "   functional: " + tempList.Count;

                    //---------- THRUSTERS ------------
                    GetBlocksOfType<IMyThrust>(TorpedoName, out tempList);
                    CheckTempListLength(tempList, "Thrusters", out reportOfCheckingBlocks);
                    fullReport += reportOfCheckingBlocks;

                    _gts.GetBlocksOfType<IMyThrust>(tempList, (b) => (b.CustomName.Contains(TorpedoName) && b.IsFunctional));
                    fullReport += "   functional: " + tempList.Count;

                    //---------- GYROS ------------
                    GetBlocksOfType<IMyGyro>(TorpedoName, out tempList);
                    CheckTempListLength(tempList, "Gyro", out reportOfCheckingBlocks);
                    fullReport += reportOfCheckingBlocks;

                    _gts.GetBlocksOfType<IMyGyro>(tempList, (b) => (b.CustomName.Contains(TorpedoName) && b.IsFunctional));
                    fullReport += "   functional: " + tempList.Count;

                    //---------- WARHEADS ------------
                    GetBlocksOfType<IMyWarhead>(TorpedoName, out tempList);
                    CheckTempListLength(tempList, "Warhead", out reportOfCheckingBlocks);
                    fullReport += reportOfCheckingBlocks;

                    _gts.GetBlocksOfType<IMyWarhead>(tempList, (b) => (b.CustomName.Contains(TorpedoName) && b.IsFunctional));
                    fullReport += "   functional: " + tempList.Count;
                    reportOfCheckingBlocks = fullReport;
                }
                catch (Exception e)
                {
                    reportOfCheckingBlocks += e.Message;
                }

                reportOfCheckingBlocks += "\n-------------------------\n";
                return true;
            }

            public bool CheckHealth()
            {
                if (!_remoteControl.IsFunctional)
                    return false;
                if (_batteryList.FindAll((b) => (b.IsFunctional)).Count == 0)
                    return false;
                if (_thrusterList.FindAll((b) => (b.IsFunctional)).Count == 0)
                    return false;
                if (_gyroList.FindAll((b) => (b.IsFunctional)).Count == 0)
                    return false;
                return true;
            }

            public void Launch()
            {
                foreach (var myTerminalBlock in _batteryList)
                {
                    var battery = (IMyBatteryBlock)myTerminalBlock;
                    battery.Enabled = true;
                    battery.ChargeMode = ChargeMode.Discharge;
                }
                
                foreach (var myTerminalBlock in _gyroList)
                {
                    var gyro = (IMyGyro)myTerminalBlock;
                    if (!gyro.Enabled)
                    {
                        gyro.Enabled = true;
                    }
                    gyro.GyroPower = 1f;
                    gyro.Pitch = 0;
                    gyro.Yaw = 0;
                    gyro.Roll = 0;
                    if (!gyro.GyroOverride)
                    {
                        gyro.GyroOverride = true;
                    }
                }
                
                foreach (var myTerminalBlock in _rotorList)
                {
                    var motorStator = (IMyMotorStator)myTerminalBlock;
                    motorStator.Detach();
                }
                
                foreach (var myTerminalBlock in _mergeList)
                {
                    var mergeBlock = (IMyShipMergeBlock)myTerminalBlock;
                    mergeBlock.Enabled = false;
                }

                if (WarheadTimer > 30)
                    foreach (var myTerminalBlock in _warheadList)
                    {
                        var warhead = (IMyWarhead)myTerminalBlock;
                        warhead.DetonationTime = WarheadTimer;
                        warhead.StartCountdown();
                    }
                
                foreach (var myTerminalBlock in _thrusterList)
                {
                    var thrust = (IMyThrust)myTerminalBlock;
                    thrust.Enabled = true;
                    thrust.ThrustOverridePercentage = 1f;
                }

                Status = StatusEnum.Launched;
            }

            public void Update(Vector3D target)
            {
                if (Status != StatusEnum.Launched)
                {
                    _tickPassed++;
                }

                if (_tickPassed > DelayAfterLaunch)
                {
                    if (_remoteControl.IsFunctional)
                    {
                        var vectorToTarget = _remoteControl.GetPosition() - target;
                        var vectorToTargetNormalized = Vector3D.Normalize(vectorToTarget);
                        Vector3D targetVector;
                        
                        switch (_launchMode)
                        {
                            case LaunchMode.Direct:
                                
                                if (vectorToTarget.Length() < 1500)
                                {
                                    var velocityVector = _remoteControl.GetPosition() - _prevPosition; 
                                    targetVector = Vector3D.Reflect(velocityVector, vectorToTargetNormalized);
                                    targetVector = targetVector.Cross(_remoteControl.WorldMatrix.Backward);
                                    if (vectorToTarget.Dot(_remoteControl.WorldMatrix.Backward) < 0)
                                    {
                                        targetVector = -Vector3D.Normalize(targetVector);
                                    }
                                }
                                else
                                {
                                    targetVector = vectorToTargetNormalized.Cross(_remoteControl.WorldMatrix.Backward);
                                }

                                foreach (var myTerminalBlock in _gyroList)
                                {
                                    var gyro = (IMyGyro)myTerminalBlock;
                                    gyro.Pitch = (float)targetVector.Dot(gyro.WorldMatrix.Right) * GyroMultiply;
                                    gyro.Yaw = (float)targetVector.Dot(gyro.WorldMatrix.Up) * GyroMultiply;
                                    gyro.Roll = (float)targetVector.Dot(gyro.WorldMatrix.Backward) * GyroMultiply;
                                }
                                _prevPosition = _remoteControl.GetPosition();

                                if (vectorToTarget.Length() <= WarheadArmDist)
                                {
                                    foreach (var warhead in _warheadList.Cast<IMyWarhead>())
                                    {
                                        warhead.Detonate();
                                    }
                                }
                                break;
                            case LaunchMode.Ballistic:

                                var gravity = Vector3D.Normalize(_remoteControl.GetNaturalGravity());

                                var gravityReflected = Vector3D.Reflect(gravity, vectorToTargetNormalized);

                                targetVector = gravityReflected.Cross(_remoteControl.WorldMatrix.Backward);

                                foreach (var myTerminalBlock in _gyroList)
                                {
                                    var gyro = (IMyGyro)myTerminalBlock;
                                    gyro.Pitch = (float)targetVector.Dot(gyro.WorldMatrix.Right) * GyroMultiply;
                                    gyro.Yaw = (float)targetVector.Dot(gyro.WorldMatrix.Up) * GyroMultiply;
                                    gyro.Roll = (float)targetVector.Dot(gyro.WorldMatrix.Backward) * GyroMultiply;
                                }

                                if (gravity.Dot(vectorToTargetNormalized) >= 0.90)
                                {
                                    _launchMode = LaunchMode.Direct;
                                }
                                break;
                            case LaunchMode.Swarm:
                                //TODO Swarm
                                //TODO VECTOR Parser for transmiting
                                break;
                        }
                    }
                }
            }

            private Vector3D FindInterceptVector(Vector3D shotOrigin, double shotSpeed,
                Vector3D targetOrigin, Vector3D targetVel)
            {
                Vector3D dirToTarget = Vector3D.Normalize(targetOrigin - shotOrigin);
                Vector3D targetVelOrth = Vector3D.Dot(targetVel, dirToTarget) * dirToTarget;
                Vector3D targetVelTang = targetVel - targetVelOrth;
                Vector3D shotVelTang = targetVelTang;
                double shotVelSpeed = shotVelTang.Length();

                if (shotVelSpeed > shotSpeed)
                {
                    return Vector3D.Normalize(targetVel) * shotSpeed;
                }
                else
                {
                    double shotSpeedOrth = Math.Sqrt(shotSpeed * shotSpeed - shotVelSpeed * shotVelSpeed);
                    Vector3D shotVelOrth = dirToTarget * shotSpeedOrth;
                    return shotVelOrth + shotVelTang;
                }
            }
        }

        #endregion
    }
}