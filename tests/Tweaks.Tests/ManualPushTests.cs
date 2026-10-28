using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using HarmonyLib;
using Tweaks;

internal static class ManualPushTests
{
    public static void AddVelocityToCar() { }
    public static void Other() { }

    internal static void Run(string managed)
    {
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
        var patched = ManualPushSpeed.Apply(code).ToList();
        Program.Check(patched.Count == code.Count, "Manual pushing retains every installed native instruction");
        var changes = Enumerable.Range(0, code.Count).Where(index => !Equals(code[index].operand, patched[index].operand)).ToArray();
        Program.Check(changes.Length == 1 && code[changes[0]].opcode == OpCodes.Ldc_R4 &&
            Equals(code[changes[0]].operand, ManualPushSpeed.NativeLimit) && Equals(patched[changes[0]].operand, ManualPushSpeed.NewLimit),
            "Only the native 3-MPH limit changes to 6 MPH");
        Program.Check(code.Select(instruction => instruction.opcode).SequenceEqual(patched.Select(instruction => instruction.opcode)),
            "Native force/weight calculation, host check and method calls are unchanged");
        Program.Check(Math.Abs(ManualPushSpeed.NewLimit / ManualPushSpeed.NativeLimit - 2f) < 0.00001f, "Manual-push speed cap doubles");
        Settings.Current.ManualPushSpeedMph = 9f;
        var custom = ManualPushSpeed.Apply(code).ToList();
        Program.Check(Math.Abs((float)custom[changes[0]].operand - ManualPushSpeed.NewLimit * 1.5f) < 0.00001f,
            "Manual push transpiler uses configured MPH cap");
        Settings.Current = new Settings();
        foreach (var invalid in new[] {
            new List<CodeInstruction> { new CodeInstruction(OpCodes.Ret) },
            code.Concat(code).ToList(),
            new List<CodeInstruction> { new CodeInstruction(OpCodes.Ldc_R4, ManualPushSpeed.NativeLimit), new CodeInstruction(OpCodes.Call, typeof(ManualPushTests).GetMethod("Other")) }
        })
        {
            var rejected = false;
            try { ManualPushSpeed.Apply(invalid).ToList(); } catch (NotSupportedException) { rejected = true; }
            Program.Check(rejected, "Missing, duplicate or unrelated speed-limit IL is rejected");
        }
    }
}
