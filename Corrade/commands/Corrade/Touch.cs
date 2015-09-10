///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> touch = (commandGroup, message, result) =>
            {
                if (
                    !HasCorradePermission(commandGroup.Name, (int) Permissions.Interact))
                {
                    throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                }
                float range;
                if (
                    !float.TryParse(
                        wasInput(wasKeyValueGet(
                            wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.RANGE)), message)),
                        out range))
                {
                    range = corradeConfiguration.Range;
                }
                Primitive primitive = null;
                if (
                    !FindPrimitive(
                        StringOrUUID(wasInput(wasKeyValueGet(
                            wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.ITEM)), message))),
                        range,
                        ref primitive, corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout))
                {
                    throw new ScriptException(ScriptError.PRIMITIVE_NOT_FOUND);
                }
                Client.Self.Touch(primitive.LocalID);
            };
        }
    }
}