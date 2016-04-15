///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public static partial class RLVBehaviours
        {
            public static Action<string, RLVRule, UUID> clear = (message, rule, senderUUID) =>
            {
                switch (!string.IsNullOrEmpty(rule.Option))
                {
                    case true:
                        lock (RLVRulesLock)
                        {
                            RLVRules.RemoveWhere(o => o.Behaviour.Contains(rule.Behaviour));
                        }
                        break;
                    case false:
                        lock (RLVRulesLock)
                        {
                            RLVRules.RemoveWhere(o => o.ObjectUUID.Equals(senderUUID));
                        }
                        break;
                }
            };
        }
    }
}