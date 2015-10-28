﻿///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CorradeConfiguration;
using OpenMetaverse;
using wasSharp;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<CorradeCommandParameters, Dictionary<string, string>> flyto =
                (corradeCommandParameters, result) =>
                {
                    if (
                        !HasCorradePermission(corradeCommandParameters.Group.Name,
                            (int) Configuration.Permissions.Movement))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }

                    Vector3 position;
                    if (!Vector3.TryParse(wasInput(
                        KeyValue.wasKeyValueGet(wasOutput(Reflection.wasGetNameFromEnumValue(ScriptKeys.POSITION)),
                            corradeCommandParameters.Message)),
                        out position))
                    {
                        throw new ScriptException(ScriptError.INVALID_POSITION);
                    }
                    uint duration;
                    if (!uint.TryParse(wasInput(
                        KeyValue.wasKeyValueGet(wasOutput(Reflection.wasGetNameFromEnumValue(ScriptKeys.DURATION)),
                            corradeCommandParameters.Message)),
                        out duration))
                    {
                        duration = corradeConfiguration.ServicesTimeout;
                    }
                    float vicinity;
                    if (!float.TryParse(wasInput(
                        KeyValue.wasKeyValueGet(wasOutput(Reflection.wasGetNameFromEnumValue(ScriptKeys.VICINITY)),
                            corradeCommandParameters.Message)),
                        out vicinity))
                    {
                        vicinity = 2;
                    }
                    int affinity;
                    if (!int.TryParse(wasInput(
                        KeyValue.wasKeyValueGet(wasOutput(Reflection.wasGetNameFromEnumValue(ScriptKeys.AFFINITY)),
                            corradeCommandParameters.Message)),
                        out affinity))
                    {
                        affinity = 2;
                    }

                    // Generate the powers.
                    HashSet<int> segments =
                        new HashSet<int>(Enumerable.Range(0, affinity).Select(x => (int) Math.Pow(2, x)).Reverse());

                    ManualResetEvent PositionReachedEvent = new ManualResetEvent(false);
                    EventHandler<TerseObjectUpdateEventArgs> TerseObjectUpdateEvent = (sender, args) =>
                    {
                        // If the distance is within the vicinity
                        if (Vector3.Distance(position, Client.Self.SimPosition) <= vicinity)
                        {
                            Client.Self.Movement.AtPos = false;
                            Client.Self.Movement.AtNeg = false;
                            Client.Self.Movement.UpPos = false;
                            Client.Self.Movement.UpNeg = false;
                            PositionReachedEvent.Set();
                            return;
                        }

                        // Only care about us.
                        if (!args.Update.LocalID.Equals(Client.Self.LocalID)) return;

                        // ZMovement
                        float diff = position.Z - Client.Self.SimPosition.Z;
                        Client.Self.Movement.UpPos = diff > 16 || segments.Select(
                            o =>
                                new
                                {
                                    f = new Func<int, bool>(
                                        p => diff > vicinity*p && Client.Self.Velocity.Z < p*2),
                                    i = o
                                })
                            .Select(p => p.f.Invoke(p.i)).Any(o => o.Equals(true));
                        Client.Self.Movement.UpNeg = diff < -23 || segments.Select(
                            o =>
                                new
                                {
                                    f = new Func<int, bool>(
                                        p => diff < -vicinity*p && Client.Self.Velocity.Z > -p*2),
                                    i = o
                                })
                            .Select(p => p.f.Invoke(p.i)).Any(o => o.Equals(true));

                        // XYMovement
                        diff = Vector2.Distance(new Vector2(position.X, position.Y),
                            new Vector2(Client.Self.SimPosition.X, Client.Self.SimPosition.Y));
                        float velocity = new Vector2(Client.Self.Velocity.X, Client.Self.Velocity.Y).Length();
                        Client.Self.Movement.AtPos = diff >= 16 || segments.Select(o => new
                        {
                            f = new Func<int, bool>(
                                p => diff >= vicinity*p && velocity < p*2),
                            i = o
                        }).Select(p => p.f.Invoke(p.i)).Any(o => o.Equals(true));
                        Client.Self.Movement.AtNeg = false;

                        Client.Self.Movement.TurnToward(position);
                    };

                    bool succeeded = true;

                    lock (ClientInstanceSelfLock)
                    {
                        Client.Objects.TerseObjectUpdate += TerseObjectUpdateEvent;
                        Client.Self.Movement.AtPos = false;
                        Client.Self.Movement.AtNeg = false;
                        Client.Self.Movement.UpPos = false;
                        Client.Self.Movement.UpNeg = false;
                        Client.Self.Fly(true);
                        Client.Self.Movement.NudgeUpPos = true;
                        if (!PositionReachedEvent.WaitOne((int) duration, false))
                            succeeded = false;
                        Client.Objects.TerseObjectUpdate -= TerseObjectUpdateEvent;
                        Client.Self.Movement.AtPos = false;
                        Client.Self.Movement.AtNeg = false;
                        Client.Self.Movement.UpPos = false;
                        Client.Self.Movement.UpNeg = false;
                    }

                    // in case the flying timed out, then bail
                    if (!succeeded)
                    {
                        throw new ScriptException(ScriptError.TIMEOUT_REACHING_DESTINATION);
                    }

                    // perform the post-action
                    bool fly;
                    switch (bool.TryParse(wasInput(
                        KeyValue.wasKeyValueGet(wasOutput(Reflection.wasGetNameFromEnumValue(ScriptKeys.FLY)),
                            corradeCommandParameters.Message)), out fly))
                    {
                        case true:
                            lock (ClientInstanceSelfLock)
                            {
                                Client.Self.Fly(fly);
                            }
                            break;
                    }

                    SaveMovementState.Invoke();
                };
        }
    }
}