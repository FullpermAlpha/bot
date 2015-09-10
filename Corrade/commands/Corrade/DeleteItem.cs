///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;
using Parallel = System.Threading.Tasks.Parallel;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> deleteitem =
                (commandGroup, message, result) =>
                {
                    if (
                        !HasCorradePermission(commandGroup.Name,
                            (int) Permissions.Inventory))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    HashSet<InventoryItem> items =
                        new HashSet<InventoryItem>(FindInventory<InventoryBase>(Client.Inventory.Store.RootNode,
                            StringOrUUID(wasInput(wasKeyValueGet(
                                wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.ITEM)), message)))
                            ).Cast<InventoryItem>());
                    if (!items.Any())
                    {
                        throw new ScriptException(ScriptError.INVENTORY_ITEM_NOT_FOUND);
                    }
                    Parallel.ForEach(items, o =>
                    {
                        switch (o.AssetType)
                        {
                            case AssetType.Folder:
                                Client.Inventory.MoveFolder(o.UUID,
                                    Client.Inventory.FindFolderForType(AssetType.TrashFolder));
                                break;
                            default:
                                Client.Inventory.MoveItem(o.UUID,
                                    Client.Inventory.FindFolderForType(AssetType.TrashFolder));
                                break;
                        }
                    });
                };
        }
    }
}