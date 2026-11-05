using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection;
using System;
using HarmonyLib;
using Tweaks.Tests.Infrastructure;
using Tweaks.Tests;

namespace Tweaks.Tests.Camera
{
    internal static class FollowAnchorTests
    {
        public static int Anchor() => 10;
        public static int Adjust(int value, object camera) => value + (int)camera;

        internal static void InspectNative(byte[] bytes, MetadataReader metadata)
        {
            var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
                .ToDictionary(op => unchecked((ushort)op.Value));
            var matches = 0;
            for (var i = 0; i < bytes.Length;)
            {
                var first = bytes[i++];
                var op = opcodes[first == 0xfe ? (ushort)(0xfe00 | bytes[i++]) : first];
                if (op.OperandType == OperandType.InlineMethod)
                {
                    var handle = MetadataTokens.EntityHandle(BitConverter.ToInt32(bytes, i));
                    if (handle.Kind == HandleKind.MethodDefinition &&
                        metadata.GetString(metadata.GetMethodDefinition((MethodDefinitionHandle)handle).Name) == "GetMoverTargetPositionRotation") matches++;
                }
                i += op.OperandType switch {
                    OperandType.InlineNone => 0,
                    OperandType.ShortInlineI or OperandType.ShortInlineVar or OperandType.ShortInlineBrTarget => 1,
                    OperandType.InlineVar => 2,
                    OperandType.InlineI8 or OperandType.InlineR => 8,
                    OperandType.InlineSwitch => 4 + BitConverter.ToInt32(bytes, i) * 4,
                    _ => 4
                };
            }
            Program.Check(matches == 1, "Installed follow-camera IL has exactly one pose anchor for the guarded rewrite");
        }

        internal static void Run(string managed, string modPath)
        {
            var mod = CompiledMod.Load(managed, modPath);
            var game = Assembly.LoadFrom(Path.Combine(managed, "Railroader.dll"));
            var anchor = game.GetType("Model.Car", true)!.GetMethod("GetMoverTargetPositionRotation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            var adjust = mod.GetType("Tweaks.Camera.MaintenanceCameraFocus", true)!.GetMethod("Adjust", BindingFlags.NonPublic | BindingFlags.Static)!;
            var transpiler = mod.GetType("Tweaks.Camera.MaintenanceCameraFocusPatch", true)!
                .GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static)!;
            IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions) =>
                CompiledMod.Invoke<IEnumerable<CodeInstruction>>(transpiler, instructions);
            var code = new[] { new CodeInstruction(OpCodes.Call, anchor), new CodeInstruction(OpCodes.Ldc_I4_5),
                new CodeInstruction(OpCodes.Add), new CodeInstruction(OpCodes.Ret) };
            var patched = Rewrite(code).ToArray();
            var method = new DynamicMethod("PatchedFollowAnchor", typeof(int), new[] { typeof(object) }, typeof(FollowAnchorTests).Module, true);
            var il = method.GetILGenerator();
            foreach (var instruction in patched)
                if (instruction.operand is MethodInfo call)
                    il.Emit(instruction.opcode, call == anchor ? typeof(FollowAnchorTests).GetMethod(nameof(Anchor))!
                        : call == adjust ? typeof(FollowAnchorTests).GetMethod(nameof(Adjust))! : call);
                else il.Emit(instruction.opcode);
            var run = (Func<object, int>)method.CreateDelegate(typeof(Func<object, int>));
            Program.Check(run(20) == 35 && run(0) == 15, "Compiled camera transpiler supplies camera context and preserves subsequent native offsets");
            Program.Check(patched.Length == code.Length + 2 && code.Length == 4, "Follow anchor rewrite only inserts the scoped adjustment");
            foreach (var invalid in new[] { code.Skip(1), code.Concat(code) })
            {
                var rejected = false;
                try { Rewrite(invalid).ToArray(); }
                catch (InvalidOperationException) { rejected = true; }
                Program.Check(rejected, "Missing or duplicate follow anchors are rejected");
            }
        }
    }
}
