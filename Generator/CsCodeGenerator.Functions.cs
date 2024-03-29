﻿namespace Generator
{
    using CppAst;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;

    public static partial class CsCodeGenerator
    {
        private static readonly HashSet<string> DefinedVariationsFunctions = new()
        {
        };

        public static readonly HashSet<string> DefinedFunctions = new()
        {
        };

        private static readonly HashSet<string> s_outReturnFunctions = new()
        {
        };

        private static void GenerateFunctions(CppCompilation compilation, string outputPath)
        {
            DefinedVariationsFunctions.Clear();
            string[] usings = { "System", "System.Runtime.CompilerServices", "System.Runtime.InteropServices" };

            string outDir = Path.Combine(outputPath, "Functions");
            string filePath = Path.Combine(outDir, "Functions.cs");

            if (Directory.Exists(outDir))
                Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            using var writer = new SplitCodeWriter(filePath, CsCodeGeneratorSettings.Default.Namespace, 2, usings.Concat(CsCodeGeneratorSettings.Default.Usings).ToArray());

            using (writer.PushBlock($"public unsafe partial class {CsCodeGeneratorSettings.Default.ApiName}"))
            {
                writer.WriteLine($"internal const string LibName = \"{CsCodeGeneratorSettings.Default.LibName}\";\n");
                List<CsFunction> commands = new();
                for (int i = 0; i < compilation.Functions.Count; i++)
                {
                    CppFunction? cppFunction = compilation.Functions[i];
                    if (CsCodeGeneratorSettings.Default.AllowedFunctions.Count != 0 && !CsCodeGeneratorSettings.Default.AllowedFunctions.Contains(cppFunction.Name))
                        continue;
                    if (CsCodeGeneratorSettings.Default.IgnoredFunctions.Contains(cppFunction.Name))
                        continue;

                    var cppHeader = $"{cppFunction.ReturnType} {cppFunction.Name}({string.Join(", ", cppFunction.Parameters)})";
                    if (DefinedFunctions.Contains(cppHeader))
                        continue;
                    DefinedFunctions.Add(cppHeader);

                    string? csName = GetPrettyCommandName(cppFunction.Name);
                    string returnCsName = GetCsTypeName(cppFunction.ReturnType, false);
                    CppPrimitiveKind returnKind = GetPrimitiveKind(cppFunction.ReturnType, false);

                    bool boolReturn = returnCsName == "bool";
                    bool canUseOut = s_outReturnFunctions.Contains(cppFunction.Name);
                    var argumentsString = GetParameterSignature(cppFunction, canUseOut);
                    var header = $"{returnCsName} {csName}Native({argumentsString})";

                    WriteCsSummary(cppFunction.Comment, writer);

                    string modifierString;

                    if (CsCodeGeneratorSettings.Default.UseLibraryImport)
                    {
                        writer.WriteLine($"[LibraryImport(LibName, EntryPoint = \"{cppFunction.Name}\")]");
                        writer.WriteLine($"[UnmanagedCallConv(CallConvs = new Type[] {{typeof({GetCallingConventionLibrary(cppFunction.CallingConvention)})}})]");
                        modifierString = "internal static partial";
                    }
                    else
                    {
                        writer.WriteLine($"[DllImport(LibName, CallingConvention = CallingConvention.{GetCallingConvention(cppFunction.CallingConvention)}, EntryPoint = \"{cppFunction.Name}\")]");
                        modifierString = "internal static extern";
                    }

                    if (boolReturn)
                    {
                        writer.WriteLine($"{modifierString} byte {csName}Native({argumentsString});");
                    }
                    else
                    {
                        writer.WriteLine($"{modifierString} {header};");
                    }

                    writer.WriteLine();

                    CsFunction? function = null;
                    for (int j = 0; j < commands.Count; j++)
                    {
                        if (commands[j].Name == csName)
                        {
                            function = commands[j];
                            break;
                        }
                    }

                    if (function == null)
                    {
                        WriteCsSummary(cppFunction.Comment, out string? comment);
                        function = new(csName, comment);
                        commands.Add(function);
                    }

                    CsFunctionOverload overload = new(cppFunction.Name, csName, "", false, false, false, new(returnCsName, returnKind));
                    for (int j = 0; j < cppFunction.Parameters.Count; j++)
                    {
                        var cppParameter = cppFunction.Parameters[j];
                        var paramCsTypeName = GetCsTypeName(cppParameter.Type, false);
                        var paramCsName = GetParameterName(cppParameter.Type, cppParameter.Name);
                        var direction = GetDirection(cppParameter.Type);
                        var kind = GetPrimitiveKind(cppParameter.Type, false);

                        CsType csType = new(paramCsTypeName, kind);

                        CsParameterInfo csParameter = new(paramCsName, csType, direction);

                        overload.Parameters.Add(csParameter);
                        if (TryGetDefaultValue(cppFunction.Name, cppParameter, false, out var defaultValue))
                        {
                            overload.DefaultValues.Add(paramCsName, defaultValue);
                        }
                    }

                    function.Overloads.Add(overload);
                    GenerateVariations(cppFunction.Parameters, overload, false);
                    WriteMethods(writer, DefinedVariationsFunctions, function, overload, false, false, "public static");
                }
            }
        }

        public static void WriteMethods(ICodeWriter writer, HashSet<string> definedFunctions, CsFunction csFunction, CsFunctionOverload overload, bool useThis, bool useHandle, params string[] modifiers)
        {
            for (int j = 0; j < overload.Variations.Count; j++)
            {
                WriteMethod(writer, definedFunctions, csFunction, overload, overload.Variations[j], useThis, useHandle, modifiers);
            }
        }

        private static void WriteMethod(ICodeWriter writer, HashSet<string> definedFunctions, CsFunction function, CsFunctionOverload overload, CsFunctionVariation variation, bool useThis, bool useHandle, params string[] modifiers)
        {
            CsType csReturnType = variation.ReturnType;
            if (WrappedPointers.TryGetValue(csReturnType.Name, out string? value))
            {
                csReturnType.Name = value;
            }

            for (int i = 0; i < variation.Parameters.Count; i++)
            {
                var cppParameter = variation.Parameters[i];
                if (WrappedPointers.TryGetValue(cppParameter.Type.Name, out string? v))
                {
                    cppParameter.Type.Name = v;
                    cppParameter.Type.Classify();
                }
            }

            string modifierString = string.Join(" ", modifiers);
            string signature;

            if (useThis || useHandle)
            {
                signature = string.Join(", ", variation.Parameters.Skip(1).Select(x => $"{x.Type} {x.Name}"));
            }
            else
            {
                signature = string.Join(", ", variation.Parameters.Select(x => $"{x.Type} {x.Name}"));
            }

            string header = $"{csReturnType.Name} {variation.Name}({signature})";

            if (definedFunctions.Contains(header))
            {
                return;
            }
            definedFunctions.Add(header);

            Console.WriteLine(header);

            if (function.Comment != null)
                writer.Write(function.Comment);
            using (writer.PushBlock($"{modifierString} {header}"))
            {
                StringBuilder sb = new();
                bool firstParamReturn = false;
                if (!csReturnType.IsString && csReturnType.Name != overload.ReturnType.Name)
                {
                    firstParamReturn = true;
                }

                int offset = firstParamReturn ? 1 : 0;

                bool hasManaged = false;
                for (int j = 0; j < overload.Parameters.Count - offset; j++)
                {
                    var cppParameter = overload.Parameters[j + offset];
                    if (variation.HasParameter(cppParameter))
                        continue;
                    var paramCsDefault = overload.DefaultValues[cppParameter.Name];
                    if (cppParameter.Type.IsString || paramCsDefault.StartsWith("\"") && paramCsDefault.EndsWith("\""))
                        hasManaged = true;
                }

                if (!firstParamReturn && (!csReturnType.IsVoid || csReturnType.IsVoid && csReturnType.IsPointer))
                {
                    if (csReturnType.IsBool && !csReturnType.IsPointer && !hasManaged)
                    {
                        sb.Append($"byte ret = ");
                    }
                    else
                    {
                        sb.Append($"{csReturnType.Name} ret = ");
                    }
                }

                if (csReturnType.IsString)
                {
                    WriteStringConvertToManaged(sb, variation.ReturnType);
                }

                if (useThis || useHandle)
                    sb.Append($"{CsCodeGeneratorSettings.Default.ApiName}.");
                if (hasManaged)
                    sb.Append($"{overload.Name}(");
                else if (firstParamReturn)
                    sb.Append($"{overload.Name}Native(&ret" + (overload.Parameters.Count > 1 ? ", " : ""));
                else
                    sb.Append($"{overload.Name}Native(");
                Stack<(string, CsParameterInfo, string)> stack = new();
                int strings = 0;
                Stack<string> arrays = new();
                int stacks = 0;

                for (int i = 0; i < overload.Parameters.Count - offset; i++)
                {
                    var cppParameter = overload.Parameters[i + offset];
                    var isRef = false;
                    var isPointer = false;
                    var isStr = false;
                    var isArray = false;
                    var isBool = false;
                    var isConst = true;

                    for (int j = 0; j < variation.Parameters.Count; j++)
                    {
                        var param = variation.Parameters[j];
                        if (param.Name == cppParameter.Name)
                        {
                            cppParameter = param;
                            isRef = param.Type.IsRef;
                            isPointer = param.Type.IsPointer;
                            isStr = param.Type.IsString;
                            isArray = param.Type.IsArray;
                            isBool = param.Type.IsBool;
                            isConst = false;
                        }
                    }
                    if (useHandle && i == 0)
                    {
                        sb.Append("Handle");
                    }
                    else if (useThis && i == 0 && overload.Parameters[i].Type.IsPointer)
                    {
                        writer.BeginBlock($"fixed ({overload.Parameters[i].Type.Name} @this = &this)");
                        sb.Append("@this");
                        stacks++;
                    }
                    else if (useThis && i == 0)
                    {
                        sb.Append("this");
                    }
                    else if (isConst)
                    {
                        var rootParam = overload.Parameters[i + offset];
                        var paramCsDefault = overload.DefaultValues[cppParameter.Name];
                        if (cppParameter.Type.IsString || paramCsDefault.StartsWith("\"") && paramCsDefault.EndsWith("\""))
                            sb.Append($"(string){paramCsDefault}");
                        else if (cppParameter.Type.IsBool && !cppParameter.Type.IsPointer && !cppParameter.Type.IsArray)
                            sb.Append($"(byte)({paramCsDefault})");
                        else if (cppParameter.Type.IsPrimitive || cppParameter.Type.IsPointer || cppParameter.Type.IsArray)
                            sb.Append($"({rootParam.Type.Name})({paramCsDefault})");
                        else
                            sb.Append($"{paramCsDefault}");
                    }
                    else if (isStr)
                    {
                        if (isArray)
                        {
                            WriteStringArrayConvertToUnmanaged(writer, cppParameter.Type, cppParameter.Name, arrays.Count);
                            sb.Append($"pStrArray{arrays.Count}");
                            arrays.Push(cppParameter.Name);
                        }
                        else
                        {
                            if (isRef)
                            {
                                stack.Push((cppParameter.Name, cppParameter, $"pStr{strings}"));
                            }

                            WriteStringConvertToUnmanaged(writer, cppParameter.Type, cppParameter.Name, strings);
                            sb.Append($"pStr{strings}");
                            strings++;
                        }
                    }
                    else if (isRef)
                    {
                        writer.BeginBlock($"fixed ({cppParameter.Type.CleanName}* p{cppParameter.Name} = &{cppParameter.Name})");
                        sb.Append($"({overload.Parameters[i + offset].Type.Name})p{cppParameter.Name}");
                        stacks++;
                    }
                    else if (isArray)
                    {
                        writer.BeginBlock($"fixed ({cppParameter.Type.CleanName}* p{cppParameter.Name} = {cppParameter.Name})");
                        sb.Append($"({overload.Parameters[i + offset].Type.Name})p{cppParameter.Name}");
                        stacks++;
                    }
                    else if (isBool && !isRef && !isPointer)
                    {
                        sb.Append($"{cppParameter.Name} ? (byte)1 : (byte)0");
                    }
                    else
                    {
                        sb.Append(cppParameter.Name);
                    }

                    if (i != overload.Parameters.Count - 1 - offset)
                    {
                        sb.Append(", ");
                    }
                }

                if (csReturnType.IsString)
                {
                    sb.Append("));");
                }
                else
                {
                    sb.Append(");");
                }

                if (firstParamReturn)
                {
                    writer.WriteLine($"{csReturnType.Name} ret;");
                }
                writer.WriteLine(sb.ToString());

                while (stack.TryPop(out var stackItem))
                {
                    WriteStringConvertToManaged(writer, stackItem.Item2.Type, stackItem.Item1, stackItem.Item3);
                }

                while (arrays.TryPop(out var arrayName))
                {
                    WriteFreeUnmanagedStringArray(writer, arrayName, arrays.Count);
                }

                while (strings > 0)
                {
                    strings--;
                    WriteFreeString(writer, strings);
                }

                if (firstParamReturn || !csReturnType.IsVoid || csReturnType.IsVoid && csReturnType.IsPointer)
                {
                    if (csReturnType.IsBool && !csReturnType.IsPointer && !hasManaged)
                    {
                        writer.WriteLine("return ret != 0;");
                    }
                    else
                    {
                        writer.WriteLine("return ret;");
                    }
                }

                while (stacks > 0)
                {
                    stacks--;
                    writer.EndBlock();
                }
            }

            writer.WriteLine();
        }

        public static string GetParameterSignature(CppFunction cppFunction, bool canUseOut)
        {
            return GetParameterSignature(cppFunction.Parameters, canUseOut);
        }

        private static string GetCleanParamType(string param)
        {
            param = param.Replace("ref ", string.Empty);
            return param.Split(" ")[0];
        }

        private static bool IsVoid(CppType cppType)
        {
            if (cppType is CppPrimitiveType type)
            {
                return type.Kind == CppPrimitiveKind.Void;
            }
            return false;
        }

        private static bool IsString(CppType cppType, bool isPointer = false)
        {
            if (cppType is CppPointerType pointer && !isPointer)
            {
                return IsString(pointer.ElementType, true);
            }

            if (cppType is CppQualifiedType qualified)
            {
                return IsString(qualified.ElementType, isPointer);
            }

            if (isPointer && cppType is CppPrimitiveType primitive)
            {
                return primitive.Kind == CppPrimitiveKind.WChar || primitive.Kind == CppPrimitiveKind.Char;
            }

            return false;
        }

        private static CppPrimitiveKind GetPrimitiveKind(CppType cppType, bool isPointer)
        {
            if (cppType is CppArrayType arrayType)
            {
                return GetPrimitiveKind(arrayType.ElementType, true);
            }

            if (cppType is CppPointerType pointer)
            {
                return GetPrimitiveKind(pointer.ElementType, true);
            }

            if (cppType is CppQualifiedType qualified)
            {
                return GetPrimitiveKind(qualified.ElementType, isPointer);
            }

            if (isPointer && cppType is CppPrimitiveType primitive)
            {
                return primitive.Kind;
            }

            return CppPrimitiveKind.Void;
        }

        private static void WriteStringConvertToManaged(StringBuilder sb, CppType type)
        {
            CppPrimitiveKind primitiveKind = GetPrimitiveKind(type, false);
            if (primitiveKind == CppPrimitiveKind.Char)
            {
                sb.Append("Utils.DecodeStringUTF8(");
            }
            if (primitiveKind == CppPrimitiveKind.WChar)
            {
                sb.Append("Utils.DecodeStringUTF16(");
            }
        }

        private static void WriteStringConvertToManaged(StringBuilder sb, CsType type)
        {
            if (type.StringType == CsStringType.StringUTF8)
            {
                sb.Append("Utils.DecodeStringUTF8(");
            }
            if (type.StringType == CsStringType.StringUTF16)
            {
                sb.Append("Utils.DecodeStringUTF16(");
            }
        }

        private static void WriteStringConvertToManaged(ICodeWriter writer, CppType type, string variable, string pointer)
        {
            CppPrimitiveKind primitiveKind = GetPrimitiveKind(type, false);
            if (primitiveKind == CppPrimitiveKind.Char)
            {
                writer.WriteLine($"{variable} = Marshal.DecodeStringUTF8({pointer});");
            }
            if (primitiveKind == CppPrimitiveKind.WChar)
            {
                writer.WriteLine($"{variable} = Marshal.DecodeStringUTF16({pointer});");
            }
        }

        private static void WriteStringConvertToManaged(ICodeWriter writer, CsType type, string variable, string pointer)
        {
            if (type.StringType == CsStringType.StringUTF8)
            {
                writer.WriteLine($"{variable} = Utils.DecodeStringUTF8({pointer});");
            }
            if (type.StringType == CsStringType.StringUTF16)
            {
                writer.WriteLine($"{variable} = Utils.DecodeStringUTF16({pointer});");
            }
        }

        private static void WriteStringConvertToUnmanaged(ICodeWriter writer, CppType type, string name, int i)
        {
            CppPrimitiveKind primitiveKind = GetPrimitiveKind(type, false);
            if (primitiveKind == CppPrimitiveKind.Char)
            {
                writer.WriteLine($"byte* pStr{i} = null;");
                writer.WriteLine($"int pStrSize{i} = 0;");
                using (writer.PushBlock($"if ({name} != null)"))
                {
                    writer.WriteLine($"pStrSize{i} = Utils.GetByteCountUTF8({name});");
                    using (writer.PushBlock($"if (pStrSize{i} >= Utils.MaxStackallocSize)"))
                    {
                        writer.WriteLine($"pStr{i} = Utils.Alloc<byte>(pStrSize{i} + 1);");
                    }
                    using (writer.PushBlock("else"))
                    {
                        writer.WriteLine($"byte* pStrStack{i} = stackalloc byte[pStrSize{i} + 1];");
                        writer.WriteLine($"pStr{i} = pStrStack{i};");
                    }
                    writer.WriteLine($"int pStrOffset{i} = Utils.EncodeStringUTF8({name}, pStr{i}, pStrSize{i});");
                    writer.WriteLine($"pStr{i}[pStrOffset{i}] = 0;");
                }
            }
            if (primitiveKind == CppPrimitiveKind.WChar)
            {
                writer.WriteLine($"char* pStr{i} = (char*)Marshal.StringToHGlobalUni({name});");
            }
        }

        private static void WriteStringConvertToUnmanaged(ICodeWriter writer, CsType type, string name, int i)
        {
            if (type.StringType == CsStringType.StringUTF8)
            {
                writer.WriteLine($"byte* pStr{i} = null;");
                writer.WriteLine($"int pStrSize{i} = 0;");
                using (writer.PushBlock($"if ({name} != null)"))
                {
                    writer.WriteLine($"pStrSize{i} = Utils.GetByteCountUTF8({name});");
                    using (writer.PushBlock($"if (pStrSize{i} >= Utils.MaxStackallocSize)"))
                    {
                        writer.WriteLine($"pStr{i} = Utils.Alloc<byte>(pStrSize{i} + 1);");
                    }
                    using (writer.PushBlock("else"))
                    {
                        writer.WriteLine($"byte* pStrStack{i} = stackalloc byte[pStrSize{i} + 1];");
                        writer.WriteLine($"pStr{i} = pStrStack{i};");
                    }
                    writer.WriteLine($"int pStrOffset{i} = Utils.EncodeStringUTF8({name}, pStr{i}, pStrSize{i});");
                    writer.WriteLine($"pStr{i}[pStrOffset{i}] = 0;");
                }
            }
            if (type.StringType == CsStringType.StringUTF16)
            {
                writer.WriteLine($"char* pStr{i} = (char*)Marshal.StringToHGlobalUni({name});");
            }
        }

        private static void WriteFreeString(ICodeWriter writer, int i)
        {
            using (writer.PushBlock($"if (pStrSize{i} >= Utils.MaxStackallocSize)"))
            {
                writer.WriteLine($"Utils.Free(pStr{i});");
            }
        }

        private static void WriteStringArrayConvertToUnmanaged(ICodeWriter writer, CppType type, string name, int i)
        {
            CppPrimitiveKind primitiveKind = GetPrimitiveKind(type, false);
            if (primitiveKind == CppPrimitiveKind.Char)
            {
                writer.WriteLine($"byte** pStrArray{i} = null;");
                writer.WriteLine($"int pStrArraySize{i} = Utils.GetByteCountArray({name});");
                using (writer.PushBlock($"if ({name} != null)"))
                {
                    using (writer.PushBlock($"if (pStrArraySize{i} > Utils.MaxStackallocSize)"))
                    {
                        writer.WriteLine($"pStrArray{i} = (byte**)Utils.Alloc<byte>(pStrArraySize{i});");
                    }
                    using (writer.PushBlock("else"))
                    {
                        writer.WriteLine($"byte* pStrArrayStack{i} = stackalloc byte[pStrArraySize{i}];");
                        writer.WriteLine($"pStrArray{i} = (byte**)pStrArrayStack{i};");
                    }
                }
                using (writer.PushBlock($"for (int i = 0; i < {name}.Length; i++)"))
                {
                    writer.WriteLine($"pStrArray{i}[i] = (byte*)Marshal.StringToHGlobalAnsi({name}[i]);");
                }
            }
            if (primitiveKind == CppPrimitiveKind.WChar)
            {
                writer.WriteLine($"char** pAStr{i} = (char**)Marshal.AllocHGlobal(sizeof(nuint) * {name}.Length);");
                using (writer.PushBlock($"for (int i = 0; i < {name}.Length; i++)"))
                {
                    writer.WriteLine($"pAStr{i}[i] = (char*)Marshal.StringToHGlobalUni({name}[i]);");
                }
            }
        }

        private static void WriteStringArrayConvertToUnmanaged(ICodeWriter writer, CsType type, string name, int i)
        {
            if (type.StringType == CsStringType.StringUTF8)
            {
                writer.WriteLine($"byte** pStrArray{i} = null;");
                writer.WriteLine($"int pStrArraySize{i} = Utils.GetByteCountArray({name});");
                using (writer.PushBlock($"if ({name} != null)"))
                {
                    using (writer.PushBlock($"if (pStrArraySize{i} > Utils.MaxStackallocSize)"))
                    {
                        writer.WriteLine($"pStrArray{i} = (byte**)Utils.Alloc<byte>(pStrArraySize{i});");
                    }
                    using (writer.PushBlock("else"))
                    {
                        writer.WriteLine($"byte* pStrArrayStack{i} = stackalloc byte[pStrArraySize{i}];");
                        writer.WriteLine($"pStrArray{i} = (byte**)pStrArrayStack{i};");
                    }
                }
                using (writer.PushBlock($"for (int i = 0; i < {name}.Length; i++)"))
                {
                    writer.WriteLine($"pStrArray{i}[i] = (byte*)Marshal.StringToHGlobalAnsi({name}[i]);");
                }
            }
            if (type.StringType == CsStringType.StringUTF16)
            {
                writer.WriteLine($"char** pAStr{i} = (char**)Marshal.AllocHGlobal(sizeof(nuint) * {name}.Length);");
                using (writer.PushBlock($"for (int i = 0; i < {name}.Length; i++)"))
                {
                    writer.WriteLine($"pAStr{i}[i] = (char*)Marshal.StringToHGlobalUni({name}[i]);");
                }
            }
        }

        private static void WriteFreeUnmanagedStringArray(ICodeWriter writer, string name, int i)
        {
            using (writer.PushBlock($"for (int i = 0; i < {name}.Length; i++)"))
            {
                writer.WriteLine($"Utils.Free(pStrArray{i}[i]);");
            }
            using (writer.PushBlock($"if (pStrArraySize{i} >= Utils.MaxStackallocSize)"))
            {
                writer.WriteLine($"Utils.Free(pStrArray{i});");
            }
        }

        private static string GetParameterSignature(IList<CppParameter> parameters, bool canUseOut)
        {
            StringBuilder argumentBuilder = new();
            int index = 0;

            for (int i = 0; i < parameters.Count; i++)
            {
                CppParameter cppParameter = parameters[i];
                var paramCsTypeName = GetCsTypeName(cppParameter.Type, false);
                var paramCsName = GetParameterName(cppParameter.Type, cppParameter.Name);

                if (paramCsTypeName == "bool")
                    paramCsTypeName = "byte";

                if (canUseOut && CanBeUsedAsOutput(cppParameter.Type, out CppTypeDeclaration? cppTypeDeclaration))
                {
                    argumentBuilder.Append("out ");
                    paramCsTypeName = GetCsTypeName(cppTypeDeclaration, false);
                }

                argumentBuilder.Append(paramCsTypeName).Append(' ').Append(paramCsName);

                if (index < parameters.Count - 1)
                {
                    argumentBuilder.Append(", ");
                }

                index++;
            }

            return argumentBuilder.ToString();
        }

        private static string GetNamelessParameterSignature(IList<CppParameter> parameters, bool canUseOut)
        {
            var argumentBuilder = new StringBuilder();
            int index = 0;

            foreach (CppParameter cppParameter in parameters)
            {
                string direction = string.Empty;
                var paramCsTypeName = GetCsTypeName(cppParameter.Type, false);

                if (canUseOut && CanBeUsedAsOutput(cppParameter.Type, out CppTypeDeclaration? cppTypeDeclaration))
                {
                    argumentBuilder.Append("out ");
                    paramCsTypeName = GetCsTypeName(cppTypeDeclaration, false);
                }

                argumentBuilder.Append(paramCsTypeName);
                if (index < parameters.Count - 1)
                {
                    argumentBuilder.Append(", ");
                }

                index++;
            }

            return argumentBuilder.ToString();
        }

        private static void GenerateVariations(IList<CppParameter> parameters, CsFunctionOverload function, bool isMember)
        {
            CsCodeGeneratorSettings.Default.TryGetFunctionMapping(function.ExportedName, out var mapping);

            for (long ix = 0; ix < Math.Pow(2, parameters.Count); ix++)
            {
                {
                    CsParameterInfo[] refParameterList = new CsParameterInfo[parameters.Count];
                    CsParameterInfo[] stringParameterList = new CsParameterInfo[parameters.Count];
                    CsParameterInfo[][] customParameterList = new CsParameterInfo[mapping?.CustomVariations.Count ?? 0][];
                    for (int i = 0; i < (mapping?.CustomVariations.Count ?? 0); i++)
                    {
                        customParameterList[i] = new CsParameterInfo[parameters.Count];
                    }
                    for (int j = 0; j < parameters.Count; j++)
                    {
                        var bit = (ix & (1 << j - 64)) != 0;
                        CppParameter cppParameter = parameters[j];
                        CppPrimitiveKind kind = GetPrimitiveKind(cppParameter.Type, false);

                        var paramCsName = GetParameterName(cppParameter.Type, cppParameter.Name);
                        var direction = GetDirection(cppParameter.Type);

                        if (bit)
                        {
                            if (cppParameter.Type is CppArrayType arrayType)
                            {
                                if (arrayType.Size > 0)
                                {
                                    refParameterList[j] = new(paramCsName, new("ref " + GetCsTypeName(arrayType.ElementType, false), kind), direction);
                                }
                                else
                                {
                                    refParameterList[j] = new(paramCsName, new(GetCsWrapperTypeName(cppParameter.Type, false), kind), direction);
                                }

                                if (IsString(arrayType.ElementType))
                                {
                                    stringParameterList[j] = new(paramCsName, new("string[]", kind), direction);
                                }
                                else
                                {
                                    stringParameterList[j] = new(paramCsName, new(GetCsWrapperTypeName(cppParameter.Type, false), kind), direction);
                                }
                            }
                            else
                            {
                                refParameterList[j] = new(paramCsName, new(GetCsWrapperTypeName(cppParameter.Type, false), kind), direction);

                                if (IsString(cppParameter.Type))
                                {
                                    stringParameterList[j] = new(paramCsName, new(direction == Direction.InOut ? "ref string" : "string", kind), direction);
                                }
                                else
                                {
                                    stringParameterList[j] = new(paramCsName, new(GetCsWrapperTypeName(cppParameter.Type, false), kind), direction);
                                }
                            }

                            if (mapping != null)
                            {
                                for (int i = 0; i < mapping.CustomVariations.Count; i++)
                                {
                                    if (mapping.CustomVariations[i].TryGetValue(paramCsName, out var paramType))
                                    {
                                        customParameterList[i][j] = new(paramCsName, new(paramType, kind), direction);
                                    }
                                    else
                                    {
                                        customParameterList[i][j] = new(paramCsName, new(GetCsWrapperTypeName(cppParameter.Type, false), kind), direction);
                                    }
                                }
                            }
                        }
                        else
                        {
                            refParameterList[j] = new(paramCsName, new(GetCsTypeName(cppParameter.Type, false), kind), direction);
                            stringParameterList[j] = new(paramCsName, new(GetCsTypeName(cppParameter.Type, false), kind), direction);
                            if (mapping != null)
                            {
                                for (int i = 0; i < mapping.CustomVariations.Count; i++)
                                {
                                    customParameterList[i][j] = new(paramCsName, new(GetCsTypeName(cppParameter.Type, false), kind), direction);
                                }
                            }
                        }
                    }

                    CsFunctionVariation refVariation = function.CreateVariationWith();
                    refVariation.Parameters.AddRange(refParameterList);
                    CsFunctionVariation stringVariation = function.CreateVariationWith();
                    stringVariation.Parameters.AddRange(stringParameterList);

                    if (!function.HasVariation(refVariation))
                    {
                        function.Variations.Add(refVariation);
                        GenerateDefaultValueVariations(parameters, function, refVariation, isMember);
                        GenerateReturnVariations(function, refVariation, isMember);
                    }
                    if (!function.HasVariation(stringVariation))
                    {
                        function.Variations.Add(stringVariation);
                        GenerateDefaultValueVariations(parameters, function, stringVariation, isMember);
                        GenerateReturnVariations(function, stringVariation, isMember);
                    }
                    for (int i = 0; i < (mapping?.CustomVariations.Count ?? 0); i++)
                    {
                        CsFunctionVariation customVariation = function.CreateVariationWith();
                        customVariation.Parameters.AddRange(customParameterList[i]);
                        if (!function.HasVariation(customVariation))
                        {
                            function.Variations.Add(customVariation);
                            GenerateDefaultValueVariations(parameters, function, customVariation, isMember);
                            GenerateReturnVariations(function, customVariation, isMember);
                        }
                    }
                }
            }
        }

        private static unsafe void GenerateDefaultValueVariations(IList<CppParameter> parameters, CsFunctionOverload function, CsFunctionVariation variation, bool isMember)
        {
            if (function.DefaultValues.Count == 0)
                return;

            for (int i = variation.Parameters.Count - 1; i >= 0; i--)
            {
                var param = variation.Parameters[i];

                if (!function.DefaultValues.TryGetValue(param.Name, out _))
                {
                    continue;
                }

                CsFunctionVariation defaultVariation = new(variation.ExportedName, variation.Name, variation.StructName, variation.IsMember, variation.IsConstructor, variation.IsDestructor, variation.ReturnType);
                for (int j = 0; j < variation.Parameters.Count; j++)
                {
                    var iationParameter = variation.Parameters[j];
                    if (param != iationParameter)
                        defaultVariation.Parameters.Add(iationParameter);
                }

                if (function.HasVariation(defaultVariation))

                    continue;

                function.Variations.Add(defaultVariation);

                GenerateDefaultValueVariations(parameters, function, defaultVariation, isMember);
                GenerateReturnVariations(function, defaultVariation, isMember);
            }
        }

        private static void GenerateReturnVariations(CsFunctionOverload function, CsFunctionVariation variation, bool isMember)
        {
            if (!isMember && variation.ReturnType.IsVoid && !variation.ReturnType.IsPointer && variation.Parameters.Count > 0)
            {
                if (variation.Parameters[0].Name == "output" && variation.Parameters[0].Type.IsPointer)
                {
                    CsFunctionVariation returnVariation = new(variation.ExportedName, variation.Name, variation.StructName, variation.IsMember, variation.IsConstructor, variation.IsDestructor, variation.ReturnType);
                    returnVariation.Parameters = variation.Parameters.Skip(1).ToList();
                    if (!function.HasVariation(returnVariation))
                    {
                        function.Variations.Add(returnVariation);
                        function.Variations.Remove(variation);
                        returnVariation.ReturnType = new(variation.Parameters[0].Type.Name[..^1], variation.Parameters[0].Type.PrimitiveType);
                    }
                }
            }
            if (variation.ReturnType.IsPointer && variation.ReturnType.PrimitiveType == CsPrimitiveType.Byte || variation.ReturnType.PrimitiveType == CsPrimitiveType.Char)
            {
                CsFunctionVariation returnVariation = new(variation.ExportedName, variation.Name + "S", variation.StructName, variation.IsMember, variation.IsConstructor, variation.IsDestructor, variation.ReturnType);
                returnVariation.Parameters = variation.Parameters.ToList();
                function.Variations.Add(returnVariation);
                returnVariation.ReturnType = new("string", variation.ReturnType.PrimitiveType);
            }
        }

        private static string GetParameterName(CppType type, string name)
        {
            if (name == "out")
            {
                return "output";
            }
            if (name == "ref")
            {
                return "reference";
            }
            if (name == "in")
            {
                return "input";
            }
            if (name == "base")
            {
                return "baseValue";
            }
            if (name == "void")
            {
                return "voidValue";
            }
            if (name == "int")
            {
                return "intValue";
            }
            if (CsCodeGeneratorSettings.Default.Keywords.Contains(name))
            {
                return "@" + name;
            }

            if (name.StartsWith('p') && name.Length > 1 && char.IsUpper(name[1]))
            {
                name = char.ToLower(name[1]) + name[2..];
                return GetParameterName(type, name);
            }

            if (name == string.Empty)
            {
                switch (type.TypeKind)
                {
                    case CppTypeKind.Primitive:
                        return GetParameterName(type, (type as CppPrimitiveType).GetDisplayName());

                    case CppTypeKind.Pointer:
                        return GetParameterName((type as CppPointerType).ElementType, (type as CppPointerType).ElementType.GetDisplayName());

                    case CppTypeKind.Reference:
                        break;

                    case CppTypeKind.Array:
                        break;

                    case CppTypeKind.Qualified:
                        return (type as CppQualifiedType).ElementType.GetDisplayName();

                    case CppTypeKind.Function:
                        break;

                    case CppTypeKind.Typedef:
                        return GetParameterName((type as CppTypedef).ElementType, name);

                    case CppTypeKind.StructOrClass:
                        break;

                    case CppTypeKind.Enum:
                        return (type as CppEnum).GetDisplayName();

                    case CppTypeKind.TemplateParameterType:
                        break;

                    case CppTypeKind.TemplateParameterNonType:
                        break;

                    case CppTypeKind.Unexposed:
                        break;
                }
            }

            return NormalizeParameterName(name);
        }

        public static bool TryGetDefaultValue(string functionName, CppParameter parameter, bool sanitize, out string? defaultValue)
        {
            if (CsCodeGeneratorSettings.Default.TryGetFunctionMapping(functionName, out var mapping))
            {
                if (mapping.Defaults.TryGetValue(parameter.Name, out var value))
                {
                    defaultValue = NormalizeValue(value, sanitize);
                    return true;
                }
            }
            defaultValue = null;
            return false;
        }

        public static string? GetDefaultValue(string functionName, CppParameter parameter, bool sanitize = true)
        {
            if (CsCodeGeneratorSettings.Default.TryGetFunctionMapping(functionName, out var mapping))
            {
                if (mapping.Defaults.TryGetValue(parameter.Name, out var value))
                    return NormalizeValue(value, sanitize);
            }

            return null;
        }

        public static string? NormalizeValue(string value, bool sanitize)
        {
            if (CsCodeGeneratorSettings.Default.KnownDefaultValueNames.TryGetValue(value, out var names))
            {
                return names;
            }

            if (value == "NULL")
                return "default";
            if (value == "FLT_MAX")
                return "float.MaxValue";
            if (value == "-FLT_MAX")
                return "-float.MaxValue";
            if (value == "FLT_MIN")
                return "float.MinValue";
            if (value == "-FLT_MIN")
                return "-float.MinValue";
            if (value == "nullptr")
                return "default";
            if (value == "false")
                return "0";
            if (value == "true")
                return "1";

            if (value.StartsWith("ImVec") && sanitize)
                return null;
            if (value.StartsWith("ImVec2"))
            {
                value = value[7..][..(value.Length - 8)];
                var parts = value.Split(',');
                return $"new Vector2({NormalizeValue(parts[0], sanitize)},{NormalizeValue(parts[1], sanitize)})";
            }
            if (value.StartsWith("ImVec4"))
            {
                value = value[7..][..(value.Length - 8)];
                var parts = value.Split(',');
                return $"new Vector4({NormalizeValue(parts[0], sanitize)},{NormalizeValue(parts[1], sanitize)},{NormalizeValue(parts[2], sanitize)},{NormalizeValue(parts[3], sanitize)})";
            }
            return value;
        }

        private static string NormalizeParameterName(string name)
        {
            var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new();
            for (int i = 0; i < parts.Length; i++)
            {
                if (i == 0)
                {
                    sb.Append(char.ToLower(parts[i][0]));
                    sb.Append(parts[i][1..]);
                }
                else
                {
                    sb.Append(char.ToUpper(parts[i][0]));
                    sb.Append(parts[i][1..]);
                }
            }
            name = sb.ToString();
            if (CsCodeGeneratorSettings.Default.Keywords.Contains(name))
            {
                return "@" + name;
            }

            return name;
        }

        private static bool CanBeUsedAsOutput(CppType type, out CppTypeDeclaration? elementTypeDeclaration)
        {
            if (type is CppPointerType pointerType)
            {
                if (pointerType.ElementType is CppTypedef typedef)
                {
                    elementTypeDeclaration = typedef;
                    return true;
                }
                else if (pointerType.ElementType is CppClass @class
                    && @class.ClassKind != CppClassKind.Class
                    && @class.SizeOf > 0)
                {
                    elementTypeDeclaration = @class;
                    return true;
                }
                else if (pointerType.ElementType is CppEnum @enum
                    && @enum.SizeOf > 0)
                {
                    elementTypeDeclaration = @enum;
                    return true;
                }
            }

            elementTypeDeclaration = null;
            return false;
        }

        public static string GetPrettyCommandName(string function)
        {
            if (CsCodeGeneratorSettings.Default.TryGetFunctionMapping(function, out var mapping))
            {
                return mapping.FriendlyName;
            }

            string[] parts = GetCsCleanName(function).SplitByCase();

            StringBuilder sb = new();
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                if (CsCodeGeneratorSettings.Default.IgnoredParts.Contains(part))
                {
                    continue;
                }

                sb.Append(part);
            }

            return sb.ToString();
        }

        public static CallingConvention GetCallingConvention(CppCallingConvention convention)
        {
            return convention switch
            {
                CppCallingConvention.C => CallingConvention.Cdecl,
                CppCallingConvention.Win64 => CallingConvention.Winapi,
                CppCallingConvention.X86FastCall => CallingConvention.FastCall,
                CppCallingConvention.X86StdCall => CallingConvention.StdCall,
                CppCallingConvention.X86ThisCall => CallingConvention.ThisCall,
                _ => throw new NotSupportedException(),
            };
        }

        public static string GetCallingConventionDelegate(CppCallingConvention convention)
        {
            return convention switch
            {
                CppCallingConvention.C => "Cdecl",
                CppCallingConvention.X86FastCall => "Fastcall",
                CppCallingConvention.X86StdCall => "Stdcall",
                CppCallingConvention.X86ThisCall => "Thiscall",
                _ => throw new NotSupportedException(),
            };
        }

        public static string GetCallingConventionLibrary(CppCallingConvention convention)
        {
            return convention switch
            {
                CppCallingConvention.C => "System.Runtime.CompilerServices.CallConvCdecl",
                CppCallingConvention.X86FastCall => " System.Runtime.CompilerServices.CallConvFastcall",
                CppCallingConvention.X86StdCall => "System.Runtime.CompilerServices.CallConvStdcall",
                CppCallingConvention.X86ThisCall => "System.Runtime.CompilerServices.CallConvThiscall",
                _ => throw new NotSupportedException(),
            };
        }
    }
}