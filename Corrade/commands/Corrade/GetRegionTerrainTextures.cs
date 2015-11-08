﻿///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using CorradeConfiguration;
using OpenMetaverse;
using wasSharp;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<CorradeCommandParameters, Dictionary<string, string>> getregionterraintextures =
                (corradeCommandParameters, result) =>
                {
                    if (!HasCorradePermission(corradeCommandParameters.Group.Name, (int) Configuration.Permissions.Land))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    List<UUID> data = new List<UUID>
                    {
                        Client.Network.CurrentSim.TerrainDetail0,
                        Client.Network.CurrentSim.TerrainDetail1,
                        Client.Network.CurrentSim.TerrainDetail2,
                        Client.Network.CurrentSim.TerrainDetail3
                    };
                    result.Add(Reflection.GetNameFromEnumValue(ResultKeys.DATA),
                        CSV.FromEnumerable(data.Select(o => o.ToString())));
                };
        }
    }
}