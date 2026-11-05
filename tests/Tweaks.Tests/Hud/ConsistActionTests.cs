using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Tweaks.Tests.Infrastructure;

namespace Tweaks.Tests.Hud
{
    internal static class ConsistActionTests
    {
        internal static void Run(string managed, string modPath)
        {
            var mod = CompiledMod.Load(managed, modPath);
            var actions = mod.GetType("Tweaks.Hud.ConsistActions", true)!;
            foreach (var (click, repair, camera) in new[] {
                ("ShowAirProblem", "RepairAir", "FollowAir"),
                ("ShowAppliedHandbrake", "ReleaseHandbrakes", "ZoomToCar"),
                ("ShowOilWarning", "FillOil", "FollowOil") })
            {
                var method = actions.GetMethod(click, BindingFlags.NonPublic | BindingFlags.Instance)!;
                var code = ReadInstructions(method).ToList();
                Program.Check(code.Any(instruction => instruction.operand is MethodInfo call && call.Name == "get_IsShiftDown"),
                    click + " checks the native Shift modifier");
                Program.Check(code.Any(instruction => instruction.opcode == OpCodes.Ldftn && instruction.operand is MethodInfo call
                    && call.DeclaringType == actions && call.Name == repair), click + " selects the corresponding train repair");
                var repairIndex = code.FindIndex(instruction => instruction.operand is MethodInfo call && call.Name == "RepairConsist");
                var cameraIndex = code.FindIndex(instruction => instruction.operand is MethodInfo call && call.Name == camera);
                Program.Check(repairIndex >= 0 && cameraIndex > repairIndex
                    && code.Skip(repairIndex + 1).Take(cameraIndex - repairIndex - 1).Any(instruction => instruction.opcode == OpCodes.Ret),
                    click + " returns after Shift repair while retaining the normal camera path");
            }
            var authorize = actions.GetMethod("ApplyAuthorized", BindingFlags.NonPublic | BindingFlags.Static)!;
            var authorization = ReadInstructions(authorize).ToList();
            var check = authorization.FindIndex(instruction => instruction.operand is MethodInfo call && call.Name == "CheckAuthorizedToSendMessage");
            var apply = authorization.FindIndex(instruction => instruction.operand is MethodInfo call && call.Name == "ApplyLocal");
            Program.Check(check >= 0 && apply > check && authorization.Skip(check + 1).Take(apply - check - 1)
                .Any(instruction => instruction.opcode.FlowControl == FlowControl.Cond_Branch), "Maintenance requests are gated by native authorization before sending");
            var repairConsist = actions.GetMethod("RepairConsist", BindingFlags.NonPublic | BindingFlags.Instance)!;
            Program.Check(ReadInstructions(repairConsist).Any(instruction => instruction.operand is MethodInfo call
                && call.Name == "CurrentCars"), "Repairs resolve the current selected consist at click time");
        }

        private static IEnumerable<CodeInstruction> ReadInstructions(MethodInfo method)
        {
            var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null)!)
                .ToDictionary(opcode => unchecked((ushort)opcode.Value));
            var bytes = method.GetMethodBody()!.GetILAsByteArray()!;
            for (var index = 0; index < bytes.Length;)
            {
                var first = bytes[index++];
                var opcode = opcodes[first == 0xfe ? (ushort)(0xfe00 | bytes[index++]) : first];
                object? operand = null;
                if (opcode.OperandType == OperandType.InlineMethod)
                    operand = method.Module.ResolveMethod(BitConverter.ToInt32(bytes, index),
                        method.DeclaringType?.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
                var size = opcode.OperandType switch {
                    OperandType.InlineNone => 0,
                    OperandType.ShortInlineI or OperandType.ShortInlineVar or OperandType.ShortInlineBrTarget => 1,
                    OperandType.InlineVar => 2,
                    OperandType.InlineI8 or OperandType.InlineR => 8,
                    OperandType.InlineSwitch => 4 + BitConverter.ToInt32(bytes, index) * 4,
                    _ => 4
                };
                yield return new CodeInstruction(opcode, operand);
                index += size;
            }
        }
    }
}
