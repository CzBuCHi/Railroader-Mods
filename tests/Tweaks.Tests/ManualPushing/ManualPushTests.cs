using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection;
using System;
using HarmonyLib;
using Tweaks.Tests.Infrastructure;
using Tweaks.Tests;
using Settings = global::Tweaks.Configuration.Settings;

namespace Tweaks.Tests.ManualPushing
{
    internal static class ManualPushTests
    {
        public static void AddVelocityToCar() { }
        public static void Other() { }

        internal static void Run(string managed, string modPath)
        {
            var mod = CompiledMod.Load(managed, modPath);
            var patch = mod.GetType("Tweaks.ManualPushing.FastStrongManPatch", true)!;
            var transpiler = patch.GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static)!;
            var nativeLimit = (float)patch.GetField("NativeLimit", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;
            var newLimit = (float)patch.GetField("NewLimit", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;
            IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions) =>
                CompiledMod.Invoke<IEnumerable<CodeInstruction>>(transpiler, instructions);
            var settings = mod.GetType("Tweaks.Configuration.Settings", true)!.GetProperty("Current", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            var speed = settings.GetType().GetProperty("ManualPushSpeedMph")!;
            using var stream = File.OpenRead(Path.Combine(managed, "Railroader.dll"));
            using var pe = new PEReader(stream);
            var metadata = pe.GetMetadataReader();
            var type = metadata.TypeDefinitions.Select(handle => metadata.GetTypeDefinition(handle))
                .Single(item => metadata.GetString(item.Name) == "TrainController");
            var method = type.GetMethods().Select(handle => metadata.GetMethodDefinition(handle))
                .Single(item => metadata.GetString(item.Name) == "HandleManualMoveCar");
            var bytes = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
            var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null)!)
                .ToDictionary(opcode => unchecked((ushort)opcode.Value));
            var code = new List<CodeInstruction>();
            for (var i = 0; i < bytes.Length;)
            {
                var value = bytes[i++];
                var opcode = opcodes[value == 0xfe ? (ushort)(0xfe00 | bytes[i++]) : value];
                object? operand = null;
                var size = opcode.OperandType switch {
                    OperandType.InlineNone => 0,
                    OperandType.ShortInlineI or OperandType.ShortInlineVar or OperandType.ShortInlineBrTarget => 1,
                    OperandType.InlineVar => 2,
                    OperandType.InlineI8 or OperandType.InlineR => 8,
                    OperandType.InlineSwitch => 4 + BitConverter.ToInt32(bytes, i) * 4,
                    _ => 4
                };
                if (opcode.OperandType == OperandType.ShortInlineR) operand = BitConverter.ToSingle(bytes, i);
                if (opcode.OperandType == OperandType.InlineMethod)
                {
                    var handle = MetadataTokens.EntityHandle(BitConverter.ToInt32(bytes, i));
                    var name = handle.Kind switch {
                        HandleKind.MethodDefinition => metadata.GetString(metadata.GetMethodDefinition((MethodDefinitionHandle)handle).Name),
                        HandleKind.MemberReference => metadata.GetString(metadata.GetMemberReference((MemberReferenceHandle)handle).Name),
                        _ => "Other"
                    };
                    operand = typeof(ManualPushTests).GetMethod(name == "AddVelocityToCar" ? name : "Other");
                }
                code.Add(new CodeInstruction(opcode, operand));
                i += size;
            }
            var patched = Rewrite(code).ToList();
            Program.Check(patched.Count == code.Count, "Manual pushing retains every installed native instruction");
            var changes = Enumerable.Range(0, code.Count).Where(index => !Equals(code[index].operand, patched[index].operand)).ToArray();
            Program.Check(changes.Length == 1 && code[changes[0]].opcode == OpCodes.Ldc_R4 &&
                Equals(code[changes[0]].operand, nativeLimit) && Equals(patched[changes[0]].operand, newLimit),
                "Only the native 3-MPH limit changes to 6 MPH");
            Program.Check(code.Select(instruction => instruction.opcode).SequenceEqual(patched.Select(instruction => instruction.opcode)),
                "Native force/weight calculation, host check and method calls are unchanged");
            Program.Check(Math.Abs(newLimit / nativeLimit - 2f) < 0.00001f, "Manual-push speed cap doubles");
            speed.SetValue(settings, 9f);
            var custom = Rewrite(code).ToList();
            Program.Check(Math.Abs((float)custom[changes[0]].operand - newLimit * 1.5f) < 0.00001f,
                "Manual push transpiler uses configured MPH cap");
            speed.SetValue(settings, 6f);
            foreach (var invalid in new[] {
                new List<CodeInstruction> { new CodeInstruction(OpCodes.Ret) },
                code.Concat(code).ToList(),
                new List<CodeInstruction> { new CodeInstruction(OpCodes.Ldc_R4, nativeLimit), new CodeInstruction(OpCodes.Call, typeof(ManualPushTests).GetMethod("Other")) }
            })
            {
                var rejected = false;
                try { Rewrite(invalid).ToList(); } catch (NotSupportedException) { rejected = true; }
                Program.Check(rejected, "Missing, duplicate or unrelated speed-limit IL is rejected");
            }
        }
    }
}
