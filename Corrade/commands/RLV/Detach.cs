///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;
using System.Text.RegularExpressions;
using OpenMetaverse;
using Inventory = wasOpenMetaverse.Inventory;
using Parallel = System.Threading.Tasks.Parallel;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class RLVBehaviours
        {
            public static Action<string, RLVRule, UUID> detach = (message, rule, senderUUID) =>
            {
                if (!rule.Param.Equals(RLV_CONSTANTS.FORCE))
                {
                    return;
                }
                InventoryNode RLVFolder =
                    Inventory.FindInventory<InventoryNode>(Client, Client.Inventory.Store.RootNode,
                        RLV_CONSTANTS.SHARED_FOLDER_NAME)
                        .AsParallel()
                        .FirstOrDefault(o => o.Data is InventoryFolder);
                if (RLVFolder == null)
                {
                    return;
                }
                switch (!string.IsNullOrEmpty(rule.Option))
                {
                    case true:
                        RLVAttachment RLVattachment =
                            RLVAttachments.AsParallel().FirstOrDefault(
                                o => o.Name.Equals(rule.Option, StringComparison.InvariantCultureIgnoreCase));
                        switch (!RLVattachment.Equals(default(RLVAttachment)))
                        {
                            case true: // detach by attachment point
                                Parallel.ForEach(
                                    Inventory.GetAttachments(Client, corradeConfiguration.DataTimeout)
                                        .AsParallel()
                                        .Where(o => o.Value.Equals(RLVattachment.AttachmentPoint))
                                        .SelectMany(
                                            p =>
                                                Inventory.FindInventory<InventoryBase>(Client,
                                                    Client.Inventory.Store.RootNode,
                                                    p.Key.Properties.Name)
                                                    .AsParallel().Where(
                                                        o =>
                                                            o is InventoryAttachment || o is InventoryObject))
                                        .Where(o => o != null)
                                        .Select(o => o as InventoryItem), o =>
                                            Inventory.Detach(Client, CurrentOutfitFolder, o,
                                                corradeConfiguration.ServicesTimeout));
                                break;
                            default: // detach by folder(s) name
                                if (string.IsNullOrEmpty(rule.Option)) break;
                                Parallel.ForEach(
                                    rule.Option.Split(RLV_CONSTANTS.PATH_SEPARATOR[0])
                                        .AsParallel().Select(
                                            p =>
                                                Inventory.FindInventory<InventoryBase>(Client, RLVFolder,
                                                    new Regex(Regex.Escape(p),
                                                        RegexOptions.Compiled | RegexOptions.IgnoreCase)
                                                    ).AsParallel().FirstOrDefault(o => o is InventoryFolder))
                                        .Where(o => o != null)
                                        .SelectMany(
                                            o =>
                                                Client.Inventory.Store.GetContents(o as InventoryFolder)
                                                    .AsParallel().Where(Inventory.CanBeWorn)), o =>
                                                    {
                                                        if (o is InventoryWearable)
                                                        {
                                                            Inventory.UnWear(Client, CurrentOutfitFolder,
                                                                o as InventoryItem, corradeConfiguration.ServicesTimeout);
                                                            return;
                                                        }
                                                        if (o is InventoryAttachment || o is InventoryObject)
                                                        {
                                                            Inventory.Detach(Client, CurrentOutfitFolder,
                                                                o as InventoryItem, corradeConfiguration.ServicesTimeout);
                                                        }
                                                    });
                                RebakeTimer.Change(corradeConfiguration.RebakeDelay, 0);
                                break;
                        }
                        break;
                    default:
                        Parallel.ForEach(
                            Inventory.GetAttachments(Client, corradeConfiguration.DataTimeout)
                                .AsParallel()
                                .Where(o => RLVAttachments.Any(p => p.AttachmentPoint.Equals(o.Value)))
                                .SelectMany(
                                    o =>
                                        o.Key.NameValues.AsParallel()
                                            .Where(p => p.Name.Equals("AttachItemID", StringComparison.Ordinal))), o =>
                                            {
                                                UUID itemUUID;
                                                if (UUID.TryParse(o.Value.ToString(), out itemUUID))
                                                {
                                                    Inventory.Detach(Client, CurrentOutfitFolder,
                                                        Client.Inventory.Store.Items[itemUUID].Data as InventoryItem,
                                                        corradeConfiguration.ServicesTimeout);
                                                }
                                            });
                        break;
                }
            };
        }
    }
}