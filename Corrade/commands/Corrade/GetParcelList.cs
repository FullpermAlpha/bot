///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using OpenMetaverse;
using Parallel = System.Threading.Tasks.Parallel;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> getparcellist =
                (commandGroup, message, result) =>
                {
                    if (!HasCorradePermission(commandGroup.Name, (int) Permissions.Land))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }

                    Vector3 position;
                    if (
                        !Vector3.TryParse(
                            wasInput(
                                wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.POSITION)),
                                    message)),
                            out position))
                    {
                        position = Client.Self.SimPosition;
                    }
                    string region =
                        wasInput(wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.REGION)),
                            message));
                    Simulator simulator =
                        Client.Network.Simulators.FirstOrDefault(
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
                    FieldInfo accessField = typeof (AccessList).GetFields(
                        BindingFlags.Public | BindingFlags.Static)
                        .AsParallel().FirstOrDefault(
                            o =>
                                o.Name.Equals(
                                    wasInput(
                                        wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.TYPE)),
                                            message)),
                                    StringComparison.Ordinal));
                    if (accessField == null)
                    {
                        throw new ScriptException(ScriptError.UNKNOWN_ACCESS_LIST_TYPE);
                    }
                    AccessList accessType = (AccessList) accessField.GetValue(null);
                    if (!simulator.IsEstateManager)
                    {
                        if (!parcel.OwnerID.Equals(Client.Self.AgentID))
                        {
                            if (!parcel.IsGroupOwned && !parcel.GroupID.Equals(commandGroup.UUID))
                            {
                                throw new ScriptException(ScriptError.NO_GROUP_POWER_FOR_COMMAND);
                            }
                            switch (accessType)
                            {
                                case AccessList.Access:
                                    if (
                                        !HasGroupPowers(Client.Self.AgentID, commandGroup.UUID,
                                            GroupPowers.LandManageAllowed, corradeConfiguration.ServicesTimeout,
                                            corradeConfiguration.DataTimeout))
                                    {
                                        throw new ScriptException(ScriptError.NO_GROUP_POWER_FOR_COMMAND);
                                    }
                                    break;
                                case AccessList.Ban:
                                    if (
                                        !HasGroupPowers(Client.Self.AgentID, commandGroup.UUID,
                                            GroupPowers.LandManageBanned,
                                            corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout))
                                    {
                                        throw new ScriptException(ScriptError.NO_GROUP_POWER_FOR_COMMAND);
                                    }
                                    break;
                                case AccessList.Both:
                                    if (
                                        !HasGroupPowers(Client.Self.AgentID, commandGroup.UUID,
                                            GroupPowers.LandManageAllowed, corradeConfiguration.ServicesTimeout,
                                            corradeConfiguration.DataTimeout))
                                    {
                                        throw new ScriptException(ScriptError.NO_GROUP_POWER_FOR_COMMAND);
                                    }
                                    if (
                                        !HasGroupPowers(Client.Self.AgentID, commandGroup.UUID,
                                            GroupPowers.LandManageBanned,
                                            corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout))
                                    {
                                        throw new ScriptException(ScriptError.NO_GROUP_POWER_FOR_COMMAND);
                                    }
                                    break;
                            }
                        }
                    }
                    ManualResetEvent ParcelAccessListEvent = new ManualResetEvent(false);
                    List<ParcelManager.ParcelAccessEntry> accessList = null;
                    EventHandler<ParcelAccessListReplyEventArgs> ParcelAccessListHandler = (sender, args) =>
                    {
                        accessList = args.AccessList;
                        ParcelAccessListEvent.Set();
                    };
                    lock (ClientInstanceParcelsLock)
                    {
                        Client.Parcels.ParcelAccessListReply += ParcelAccessListHandler;
                        Client.Parcels.RequestParcelAccessList(simulator, parcel.LocalID, accessType, 0);
                        if (!ParcelAccessListEvent.WaitOne((int) corradeConfiguration.ServicesTimeout, false))
                        {
                            Client.Parcels.ParcelAccessListReply -= ParcelAccessListHandler;
                            throw new ScriptException(ScriptError.TIMEOUT_GETTING_PARCELS);
                        }
                        Client.Parcels.ParcelAccessListReply -= ParcelAccessListHandler;
                    }
                    List<string> csv = new List<string>();
                    object LockObject = new object();
                    Parallel.ForEach(accessList, o =>
                    {
                        string agent = string.Empty;
                        if (
                            !AgentUUIDToName(o.AgentID, corradeConfiguration.ServicesTimeout,
                                ref agent))
                            return;
                        lock (LockObject)
                        {
                            csv.Add(agent);
                            csv.Add(o.AgentID.ToString());
                            csv.Add(o.Flags.ToString());
                            csv.Add(o.Time.ToString(CultureInfo.InvariantCulture));
                        }
                    });
                    if (csv.Any())
                    {
                        result.Add(wasGetDescriptionFromEnumValue(ResultKeys.DATA),
                            wasEnumerableToCSV(csv));
                    }
                };
        }
    }
}