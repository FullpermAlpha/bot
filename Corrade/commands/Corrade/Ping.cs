///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using wasSharp;

namespace Corrade
{
    public partial class Corrade
    {
        public static partial class CorradeCommands
        {
            public static Action<CorradeCommandParameters, Dictionary<string, string>> ping =
                (corradeCommandParameters, result) =>
                {
                    result.Add(Reflection.GetNameFromEnumValue(ResultKeys.DATA),
                        Reflection.GetNameFromEnumValue(ScriptKeys.PONG));
                };
        }
    }
}