///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenMetaverse;
using Parallel = System.Threading.Tasks.Parallel;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> getrolesmembers =
                (commandGroup, message, result) =>
                {
                    if (!HasCorradePermission(commandGroup.Name, (int) Permissions.Group))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    IEnumerable<UUID> currentGroups = Enumerable.Empty<UUID>();
                    if (
                        !GetCurrentGroups(corradeConfiguration.ServicesTimeout,
                            ref currentGroups))
                    {
                        throw new ScriptException(ScriptError.COULD_NOT_GET_CURRENT_GROUPS);
                    }
                    if (!new HashSet<UUID>(currentGroups).Contains(commandGroup.UUID))
                    {
                        throw new ScriptException(ScriptError.NOT_IN_GROUP);
                    }
                    HashSet<KeyValuePair<UUID, UUID>> groupRolesMembers = null;
                    ManualResetEvent GroupRoleMembersReplyEvent = new ManualResetEvent(false);
                    EventHandler<GroupRolesMembersReplyEventArgs> GroupRolesMembersEventHandler =
                        (sender, args) =>
                        {
                            groupRolesMembers = new HashSet<KeyValuePair<UUID, UUID>>(args.RolesMembers);
                            GroupRoleMembersReplyEvent.Set();
                        };
                    lock (ClientInstanceGroupsLock)
                    {
                        Client.Groups.GroupRoleMembersReply += GroupRolesMembersEventHandler;
                        Client.Groups.RequestGroupRolesMembers(commandGroup.UUID);
                        if (!GroupRoleMembersReplyEvent.WaitOne((int) corradeConfiguration.ServicesTimeout, false))
                        {
                            Client.Groups.GroupRoleMembersReply -= GroupRolesMembersEventHandler;
                            throw new ScriptException(ScriptError.TIMEOUT_GETING_GROUP_ROLES_MEMBERS);
                        }
                        Client.Groups.GroupRoleMembersReply -= GroupRolesMembersEventHandler;
                    }
                    // First resolve the all the role names to role UUIDs
                    Hashtable roleUUIDNames = new Hashtable(groupRolesMembers.Count);
                    foreach (
                        UUID roleUUID in
                            groupRolesMembers.AsParallel().GroupBy(o => o.Key).Select(o => o.First().Key))
                    {
                        string roleName = string.Empty;
                        switch (
                            !RoleUUIDToName(roleUUID, commandGroup.UUID, corradeConfiguration.ServicesTimeout,
                                corradeConfiguration.DataTimeout,
                                ref roleName))
                        {
                            case true:
                                continue;
                            default:
                                roleUUIDNames.Add(roleUUID, roleName);
                                break;
                        }
                    }
                    // Next, associate role names with agent names and UUIDs.
                    List<string> csv = new List<string>();
                    object LockObject = new object();
                    Parallel.ForEach(groupRolesMembers, o =>
                    {
                        if (!roleUUIDNames.ContainsKey(o.Key)) return;
                        string agentName = string.Empty;
                        if (AgentUUIDToName(o.Value, corradeConfiguration.ServicesTimeout, ref agentName))
                        {
                            lock (LockObject)
                            {
                                csv.Add(roleUUIDNames[o.Key] as string);
                                csv.Add(agentName);
                                csv.Add(o.Value.ToString());
                            }
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