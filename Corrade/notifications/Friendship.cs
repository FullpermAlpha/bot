﻿///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenMetaverse;
using wasSharp;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeNotifications
        {
            public static Action<CorradeNotificationParameters, Dictionary<string, string>> friendship =
                (corradeNotificationParameters, notificationData) =>
                {
                    System.Type friendshipNotificationType = corradeNotificationParameters.Event.GetType();
                    if (friendshipNotificationType == typeof (FriendInfoEventArgs))
                    {
                        FriendInfoEventArgs friendInfoEventArgs =
                            (FriendInfoEventArgs) corradeNotificationParameters.Event;
                        // In case we should send specific data then query the structure and return.
                        if (corradeNotificationParameters.Notification.Data != null &&
                            corradeNotificationParameters.Notification.Data.Any())
                        {
                            notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.DATA),
                                CSV.FromEnumerable(GetStructuredData(friendInfoEventArgs,
                                    CSV.FromEnumerable(corradeNotificationParameters.Notification.Data))));
                            return;
                        }
                        IEnumerable<string> name = GetAvatarNames(friendInfoEventArgs.Friend.Name);
                        if (name != null)
                        {
                            List<string> fullName = new List<string>(name);
                            if (fullName.Count.Equals(2))
                            {
                                notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.FIRSTNAME),
                                    fullName.First());
                                notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.LASTNAME),
                                    fullName.Last());
                            }
                        }
                        notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.AGENT),
                            friendInfoEventArgs.Friend.UUID.ToString());
                        notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.STATUS),
                            friendInfoEventArgs.Friend.IsOnline
                                ? Reflection.GetNameFromEnumValue(Action.ONLINE)
                                : Reflection.GetNameFromEnumValue(Action.OFFLINE));
                        notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.RIGHTS),
                            // Return the friend rights as a nice CSV string.
                            CSV.FromEnumerable(typeof (FriendRights).GetFields(BindingFlags.Public |
                                                                               BindingFlags.Static)
                                .AsParallel().Where(
                                    p =>
                                        !(((int) p.GetValue(null) &
                                           (int) friendInfoEventArgs.Friend.MyFriendRights))
                                            .Equals(
                                                0))
                                .Select(p => p.Name)));
                        notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.ACTION),
                            Reflection.GetNameFromEnumValue(Action.UPDATE));
                        return;
                    }
                    if (friendshipNotificationType == typeof (FriendshipResponseEventArgs))
                    {
                        FriendshipResponseEventArgs friendshipResponseEventArgs =
                            (FriendshipResponseEventArgs) corradeNotificationParameters.Event;
                        // In case we should send specific data then query the structure and return.
                        if (corradeNotificationParameters.Notification.Data != null &&
                            corradeNotificationParameters.Notification.Data.Any())
                        {
                            notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.DATA),
                                CSV.FromEnumerable(GetStructuredData(friendshipResponseEventArgs,
                                    CSV.FromEnumerable(corradeNotificationParameters.Notification.Data))));
                            return;
                        }
                        IEnumerable<string> name = GetAvatarNames(friendshipResponseEventArgs.AgentName);
                        if (name != null)
                        {
                            List<string> fullName = new List<string>(name);
                            if (fullName.Count.Equals(2))
                            {
                                notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.FIRSTNAME),
                                    fullName.First());
                                notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.LASTNAME),
                                    fullName.Last());
                            }
                        }
                        notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.AGENT),
                            friendshipResponseEventArgs.AgentID.ToString());
                        notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.ACTION),
                            Reflection.GetNameFromEnumValue(Action.RESPONSE));
                        return;
                    }
                    if (friendshipNotificationType == typeof (FriendshipOfferedEventArgs))
                    {
                        FriendshipOfferedEventArgs friendshipOfferedEventArgs =
                            (FriendshipOfferedEventArgs) corradeNotificationParameters.Event;
                        // In case we should send specific data then query the structure and return.
                        if (corradeNotificationParameters.Notification.Data != null &&
                            corradeNotificationParameters.Notification.Data.Any())
                        {
                            notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.DATA),
                                CSV.FromEnumerable(GetStructuredData(friendshipOfferedEventArgs,
                                    CSV.FromEnumerable(corradeNotificationParameters.Notification.Data))));
                            return;
                        }
                        IEnumerable<string> name = GetAvatarNames(friendshipOfferedEventArgs.AgentName);
                        if (name != null)
                        {
                            List<string> fullName = new List<string>(name);
                            if (fullName.Count.Equals(2))
                            {
                                notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.FIRSTNAME),
                                    fullName.First());
                                notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.LASTNAME),
                                    fullName.Last());
                            }
                        }
                        notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.AGENT),
                            friendshipOfferedEventArgs.AgentID.ToString());
                        notificationData.Add(Reflection.GetNameFromEnumValue(ScriptKeys.ACTION),
                            Reflection.GetNameFromEnumValue(Action.REQUEST));
                    }
                };
        }
    }
}