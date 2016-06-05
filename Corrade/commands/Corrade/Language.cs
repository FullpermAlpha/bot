﻿///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CorradeConfiguration;
using NTextCat;
using wasSharp;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<CorradeCommandParameters, Dictionary<string, string>> language =
                (corradeCommandParameters, result) =>
                {
                    if (
                        !HasCorradePermission(corradeCommandParameters.Group.UUID,
                            (int)Configuration.Permissions.Talk))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    switch (Reflection.GetEnumValueFromName<Action>(
                        wasInput(
                            KeyValue.Get(
                                wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.ACTION)),
                                corradeCommandParameters.Message))
                            .ToLowerInvariant()))
                    {
                        case Action.DETECT:
                            string message = wasInput(
                                KeyValue.Get(
                                    wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.MESSAGE)),
                                    corradeCommandParameters.Message));
                            if (string.IsNullOrEmpty(message))
                            {
                                throw new ScriptException(ScriptError.NO_MESSAGE_PROVIDED);
                            }
                            // language detection
                            string profilePath = IO.PathCombine(CORRADE_CONSTANTS.LIBS_DIRECTORY,
                                CORRADE_CONSTANTS.LANGUAGE_PROFILE_FILE);
                            List<string> csv = new List<string>();
                            if (File.Exists(profilePath))
                            {
                                HashSet<Tuple<LanguageInfo, double>> detectedLanguages = new HashSet
                                    <Tuple<LanguageInfo, double>>(
                                    new RankedLanguageIdentifierFactory().Load(profilePath)
                                        .Identify(message));
                                if (detectedLanguages.Any())
                                {
                                    foreach (Tuple<LanguageInfo, double> language in detectedLanguages)
                                    {
                                        csv.Add(language.Item1.Iso639_3);
                                        csv.Add(language.Item2.ToString(CultureInfo.InvariantCulture));
                                    }
                                }
                            }
                            if (csv.Any())
                            {
                                result.Add(Reflection.GetNameFromEnumValue(ResultKeys.DATA), CSV.FromEnumerable(csv));
                            }
                            break;
                        default:
                            throw new ScriptException(ScriptError.UNKNOWN_ACTION);
                    }
                };
        }
    }
}