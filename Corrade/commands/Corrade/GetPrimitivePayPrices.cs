///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CorradeConfiguration;
using OpenMetaverse;
using wasOpenMetaverse;
using wasSharp;

namespace Corrade
{
    public partial class Corrade
    {
        public static partial class CorradeCommands
        {
            public static Action<CorradeCommandParameters, Dictionary<string, string>> getprimitivepayprices =
                (corradeCommandParameters, result) =>
                {
                    if (
                        !HasCorradePermission(corradeCommandParameters.Group.UUID,
                            (int) Configuration.Permissions.Interact))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    float range;
                    if (
                        !float.TryParse(
                            wasInput(KeyValue.Get(
                                wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.RANGE)),
                                corradeCommandParameters.Message)),
                            out range))
                    {
                        range = corradeConfiguration.Range;
                    }
                    Primitive primitive = null;
                    string item = wasInput(KeyValue.Get(
                        wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.ITEM)),
                        corradeCommandParameters.Message));
                    if (string.IsNullOrEmpty(item))
                    {
                        throw new ScriptException(ScriptError.NO_ITEM_SPECIFIED);
                    }
                    UUID itemUUID;
                    if (UUID.TryParse(item, out itemUUID))
                    {
                        if (
                            !Services.FindPrimitive(Client,
                                itemUUID,
                                range,
                                corradeConfiguration.Range,
                                ref primitive, corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout,
                                new Time.DecayingAlarm(corradeConfiguration.DataDecayType)))
                        {
                            throw new ScriptException(ScriptError.PRIMITIVE_NOT_FOUND);
                        }
                    }
                    else
                    {
                        if (
                            !Services.FindPrimitive(Client,
                                item,
                                range,
                                corradeConfiguration.Range,
                                ref primitive, corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout,
                                new Time.DecayingAlarm(corradeConfiguration.DataDecayType)))
                        {
                            throw new ScriptException(ScriptError.PRIMITIVE_NOT_FOUND);
                        }
                    }
                    if (primitive.Properties.SaleType.Equals(SaleType.Not))
                    {
                        throw new ScriptException(ScriptError.PRIMITIVE_NOT_FOR_SALE);
                    }
                    List<string> csv = new List<string>();
                    ManualResetEvent PayPrceReceivedEvent = new ManualResetEvent(false);
                    EventHandler<PayPriceReplyEventArgs> PayPriceReplyEventHandler = (sender, args) =>
                    {
                        csv.Add(args.DefaultPrice.ToString(Utils.EnUsCulture));
                        csv.AddRange(
                            args.ButtonPrices.Select(o => o.ToString(Utils.EnUsCulture)));
                        PayPrceReceivedEvent.Set();
                    };
                    lock (Locks.ClientInstanceObjectsLock)
                    {
                        Client.Objects.PayPriceReply += PayPriceReplyEventHandler;
                        Client.Objects.RequestPayPrice(
                            Client.Network.Simulators.AsParallel()
                                .FirstOrDefault(o => o.Handle.Equals(primitive.RegionHandle)),
                            primitive.ID);
                        if (!PayPrceReceivedEvent.WaitOne((int) corradeConfiguration.ServicesTimeout, false))
                        {
                            Client.Objects.PayPriceReply -= PayPriceReplyEventHandler;
                            throw new ScriptException(ScriptError.TIMEOUT_REQUESTING_PRICE);
                        }
                        Client.Objects.PayPriceReply -= PayPriceReplyEventHandler;
                    }
                    if (csv.Any())
                    {
                        result.Add(Reflection.GetNameFromEnumValue(ResultKeys.DATA),
                            CSV.FromEnumerable(csv));
                    }
                };
        }
    }
}