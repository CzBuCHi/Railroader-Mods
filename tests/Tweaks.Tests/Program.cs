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

var checks = 0;
void Check(bool value, string name) { if (!value) throw new Exception("FAIL: " + name); checks++; }
Check(NextServiceSelection.Find(100, Array.Empty<double>()) == null, "No interchange fallback");
Check(NextServiceSelection.Find(100, new[] { 500.0, 250.0, 900.0 }) == 250, "Earliest active service");
Check(NextServiceSelection.Find(86300, new[] { 86450.0, 172800.0 }) == 86450, "Midnight");
Check(NextServiceSelection.Find(100, new[] { 900.0, 150.0 }) == 150, "Extra service precedes daily service");
Check(NextServiceSelection.Find(100, new[] { 90.0, 900.0 }) == 101, "Overdue service is immediate");
Check(NextServiceSelection.Find(100, new[] { 100.0 }) == 101, "Exactly due service");
Check(NextServiceSelection.Find(100, new[] { double.NaN, double.PositiveInfinity }) == null, "Invalid services");
Check(NextServiceSelection.Find(100, new[] { 100.25 }) == 101, "Tiny interval keeps native time advancing");

Check(PrankTextList.Parse(new[] { "", "  " }).Length == 0, "Empty prank list disables text changes");
Check(PrankTextList.Parse(new[] { "\uFEFFPrvni", " Druhy ", "Prvni" }).SequenceEqual(new[] { "Prvni", "Druhy" }), "Prank list strips BOM, trims and removes duplicates");
Check(PrankTextList.Parse(new[] { "Opravář říká: jede to!" }).Single() == "Opravář říká: jede to!", "Prank list keeps Unicode text");
Check(PrankTextList.Parse(new[] { new string('x', 120), new string('x', 100) }).Single().Length == 100, "Native lettering limit and deduplication after truncation");
Check(PrankTextList.Parse(new[] { @"FIRST\nSECOND", "THIRD" }).SequenceEqual(new[] { "FIRST\nSECOND", "THIRD" }), "Literal newline escape makes one multiline sign; physical lines stay separate choices");
Check(PrankTextList.Parse(new[] { @"FIRST\nSECOND", "FIRST\nSECOND" }).Length == 1, "Deduplication applies to decoded lettering");
var repairStop = new RepairStopState(null);
Check(!repairStop.Observe(false, "A") && repairStop.LastTrack == null, "Passing a repair track does not trigger");
Check(repairStop.Observe(true, "A") && repairStop.LastTrack == "A", "First stopped repair visit triggers");
Check(!repairStop.Observe(true, "A"), "Repeated stopped frames do not trigger");
Check(!repairStop.Observe(false, null) && repairStop.LastTrack == "A", "Leaving without stopping retains latch");
Check(!repairStop.Observe(false, "B") && !repairStop.Observe(true, "A"), "Passing another repair track does not rearm original");
Check(repairStop.Observe(true, "B") && repairStop.LastTrack == "B", "Stopping at another repair track triggers");
Check(!new RepairStopState(repairStop.LastTrack).Observe(true, "B"), "Saved locomotive flag prevents retrigger after reload");
Check(!repairStop.Observe(true, null) && repairStop.LastTrack == null, "Stopping outside clears latch");
Check(repairStop.Observe(true, "B"), "Return after outside stop triggers again");
Check(ShopWorkTiming.Duration == 300f, "Installation lasts five game minutes");
Check(ShopWorkTiming.Remaining(300, 100, 399, true) == 1, "Installation does not finish early");
Check(ShopWorkTiming.Remaining(300, 100, 400, true) == 0, "Installation finishes at five game minutes");
Check(ShopWorkTiming.Remaining(300, 100, 600, true) == 0, "Accelerated game time completes installation without a negative timer");
Check(ShopWorkTiming.Remaining(300, 100, 400, false) == 300, "Moving pauses installation");
Check(ShopWorkTiming.Remaining(250, 100, 100, true) == 250, "Paused game clock preserves timer");
Check(ShopWorkTiming.Remaining(250, 100, 90, true) == 250, "Backward game clock does not add time");
Check(ShopWorkTiming.Remaining(125, 200, 225, true) == 100, "Persisted remaining time resumes correctly");
var path = args.Single();
using var stream = File.OpenRead(path);
using var pe = new PEReader(stream);
var metadata = pe.GetMetadataReader();
var typeHandle = metadata.TypeDefinitions.Single(handle => metadata.GetString(metadata.GetTypeDefinition(handle).Name) == "TrainController");
var methodHandle = metadata.GetTypeDefinition(typeHandle).GetMethods().Single(handle => metadata.GetString(metadata.GetMethodDefinition(handle).Name) == "HandleManualMoveCar");
var method = metadata.GetMethodDefinition(methodHandle);
var bytes = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
var opcodes = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public)
    .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null)!)
    .ToDictionary(opcode => unchecked((ushort)opcode.Value));
var instructions = new List<CodeInstruction>();
for (var i = 0; i < bytes.Length;)
{
    ushort key = bytes[i++];
    if (key == 0xfe) key = (ushort)(0xfe00 | bytes[i++]);
    var opcode = opcodes[key];
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
        operand = typeof(NativeCall).GetMethod(name == "AddVelocityToCar" ? name : "Other");
    }
    instructions.Add(new CodeInstruction(opcode, operand));
    i += size;
}
var transpiler = typeof(FastStrongManPatch).GetMethod("Transpiler", BindingFlags.Static | BindingFlags.NonPublic)!;
var patched = ((IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { instructions })!).ToList();
Check(patched.Count == instructions.Count + 1, "Actual installed native IL receives one insertion");
Check(patched.Count(instruction => instruction.operand is MethodInfo call && call.Name == "SpeedLimit") == 1, "One speed limit hook");
var speed = typeof(FastStrongManPatch).GetMethod("SpeedLimit", BindingFlags.Static | BindingFlags.NonPublic)!;
TweaksPlugin.Settings.FastStrongMan = true;
Check(Math.Abs((float)speed.Invoke(null, new object[] { 1.3411179f })! - 2.682236f) < 0.000001, "Enabled limit is 6 mph");
TweaksPlugin.Settings.FastStrongMan = false;
Check((float)speed.Invoke(null, new object[] { 1.3411179f })! == 1.3411179f, "Disabled retains exact native limit");
try
{
    transpiler.Invoke(null, new object[] { new[] { new CodeInstruction(OpCodes.Ret) } });
    throw new Exception("FAIL: Changed native IL must be rejected");
}
catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { checks++; }
Console.WriteLine($"PASS: {checks} checks: interchange timing, installed native push IL, enabled/disabled 6 mph limit, changed-IL rejection, and repair-stop rearming including saved flags, editable prank text lists, and five-minute shop installation timing.");
