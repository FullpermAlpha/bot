///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using CorradeConfiguration;
using wasSharp;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<CorradeCommandParameters, Dictionary<string, string>> getselfdata =
                (corradeCommandParameters, result) =>
                {
                    if (
                        !HasCorradePermission(corradeCommandParameters.Group.Name,
                            (int) Configuration.Permissions.Grooming))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    List<string> data = GetStructuredData(Client.Self,
                        wasInput(KeyValue.wasKeyValueGet(wasOutput(Reflection.wasGetNameFromEnumValue(ScriptKeys.DATA)),
                            corradeCommandParameters.Message))).ToList();
                    if (data.Any())
                    {
                        result.Add(Reflection.wasGetNameFromEnumValue(ResultKeys.DATA),
                            CSV.wasEnumerableToCSV(data));
                    }
                };
        }
    }
}