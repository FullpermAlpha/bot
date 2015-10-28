///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CorradeConfiguration;
using OpenMetaverse;
using wasSharp;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<CorradeCommandParameters, Dictionary<string, string>> creategrass =
                (corradeCommandParameters, result) =>
                {
                    if (
                        !HasCorradePermission(corradeCommandParameters.Group.Name,
                            (int) Configuration.Permissions.Interact))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    Vector3 position;
                    if (
                        !Vector3.TryParse(
                            wasInput(
                                KeyValue.wasKeyValueGet(
                                    wasOutput(Reflection.wasGetNameFromEnumValue(ScriptKeys.POSITION)),
                                    corradeCommandParameters.Message)),
                            out position))
                    {
                        throw new ScriptException(ScriptError.INVALID_POSITION);
                    }
                    Quaternion rotation;
                    if (
                        !Quaternion.TryParse(
                            wasInput(
                                KeyValue.wasKeyValueGet(
                                    wasOutput(Reflection.wasGetNameFromEnumValue(ScriptKeys.ROTATION)),
                                    corradeCommandParameters.Message)),
                            out rotation))
                    {
                        rotation = Quaternion.Identity;
                    }
                    string region =
                        wasInput(
                            KeyValue.wasKeyValueGet(wasOutput(Reflection.wasGetNameFromEnumValue(ScriptKeys.REGION)),
                                corradeCommandParameters.Message));
                    Simulator simulator =
                        Client.Network.Simulators.AsParallel().FirstOrDefault(
                            o =>
                                o.Name.Equals(
                                    string.IsNullOrEmpty(region) ? Client.Network.CurrentSim.Name : region,
                                    StringComparison.OrdinalIgnoreCase));
                    if (simulator == null)
                    {
                        throw new ScriptException(ScriptError.REGION_NOT_FOUND);
                    }
                    Parcel parcel = null;
                    if (!GetParcelAtPosition(simulator, position, ref parcel))
                    {
                        throw new ScriptException(ScriptError.COULD_NOT_FIND_PARCEL);
                    }
                    if (!parcel.OwnerID.Equals(Client.Self.AgentID))
                    {
                        if (!parcel.IsGroupOwned && !parcel.GroupID.Equals(corradeCommandParameters.Group.UUID))
                        {
                            throw new ScriptException(ScriptError.NO_GROUP_POWER_FOR_COMMAND);
                        }
                        if (
                            !HasGroupPowers(Client.Self.AgentID, corradeCommandParameters.Group.UUID,
                                GroupPowers.LandGardening,
                                corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout))
                        {
                            throw new ScriptException(ScriptError.NO_GROUP_POWER_FOR_COMMAND);
                        }
                    }
                    Vector3 scale;
                    if (
                        !Vector3.TryParse(
                            wasInput(
                                KeyValue.wasKeyValueGet(wasOutput(Reflection.wasGetNameFromEnumValue(ScriptKeys.SCALE)),
                                    corradeCommandParameters.Message)),
                            out scale))
                    {
                        scale = new Vector3(0.5f, 0.5f, 0.5f);
                    }
                    if (IsSecondLife() &&
                        ((scale.X < LINDEN_CONSTANTS.PRIMITIVES.MINIMUM_SIZE_X ||
                          scale.Y < LINDEN_CONSTANTS.PRIMITIVES.MINIMUM_SIZE_Y ||
                          scale.Z < LINDEN_CONSTANTS.PRIMITIVES.MINIMUM_SIZE_Z ||
                          scale.X > LINDEN_CONSTANTS.PRIMITIVES.MAXIMUM_SIZE_X ||
                          scale.Y > LINDEN_CONSTANTS.PRIMITIVES.MAXIMUM_SIZE_Y ||
                          scale.Z > LINDEN_CONSTANTS.PRIMITIVES.MAXIMUM_SIZE_Z)))
                    {
                        throw new ScriptException(ScriptError.SCALE_WOULD_EXCEED_BUILDING_CONSTRAINTS);
                    }
                    string type = wasInput(
                        KeyValue.wasKeyValueGet(
                            wasOutput(Reflection.wasGetNameFromEnumValue(ScriptKeys.TYPE)),
                            corradeCommandParameters.Message));
                    FieldInfo grassFieldInfo = typeof (Grass).GetFields(
                        BindingFlags.Public |
                        BindingFlags.Static)
                        .AsParallel().FirstOrDefault(
                            o =>
                                o.Name.Equals(type,
                                    StringComparison.OrdinalIgnoreCase));
                    if (grassFieldInfo == null)
                    {
                        throw new ScriptException(ScriptError.UNKNOWN_GRASS_TYPE);
                    }
                    // Finally, add the grass to the simulator.
                    Client.Objects.AddGrass(simulator, scale, rotation, position,
                        (Grass) grassFieldInfo.GetValue(null),
                        corradeCommandParameters.Group.UUID);
                };
        }
    }
}