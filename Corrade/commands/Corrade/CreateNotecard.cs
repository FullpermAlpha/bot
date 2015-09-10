///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.Assets;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> createnotecard =
                (commandGroup, message, result) =>
                {
                    if (!HasCorradePermission(commandGroup.Name, (int) Permissions.Inventory))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    string text =
                        wasInput(wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.TEXT)),
                            message));
                    if (IsSecondLife() &&
                        Encoding.UTF8.GetByteCount(text) >
                        LINDEN_CONSTANTS.ASSETS.NOTECARD.MAXIMUM_BODY_LENTH)
                    {
                        throw new ScriptException(ScriptError.NOTECARD_MESSAGE_BODY_TOO_LARGE);
                    }
                    string name =
                        wasInput(wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.NAME)),
                            message));
                    if (string.IsNullOrEmpty(name))
                    {
                        throw new ScriptException(ScriptError.NO_NAME_PROVIDED);
                    }
                    ManualResetEvent CreateNotecardEvent = new ManualResetEvent(false);
                    bool succeeded = false;
                    InventoryItem newItem = null;
                    Client.Inventory.RequestCreateItem(Client.Inventory.FindFolderForType(AssetType.Notecard),
                        name,
                        wasInput(
                            wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.DESCRIPTION)),
                                message)),
                        AssetType.Notecard,
                        UUID.Random(), InventoryType.Notecard, PermissionMask.All,
                        delegate(bool completed, InventoryItem createdItem)
                        {
                            succeeded = completed;
                            newItem = createdItem;
                            CreateNotecardEvent.Set();
                        });
                    if (!CreateNotecardEvent.WaitOne((int) corradeConfiguration.ServicesTimeout, false))
                    {
                        throw new ScriptException(ScriptError.TIMEOUT_CREATING_ITEM);
                    }
                    if (!succeeded)
                    {
                        throw new ScriptException(ScriptError.UNABLE_TO_CREATE_ITEM);
                    }
                    AssetNotecard blank = new AssetNotecard
                    {
                        BodyText = LINDEN_CONSTANTS.ASSETS.NOTECARD.NEWLINE
                    };
                    blank.Encode();
                    ManualResetEvent UploadBlankNotecardEvent = new ManualResetEvent(false);
                    succeeded = false;
                    Client.Inventory.RequestUploadNotecardAsset(blank.AssetData, newItem.UUID,
                        delegate(bool completed, string status, UUID itemUUID, UUID assetUUID)
                        {
                            succeeded = completed;
                            UploadBlankNotecardEvent.Set();
                        });
                    if (!UploadBlankNotecardEvent.WaitOne((int) corradeConfiguration.ServicesTimeout, false))
                    {
                        throw new ScriptException(ScriptError.TIMEOUT_UPLOADING_ITEM);
                    }
                    if (!succeeded)
                    {
                        throw new ScriptException(ScriptError.UNABLE_TO_UPLOAD_ITEM);
                    }
                    if (!string.IsNullOrEmpty(text))
                    {
                        AssetNotecard notecard = new AssetNotecard
                        {
                            BodyText = text
                        };
                        notecard.Encode();
                        ManualResetEvent UploadNotecardDataEvent = new ManualResetEvent(false);
                        succeeded = false;
                        Client.Inventory.RequestUploadNotecardAsset(notecard.AssetData, newItem.UUID,
                            delegate(bool completed, string status, UUID itemUUID, UUID assetUUID)
                            {
                                succeeded = completed;
                                UploadNotecardDataEvent.Set();
                            });
                        if (!UploadNotecardDataEvent.WaitOne((int) corradeConfiguration.ServicesTimeout, false))
                        {
                            throw new ScriptException(ScriptError.TIMEOUT_UPLOADING_ITEM_DATA);
                        }
                        if (!succeeded)
                        {
                            throw new ScriptException(ScriptError.UNABLE_TO_UPLOAD_ITEM_DATA);
                        }
                    }
                };
        }
    }
}