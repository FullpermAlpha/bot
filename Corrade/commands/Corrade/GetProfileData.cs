///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> getprofiledata =
                (commandGroup, message, result) =>
                {
                    if (
                        !HasCorradePermission(commandGroup.Name,
                            (int) Permissions.Interact))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    UUID agentUUID;
                    if (
                        !UUID.TryParse(
                            wasInput(wasKeyValueGet(
                                wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.AGENT)), message)),
                            out agentUUID) && !AgentNameToUUID(
                                wasInput(
                                    wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.FIRSTNAME)),
                                        message)),
                                wasInput(
                                    wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.LASTNAME)),
                                        message)),
                                corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout,
                                ref agentUUID))
                    {
                        throw new ScriptException(ScriptError.AGENT_NOT_FOUND);
                    }
                    wasAdaptiveAlarm ProfileDataReceivedAlarm =
                        new wasAdaptiveAlarm(corradeConfiguration.DataDecayType);
                    Avatar.AvatarProperties properties = new Avatar.AvatarProperties();
                    Avatar.Interests interests = new Avatar.Interests();
                    List<AvatarGroup> groups = new List<AvatarGroup>();
                    AvatarPicksReplyEventArgs picks = null;
                    AvatarClassifiedReplyEventArgs classifieds = null;
                    object LockObject = new object();
                    EventHandler<AvatarInterestsReplyEventArgs> AvatarInterestsReplyEventHandler = (sender, args) =>
                    {
                        ProfileDataReceivedAlarm.Alarm(corradeConfiguration.DataTimeout);
                        interests = args.Interests;
                    };
                    EventHandler<AvatarPropertiesReplyEventArgs> AvatarPropertiesReplyEventHandler =
                        (sender, args) =>
                        {
                            ProfileDataReceivedAlarm.Alarm(corradeConfiguration.DataTimeout);
                            properties = args.Properties;
                        };
                    EventHandler<AvatarGroupsReplyEventArgs> AvatarGroupsReplyEventHandler = (sender, args) =>
                    {
                        ProfileDataReceivedAlarm.Alarm(corradeConfiguration.DataTimeout);
                        lock (LockObject)
                        {
                            groups.AddRange(args.Groups);
                        }
                    };
                    EventHandler<AvatarPicksReplyEventArgs> AvatarPicksReplyEventHandler =
                        (sender, args) =>
                        {
                            ProfileDataReceivedAlarm.Alarm(corradeConfiguration.DataTimeout);
                            picks = args;
                        };
                    EventHandler<AvatarClassifiedReplyEventArgs> AvatarClassifiedReplyEventHandler =
                        (sender, args) =>
                        {
                            ProfileDataReceivedAlarm.Alarm(corradeConfiguration.DataTimeout);
                            classifieds = args;
                        };
                    lock (ClientInstanceAvatarsLock)
                    {
                        Client.Avatars.AvatarInterestsReply += AvatarInterestsReplyEventHandler;
                        Client.Avatars.AvatarPropertiesReply += AvatarPropertiesReplyEventHandler;
                        Client.Avatars.AvatarGroupsReply += AvatarGroupsReplyEventHandler;
                        Client.Avatars.AvatarPicksReply += AvatarPicksReplyEventHandler;
                        Client.Avatars.AvatarClassifiedReply += AvatarClassifiedReplyEventHandler;
                        Client.Avatars.RequestAvatarProperties(agentUUID);
                        Client.Avatars.RequestAvatarPicks(agentUUID);
                        Client.Avatars.RequestAvatarClassified(agentUUID);
                        if (
                            !ProfileDataReceivedAlarm.Signal.WaitOne((int) corradeConfiguration.ServicesTimeout,
                                false))
                        {
                            Client.Avatars.AvatarInterestsReply -= AvatarInterestsReplyEventHandler;
                            Client.Avatars.AvatarPropertiesReply -= AvatarPropertiesReplyEventHandler;
                            Client.Avatars.AvatarGroupsReply -= AvatarGroupsReplyEventHandler;
                            Client.Avatars.AvatarPicksReply -= AvatarPicksReplyEventHandler;
                            Client.Avatars.AvatarClassifiedReply -= AvatarClassifiedReplyEventHandler;
                            throw new ScriptException(ScriptError.TIMEOUT_GETTING_AVATAR_DATA);
                        }
                        Client.Avatars.AvatarInterestsReply -= AvatarInterestsReplyEventHandler;
                        Client.Avatars.AvatarPropertiesReply -= AvatarPropertiesReplyEventHandler;
                        Client.Avatars.AvatarGroupsReply -= AvatarGroupsReplyEventHandler;
                        Client.Avatars.AvatarPicksReply -= AvatarPicksReplyEventHandler;
                        Client.Avatars.AvatarClassifiedReply -= AvatarClassifiedReplyEventHandler;
                    }
                    string fields =
                        wasInput(wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.DATA)),
                            message));
                    List<string> csv = new List<string>();
                    csv.AddRange(GetStructuredData(properties, fields));
                    csv.AddRange(GetStructuredData(interests, fields));
                    csv.AddRange(GetStructuredData(groups, fields));
                    if (picks != null)
                    {
                        csv.AddRange(GetStructuredData(picks, fields));
                    }
                    if (classifieds != null)
                    {
                        csv.AddRange(GetStructuredData(classifieds, fields));
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