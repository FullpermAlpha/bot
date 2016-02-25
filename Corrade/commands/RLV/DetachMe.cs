///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;
using Inventory = wasOpenMetaverse.Inventory;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class RLVBehaviours
        {
            public static Action<string, RLVRule, UUID> detachme = (message, rule, senderUUID) =>
            {
                if (!rule.Param.Equals(RLV_CONSTANTS.FORCE))
                {
                    return;
                }
                KeyValuePair<Primitive, AttachmentPoint> attachment =
                    Inventory.GetAttachments(Client, corradeConfiguration.DataTimeout)
                        .AsParallel().FirstOrDefault(o => o.Key.ID.Equals(senderUUID));
                switch (!attachment.Equals(default(KeyValuePair<Primitive, AttachmentPoint>)))
                {
                    case true:
                        InventoryBase inventoryBase =
                            Inventory.FindInventory<InventoryBase>(Client, Client.Inventory.Store.RootNode,
                                attachment.Key.Properties.ItemID
                                )
                                .AsParallel().FirstOrDefault(
                                    p =>
                                        (p is InventoryItem) &&
                                        ((InventoryItem) p).AssetType.Equals(AssetType.Object));
                        if (inventoryBase is InventoryAttachment || inventoryBase is InventoryObject)
                        {
                            Inventory.Detach(Client, CurrentOutfitFolder, inventoryBase as InventoryItem,
                                corradeConfiguration.ServicesTimeout);
                        }
                        RebakeTimer.Change(corradeConfiguration.RebakeDelay, 0);
                        break;
                    default:
                        return;
                }
            };
        }
    }
}