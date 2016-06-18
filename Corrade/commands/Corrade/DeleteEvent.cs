﻿///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2016 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using CorradeConfiguration;
using HtmlAgilityPack;
using wasSharp;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<CorradeCommandParameters, Dictionary<string, string>> deleteevent =
                (corradeCommandParameters, result) =>
                {
                    if (
                        !HasCorradePermission(corradeCommandParameters.Group.UUID,
                            (int) Configuration.Permissions.Interact))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }

                    var firstname = wasInput(
                        KeyValue.Get(
                            wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.FIRSTNAME)),
                            corradeCommandParameters.Message));

                    var lastname = wasInput(
                        KeyValue.Get(
                            wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.LASTNAME)),
                            corradeCommandParameters.Message));

                    if (string.IsNullOrEmpty(firstname) && string.IsNullOrEmpty(lastname))
                    {
                        firstname = Client.Self.FirstName;
                        lastname = Client.Self.LastName;
                    }

                    var secret = wasInput(
                        KeyValue.Get(
                            wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.SECRET)),
                            corradeCommandParameters.Message));
                    if (string.IsNullOrEmpty(secret))
                        throw new ScriptException(ScriptError.NO_SECRET_PROVIDED);

                    uint id;
                    if (!uint.TryParse(wasInput(
                        KeyValue.Get(
                            wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.ID)),
                            corradeCommandParameters.Message)), out id))
                        throw new ScriptException(ScriptError.NO_EVENT_IDENTIFIER_PROVIDED);

                    var cookieContainer = new CookieContainer();

                    var postData = wasPOST("https://id.secondlife.com/openid/loginsubmit",
                        new Dictionary<string, string>
                        {
                            {"username", $"{firstname} {lastname}"},
                            {"password", secret},
                            {"language", "en_US"},
                            {"previous_language", "en_US"},
                            {"from_amazon", "False"},
                            {"stay_logged_in", "True"},
                            {"show_join", "False"},
                            {"return_to", "https://secondlife.com/auth/oid_return.php"}
                        }, cookieContainer, corradeConfiguration.ServicesTimeout);

                    if (postData.Result == null)
                        throw new ScriptException(ScriptError.UNABLE_TO_AUTHENTICATE);

                    var doc = new HtmlDocument();
                    HtmlNode.ElementsFlags.Remove("form");
                    doc.LoadHtml(Encoding.UTF8.GetString(postData.Result));

                    var openIDNodes = doc.DocumentNode.SelectNodes("//form[@id='openid_message']/input[@type='hidden']");
                    if (openIDNodes == null || !openIDNodes.Any())
                        throw new ScriptException(ScriptError.UNABLE_TO_AUTHENTICATE);

                    var openID =
                        openIDNodes.AsParallel()
                            .Where(
                                o =>
                                    o.Attributes.Contains("name") && o.Attributes["name"].Value != null &&
                                    o.Attributes.Contains("value") && o.Attributes["value"].Value != null)
                            .ToDictionary(o => o.Attributes["name"].Value,
                                o => o.Attributes["value"].Value);

                    if (!openID.Any())
                        throw new ScriptException(ScriptError.UNABLE_TO_AUTHENTICATE);

                    postData = wasPOST("https://id.secondlife.com/openid/openidserver", openID, cookieContainer,
                        corradeConfiguration.ServicesTimeout);

                    if (postData.Result == null)
                        throw new ScriptException(ScriptError.UNABLE_TO_AUTHENTICATE);

                    // Events
                    postData = wasGET("https://secondlife.com/my/community/events/tos.php",
                        new Dictionary<string, string>(),
                        cookieContainer, corradeConfiguration.ServicesTimeout);

                    if (postData.Result == null)
                        throw new ScriptException(ScriptError.UNABLE_TO_AGREE_TO_TOS);

                    doc = new HtmlDocument();
                    HtmlNode.ElementsFlags.Remove("form");
                    doc.LoadHtml(Encoding.UTF8.GetString(postData.Result));
                    var ToSNodes = doc.DocumentNode.SelectNodes("//form[@action='tos.php']/input[@type='hidden']");
                    if (ToSNodes == null || !ToSNodes.Any())
                        throw new ScriptException(ScriptError.UNABLE_TO_AGREE_TO_TOS);

                    var eventToS =
                        ToSNodes
                            .ToDictionary(input => input.Attributes["name"].Value,
                                input => input.Attributes["value"].Value);
                    eventToS.Add("action", "I Agree");

                    postData = wasPOST("https://secondlife.com/my/community/events/tos.php", eventToS, cookieContainer,
                        corradeConfiguration.ServicesTimeout);

                    if (postData.Result == null)
                        throw new ScriptException(ScriptError.UNABLE_TO_AGREE_TO_TOS);

                    postData = wasGET("https://secondlife.com/my/community/events/delete.php",
                        new Dictionary<string, string>
                        {
                            {"id", id.ToString()}
                        }, cookieContainer,
                        corradeConfiguration.ServicesTimeout);

                    if (postData.Result == null)
                        throw new ScriptException(ScriptError.UNABLE_TO_DELETE_EVENT);
                };
        }
    }
}