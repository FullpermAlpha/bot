///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> getgroupinvites =
                (commandGroup, message, result) =>
                {
                    if (!HasCorradePermission(commandGroup.Name, (int) Permissions.Group))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    List<string> csv = new List<string>();
                    object LockObject = new object();
                    lock (GroupInviteLock)
                    {
                        Parallel.ForEach(GroupInvites, o =>
                        {
                            lock (LockObject)
                            {
                                csv.AddRange(new[]
                                {wasGetStructureMemberDescription(o.Agent, o.Agent.FirstName), o.Agent.FirstName});
                                csv.AddRange(new[]
                                {wasGetStructureMemberDescription(o.Agent, o.Agent.LastName), o.Agent.LastName});
                                csv.AddRange(new[]
                                {wasGetStructureMemberDescription(o.Agent, o.Agent.UUID), o.Agent.UUID.ToString()});
                                csv.AddRange(new[] {wasGetStructureMemberDescription(o, o.Group), o.Group});
                                csv.AddRange(new[]
                                {wasGetStructureMemberDescription(o, o.Session), o.Session.ToString()});
                                csv.AddRange(new[]
                                {
                                    wasGetStructureMemberDescription(o, o.Fee),
                                    o.Fee.ToString(CultureInfo.InvariantCulture)
                                });
                            }
                        });
                    }
                    if (csv.Any())
                    {
                        result.Add(wasGetDescriptionFromEnumValue(ResultKeys.DATA),
                            wasEnumerableToCSV(csv));
                    }
                };
        }
    }
}