///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using CorradeConfiguration;
using OpenMetaverse;
using wasSharp;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<CorradeCommandParameters, Dictionary<string, string>> setprimitivetexturedata =
                (corradeCommandParameters, result) =>
                {
                    if (
                        !HasCorradePermission(corradeCommandParameters.Group.Name,
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
                    if (
                        !FindPrimitive(
                            StringOrUUID(wasInput(KeyValue.Get(
                                wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.ITEM)),
                                corradeCommandParameters.Message))),
                            range,
                            ref primitive, corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout))
                    {
                        throw new ScriptException(ScriptError.PRIMITIVE_NOT_FOUND);
                    }
                    string face =
                        wasInput(KeyValue.Get(wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.FACE)),
                            corradeCommandParameters.Message));
                    int i;
                    switch (!int.TryParse(face, out i))
                    {
                        case true:
                            switch (face.ToLowerInvariant())
                            {
                                case "all":
                                    i = primitive.Textures.FaceTextures.Count() - 1;
                                    do
                                    {
                                        if (primitive.Textures.FaceTextures[i] == null)
                                        {
                                            primitive.Textures.FaceTextures[i] =
                                                primitive.Textures.CreateFace((uint) i);
                                        }
                                        wasCSVToStructure(
                                            wasInput(
                                                KeyValue.Get(
                                                    wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.DATA)),
                                                    corradeCommandParameters.Message)),
                                            ref primitive.Textures.FaceTextures[i]);
                                    } while (--i > -1);
                                    break;
                                case "default":
                                    wasCSVToStructure(
                                        wasInput(
                                            KeyValue.Get(
                                                wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.DATA)),
                                                corradeCommandParameters.Message)),
                                        ref primitive.Textures.DefaultTexture);
                                    break;
                                default:
                                    throw new ScriptException(ScriptError.INVALID_FACE_SPECIFIED);
                            }
                            break;
                        default:
                            if (i < 0 || i > Primitive.TextureEntry.MAX_FACES)
                                throw new ScriptException(ScriptError.INVALID_FACE_SPECIFIED);
                            if (primitive.Textures.FaceTextures[i] == null)
                            {
                                primitive.Textures.FaceTextures[i] = primitive.Textures.CreateFace((uint) i);
                            }
                            wasCSVToStructure(
                                wasInput(
                                    KeyValue.Get(
                                        wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.DATA)),
                                        corradeCommandParameters.Message)),
                                ref primitive.Textures.FaceTextures[i]);
                            break;
                    }
                    Client.Objects.SetTextures(
                        Client.Network.Simulators.AsParallel()
                            .FirstOrDefault(o => o.Handle.Equals(primitive.RegionHandle)),
                        primitive.LocalID, primitive.Textures);
                };
        }
    }
}