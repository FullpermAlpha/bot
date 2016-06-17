﻿///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2016 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Serialization;
using CorradeConfiguration;
using HtmlAgilityPack;
using wasOpenMetaverse;
using wasSharp;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<CorradeCommandParameters, Dictionary<string, string>> getaccounttransactionsdata =
                (corradeCommandParameters, result) =>
                {
                    if (
                        !HasCorradePermission(corradeCommandParameters.Group.UUID,
                            (int) Configuration.Permissions.Interact))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }

                    if (!Helpers.IsSecondLife(Client))
                    {
                        throw new ScriptException(ScriptError.FEATURE_ONLY_AVAILABLE_IN_SECONDLIFE);
                    }

                    var firstname = wasInput(
                        KeyValue.Get(
                            wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.FIRSTNAME)),
                            corradeCommandParameters.Message));
                    if (string.IsNullOrEmpty(firstname))
                    {
                        firstname = Client.Self.FirstName;
                    }

                    var lastname = wasInput(
                        KeyValue.Get(
                            wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.LASTNAME)),
                            corradeCommandParameters.Message));
                    if (string.IsNullOrEmpty(lastname))
                    {
                        lastname = Client.Self.LastName;
                    }

                    var secret = wasInput(
                        KeyValue.Get(
                            wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.SECRET)),
                            corradeCommandParameters.Message));
                    if (string.IsNullOrEmpty(secret))
                        throw new ScriptException(ScriptError.NO_SECRET_PROVIDED);

                    DateTime from;
                    if (!DateTime.TryParse(wasInput(
                        KeyValue.Get(
                            wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.FROM)),
                            corradeCommandParameters.Message)), out from))
                    {
                        throw new ScriptException(ScriptError.INVALID_DATE);
                    }

                    DateTime to;
                    if (!DateTime.TryParse(wasInput(
                        KeyValue.Get(
                            wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.TO)),
                            corradeCommandParameters.Message)), out to))
                    {
                        throw new ScriptException(ScriptError.INVALID_DATE);
                    }

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

                    postData = wasGET("https://secondlife.com/my/account/download_transactions.php",
                        new Dictionary<string, string>
                        {
                            {"date_start", from.ToString("yyyy-MM-dd ")},
                            {"date_end", to.ToString("yyyy-MM-dd ")},
                            {"type", "xml"},
                            {"include_zero", "yes"}
                        }, cookieContainer, corradeConfiguration.ServicesTimeout);

                    if (postData.Result == null)
                        throw new ScriptException(ScriptError.NO_TRANSACTIONS_FOUND);

                    Transactions transactions;
                    var serializer = new XmlSerializer(typeof (Transactions));
                    try
                    {
                        using (TextReader reader = new StringReader(Encoding.UTF8.GetString(postData.Result)))
                        {
                            transactions = (Transactions) serializer.Deserialize(reader);
                        }
                    }
                    catch (Exception)
                    {
                        throw new ScriptException(ScriptError.UNABLE_TO_RETRIEVE_TRANSACTIONS);
                    }
                    var data = wasInput(KeyValue.Get(wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.DATA)),
                        corradeCommandParameters.Message));
                    var csv = new List<string>(transactions.list.SelectMany(o => GetStructuredData(o, data)));
                    if (csv.Any())
                    {
                        result.Add(Reflection.GetNameFromEnumValue(ResultKeys.DATA),
                            CSV.FromEnumerable(csv));
                    }
                };
        }
    }
}