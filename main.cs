/*▄▄▄    ███▄ ▄███▓  ▄████  ▄▄▄██▀▀▀▓█████▄▄▄█████▓
▓█████▄ ▓██▒▀█▀ ██▒ ██▒ ▀█▒   ▒██   ▓█   ▀▓  ██▒ ▓▒
▒██▒ ▄██▓██    ▓██░▒██░▄▄▄░   ░██   ▒███  ▒ ▓██░ ▒░
▒██░█▀  ▒██    ▒██ ░▓█  ██▓▓██▄██▓  ▒▓█  ▄░ ▓██▓ ░ 
░▓█  ▀█▓▒██▒   ░██▒░▒▓███▀▒ ▓███▒   ░▒████▒ ▒██▒ ░ 
░▒▓███▀▒░ ▒░   ░  ░ ░▒   ▒  ▒▓▒▒░   ░░ ▒░ ░ ▒ ░░   
▒░▒   ░ ░  ░      ░  ░   ░  ▒ ░▒░    ░ ░  ░   ░    
 ░    ░ ░      ░   ░ ░   ░  ░ ░ ░      ░    ░      
 ░             ░         ░  ░   ░      ░  ░*/
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using HarmonyLib;
using Unity.Mathematics;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using Unity.Collections;
using System.Security.Cryptography;
using System.Xml.Serialization;
using ProtoBuf;
using System.Drawing;
using Facepunch.Utility;
using System.Drawing.Imaging;
using Color = System.Drawing.Color;
using System.Runtime.InteropServices;
using System.Collections;
using Unity.Jobs;
using Unity.Burst;
using Newtonsoft.Json;

namespace MapGenny
{
    namespace MapGenny
    {
        [HarmonyPatch(typeof(WorldConfig), nameof(WorldConfig.LoadScriptableConfigs))]
        public static class WorldConfig_LoadScriptableConfigs
        {
            [HarmonyPostfix]
            private static void Postfix(WorldConfig __instance)
            {
                __instance.AboveGroundRails = Library.IsSwitchEnabled("wc.aboverails", true);
                __instance.BelowGroundRails = Library.IsSwitchEnabled("wc.belowrails", true);
                __instance.PercentageTier0 = float.Parse(Library.GetSwitch("wc.tier0", "0.3"));
                __instance.PercentageTier1 = float.Parse(Library.GetSwitch("wc.tier1", "0.3"));
                __instance.PercentageTier2 = float.Parse(Library.GetSwitch("wc.tier2", "0.3"));
                __instance.PercentageBiomeArctic = float.Parse(Library.GetSwitch("wc.biome.arctic", "0.3"));
                __instance.PercentageBiomeArid = float.Parse(Library.GetSwitch("wc.biome.arid", "0.4"));
                __instance.PercentageBiomeJungle = float.Parse(Library.GetSwitch("wc.biome.jungle", "0.5"));
                __instance.PercentageBiomeTemperate = float.Parse(Library.GetSwitch("wc.biome.temperate", "0.15"));
                __instance.PercentageBiomeTundra = float.Parse(Library.GetSwitch("wc.biome.tundra", "0.15"));
                __instance.MainRoads = Library.IsSwitchEnabled("wc.mainroads", true);
                __instance.SideRoads = Library.IsSwitchEnabled("wc.sideroads", true);
                __instance.Trails = Library.IsSwitchEnabled("wc.trails", true);
                __instance.Rivers = Library.IsSwitchEnabled("wc.rivers", true);
                __instance.Powerlines = Library.IsSwitchEnabled("wc.powerlines", true);
                __instance.UnderwaterLabs = Library.IsSwitchEnabled("wc.underwaterlabs", true);
                string bl = Library.GetSwitch("wc.prefabblacklist", "");
                if (!string.IsNullOrEmpty(bl) && bl.Contains(","))
                {
                    string[] blacklist = bl.Split(',');
                    if (blacklist.Length > 0) { __instance.PrefabBlacklist.AddRange(blacklist); }
                }
                else if (!string.IsNullOrEmpty(bl)) { __instance.PrefabBlacklist.Add(bl); }
                string wl = Library.GetSwitch("wc.prefabwhitelist", "");
                if (!string.IsNullOrEmpty(wl) && wl.Contains(","))
                {
                    string[] whitelist = wl.Split(',');
                    if (whitelist.Length > 0) { __instance.PrefabWhitelist.AddRange(whitelist); }
                }
                else if (!string.IsNullOrEmpty(wl)) { __instance.PrefabWhitelist.Add(wl); }
            }
        }

        //PhysicsScene Overflow fix on small maps
        [HarmonyPatch(typeof(EnvironmentVolumeCheckEx), "ApplyEnvironmentVolumeChecks")]
        public static class EnvironmentVolumeCheckEx_Patch
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
                LocalBuilder visitedLocal = generator.DeclareLocal(typeof(HashSet<EnvironmentVolumeCheck>));
                List<CodeInstruction> initVisited = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Newobj, typeof(HashSet<EnvironmentVolumeCheck>).GetConstructor(Type.EmptyTypes)),
                    new CodeInstruction(OpCodes.Stloc, visitedLocal)
                };
                codes.InsertRange(0, initVisited);
                for (int i = 0; i < codes.Count; i++)
                {
                    CodeInstruction ci = codes[i];
                    MethodInfo checkMethod = typeof(EnvironmentVolumeCheck).GetMethod("Check");
                    if (ci.opcode == OpCodes.Callvirt && ci.operand as MethodInfo == checkMethod)
                    {
                        Label skipLabel = generator.DefineLabel();
                        List<CodeInstruction> insert = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldloc, visitedLocal),   // load visited
                            new CodeInstruction(OpCodes.Ldloc_1),              // load envCheck (verify IL)
                            new CodeInstruction(OpCodes.Callvirt, typeof(HashSet<EnvironmentVolumeCheck>).GetMethod("Add")),
                            new CodeInstruction(OpCodes.Brtrue_S, ci.labels.Count > 0 ? ci.labels[0] : skipLabel) // if Add=true, continue
                        };
                        ci.labels.Clear();
                        ci.labels.Add(skipLabel);

                        codes.InsertRange(i, insert);
                        break;
                    }
                }
                return codes;
            }
        }

        //Block rcon
        [HarmonyPatch(typeof(Facepunch.RCon), nameof(Facepunch.RCon.Initialize))]
        public static class RCon_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix() { return false; }//Just Block It            
        }

        //Block nexus
        [HarmonyPatch(typeof(Bootstrap), nameof(Bootstrap.StartNexusServer))]
        public static class Nexus_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ref IEnumerator __result)
            {
                // Create any folders or setup you need
                Directory.CreateDirectory(global::ConVar.Server.rootFolder);
                __result = DummyRoutine();
                // Skip the original
                return false;
            }

            private static IEnumerator DummyRoutine() { yield break; }
        }

        //Log all generation timers
        [HarmonyPatch(typeof(Timing), "Start")]
        public static class Timing_Start_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(string name)
            {
                try
                {
                    if (name == "Processing World") { return; }
                    Console.WriteLine($"[Running {name}]");
                }
                catch { }

            }
        }
        //[Running Processing World]
        //Log all generation timers
        [HarmonyPatch(typeof(Timing), "End")]
        public static class Timing_End_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Timing __instance, Stopwatch ___sw, string ___name)
            {
                try
                {
                    try
                    {
                        var log = Library.log;
                        if (log != null && log._buffer.Length > 0)
                        {
                            string target = $"[Running {___name}]";
                            int index = log._buffer.ToString().IndexOf(target, StringComparison.Ordinal);
                            if (index >= 0)
                            {
                                int lineStart = index;
                                int lineEnd = log._buffer.ToString().IndexOf('\n', index);
                                if (lineEnd == -1) { lineEnd = log._buffer.Length - 1; }
                                int lengthToRemove = (lineEnd - lineStart) + 1;
                                log._buffer.Remove(lineStart, lengthToRemove);
                            }
                        }
                    }
                    catch { }
                    Console.WriteLine("[" + ___sw.Elapsed.TotalSeconds.ToString("0.0") + $"s] {___name}");
                    Facepunch.Rust.PerformanceLogging server = Facepunch.Rust.PerformanceLogging.server;
                    if (server == null) { return false; }
                    server.SetTiming(___name, ___sw.Elapsed);
                }
                catch { }
                return false;
            }
        }

        //Underground Rail
        [HarmonyPatch(typeof(GenerateDungeonGrid), "Process")]
        public static class Patch_RailGenerateDungeonGrid_Process
        {
            public static class DungeonDepthSettings
            {
                private static float _downwardStep = 3f;

                public static float DownwardStep
                {
                    get => _downwardStep;
                    set { _downwardStep = Mathf.Round(value / 3f) * 3f; }
                }
            }

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var code in instructions)
                {
                    if (code.opcode == OpCodes.Ldc_R4 && (float)code.operand == 3f)
                    {
                        // Instead of hardcoded 3f, load our property getter via call
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(DungeonDepthSettings), nameof(DungeonDepthSettings.DownwardStep)));
                        continue;
                    }

                    yield return code;
                }
            }
        }

        [HarmonyPatch(typeof(GenerateDungeonBase))]
        [HarmonyPatch("Process", typeof(uint))]
        public static class GenerateDungeonBase_Process_Patch
        {
            public static class DungeonLabSettings
            {
                public static int IterationCount = 25;
                public static int MinSegmentCount = 5;
                public static int LargeSegmentCount = 25;
                public static int StartBudget = 3;
                public static int StartFloors = 2;
                public static int MidBudget = 4;
                public static int EndBudget = 5;
                public static int LabCount = 0;
                public static float edgeBuffer = 400f;
                public static float minDepth = 20f;
                public static List<Vector3> LabPos = new List<Vector3>();
                public static uint[] IDs = new uint[] { 2859012016, 693730846, 1462376378, 2357885450 };
            }

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    var code = codes[i];
                    // keep your tuning replacements as before
                    if (code.opcode == OpCodes.Ldc_I4_S && (sbyte)code.operand == 25)
                        codes[i] = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(DungeonLabSettings), nameof(DungeonLabSettings.IterationCount)));
                    else if (code.opcode == OpCodes.Ldc_I4_5)
                        codes[i] = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(DungeonLabSettings), nameof(DungeonLabSettings.MinSegmentCount)));
                    else if (code.opcode == OpCodes.Ldc_I4_S && (sbyte)code.operand == 25)
                        codes[i] = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(DungeonLabSettings), nameof(DungeonLabSettings.LargeSegmentCount)));
                    else if (code.opcode == OpCodes.Ldc_I4_3 && NextCalls(codes, i, "PlaceSegments"))
                        codes[i] = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(DungeonLabSettings), nameof(DungeonLabSettings.StartBudget)));
                    else if (code.opcode == OpCodes.Ldc_I4_4 && NextCalls(codes, i, "PlaceSegments"))
                        codes[i] = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(DungeonLabSettings), nameof(DungeonLabSettings.MidBudget)));
                    else if (code.opcode == OpCodes.Ldc_I4_5 && NextCalls(codes, i, "PlaceSegments"))
                        codes[i] = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(DungeonLabSettings), nameof(DungeonLabSettings.EndBudget)));
                    else if (code.opcode == OpCodes.Ldc_I4_2 && NextCalls(codes, i, "PlaceSegments"))
                        codes[i] = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(DungeonLabSettings), nameof(DungeonLabSettings.StartFloors)));
                }
                return codes;
            }

            //Labs
            [HarmonyPrefix]
            private static void Prefix(GenerateDungeonBase __instance, uint seed)
            {
                // Spawn additional labs if needed
                if (DungeonLabSettings.LabCount == 0) { return; }
                for (int i = 1; i < DungeonLabSettings.LabCount + 1; i++)
                {
                    Vector3 pos = GetUniqueOceanPosition();
                    if (pos == Vector3.zero) { Console.WriteLine($"No Valid Lab Spawn Position Lab#{i}"); continue; }
                    DungeonLabSettings.LabPos.Add(pos);
                    Console.WriteLine($"[Add Additional Underwater Lab#{i} @ {pos}]");
                    World.AddPrefab("DungeonBase", Prefab.Load(DungeonLabSettings.IDs.GetRandom()), pos, Quaternion.Euler(Vector3.zero), Vector3.one);
                }
            }

            private static Vector3 GetUniqueOceanPosition()
            {
                for (int i = 0; i < 250; i++)
                {
                    Vector3 candidate = RandomOceanPosition();
                    if (candidate == Vector3.zero) { continue; }
                    bool tooClose = false;
                    foreach (var existing in DungeonLabSettings.LabPos)
                    {
                        if (Vector3.Distance(existing, candidate) < 300f) // avoid overlap radius
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (!tooClose) { return candidate; }
                }
                return Vector3.zero;
            }

            private static Vector3 RandomOceanPosition()
            {
                float half = World.Size * 0.5f;
                float minWaterWidth = DungeonLabSettings.edgeBuffer;   // typical ocean width before land
                float maxWaterWidth = minWaterWidth * 2;  // cap for deep ocean band
                float safeDistanceFromLand = 200f; // how far from coast we must be
                float minDepth = DungeonLabSettings.minDepth;
                for (int i = 0; i < 1000; i++)
                {
                    // Choose a random direction (N/S/E/W edge band)
                    int edge = UnityEngine.Random.Range(0, 4);
                    float offset = UnityEngine.Random.Range(minWaterWidth, maxWaterWidth);
                    float x = 0f, z = 0f;
                    switch (edge)
                    {
                        case 0: // North
                            x = UnityEngine.Random.Range(-half + 200f, half - 200f);
                            z = half - offset;
                            break;
                        case 1: // South
                            x = UnityEngine.Random.Range(-half + 200f, half - 200f);
                            z = -half + offset;
                            break;
                        case 2: // East
                            x = half - offset;
                            z = UnityEngine.Random.Range(-half + 200f, half - 200f);
                            break;
                        case 3: // West
                            x = -half + offset;
                            z = UnityEngine.Random.Range(-half + 200f, half - 200f);
                            break;
                    }

                    var pos = new Vector3(x, 0f, z);
                    float terrain = TerrainMeta.HeightMap.GetHeight(pos);
                    // Require it to be deep ocean, not shallow shore
                    if (terrain > -minDepth) { continue; }
                    // Sample around the point to make sure it’s not near land
                    if (IsNearLand(pos, safeDistanceFromLand)) { continue; }
                    pos.y = terrain;
                    return pos;
                }

                // fallback: return zero to signal failure
                return Vector3.zero;
            }

            private static bool IsNearLand(Vector3 center, float radius)
            {
                const int checks = 8;
                float angleStep = 360f / checks;
                for (int i = 0; i < checks; i++)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    Vector3 sample = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    float terrain = TerrainMeta.HeightMap.GetHeight(sample);
                    if (terrain > -1f) // any land above or near sea level
                        return true;
                }

                return false;
            }

            private static bool NextCalls(List<CodeInstruction> codes, int index, string methodName)
            {
                for (int i = index + 1; i < Math.Min(index + 6, codes.Count); i++)
                {
                    if (codes[i].Calls(AccessTools.Method(typeof(GenerateDungeonBase), methodName)))
                        return true;
                }
                return false;
            }
        }

        //River Limiter
        [HarmonyPatch(typeof(GenerateRiverLayout), nameof(GenerateRiverLayout.Process))]
        public static class GenerateRiverLayout_Process
        {
            public static class RiverSettings
            {
                // Default values from the vanilla game
                public static float DefaultRiverWidth = 8f;
                public static float DefaultTerrainOffset = -1.5f;

                public static List<PathList> LimitRivers(List<PathList> rivers)
                {
                    if (Library.ConfigVars.TryGetValue("river.max", out string value) && int.TryParse(value, out int max))
                    {
                        if (rivers.Count > max)
                        {
                            rivers.RemoveRange(max, rivers.Count - max);
                            Console.WriteLine($"[Limited rivers to {max}]");
                        }
                    }
                    return rivers;
                }

                public static float GetRiverWidth()
                {
                    if (Library.ConfigVars != null && Library.ConfigVars.TryGetValue("river.width", out string val) &&
                        float.TryParse(val, out float parsed))
                        return parsed;
                    return DefaultRiverWidth;
                }

                public static float GetRiverTerrainOffset()
                {
                    if (Library.ConfigVars != null && Library.ConfigVars.TryGetValue("river.depth", out string val) &&
                        float.TryParse(val, out float parsed))
                        return -parsed;
                    return DefaultTerrainOffset;
                }
            }

            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                var addRangeMethod = AccessTools.Method(typeof(List<PathList>), "AddRange");
                int? listLocalIndex = null;

                for (int i = 0; i < codes.Count; i++)
                {
                    var code = codes[i];
                    // Detect local variable index of 'list'
                    if (code.opcode == OpCodes.Ldloc_0) listLocalIndex = 0;
                    else if (code.opcode == OpCodes.Ldloc_1) listLocalIndex = 1;
                    else if (code.opcode == OpCodes.Ldloc_2) listLocalIndex = 2;
                    else if (code.opcode == OpCodes.Ldloc_3) listLocalIndex = 3;
                    else if (code.opcode == OpCodes.Ldloc_S || code.opcode == OpCodes.Ldloc) { if (code.operand is int idx) listLocalIndex = idx; }
                    if (code.Calls(addRangeMethod) && listLocalIndex.HasValue)
                    {
                        codes.InsertRange(i, new[]
                        {
                            new CodeInstruction(OpCodes.Ldloc, listLocalIndex.Value),
                            new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(GenerateRiverLayout_Process.RiverSettings),
                            nameof(RiverSettings.LimitRivers))),new CodeInstruction(OpCodes.Stloc, listLocalIndex.Value)
                        });
                        break;
                    }
                }

                // 🔹 Replace the two constants (Width = 8f, TerrainOffset = -1.5f)
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float f)
                    {
                        if (Math.Abs(f - 8f) < 0.001f)
                        {
                            codes[i] = new CodeInstruction(OpCodes.Call,
                                AccessTools.Method(typeof(GenerateRiverLayout_Process.RiverSettings),
                                    nameof(RiverSettings.GetRiverWidth)));
                        }
                        else if (Math.Abs(f - (-1.5f)) < 0.001f)
                        {
                            codes[i] = new CodeInstruction(OpCodes.Call,
                                AccessTools.Method(typeof(GenerateRiverLayout_Process.RiverSettings),
                                    nameof(RiverSettings.GetRiverTerrainOffset)));
                        }
                    }
                }
                return codes;
            }
        }

        //Road Settings
        [HarmonyPatch(typeof(GenerateRoadLayout), "CreateSegment", typeof(int), typeof(Vector3[]))]
        public static class GenerateRoadLayout_CreateSegment
        {
            [HarmonyPostfix]
            private static void Postfix(GenerateRoadLayout __instance, ref PathList __result)
            {
                string key = null;

                switch (__instance.RoadType)
                {
                    case InfrastructureType.Road:
                        key = "height.roadwidth";
                        break;
                    case InfrastructureType.Trail:
                        key = "height.trailwidth";
                        break;
                    default:
                        key = null;
                        break;
                }

                if (key != null)
                {
                    string value;
                    if (Library.ConfigVars.TryGetValue(key, out value))
                    {
                        int width;
                        if (int.TryParse(value, out width))
                        {
                            __result.Width = width;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GenerateRoadRing), nameof(GenerateRoadRing.Process))]
        public static class GenerateRoadRing_Process_Transpiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                for (int i = 0; i < codes.Count; i++)
                {
                    // Look for: ldc.r4 12 followed by stfld float32 PathList::Width
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 12f)
                    {
                        if (i + 1 < codes.Count && codes[i + 1].opcode == OpCodes.Stfld)
                        {
                            var field = codes[i + 1].operand as FieldInfo;
                            if (field != null && field.Name == "Width" && field.DeclaringType == typeof(PathList))
                            {
                                // Replace loading 12f with call to helper method
                                codes[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenerateRoadRing_Process_Transpiler), nameof(GetRoadWidth)));
                            }
                        }
                    }
                }
                return codes;
            }

            // Returns configured width
            public static float GetRoadWidth()
            {
                string value;
                if (Library.ConfigVars.TryGetValue("height.roadwidth", out value))
                {
                    float width;
                    if (float.TryParse(value, out width))
                        return width;
                }
                return 12f; // fallback default
            }
        }

        [HarmonyPatch(typeof(World), nameof(World.CanLoadFromDisk))]
        public static class World_CanLoadFromDisk
        {
            [HarmonyPostfix]
            private static void Postfix(ref bool __result)
            {
                __result = false;
            }
        }

        [HarmonyPatch(typeof(WorldSerialization), nameof(WorldSerialization.Save))]
        public static class WorldSerialization_Save
        {
            [HarmonyPrefix]
            private static void Prefix(ref string fileName)
            {
                if (Library.png2cubes == true) { return; }
                Library.RemoveTopology(Library.IsSwitchEnabled("height.roadtopology", false), Library.IsSwitchEnabled("height.railtopology", false));
                if (Library.IsSwitchEnabled("height.mountainarctic", false))
                {
                    int minHeight = 120;
                    if (Library.ConfigVars.TryGetValue("height.mountainheight", out string val))
                    {
                        if (int.TryParse(val.ToString(), out int parsed)) { minHeight = parsed; }
                        Timing timer = new Timing($"Removing Arctic biome below height {minHeight}");
                        Library.ModArcticToTundraGrassBelowHeight(minHeight);
                        timer.End();
                    }
                }

                if (Library.ConfigVars.TryGetValue("height.name", out string val2))
                {
                    if (!string.IsNullOrEmpty(val2))
                    {
                        fileName = fileName.Replace("proceduralmap", val2 + ".proceduralmap");
                    }
                }
                //Do custom prefabs
                Timing timer2 = new Timing("Settings Up Custom Prefabs");
                Library.CustomPrefabLoader(fileName);
                timer2.End();
                //Cargo
                Library.GenerateOceanPatrolPath(); //Create cargo path and embed into map.
            }

            [HarmonyPostfix]
            private static void Postfix(string fileName)
            {
                try
                {
                    if (Library.png2cubes == true) { return; }
#if !DEBUG
                    string extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomPrefabs");
                    if (Directory.Exists(extractPath))
                    {
                        string[] files = Directory.GetFiles(extractPath);
                        foreach (var file in files) { File.Delete(file); }
                        string[] subdirectories = Directory.GetDirectories(extractPath);
                        foreach (var subdirectory in subdirectories) { Directory.Delete(subdirectory, true); }
                        Directory.Delete(extractPath);
                    }
#endif
                }
                catch { }
                Timing timer = new Timing("Image Generation");
                Console.WriteLine("Rendering Map And Creating Download File.");
                Console.WriteLine("Sever May Appear Frozen During This...");
                var bytes = MapImageRenderer.Render(out int width, out int height, out UnityEngine.Color color, 1);
                File.WriteAllBytes("preview.png", bytes);
                Library._savedFilePath = fileName;
                timer.End();
                try
                {
                    if (Library.HasPendingJobs && Library.pendingJobs.Count > 0)
                    {
                        Console.WriteLine("Job Generation Completed....");
                        Library.Job job = Library.pendingJobs[0];
                        job.status = "Done";
                        string jobfolder = job.path;
                        File.Copy(Library._savedFilePath, Path.Combine(job.path, Path.GetFileName(Library._savedFilePath)));
                        job.img = Convert.ToBase64String(File.ReadAllBytes("preview.png"));
                        File.WriteAllText(Path.Combine(job.path, "status.json"), JsonConvert.SerializeObject(job));
                        File.Delete(Library._savedFilePath);
                        File.Delete("preview.png");
                        Library.RestartServer();
                        ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "quit", Array.Empty<object>());
                        return;
                    }
                    Console.WriteLine("Generation Completed....");
                    Console.WriteLine("");
                    Library._continueEvent.Reset();
                    Library._continueEvent.Wait();
                    File.Delete(Library._savedFilePath);
                    File.Delete("preview.png");
                }
                catch { }
                if (Library.Restart) { Library.RestartServer(); }
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "quit", Array.Empty<object>());
            }
        }

        // Main bootstrap patch + webserver
        [HarmonyPatch(typeof(Bootstrap), "StartupShared")]
        public class BootstrapPatch
        {
            static bool Prefix()
            {
                Library.log = new UnifiedLogger();
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "skipAssetWarmup_crashes true", Array.Empty<object>()); //Speed up startup
                Library.IP = Facepunch.Utility.CommandLine.GetSwitch("+server.ip", Facepunch.Utility.CommandLine.GetSwitch("-server.ip", "localhost"));
                Library.Port = Facepunch.Utility.CommandLine.GetSwitch("+server.port", Facepunch.Utility.CommandLine.GetSwitch("-server.port", "28016"));
                Library.HTTPSPort = Facepunch.Utility.CommandLine.GetSwitch("+server.queryport", Facepunch.Utility.CommandLine.GetSwitch("-server.queryport", "28015"));
                Library.QuitPassword = Facepunch.Utility.CommandLine.GetSwitch("+rcon.password", Facepunch.Utility.CommandLine.GetSwitch("-rcon.password", "quit"));
                Library.AllowUpload = bool.Parse(Facepunch.Utility.CommandLine.GetSwitch("+allowuploads", Facepunch.Utility.CommandLine.GetSwitch("-allowuploads", "true")));
                Library.AllowJobs = bool.Parse(Facepunch.Utility.CommandLine.GetSwitch("+allowjobs", Facepunch.Utility.CommandLine.GetSwitch("-allowjobs", "true")));
                Library.AllowCubes = bool.Parse(Facepunch.Utility.CommandLine.GetSwitch("+allowcubes", Facepunch.Utility.CommandLine.GetSwitch("-allowcubes", "true")));
                Library.CubesOnly = bool.Parse(Facepunch.Utility.CommandLine.GetSwitch("+cubesonly", Facepunch.Utility.CommandLine.GetSwitch("-cubesonly", "false")));
                if (Library.IP == "localhost") { Library.AllowUpload = true; }
                if (Library.IP.Contains("/"))
                {
                    string[] urls = Library.IP.Split('/');
                    List<string> temp = new List<string>();
                    foreach (var u in urls)
                    {
                        if (!string.IsNullOrEmpty(u))
                        {
                            temp.Add("http://" + u + ":" + Library.Port + "/");
                            // temp.Add("https://" + u + ":" + Library.HTTPSPort + "/");
                        }
                    }
                    Library.URL = temp.ToArray();
                }
                else
                {
                    Library.URL = new string[] { "http://" + Library.IP + ":" + Library.Port + "/" };
                }
                Library.favico = Facepunch.Utility.Compression.Uncompress(Convert.FromBase64String("H4sIAAAAAAAEAO1aaVRU9xWfNEnbc9qe00/th35oPtimpY0bhk1hZB+WYfZhhmER2WXYZBVQcAHjCgiibC6AiigianKS2BhjjUYTd0mi4hpTNTGpxpiIsd7ee9+bjZlBPPUcv3Q8l/fmvf9773fv/3d/9/7fKJG8gP8mTpTg31ckPX+SSH4nkUj+goaHJDMkwnH+jJO4/YSOH/+r6IBJ8pggr90xgV7n5EGvD5HFWCxYtKCnMFfj7Y7JR73Wy3Fc4JTzuB2ICpisknp4/Noeu5/fq7/B80tUoT4PdFFSSDKpIcGgAHW4H5smyh8Mpgi2WLI4wazf6Zx4bOS+9bzJcbz9cYdjxnDQRE4DdZjwbJ0mWLiXUcY4lKE+wzFBU1YRZgv+KKmnDrF/a1CEQlP/Pthy8iZ0Hb0KeUWFAn55AGSvr4TSQ61Q+pGdHX62VnKoBVLmp4MmfCpoEH9CbiwU7Wu0ns/bVgt6XQgow3zuxQR56kT4P1OEeC1Whfk+nresDgYuP4KBSz/BzksPoefkLSisqAQtzoleGwI5m6qh/NRGqBjshoqzz87Kz3RB0T9WQWK+keOl1wZDRn0BVODxkWMzV80GVajvY0WQ12LE/qJUKnkJ/WlQh/vCyk27GP9OEf/Oiw9h2+AdWNDYBkZVOGhjpJBRlwdlH3c8M+xzTm6A7HUVYEyKZs6YsjSQ11crxMnF+NyeBTjOFxTB3k3jxo37BfmgDPFeScdWbtkjxt6Gn2zH+R/5nEEZDuqIaZBckQxlR9ud52Gs8yKOK96/GmNuAG20P86xP6SvoNi083y4uzZ36yL2E/E3OuHfvFvE/pMVu8X60da+eRhzW4P8FLhZcqAZys+6f5bbmB9fzzGm3KV7GeIjIGfzfCg/3fnEa634Q4T4SyWSlzB364k/xPXuj68x1pH42YYewroDn4I5Lwc0OA9xMxWQP7BkTM9lnqOvlI8pC9JBi5qglUt5LgvfbRiz7wJ+X2v8if+qMMSPPmlkUyF1Zjy0vHPEJX7yq39oGLacuAlFFXM5DqQHFLuxcChvuxhzGeqiQgrmriooP77hqebODn+TTMSPOBpoHk26KNBFYlwi/GF+wxroOfUV43Xly44LD6CqbjXExoRgTkyFrNXFMOfYOieeE5dLDq6F1Nos1hZtdADEz9LyPFjGVYo2dvwCf2QO/PGDFd19ULWyCYxqGftA+t/y9lGMu7MPNBd95+7DkvW96Hc01hx/SK3JwrrQJsyBiD23dxHEo6aQj6TBmY2FQu6PwPW/4MeeYRXhb979Acb7R1h/YBDSUhM5XgZlKFTXr4Htn9+zy4thNpqbHefvQ8e+UxAfq2BtMqWrMN4tiLEDUqrTQIeaS8+jOlr8/mrUxbHliisrRb9nlM9gXA74A19vpGcQ/l1X/8PWd+47mFPzBsRiTaa8mF1eDhsOXnDJJzrWffQaZJmzOK8Ja9zMGH6OURnG86PGe8Sb9cyb8jNP5wPViILdyyAuRcHcF/VzVPy7rpAP96CuZw+kpsQzlhTM7RWbBmD7Z985aRT50Hn4EpQuqGGs5Ed2bjY0bN/LHMwvLeU4kGaZO+dhbR3FBwv/UK9K/rkG0pflgE4VyHNpSlVa8Vv0E3vCRgt/rPhFH6gebznxJRRVViImf9BFBzLGrWduu8iJYdj26V3E/C7XOyH/H0A/5nrv2X/D0o3bQC8PBg3mcDLygOu4K70S8c9+p47nUYO5o40JYL9zuqud+T8SP+G+KpjFlx0X7sOi5g6BCzg2PWMmtO87wznsai7oeNeRq2hXoO/8D2Ku/AiNO/bBzCQjz0V8jh4KESPXW7uYkwZkYJ9DfSj1nHEZaqwR9bb8Hcl/F/gd5sE6H4+4NpjzzKxPcdooqG3t4tj2j8DftvcEakAS15OOfaf5mMXWH/gMiqvm41xOx15bBllrS6HsE0F7C3YthcQ8A2IX9CqjoQBKj7S56h9G588VOz/s/BlAH3oHv+H6wHqOvWleSZFNY4cE/DUtnRhjf+bcGx09yKFhBx+IZ2+0b8b6EcxxTipOgJRFmaBVTOccTcT+hPojru12HLPHb4m/wiX/Hzn6YHe8f+gH6Dx0HtLTk0WNDYM+UV/Jj75zP6B2LeHnYF8Oc5evYu7Y42c+oTXvOcgazWslHEvalVGXzz3SmPoHEb/GVf6SD2y2eeg98xX20y2QEIt5JZsGWdmZzOl+u/6iF3vuvOJiMKlCQYtjCrHX6D37Lc7NMBvjx5ze9Ml1WNDUjjyMZL1KwHzI21Zj66dc5LYV/2j8t/LnkWCiL2veOggzEvTMfbIFja2oMV87caPn5FeQityfnZ0MqfFKmJWTBZuP33AY03n4ImRkpjK/qHZnNhW5rMtu669d/B3xP3Kag55TN7GvaOR8o2tpbdy864ATHyy26diXrFPV5bOhstiMPbcW8V4Wzz+ARlyjGlVhXCeMWA8K9ix7yv7tCfhJP68Imtm0cx/XIeIK6QHxUxc1nfnRvOdDqzba24aDn/P4hhW1sHLpQtBjzVi3f9CKv2xhLWMgbaR14pyn6EEt+GNCvFe51R/Evg01pnjePNYHOpeQE8tr1KL3GsGUqRF6A1xTzm9Yy3y3x081G9eo0LmxDTZuaGG+1ve+LfJ+mPV2IfKeOEg+cD9KWjNW/Jzrfk0ymXP/sP2zO5iPe5EjMbw2opinLTVD2TGbHpAep8xPA60ykGORjfWA4ktr5d6zd1Bv6jlvu7vXQW9vN4+Zu6yOc5jqGvXefahHy7sHeD1H/hkSInl9MOfkRmstc4m/V8CP2Bqt+MX416zpwLpSjTU+kPVgRkki5O+y4+agzUgjqJbHzZCDZe1QUrUAiudWcX5o8XpzihFy0uKEWov9aVHlPKhcWo/1YAv2REcwVneh/b0T2BuVgBZzmLQ/bYkZSj5c+0T+qEL96j08PH5ujz9WHsTPIo7TmkrQ4C6X+Ol7fv9ift9k6QkF8xXNB9Sh3vgcL9F8reOIN6T5WdkZOG+nsTe/C4tWt1ufH5em4r7CVU9kwa8M9q3z8JDY8NN9aT2KfRX7PwKrw30w9rPa5ojv54Kw79eBRo79naoLdNoDoNOfAV3sJdAZvgSd8SZur+GxQdDpDuOYLaCWV+E1Wrx2Ouf26oH3sU98iNv9kJwYK+RFNGoqrumIq/yOYCT+EO+Vnp6Sl+3jn1KTaat7g87xtljh3nqINWDcwwNBq+pAfEOgj7uN9o2jGV3sGwXTxQ6BRtGA9wiGpDg11ocvWDs2H7sG5YuX4rpDWJcmFhghDzlvWTNY61eQ1wpPiYif89cXzOsqnHG7yCPqY3ldFZmEWG45445zxGq1Ecd0huuI0cAat3rXB7Bb7BGpP6G8SEmOE/iGz6J3c7T2GQ1/9voK13V70HE/Z1MV6KjPiohHDP8Scd3G/S8wrudAi/zR6k4iX46zafUn0c7yOcJMYwX8V/EeejBgrNe89aFTv7vj/PesZSa9XHiXqw6CBOxNuX8eC35Xc4FW/H4jGOIjmb9a1TrEdQG06h7kbBnPiVqmwnMytBDBZLgfocE+YQbmyTzQavqZP1pFE/OHOL/19E27nsvmw86LD1CrPsJaVCVwStB+7H98Gqz6I+q/Ff8TjPIpbzuuE7FHJx/U4ehLmFTQl3A8JsPcjMwAdVQOWq6wz/k6TdQgrBuyKPwuBT32323vHnHos5zXHsgp7AVa3zkK6WlJlvx16J9Jwxn/KLVjpFHfYkpT8rtLTXgAxhN7JON1t7mgM9zAMWvx+UGsMdnY17W/d8z1Wkm03SOsfutbPAcj8dP7Q7f43fCI13qHWzm3NBhLrXqro+a4MK1mAGMfipzRQ//5e4jJVbzd+1Hf86bAoVDvVYTf09PzZUWwT7MDfje9tztdTV+eI+iEslnITbd6dBvzpZN9zSua7YYr7vHTtk7ErwwV+jfKAWWI71r6/SKrtdSl3rjlkHguu6Nc6CdjarleucOvQ/waRT3nwDzUlZFr1d1P9OMRLNvYS9c/Rs7UU/2ln/cwf8sQ/3B8to7ftwjvxJ3N3fGCgaWMSSMv5VrrHv9N9HEhj13etd22Phqjbf7kKswyZ9Ga9KEiaEo+/zSJn+Bp4/+M67Fz9F6Y3idTf0xrubEa99OEPyoT9FyLbZhjHfBfR42dzfOfOtMEs7LTICffDOaCHMgRzVyQy9/5WH6OdZ/WIIlGJfe1qlCfy+Her77i8Pupn8dERbDXQeyzbuD5O2h3XZmSt752+4KpQ30ekEbqYgcRM+aA6bbYU4j7JqpvV7gGYPz+M/r9He9tPRfmc0sV4n1I5jt+kqvfgOl31Sip5/Qo6SRDlP8Ek2CTRzXZ1PEzo6WTO9D3G9QPaWJqMI8bXZpGsRy1J5zi9yB6umdvhP/E1NHvb3t+RMDEuIgAz1DphAm/df8r9lN/XpQHeRYinq8ppqRhtj7ajQnvSR7j+DvywNfnir9hPZdPlM+EP+A8X1bLpGAwFkB8egskpLdCQkarbWu1NnHbAkZTCa6PplMNuqUI8HzteeEP9f6rN+muwZALGQvvQUbNgydaJm0X3QdjfDnnfKR0Uvzzwh/m+zc/XruaygRstQI+2ma4sMxa25i45CW87pQhx58XfnnQ33+PHLioDpv2ODF7C6RWXoC0you4HUK7yJY29yIfo20qWcUFSMrdiXkc8BivvR7u99qrzws//faH+Wum/ztBfadWHo3rArUL09j25XKxD/X5PjpwcpFU+sdfPi/89KH+QzZ1sk900JStyhCvy5gPN5BTN92ZKsTnGtbOPVHSiSGyZ6A9++nPixJJNW1fsG3//3H9qaY/L9jidoW2OAvwWCKpgqfYivZfahrlML4lAAA="));
                Library.RestartPage = Encoding.UTF8.GetBytes(Pages.RestartPagehtml);
                if (Library.AllowCubes || Library.CubesOnly) { Library.PNG2CubesPage = Encoding.UTF8.GetBytes(Pages.PNG2Cubeshtml); }
                if (!Library.CubesOnly)
                {
                    Library.MainPage = Encoding.UTF8.GetBytes(Pages.MainPagehtml); 
                    if (!Library.AllowUpload) { Library.MainPage = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Library.MainPage).Replace(@"<button class=""upload-btn"" id=""uploadBtn"" onclick=""window.location.href='/upload'"">📤 Upload Map</button>", "")); }
                    if (!Library.AllowCubes) { Library.MainPage = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Library.MainPage).Replace(@"<button class=""upload-btn"" id=""png2cubesBtn"" onclick=""window.location.href='/png2cubes'"">🧊 Png2Cubes</button>", "")); }
                    if (!Library.AllowJobs) { Library.MainPage = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Library.MainPage).Replace(@"<button class=""upload-btn"" id=""runJobsBtn"" onclick=""window.location.href='/jobs'"">🎯 Run Jobs</button>", "")); }
                    if (Library.AllowJobs)
                    {
                        Library.JobsPage = Encoding.UTF8.GetBytes(Pages.JobsPagehtml);
                        if (Library.QuitPassword != "quit")
                        {
                            Library.JobsPasswordPage = Encoding.UTF8.GetBytes(Pages.StopPasswordPagehtml);
                            Library.JobsPage =  Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Library.JobsPage).Replace(@"const res = await fetch('/stopdelete', { method: 'POST' });", "window.location.href = '/stopdelete';" + System.Environment.NewLine + "const res = await fetch('/stopdelete', { method: 'POST' });"));
                        }
                    }
                    if (Library.AllowUpload) { Library.UploadPage = Encoding.UTF8.GetBytes(Pages.UploadPagehtml); }
                }
                Library.QuitPage = Encoding.UTF8.GetBytes(Pages.QuitPagehtml);
                Library.PasswordPage = Encoding.UTF8.GetBytes(Pages.PasswordPagehtml);
                Library.StartWebServer();
                try
                {
                    Console.WriteLine();
                    Console.Clear();
                    Console.WriteLine();
                }
                catch { }
                try
                {
                    Console.WriteLine(Encoding.UTF8.GetString(Facepunch.Utility.Compression.Uncompress(Convert.FromBase64String("H4sIAAAAAAAEAMVSQQ6DMAy7T9of8tQeduCwA0klHshLNkidOGWcN3UCnNR23Mq+LVgv+f7pu/v3scR/UYkuR3nXIlxHV5Fxpp9g7GW154Mobd/a+dKEIKuKgy42abJZeWsg6NBsaB3VaraCoUv0ZHY2UdobjBablDBPpXESTAMjvbJyLrcl8h4ih3Vj69mhkSytpWQAqAe7ItY6FbAXj24Hsu7b+2r+V1E8rJWY2f65hW2b0BHzXbJK6Xu0yi9neDZFf2HQu+O+loTP2tK+juDcvEggBgIj7j6aqVXRamMSyZ6J3iRznECROp+Jm3MlSXeGa2IZXDbJZZIIGA8a8R7GJcbcCjupkVpx8GyCjMxDUH0CKKKz8CfRD62CeyOjBQAA"))));
                    Console.Clear();
                    Console.WriteLine($"Loaded MapGenny v{Assembly.GetExecutingAssembly().GetName().Version} by bmgjet");
                }
                catch { }
                foreach (var url in Library.URL)
                {
                    Console.WriteLine("Listening on " + url);
                }
#if DEBUG
                Console.WriteLine("DEBUG BUILD" + System.Environment.NewLine);
#endif
                try
                {
                    Console.SetCursorPosition(0, 0);
                }
                catch { }
                if (File.Exists("Cubes.map")) { File.Delete("Cubes.map"); }
                if (File.Exists("harmony_log.txt")) { File.Delete("harmony_log.txt"); }
                if (File.Exists("preview.png")) { File.Delete("preview.png"); }
                Library.GetJobs();
                if (Library.HasPendingJobs)
                {
                    Library.ForcePageRefresh = true;
                    if (Library.pendingJobs.Count > 0)
                    {
                        Console.WriteLine("Loading Job");
                        Library.Job job = Library.pendingJobs[0];
                        string jobFile = Path.Combine(job.path, "job.json");
                        if (File.Exists(jobFile))
                        {
                            Library.MapConfig mapConfig = JsonConvert.DeserializeObject<Library.MapConfig>(File.ReadAllText(jobFile));
                            if (mapConfig != null)
                            {
                                Library.ConfigVars.Clear();
                                foreach (PropertyInfo prop in typeof(Library.MapConfig).GetProperties())
                                {
                                    string jsonName = prop.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? prop.Name;
                                    object value = prop.GetValue(mapConfig);
                                    Library.ConfigVars[jsonName] = value != null ? value.ToString() : string.Empty;
                                }
                                Library.Generating = true;
                            }

                        }
                        else
                        {
                            Directory.Delete(job.path);
                            Library._continueEvent.Wait();
                        }
                    }
                    else
                    {
                        Library._continueEvent.Wait();
                    }
                }
                else
                {
                    Library._continueEvent.Wait();
                }
                // Merge validated values into server config
                try
                {
#if DEBUG
                    foreach (var config in Library.ConfigVars)
                    {
                        Console.Write($"[MapGenny] {config.Key} = {config.Value}" + System.Environment.NewLine);
                    }
#endif
                    //Set map size
                    if (Library.ConfigVars.TryGetValue("map.size", out var msize) && uint.TryParse(msize, out var sizeVal))
                    {
                        ////Force World Size
                        //sizeVal = 12000;
                        World.Size = sizeVal;
                        ConVar.Server.worldsize = (int)sizeVal;
                    }
                    //Set Seed
                    if (Library.ConfigVars.TryGetValue("map.seed", out var mseed) && uint.TryParse(mseed, out var seedVal))
                    {
                        World.Seed = seedVal;
                        ConVar.Server.seed = (int)seedVal;
                    }
                    //Lab Settings
                    if (Library.ConfigVars.TryGetValue("lab.amount", out var value) && int.TryParse(value, out var outvalue))
                    {
                        GenerateDungeonBase_Process_Patch.DungeonLabSettings.LabCount = outvalue;
                    }
                    if (Library.ConfigVars.TryGetValue("lab.iterations", out var value2) && int.TryParse(value2, out var outvalue2))
                    {
                        GenerateDungeonBase_Process_Patch.DungeonLabSettings.IterationCount = outvalue2;
                    }
                    if (Library.ConfigVars.TryGetValue("lab.minsegmentcount", out var value3) && int.TryParse(value3, out var outvalue3))
                    {
                        GenerateDungeonBase_Process_Patch.DungeonLabSettings.MinSegmentCount = outvalue3;
                    }
                    if (Library.ConfigVars.TryGetValue("lab.largesegmentcount", out var value4) && int.TryParse(value4, out var outvalue4))
                    {
                        GenerateDungeonBase_Process_Patch.DungeonLabSettings.LargeSegmentCount = outvalue4;
                    }
                    if (Library.ConfigVars.TryGetValue("lab.startbudget", out var value5) && int.TryParse(value5, out var outvalue5))
                    {
                        GenerateDungeonBase_Process_Patch.DungeonLabSettings.StartBudget = outvalue5;
                    }
                    if (Library.ConfigVars.TryGetValue("lab.startfloor", out var value6) && int.TryParse(value6, out var outvalue6))
                    {
                        GenerateDungeonBase_Process_Patch.DungeonLabSettings.StartFloors = outvalue6;
                    }
                    if (Library.ConfigVars.TryGetValue("lab.midbudget", out var value7) && int.TryParse(value7, out var outvalue7))
                    {
                        GenerateDungeonBase_Process_Patch.DungeonLabSettings.MidBudget = outvalue7;
                    }
                    if (Library.ConfigVars.TryGetValue("lab.endbudget", out var value8) && int.TryParse(value8, out var outvalue8))
                    {
                        GenerateDungeonBase_Process_Patch.DungeonLabSettings.EndBudget = outvalue8;
                    }
                    if (Library.ConfigVars.TryGetValue("lab.edgebuffer", out var value9) && int.TryParse(value9, out var outvalue9))
                    {
                        GenerateDungeonBase_Process_Patch.DungeonLabSettings.edgeBuffer = outvalue9;
                    }
                    if (Library.ConfigVars.TryGetValue("lab.mindept", out var value0) && int.TryParse(value0, out var outvalue0))
                    {
                        GenerateDungeonBase_Process_Patch.DungeonLabSettings.minDepth = outvalue0;
                    }
                    //Rail
                    if (Library.ConfigVars.TryGetValue("height.raildepth", out var value10) && int.TryParse(value10, out var outvalue10))
                    {
                        Patch_RailGenerateDungeonGrid_Process.DungeonDepthSettings.DownwardStep = outvalue10;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Applying config vars failed: {ex}");
                }
#if !DEBUG
                //Create file structure from specified .zip via +customprefabs arg
                if (Library.HasPendingJobs && Library.pendingJobs.Count > 0)
                {
                    //Load root customprefabs first
                    string customprefabs = Path.Combine("jobs", "customprefabs.zip");
                    if (File.Exists(customprefabs))
                    {
                        Console.WriteLine("Job Using Custom Prefabs");
                        Library.PrefabsZipData = File.ReadAllBytes(customprefabs);
                    }

                    //Check in jobs folder for override.
                    customprefabs = Path.Combine(Library.pendingJobs[0].path, "customprefabs.zip");
                    if (File.Exists(customprefabs))
                    {
                        Console.WriteLine("Job Using Custom Prefabs");
                        Library.PrefabsZipData = File.ReadAllBytes(customprefabs);
                    }
                }
                if (Library.PrefabsZipData != null)
                {
                    if (!Directory.Exists("CustomPrefabs")) { Directory.CreateDirectory("CustomPrefabs"); } //Create CustomPrefabs folder
                    Console.WriteLine("Loading Custom Prefabs Structure");
                    string extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomPrefabs");
                    try
                    {
                        if (!Directory.Exists(extractPath)) { Directory.CreateDirectory(extractPath); }
                        Library.ExtractZip(extractPath, Library.PrefabsZipData);
                    }
                    catch (Exception ex) { Console.WriteLine($"An error occurred: {ex.Message}"); }
                }

#endif
                return true; // allow original StartupShared to continue
            }
        }

        internal static class MonumentTranspilerHelper
        {

            //Adds method call after the texture2D data is loaded. Allows replacing that texture2D data.
            //Example
            //After
            //		global::TextureData heightdata = new global::TextureData(this.heightmap.Get());
            //Add
            //      heightdata = HeightMapOverRide(heightData, this);
            public static IEnumerable<CodeInstruction> InjectCall(IEnumerable<CodeInstruction> instructions, string targetField, string methodName)
            {
                var codes = new List<CodeInstruction>(instructions);
                var mod = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Library), methodName)),
                new CodeInstruction(OpCodes.Stfld)
            };
                for (int i = 0; i < codes.Count; i++)
                {
                    string ilCode = codes[i].ToString();
                    if (ilCode.StartsWith("stfld TextureData ") && ilCode.EndsWith(targetField))
                    {
                        mod[2].operand = codes[i].operand;
                        mod[5].operand = codes[i].operand;
                        codes.InsertRange(i + 1, mod);
                        break;
                    }
                }
                return codes;
            }
        }
        internal static class MonumentMinSizeHelper
        {
            //Nop the return;
            //if ((ulong)World.Size < (ulong)((long)this.MinWorldSize))
            //{
            //	return; <-- removed
            //}
            //Found1 Bool
            //				if (!(component == null) && (ulong)World.Size >= (ulong)((long)component.MinWorldSize))
            //to
            //				if (!(component == null) && (ulong) World.Size >= (ulong)((long)0))
            public static IEnumerable<CodeInstruction> NOPMinSize(IEnumerable<CodeInstruction> instructions, bool PlaceMonuments = false)
            {
                var codes = new List<CodeInstruction>(instructions);
                bool Found1 = false;
                for (int i = 0; i < codes.Count; i++)
                {
                    string ilCode = codes[i].ToString();
                    if (ilCode.StartsWith("ldfld System.Int32 ") && ilCode.EndsWith("MinWorldSize"))
                    {
                        codes[i + 3].opcode = OpCodes.Nop;
                        codes[i + 3].operand = null;
                        Found1 = true;
                        if (!PlaceMonuments) { break; }
                    }
                    //Remove minsize limit on MonumentInfo
                    if (Found1 && ilCode.StartsWith("ldfld System.Int32 ") && ilCode.EndsWith("MinWorldSize"))
                    {
                        codes[i - 1].opcode = OpCodes.Nop;
                        codes[i - 1].operand = null;
                        codes[i].opcode = OpCodes.Ldc_I4_0;
                        codes[i].operand = null;
                        break;
                    }
                }
                return codes;
            }
        }
        #region Remove MinWordSize Limit
        //Remove Offical MinSize Limits
        [HarmonyPatch(typeof(PlaceMonuments), nameof(PlaceMonuments.Process))]
        internal static class PlaceMonuments_Process
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyRemoveMinSize(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                return MonumentMinSizeHelper.NOPMinSize(instructions, true).ToList();
            }
        }

        [HarmonyPatch(typeof(PlaceMonumentsRailside), "Process", typeof(uint))]
        internal static class PlaceMonumentsRailside_Process
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyNopTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentMinSizeHelper.NOPMinSize(instructions);
            }
        }

        [HarmonyPatch(typeof(PlaceMonumentsRoadside), "Process", typeof(uint))]
        internal static class PlaceMonumentsRoadside_Process
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyNopTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentMinSizeHelper.NOPMinSize(instructions);
            }
        }

        [HarmonyPatch(typeof(PlaceMonumentsOffshore), "Process", typeof(uint))]
        internal static class PlaceMonumentsOffshore_Process
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyNopTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentMinSizeHelper.NOPMinSize(instructions);
            }
        }

        [HarmonyPatch(typeof(GenerateRoadRing), "Process", typeof(uint))]
        internal static class GenerateRoadRing_Process
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyNopTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentMinSizeHelper.NOPMinSize(instructions);
            }
        }

        [HarmonyPatch(typeof(GenerateRailRing), "Process", typeof(uint))]
        internal static class GenerateRailRing_Process
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyNopTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentMinSizeHelper.NOPMinSize(instructions);
            }
        }
        #endregion

        [HarmonyPatch(typeof(Mountain))]
        internal static class MountainPatches
        {
            //Hook for height Texture2D
            [HarmonyPatch("ApplyHeight", typeof(Matrix4x4), typeof(Matrix4x4))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyHeightTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentTranspilerHelper.InjectCall(instructions, "heightdata", nameof(Library.HeightMapOverRideMountain));
            }

            //Hook for splat0 and splat1 Texture2D
            [HarmonyPatch("ApplySplat", typeof(Matrix4x4), typeof(Matrix4x4))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplySplatTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                // Inject calls for both splat0 and splat1
                codes = MonumentTranspilerHelper.InjectCall(codes, "splat0data", nameof(Library.Splat0MapOverRide)).ToList();
                codes = MonumentTranspilerHelper.InjectCall(codes, "splat1data", nameof(Library.Splat1MapOverRide)).ToList();
                return codes;
            }

            //Hook for alpha Texture2D
            [HarmonyPatch("ApplyAlpha", typeof(Matrix4x4), typeof(Matrix4x4))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyAlphaTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentTranspilerHelper.InjectCall(instructions, "alphadata", nameof(Library.AlphaMapOverRide));
            }

            //Hook for biome Texture2D
            [HarmonyPatch("ApplyBiome", typeof(Matrix4x4), typeof(Matrix4x4))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyBiomeTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentTranspilerHelper.InjectCall(instructions, "biomedata", nameof(Library.BiomeMapOverRide));
            }

            //Hook for topology Texture2D
            [HarmonyPatch("ApplyTopology", typeof(Matrix4x4), typeof(Matrix4x4))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyTopologyTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentTranspilerHelper.InjectCall(instructions, "topologydata", nameof(Library.TopologyMapOverRide));
            }
        }

        [HarmonyPatch(typeof(Monument))]
        internal static class MonumentPatches
        {
            //Hook for height Texture2D
            [HarmonyPatch("ApplyHeight", typeof(Matrix4x4), typeof(Matrix4x4))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyHeightTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentTranspilerHelper.InjectCall(instructions, "heightdata", nameof(Library.HeightMapOverRide));
            }

            //Hook for splat0 and splat1 Texture2D
            [HarmonyPatch("ApplySplat", typeof(Matrix4x4), typeof(Matrix4x4))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplySplatTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                // Inject calls for both splat0 and splat1
                codes = MonumentTranspilerHelper.InjectCall(codes, "splat0data", nameof(Library.Splat0MapOverRide)).ToList();
                codes = MonumentTranspilerHelper.InjectCall(codes, "splat1data", nameof(Library.Splat1MapOverRide)).ToList();
                return codes;
            }

            //Hook for alpha Texture2D
            [HarmonyPatch("ApplyAlpha", typeof(Matrix4x4), typeof(Matrix4x4))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyAlphaTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentTranspilerHelper.InjectCall(instructions, "alphadata", nameof(Library.AlphaMapOverRide));
            }

            //Hook for biome Texture2D
            [HarmonyPatch("ApplyBiome", typeof(Matrix4x4), typeof(Matrix4x4))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyBiomeTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentTranspilerHelper.InjectCall(instructions, "biomedata", nameof(Library.BiomeMapOverRide));
            }

            //Hook for topology Texture2D
            [HarmonyPatch("ApplyTopology", typeof(Matrix4x4), typeof(Matrix4x4))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ApplyTopologyTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return MonumentTranspilerHelper.InjectCall(instructions, "topologydata", nameof(Library.TopologyMapOverRide));
            }
        }

        [HarmonyPatch(typeof(GenerateHeight), "Process", typeof(uint))] //Entry Native Gen
        public static class GenerateHeight_Process
        {
            private static readonly Type MapType = typeof(TerrainMap<>).MakeGenericType(typeof(short));
            private static readonly FieldInfo DstField = MapType.GetField("dst", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            private static readonly FieldInfo SrcField = MapType.GetField("src", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            [HarmonyPostfix]
            private static void Postfix()
            {
                var map = TerrainMeta.HeightMap;
                var dst = (NativeArray<short>)DstField.GetValue(map);
                Library.ConvertPNG2HeightMap(ref dst);
                DstField.SetValue(map, dst);
                SrcField.SetValue(map, dst);
            }
        }

        [HarmonyPatch(typeof(GenerateTerrainMesh), "Process", typeof(uint))] //Entry Native Gen
        public static class GenerateTerrainMesh_Process
        {
            private static readonly Type MapType = typeof(TerrainMap<>).MakeGenericType(typeof(short));
            private static readonly FieldInfo SrcField = MapType.GetField("src", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            [HarmonyPrefix]
            private static void Prefix()
            {
                Timing timer = new Timing("Custom Terrain Mods");
                var map = TerrainMeta.HeightMap;
                var src = (NativeArray<short>)SrcField.GetValue(map);

                if (Library.IsSwitchEnabled("height.shallow", false))
                {
                    Console.WriteLine("[Flattening Bays And Shores]");
                    Library.FlattenBaysShores(ref src);
                }

                if (Library.IsSwitchEnabled("height.covergc", false))
                {
                    Console.WriteLine("[Covering Vector3.Zero]");
                    Library.FlattenZeroArea(ref src, 16700, 15);
                }

                if (Library.IsSwitchEnabled("height.allsand", false))
                {
                    Console.WriteLine("[Painting Ocean With Sand Splat]");
                    Library.ModOceanSplatBiome();
                }

                if (Library.IsSwitchEnabled("height.warmocean", false))
                {
                    Console.WriteLine("[Painting Ocean With Normal Biome]");
                    Library.ModOceanSplatBiome(false);
                }

                if (Library.IsSwitchEnabled("height.edge", false))
                {
                    if (Library.ConfigVars.TryGetValue("height.pixels", out string val))
                    {
                        if (int.TryParse(val, out int result))
                        {
                            if (Library.ConfigVars.TryGetValue("height.floor", out string val2))
                            {
                                if (float.TryParse(val2, out float result2))
                                {
                                    short sresult2 = BitUtility.Float2Short(result2 / 1000);
                                    Console.WriteLine("[Cutting Edge Of Map Width: " + result + "]");
                                    short lowest = Math.Min(Math.Min(src[0], src[src.Length - 1]), sresult2);
                                    Library.CutEdges(ref src, result, lowest);
                                }
                            }
                        }
                    }
                }
                SrcField.SetValue(map, src);
                timer.End();
            }
        }

        [HarmonyPatch(typeof(World), "InitSize", typeof(uint))] //Over-Ride 1000-6000 limit
        public static class World_InitSize
        {
            [HarmonyPrefix]
            private static bool Prefix(uint size)
            {
                if (size == 0U) { size = 4500; }
                if (size < 150) { size = 150; } //True1grid
                if (size > 8000) { size = 8000; } //8K
                if (Library.ConfigVars.TryGetValue("map.size", out var msize)) { uint.TryParse(msize, out size); }
                //Force world size
                //size = 12000;
                World.Size = size;
                ConVar.Server.worldsize = (int)size;
                return false;
            }
        }
        public class Library
        {
            public static string IP = "localhost";
            public static string Port = "28016";
            public static string HTTPSPort = "28015";
            public static string QuitPassword = "quit";
            public static bool AllowUpload = false;
            public static bool AllowJobs = false;
            public static bool AllowCubes = false;
            public static bool CubesOnly = false;
            public static float MAP_BOUNDARY = 3800f;
            public static byte[] PasswordFromStaticMap = new byte[0]; //Store rustedit password from static.map
            public static Dictionary<uint, CustomPrefab> CustomMapPrefabs = new Dictionary<uint, CustomPrefab>(); //Custom prefab lookup
            //RustEdit Stuff
            public static SerializedVehicleData REserializedVehicleData = new SerializedVehicleData();
            public static SerializedIOData REserializedIOData = new SerializedIOData();
            public static SerializedLootableContainerData REserializedLootableContainerData = new SerializedLootableContainerData();
            public static SerializedVendingContainerData REserializedVendingContainerData = new SerializedVendingContainerData();
            public static SerializedNPCData REserializedNPCData = new SerializedNPCData();
            public static SerializedAPCPathList REserializedAPCPathList = new SerializedAPCPathList();
            public static SerializedBlockList RESerializedBlockList = new SerializedBlockList();
            public static AnchoredPathList RESerializedAnchoredPathList = new AnchoredPathList();
            public static ManualResetEventSlim _continueEvent = new ManualResetEventSlim(false);

            //http stuff
            private static HttpListener _listener;
            private static Task _listenerTask;
            private static DateTime _serverStart = DateTime.Now;
            public static string _savedFilePath = null;
            public static bool Generating = false;
            public static bool Restart = false;
            public static bool png2cubes = false;
            public static bool HasPendingJobs = false;
            public static bool ForcePageRefresh = false;
            public static List<Job> pendingJobs = new List<Job>();

            // Store parsed variables for later use
            public static Dictionary<string, string> ConfigVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public static byte[] HeightPngData = null;
            public static byte[] PrefabsZipData = null;
            public static string[] URL;
            public static byte[] favico = null;
            public static byte[] RestartPage = NoPage();
            public static byte[] UploadPage = NoPage();
            public static byte[] PNG2CubesPage = NoPage();
            public static byte[] MainPage = NoPage();
            public static byte[] PasswordPage = NoPage();
            public static byte[] JobsPage = NoPage();
            public static byte[] JobsPasswordPage = NoPage();
            public static byte[] QuitPage = NoPage();
            public static byte[] NoPageData = new byte[] { 0x4e, 0x6f, 0x20, 0x50, 0x61, 0x67, 0x65 };
            // Volcano IDs + offsets
            public static Dictionary<uint, Vector3> volcanoOffsets = new Dictionary<uint, Vector3>
                {
                { 2440119897, new Vector3(-14, 80, -20) }, //mountain_1
                { 1986939603, new Vector3(  4, 85,  21) }, //mountain_2
                {  793299238, new Vector3(119, 73, -94) }, //mountain_3
                { 1089892705, new Vector3( 21, 46, 88) }, //mountain_4
                { 3633843436, new Vector3(-278, 145, -86) } //mountain_5
                };

            //Prefab IDs
            public const uint OILRIG_1 = 2839094107u;
            public const uint OILRIG_2 = 2038914978u;
            public const uint COMPOUND = 1879405026u;
            public const uint BANDIT = 2074025910u;
            public static UnifiedLogger log;

            //Text
            //Lookup table for char to prefabID for that letter
            public static readonly Dictionary<char, uint> prefabMap = new Dictionary<char, uint>
            {
            { 'A', 2668510687 },
            { 'B', 491222911 },
            { 'C', 3358379765 },
            { 'D', 3940114902 },
            { 'E', 3690090286 },
            { 'F', 1955124163 },
            { 'G', 1978595701 },
            { 'H', 3036864937 },
            { 'I', 1218970215 },
            { 'J', 3844406919 },
            { 'K', 3481260182 },
            { 'L', 1554136027 },
            { 'M', 2528602136 },
            { 'N', 3709867063 },
            { 'O', 2489834613 },
            { 'P', 775854739 },
            { 'Q', 4105307909 },
            { 'R', 638347284 },
            { 'S', 3782502415 },
            { 'T', 1027880868 },
            { 'U', 4129468480 },
            { 'V', 2620585288 },
            { 'W', 1666935529 },
            { 'X', 2467298332 },
            { 'Y', 3679731434 },
            { 'Z', 1697319372 },
            { '0', 2740687181 },
            { '1', 1865922381 },
            { '2', 4292972501 },
            { '3', 3389230713 },
            { '4', 2256628427 },
            { '5', 3370834221 },
            { '6', 3339647907 },
            { '7', 413403103 },
            { '8', 1963342849 },
            { '9', 1024265129 }
            };

            //Lookup table for space size since not all letters are the same width
            public static Dictionary<char, float> sizeMap = new Dictionary<char, float>
            {
            { 'A', 0.175f },
            { 'B', 0.175f },
            { 'C', 0.175f },
            { 'D', 0.175f },
            { 'E', 0.175f },
            { 'F', 0.165f },
            { 'G', 0.180f },
            { 'H', 0.195f },
            { 'I', 0.160f },
            { 'J', 0.170f },
            { 'K', 0.180f },
            { 'L', 0.175f },
            { 'M', 0.200f },
            { 'N', 0.175f },
            { 'O', 0.180f },
            { 'P', 0.175f },
            { 'Q', 0.185f },
            { 'R', 0.185f },
            { 'S', 0.170f },
            { 'T', 0.170f },
            { 'U', 0.180f },
            { 'V', 0.175f },
            { 'W', 0.195f },
            { 'X', 0.175f },
            { 'Y', 0.175f },
            { 'Z', 0.170f },
            { '0', 0.170f },
            { '1', 0.160f },
            { '2', 0.175f },
            { '3', 0.175f },
            { '4', 0.175f },
            { '5', 0.175f },
            { '6', 0.165f },
            { '7', 0.165f },
            { '8', 0.175f },
            { '9', 0.175f }
            };

            public static Dictionary<Color, uint> GetPredefinedColors(List<string> strings)
            {
                var predefinedColors = new Dictionary<Color, uint>();
                if (strings.Contains("white")) predefinedColors.Add(Color.FromArgb(255, 255, 255, 255), 3328529265);
                if (strings.Contains("black")) predefinedColors.Add(Color.FromArgb(255, 0, 0, 0), 504351302);
                if (strings.Contains("lightgray")) predefinedColors.Add(Color.FromArgb(255, 185, 174, 166), 3560857504);
                if (strings.Contains("darkgray")) predefinedColors.Add(Color.FromArgb(255, 97, 87, 79), 1349369362);
                if (strings.Contains("orange")) predefinedColors.Add(Color.FromArgb(255, 247, 145, 5), 768793565);
                if (strings.Contains("lightblue")) predefinedColors.Add(Color.FromArgb(255, 156, 186, 231), 3884555453);
                if (strings.Contains("limegreen")) predefinedColors.Add(Color.FromArgb(255, 174, 205, 6), 3297176165);
                if (strings.Contains("brown")) predefinedColors.Add(Color.FromArgb(255, 118, 76, 34), 748921282);
                if (strings.Contains("red")) predefinedColors.Add(Color.FromArgb(255, 209, 36, 28), 3000377299);
                if (strings.Contains("purple")) predefinedColors.Add(Color.FromArgb(255, 62, 10, 85), 2131825849);
                if (strings.Contains("olivegreen")) predefinedColors.Add(Color.FromArgb(255, 130, 133, 77), 1662609338);
                if (strings.Contains("gray")) predefinedColors.Add(Color.FromArgb(255, 120, 123, 123), 2609039269);
                if (strings.Contains("yellow")) predefinedColors.Add(Color.FromArgb(255, 231, 207, 124), 3995280350);
                return predefinedColors;
            }

            public class Job
            {
                public string name;
                public string size;
                public string seed;
                public string status;
                public string img;
                public string path;
            }

            public class MapConfig
            {
                [JsonProperty("height.name")]
                public string HeightName { get; set; }

                [JsonProperty("map.size")]
                public int MapSize { get; set; }

                [JsonProperty("map.seed")]
                public int MapSeed { get; set; }

                [JsonProperty("height.min")]
                public int HeightMin { get; set; }

                [JsonProperty("height.max")]
                public int HeightMax { get; set; }

                [JsonProperty("height.smooth")]
                public int HeightSmooth { get; set; }

                [JsonProperty("height.cuts")]
                public int HeightCuts { get; set; }

                [JsonProperty("height.floor")]
                public int HeightFloor { get; set; }

                [JsonProperty("height.water")]
                public int HeightWater { get; set; }

                [JsonProperty("height.bangle")]
                public int HeightBAngle { get; set; }

                [JsonProperty("height.langle")]
                public int HeightLAngle { get; set; }

                [JsonProperty("height.edge")]
                public bool HeightEdge { get; set; }

                [JsonProperty("height.pixels")]
                public int HeightPixels { get; set; }

                [JsonProperty("height.roadtopology")]
                public bool HeightRoadTopology { get; set; }

                [JsonProperty("height.railtopology")]
                public bool HeightRailTopology { get; set; }

                [JsonProperty("height.flatlakes")]
                public bool HeightFlatLakes { get; set; }

                [JsonProperty("height.volcano")]
                public bool HeightVolcano { get; set; }

                [JsonProperty("mountain.fog")]
                public bool MountainFog { get; set; }

                [JsonProperty("mountain.snow")]
                public bool MountainSnow { get; set; }

                [JsonProperty("mountain.splat")]
                public bool MountainSplat { get; set; }

                [JsonProperty("mountain.nodamage")]
                public bool MountainNoDamage { get; set; }

                [JsonProperty("mountain.nosmoke")]
                public bool MountainNoSmoke { get; set; }

                [JsonProperty("height.shallow")]
                public bool HeightShallow { get; set; }

                [JsonProperty("height.allsand")]
                public bool HeightAllSand { get; set; }

                [JsonProperty("height.warmocean")]
                public bool HeightWarmOcean { get; set; }

                [JsonProperty("height.mountainarctic")]
                public bool HeightMountainArctic { get; set; }

                [JsonProperty("height.mountainheight")]
                public int HeightMountainHeight { get; set; }

                [JsonProperty("height.oilrigingrid")]
                public bool HeightOilRigInGrid { get; set; }

                [JsonProperty("height.covergc")]
                public bool HeightCoverGC { get; set; }

                [JsonProperty("height.roadwidth")]
                public int HeightRoadWidth { get; set; }

                [JsonProperty("height.trailwidth")]
                public int HeightTrailWidth { get; set; }

                [JsonProperty("height.raildepth")]
                public int HeightRailDepth { get; set; }

                [JsonProperty("river.max")]
                public int RiverMax { get; set; }

                [JsonProperty("river.width")]
                public int RiverWidth { get; set; }

                [JsonProperty("river.depth")]
                public double RiverDepth { get; set; }

                [JsonProperty("lab.amount")]
                public int LabAmount { get; set; }

                [JsonProperty("lab.iterations")]
                public int LabIterations { get; set; }

                [JsonProperty("lab.minsegmentcount")]
                public int LabMinSegmentCount { get; set; }

                [JsonProperty("lab.largesegmentcount")]
                public int LabLargeSegmentCount { get; set; }

                [JsonProperty("lab.startbudget")]
                public int LabStartBudget { get; set; }

                [JsonProperty("lab.startfloor")]
                public int LabStartFloor { get; set; }

                [JsonProperty("lab.midbudget")]
                public int LabMidBudget { get; set; }

                [JsonProperty("lab.endbudget")]
                public int LabEndBudget { get; set; }

                [JsonProperty("lab.edgebuffer")]
                public int LabEdgeBuffer { get; set; }

                [JsonProperty("lab.mindept")]
                public int LabMinDept { get; set; }

                [JsonProperty("height.cargo")]
                public bool HeightCargo { get; set; }

                [JsonProperty("height.cargofast")]
                public bool HeightCargoFast { get; set; }

                [JsonProperty("height.cargoshore")]
                public int HeightCargoShore { get; set; }

                [JsonProperty("height.cargomin")]
                public int HeightCargoMin { get; set; }

                [JsonProperty("ads.compound")]
                public string AdsCompound { get; set; }

                [JsonProperty("ads.gate1")]
                public string AdsGate1 { get; set; }

                [JsonProperty("ads.gate2")]
                public string AdsGate2 { get; set; }

                [JsonProperty("ads.bandit")]
                public string AdsBandit { get; set; }

                [JsonProperty("wc.tier0")]
                public double WcTier0 { get; set; }

                [JsonProperty("wc.tier1")]
                public double WcTier1 { get; set; }

                [JsonProperty("wc.tier2")]
                public double WcTier2 { get; set; }

                [JsonProperty("wc.biome.arid")]
                public double WcBiomeArid { get; set; }

                [JsonProperty("wc.biome.temperate")]
                public double WcBiomeTemperate { get; set; }

                [JsonProperty("wc.biome.tundra")]
                public double WcBiomeTundra { get; set; }

                [JsonProperty("wc.biome.arctic")]
                public double WcBiomeArctic { get; set; }

                [JsonProperty("wc.biome.jungle")]
                public double WcBiomeJungle { get; set; }

                [JsonProperty("wc.mainroads")]
                public bool WcMainRoads { get; set; }

                [JsonProperty("wc.sideroads")]
                public bool WcSideRoads { get; set; }

                [JsonProperty("wc.trails")]
                public bool WcTrails { get; set; }

                [JsonProperty("wc.rivers")]
                public bool WcRivers { get; set; }

                [JsonProperty("wc.powerlines")]
                public bool WcPowerLines { get; set; }

                [JsonProperty("wc.aboverails")]
                public bool WcAboveRails { get; set; }

                [JsonProperty("wc.belowrails")]
                public bool WcBelowRails { get; set; }

                [JsonProperty("wc.underwaterlabs")]
                public bool WcUnderwaterLabs { get; set; }

                [JsonProperty("wc.prefabblacklist")]
                public string WcPrefabBlacklist { get; set; }

                [JsonProperty("wc.prefabwhitelist")]
                public string WcPrefabWhitelist { get; set; }
            }

            #region Functions
            public static byte[] NoPage() { return NoPageData; }

            public static void RestartServer()
            {
                File.WriteAllText("restart", "");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // If start.sh exists, run it
                    if (File.Exists("start.sh"))
                    {
                        ProcessStartInfo shInfo = new ProcessStartInfo
                        {
                            FileName = "/bin/bash",
                            Arguments = "start.sh",
                            UseShellExecute = true
                        };
                        Process.Start(shInfo);
                        return;
                    }
                    Process.Start(new ProcessStartInfo("RustDedicated"));
                }
                else
                {
                    if (File.Exists("start.bat"))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c start.bat",
                            UseShellExecute = true,
                        });
                        return;
                    }
                    // Otherwise start RustDedicated.exe directly
                    Process.Start(new ProcessStartInfo("RustDedicated.exe"));
                }
            }

            // Helper: split on boundary bytes into segments (removes leading/trailing)
            private static List<byte[]> SplitOnBoundary(byte[] data, byte[] boundary)
            {
                var list = new List<byte[]>();
                int i = 0;
                while (i < data.Length)
                {
                    int idx = IndexOf(data, boundary, i);
                    if (idx < 0) break;
                    int start = idx + boundary.Length + 2; // skip boundary and CRLF
                    int next = IndexOf(data, boundary, start);
                    if (next < 0) next = data.Length;
                    var len = next - start;
                    if (len > 0)
                    {
                        var segment = new byte[len];
                        Array.Copy(data, start, segment, 0, len);
                        list.Add(segment);
                    }
                    i = next;
                }
                return list;
            }

            // byte index helpers
            private static int IndexOf(byte[] haystack, byte[] needle, int start = 0)
            {
                for (int i = start; i <= haystack.Length - needle.Length; i++)
                {
                    bool ok = true;
                    for (int j = 0; j < needle.Length; j++)
                    {
                        if (haystack[i + j] != needle[j]) { ok = false; break; }
                    }
                    if (ok) return i;
                }
                return -1;
            }
            private static int IndexOf(byte[] haystack, byte[] needle) => IndexOf(haystack, needle, 0);

            public static TextureData Convert2Volcano(TextureData h, Mountain m, Vector2 offset)
            {
                Timing timer = new Timing("Volcano Mod");
                RaiseVolcanoMountain(
                    h,
                    peakHeight: BitUtility.Float2Short(0.70f),
                    radius: 90f,
                    smoothWidth: 60f,
                    offset: offset,
                    rotationDegrees: 0
                );

                DigCircularHoleSmooth(
                    h,
                    holeHeight: BitUtility.Float2Short(0.46f),
                    radius: 50f,
                    smoothWidth: 38f,
                    offset: offset,
                    rotationDegrees: 0
                );
                timer.End();
                return h;
            }

            private static float GetRotatedDistanceSq(
    float x, float y,
    float centerX, float centerY,
    float rotationRad)
            {
                float dx = x - centerX;
                float dy = y - centerY;

                // Rotation
                float cos = math.cos(rotationRad);
                float sin = math.sin(rotationRad);

                float rx = dx * cos - dy * sin;
                float ry = dx * sin + dy * cos;

                return rx * rx + ry * ry;
            }

            public static void RaiseVolcanoMountain(
                TextureData heightData,
                short peakHeight,
                float radius,
                float smoothWidth,
                Vector2 offset,
                float rotationDegrees)
            {
                int width = heightData.width;
                int height = heightData.height;

                float centerX = width * 0.5f + offset.x;
                float centerY = height * 0.5f + offset.y;

                float rotationRad = rotationDegrees * (float)(Math.PI / 180f);

                float radiusSq = radius * radius;
                float innerRadius = radius - smoothWidth;
                float innerRadiusSq = innerRadius * innerRadius;

                Parallel.For(0, height, y =>
                {
                    long rowOffset = y * width;

                    for (int x = 0; x < width; x++)
                    {
                        long index = rowOffset + x;

                        float distSq = GetRotatedDistanceSq(x, y, centerX, centerY, rotationRad);
                        if (distSq >= radiusSq) continue;

                        short original = (short)heightData.GetShort(x, y);

                        if (distSq <= innerRadiusSq)
                        {
                            heightData.colors[index] = BitUtility.EncodeShort(peakHeight);
                            continue;
                        }

                        float dist = math.sqrt(distSq);
                        float t = (dist - innerRadius) / smoothWidth;

                        if (t < 0f) t = 0f;
                        else if (t > 1f) t = 1f;

                        float blend = t * t * (3f - 2f * t);
                        float newValue = peakHeight * (1f - blend) + original * blend;

                        heightData.colors[index] = BitUtility.EncodeShort((short)newValue);
                    }
                });
            }

            public static TextureData DigCircularHoleSmooth(
    TextureData heightData,
    short holeHeight,
    float radius,
    float smoothWidth,
    Vector2 offset,
    float rotationDegrees)
            {
                int width = heightData.width;
                int height = heightData.height;

                float centerX = width * 0.5f + offset.x;
                float centerY = height * 0.5f + offset.y;

                float rotationRad = rotationDegrees * (math.PI / 180f);

                float radiusSq = radius * radius;
                float innerRadius = radius - smoothWidth;
                float innerRadiusSq = innerRadius * innerRadius;

                Parallel.For(0, height, y =>
                {
                    long rowOffset = y * width;

                    for (int x = 0; x < width; x++)
                    {
                        long index = rowOffset + x;

                        float distSq = GetRotatedDistanceSq(x, y, centerX, centerY, rotationRad);

                        if (distSq <= innerRadiusSq)
                        {
                            heightData.colors[index] = BitUtility.EncodeShort(holeHeight);
                            continue;
                        }
                        if (distSq >= radiusSq)
                            continue;

                        float dist = math.sqrt(distSq);
                        float t = (dist - innerRadius) / smoothWidth;

                        if (t < 0f) t = 0f;
                        else if (t > 1f) t = 1f;
                        float blend = t * t * (3f - 2f * t);

                        short original = (short)heightData.GetShort(x, y);

                        float newValue = holeHeight * (1f - blend) + original * blend;

                        heightData.colors[index] = BitUtility.EncodeShort((short)newValue);
                    }
                });

                return heightData;
            }

            public static TextureData FlattenFloor(TextureData heightData, short floorValue)
            {
                if (heightData.colors == null || heightData.width <= 0 || heightData.height <= 0)
                    throw new ArgumentException("Invalid TextureData passed to FlattenFloor.");

                int width = heightData.width;
                int height = heightData.height;
                var encodedFloor = BitUtility.EncodeShort(floorValue);

                Parallel.For(0, height, y =>
                {
                    long rowOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        long index = rowOffset + x;
                        short currentHeight = (short)heightData.GetShort((int)x, (int)y);
                        if (currentHeight < floorValue)
                        {
                            heightData.colors[index] = encodedFloor;
                        }
                    }
                });

                return heightData;
            }

            public static Image resizeImage(Image imgToResize, Size size)
            {
                var b = new Bitmap(size.Width, size.Height);
                var g = System.Drawing.Graphics.FromImage(b);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(imgToResize, 0, 0, size.Width, size.Height);
                g.Dispose();
                return b;
            }

            public static int ValidMax(int input, int max) //Make sure value doesnt go over max
            {
                if (input > max) { return max; }
                return input;
            }

            public static int ValidMin(int input) //Make sure value doesnt go below 0
            {
                if (input < 0) { return 0; }
                return input;
            }

            public static short GetAverage(short a, short b, short c, short d, short e, short f, short g, short h)
            {
                return (short)((a + b + c + d + e + f + g + h) / 8); //Average all 8 values together
            }

            public static void Fill(NativeArray<short> array, int startIndex, int count, short value)
            {
                for (int i = startIndex; i < startIndex + count && i < array.Length; i++) { array[i] = value; }
            }

            public static int Detections(NativeArray<short> heightMap, int resolution, int currentIndex, int scanSteps, int ratio, int maxIndex, short waterLevel)
            {
                if (!heightMap.IsCreated || heightMap.Length == 0)
                    return 0;

                waterLevel -= 8;
                int step = Math.Max(1, scanSteps / Math.Max(1, ratio));
                int resStep = resolution * step;

                // Precompute clamped indices inline
                int Clamp(int idx) => idx < 0 ? 0 : (idx > maxIndex ? maxIndex : idx);

                int up = Clamp(currentIndex - resStep);
                int down = Clamp(currentIndex + resStep);
                int left = Clamp(currentIndex - step);
                int right = Clamp(currentIndex + step);

                int upLeft = Clamp(up - step);
                int upRight = Clamp(up + step);
                int downLeft = Clamp(down - step);
                int downRight = Clamp(down + step);

                // Manual batching: store neighbors in an array to iterate
                int[] neighbors = { up, down, left, right, upLeft, upRight, downLeft, downRight };

                int detections = 0;
                for (int i = 0; i < neighbors.Length; i++)
                {
                    // Branchless increment
                    detections += (heightMap[neighbors[i]] > waterLevel) ? 1 : 0;
                }

                return detections;
            }

            public static bool IsSwitchEnabled(string Switch, bool defaultValue)
            {
                string value = defaultValue.ToString();
                try { value = ConfigVars[Switch]; }
                catch { }
                return value.ToLower() == "true";
            }

            public static string GetSwitch(string Switch, string defaultValue)
            {
                string value = defaultValue.ToString();
                try { value = ConfigVars[Switch]; }
                catch { }
                return value;
            }

            private static void CreateCornerToCornerPath(List<Vector3> path)
            {
                Point centerPoint = new Point(10, 10); // Change the values to match your desired center point
                int halfWorldSize = (int)(World.Size / 1.8f);
                path.Add(new Vector3(centerPoint.X + halfWorldSize, 0, centerPoint.Y - halfWorldSize));
                path.Add(new Vector3(centerPoint.X - halfWorldSize, 0, centerPoint.Y - halfWorldSize));
                path.Add(new Vector3(centerPoint.X - halfWorldSize, 0, centerPoint.Y + halfWorldSize));
                path.Add(new Vector3(centerPoint.X + halfWorldSize, 0, centerPoint.Y + halfWorldSize));
            }

            private static void SaveCargoPath(List<Vector3> path, string mapName)
            {
                SerializedPathList pathList = new SerializedPathList();
                foreach (var vector in path) { pathList.vectorData.Add(vector); }
                World.Serialization.AddMap(MapDataName(World.Serialization.world.prefabs.Count, mapName), SerializeToByteArray(pathList));
            }

            public static void CustomPrefabLoader(string fileName)
            {
                var prefablist = World.Serialization.world.prefabs;
                float halfSize = World.Size / 2f;

                // Switches
                bool doOilrig = Library.IsSwitchEnabled("height.oilrigingrid", false);
                bool doAds = !string.IsNullOrEmpty(ConfigVars["ads.compound"]) ||
                                 !string.IsNullOrEmpty(ConfigVars["ads.gate1"]) ||
                                 !string.IsNullOrEmpty(ConfigVars["ads.gate2"]) ||
                                 !string.IsNullOrEmpty(ConfigVars["ads.bandit"]);

                float oilLimit = 3800f;

                // Snow SETTINGS
                float gridRadius = 144f;     // Total coverage area around the point
                float step = 12f;            // Spacing between snow prefabs (12m render dist → place multiple)
                float heightOffset = 7f;     // Place effect 5m above terrain

                for (int i = 0; i < prefablist.Count; i++)
                {
                    var pd = prefablist[i];
                    if (pd == null) { continue; }
                    uint id = pd.id;

                    //  VOLCANO MOD
                    if (volcanoOffsets.TryGetValue(id, out Vector3 offset))
                    {
                        Quaternion rot = Quaternion.Euler(pd.rotation.x, pd.rotation.y, pd.rotation.z);
                        Vector3 finalPos = pd.position + rot * offset;
                        Quaternion finalRot = rot * Quaternion.Euler(pd.rotation);
                        if (Library.IsSwitchEnabled("mountain.fog", false))
                        {
                            prefablist.Add(new PrefabData()
                            {
                                category = "Decor",
                                id = 469434037,
                                position = finalPos,
                                rotation = finalRot,
                                scale = new Vector3(3f, 1, 3f)
                            });
                        }
                        if (Library.IsSwitchEnabled("mountain.snow", false))
                        {
                            for (float x = -gridRadius; x <= gridRadius; x += step)
                            {
                                for (float z = -gridRadius; z <= gridRadius; z += step)
                                {
                                    Vector3 gridPos = finalPos + new Vector3(x, 0, z);
                                    float terrainHeight = TerrainMeta.HeightMap.GetHeight(gridPos);
                                    gridPos.y = terrainHeight + heightOffset;
                                    prefablist.Add(new PrefabData()
                                    {
                                        category = "Decor",
                                        id = 3882157967,            // snow prefab
                                        position = gridPos,
                                        rotation = finalRot,
                                        scale = new Vector3(50f, 50f, 50f) // keep your scale
                                    });
                                }
                            }
                        }
                        if (Library.IsSwitchEnabled("height.volcano", false))
                        {
                            // Larva
                            prefablist.Add(new PrefabData()
                            {
                                category = "Decor",
                                id = Library.IsSwitchEnabled("mountain.nodamage", false) ? 2299999359 : 2657962325,
                                position = finalPos,
                                rotation = finalRot,
                                scale = new Vector3(pd.scale.x * 40, 1, pd.scale.z * 40)
                            });

                            // Smoke
                            if (!Library.IsSwitchEnabled("mountain.nosmoke", false))
                            {
                                prefablist.Add(new PrefabData()
                                {
                                    category = "Decor",
                                    id = 408078464,
                                    position = finalPos,
                                    rotation = finalRot,
                                    scale = new Vector3(0.8f, 1, 0.8f)
                                });
                            }

                            if (Library.IsSwitchEnabled("mountain.splat", false))
                            {
                                bool changedSplat = false;
                                int resolution = TerrainGenerator.GetSplatMapRes();
                                finalPos.y = 0;
                                for (int z = 0; z < resolution; z++)
                                {
                                    for (int x = 0; x < resolution; x++)
                                    {
                                        Vector3 worldPos = new Vector3(
                                            (x / (float)resolution) * World.Size - halfSize,
                                            0f,
                                            (z / (float)resolution) * World.Size - halfSize
                                        );

                                        try
                                        {
                                            if (Vector3.Distance(worldPos, finalPos) <= 35)
                                            {
                                                // Change splat to Gravel
                                                TerrainMeta.SplatMap.SetSplat(worldPos, TerrainSplat.GRAVEL);
                                                changedSplat = true;
                                            }

                                        }
                                        catch { continue; }
                                    }
                                }
                                if (changedSplat)
                                {
                                    TryPersistMapBytes(TerrainMeta.SplatMap.ToByteArray(), "splat");
                                }
                            }
                        }
                    }

                    //  OIL RIG IN-GRID LIMITING
                    if (doOilrig && (id == OILRIG_1 || id == OILRIG_2))
                    {
                        var pos = pd.position;
                        float maxClamp = Mathf.Min(halfSize, oilLimit);
                        float x = Mathf.Clamp(pos.x, -maxClamp, maxClamp);
                        float z = Mathf.Clamp(pos.z, -maxClamp, maxClamp);
                        if (x != pos.x || z != pos.z)
                        {
                            pd.position = new Vector3(x, pos.y, z);
                            prefablist[i] = pd;

                            Console.WriteLine($"[Moved oil rig {id} from ({pos.x:F1}, {pos.z:F1}) → ({x:F1}, {z:F1})]");
                        }
                    }

                    //  ADS TEXT PLACEMENT
                    if (doAds)
                    {
                        if (id == COMPOUND)
                        {
                            Quaternion r = pd.rotation;

                            if (!string.IsNullOrEmpty(ConfigVars["ads.compound"]))
                                CreateTxt(ConfigVars["ads.compound"], pd.position + r * new Vector3(-3.5f, 7, -1.7f), r);

                            if (!string.IsNullOrEmpty(ConfigVars["ads.gate1"]))
                                CreateTxt(ConfigVars["ads.gate1"], pd.position + r * new Vector3(-20, 7.2f, 47.8f), r * Quaternion.Euler(0, 180, 0));

                            if (!string.IsNullOrEmpty(ConfigVars["ads.gate2"]))
                                CreateTxt(ConfigVars["ads.gate2"], pd.position + r * new Vector3(-14, 7.3f, -57), r);
                        }

                        else if (id == BANDIT)
                        {
                            if (!string.IsNullOrEmpty(ConfigVars["ads.bandit"]))
                            {
                                Quaternion r = pd.rotation;
                                CreateTxt(
                                    ConfigVars["ads.bandit"],
                                    pd.position + r * new Vector3(39.39f, 10.36f, -23.5f),
                                    r * Quaternion.Euler(0, 134.98f, 0)
                                );
                            }
                        }
                    }
                }
                //Generate Watermark
                var trademarkAttr = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTrademarkAttribute>();
                CreateTxt((trademarkAttr != null ? trademarkAttr.Trademark : ""), Vector3.zero, Quaternion.Euler(new Vector3(270, 180, 0)));
                Console.WriteLine("[Applying " + CustomMapPrefabs.Count + " Custom Prefabs]");
                //Load Static CustomPrefab
                string StaticMapPath = Path.Combine("CustomPrefabs", "static.map");
                if (File.Exists(StaticMapPath))
                {
                    Console.WriteLine("[Found Static Map]");
                    WorldSerialization StaticMap = new WorldSerialization();
                    StaticMap.Load(StaticMapPath); //Load static.map
                    //Copy all rustedit data out of it
                    var customPrefab = new CustomPrefab
                    {
                        CustomPrefabs = StaticMap.world.prefabs,
                        IOData = StaticMap.GetMap(MapDataName(StaticMap.world.prefabs.Count, "ioentitydata"))?.data,
                        VehicleData = StaticMap.GetMap(MapDataName(StaticMap.world.prefabs.Count, "vehiclespawnpoints"))?.data,
                        LootData = StaticMap.GetMap(MapDataName(StaticMap.world.prefabs.Count, "lootcontainerdata"))?.data,
                        VendingData = StaticMap.GetMap(MapDataName(StaticMap.world.prefabs.Count, "vendingdata"))?.data,
                        NPCData = StaticMap.GetMap(MapDataName(StaticMap.world.prefabs.Count, "npcspawnpoints"))?.data,
                        APCData = StaticMap.GetMap(MapDataName(StaticMap.world.prefabs.Count, "bradleypathpoints"))?.data,
                        AnchorPaths = StaticMap.GetMap(MapDataName(StaticMap.world.prefabs.Count, "anchorpaths"))?.data,
                        MapPassword = StaticMap.GetMap(MapDataName(StaticMap.world.prefabs.Count, "mappassword"))?.data,
                        BuildingBlocks = StaticMap.GetMap("buildingblocks")?.data,
                        Paths = StaticMap.world.paths
                    };
                    if (customPrefab.MapPassword != null) { PasswordFromStaticMap = customPrefab.MapPassword; Console.WriteLine("[Copied Password]"); }
                    CustomMapPrefabs.Add(1, customPrefab); //Add to list of custom prefabs to be replaced.
                    StaticMapProcess(StaticMap); //Do terrain/splat/biome/topology/alpha/water modification
                }

                //Replacement Custom Prefabs
                if (CustomMapPrefabs.Count > 0)
                {
                    var ws = World.Serialization;
                    var prefabs = ws.world.prefabs;
                    //Apply prefabs from static.map first if there are any
                    if (CustomMapPrefabs.ContainsKey(1))
                    {
                        Console.WriteLine("[Applying Static Prefabs]");
                        foreach (var pd in CustomMapPrefabs[1].CustomPrefabs)
                        {
                            World.Serialization.world.prefabs.Add(new PrefabData() { category = pd.category, id = pd.id, position = pd.position, rotation = Quaternion.Euler(pd.rotation), scale = pd.scale });
                        }
                        World.Serialization.world.paths.AddRange(CustomMapPrefabs[1].Paths);
                        RustEditData(CustomMapPrefabs, ws, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), 1);
                        CustomMapPrefabs.Remove(1); //Remove from list
                    }

                    // Apply custom prefabs
                    for (var i = prefabs.Count - 1; i >= 0; i--)
                    {
                        var prefab = prefabs[i];
                        if (CustomMapPrefabs.TryGetValue(prefab.id, out var customPrefab)) // Direct dictionary access
                        {
                            Console.WriteLine("[Found Custom Prefab: " + StringPool.Get(prefab.id) + "]");
                            bool remove = false;
                            Vector3 monumentCenter = prefab.position;
                            Quaternion monumentRotation = Quaternion.Euler(prefab.rotation.x, prefab.rotation.y, prefab.rotation.z);
                            foreach (var pd in customPrefab.CustomPrefabs)
                            {
                                if (pd.id == 0 || pd.id == prefab.id) //Prevent double up of the same default prefab
                                {
                                    if (pd.category.Contains("remove")) remove = true; //If its been prefab grouped in rustedit as "remove" then remove the prefab
                                    continue;
                                }
                                Vector3 finalPosition = monumentCenter + monumentRotation * new Vector3(pd.position.x, pd.position.y, pd.position.z);
                                Quaternion finalRotation = monumentRotation * Quaternion.Euler(pd.rotation);
                                World.Serialization.world.prefabs.Add(new PrefabData() { category = pd.category, id = pd.id, position = finalPosition, rotation = finalRotation, scale = pd.scale });
                            }
                            if (customPrefab?.Paths != null) { World.Serialization.world.paths.AddRange(customPrefab.Paths); } //Copy roads/rivers/rails
                            RustEditData(CustomMapPrefabs, ws, monumentCenter, monumentRotation, prefab.id); // Fix up rustedit data
                            if (remove) { prefabs.RemoveAt(i); } //Remove orignal prefab if set to remove
                        }
                    }
                    Console.WriteLine("[Removing Duplicate Profiles]");
                    // Remove any duplicate profiles
                    REserializedLootableContainerData.entities = RemoveDuplicateLoot(REserializedLootableContainerData.entities);
                    REserializedVendingContainerData.entities = RemoveDuplicateVending(REserializedVendingContainerData.entities);
                    // Save modded data to map file in bulk to minimize repetitive calls
                    Console.WriteLine("[Saving Rustedit Map Data]");
                    var prefabCount = ws.world.prefabs.Count;
                    ws.AddMap(MapDataName(prefabCount, "ioentitydata"), SerializeToByteArray(REserializedIOData));
                    ws.AddMap(MapDataName(prefabCount, "vehiclespawnpoints"), SerializeToByteArray(REserializedVehicleData));
                    ws.AddMap(MapDataName(prefabCount, "lootcontainerdata"), SerializeToByteArray(REserializedLootableContainerData));
                    ws.AddMap(MapDataName(prefabCount, "vendingdata"), SerializeToByteArray(REserializedVendingContainerData));
                    ws.AddMap(MapDataName(prefabCount, "npcspawnpoints"), SerializeToByteArray(REserializedNPCData));
                    ws.AddMap(MapDataName(prefabCount, "bradleypathpoints"), SerializeToByteArray(REserializedAPCPathList));
                    ws.AddMap(MapDataName(prefabCount, "anchorpaths"), SerializeToByteArray(RESerializedAnchoredPathList));
                    if (PasswordFromStaticMap.Length > 0) { ws.AddMap(MapDataName(prefabCount, "mappassword"), PasswordFromStaticMap); }
                    ws.AddMap("buildingblocks", SerializeToByteArray(RESerializedBlockList));
                }
            }

            public static void CreateTxt(string inputText, Vector3 position, Quaternion rotation)
            {
                Vector3 basePos = position;
                Vector3 currentPos = position;
                float currentScale = 1.0f; // scale multiplier
                float defaultSize = 10f;    // your reference font size
                float currentLineHeight = 0.25f; // vertical spacing between lines
                for (int i = 0; i < inputText.Length;)
                {
                    if (inputText[i] == '<')
                    {
                        int endTag = inputText.IndexOf('>', i);
                        if (endTag == -1) break; // malformed tag

                        string tag = inputText.Substring(i + 1, endTag - i - 1).Trim().ToLowerInvariant();

                        // --- Handle tags ---
                        if (tag == "br")
                        {
                            // Move to a new line
                            basePos -= rotation * new Vector3(0f, currentLineHeight * currentScale, 0f);
                            currentPos = basePos;
                        }
                        else if (tag.StartsWith("size"))
                        {
                            // Handle <size=12> or <size> (reset)
                            if (tag.Contains("="))
                            {
                                string[] parts = tag.Split('=');
                                if (parts.Length == 2 && float.TryParse(parts[1], out float newSize))
                                {
                                    currentScale = newSize / defaultSize;
                                }
                            }
                            else if (tag == "size") // reset if no number
                            {
                                currentScale = 1.0f;
                            }
                            else if (tag == "/size") // closing tag -> reset
                            {
                                currentScale = 1.0f;
                            }
                        }

                        i = endTag + 1;
                        continue;
                    }

                    char c = char.ToUpperInvariant(inputText[i]);

                    try
                    {
                        uint id = SpawnPrefab(c);
                        float spacing = PrefabSize(c) * currentScale;

                        if (id != 0)
                        {
                            try
                            {
                                World.Serialization.world.prefabs.Add(new PrefabData()
                                {
                                    category = "Decor",
                                    id = id,
                                    position = currentPos,
                                    rotation = rotation.eulerAngles,
                                    scale = Vector3.one * currentScale
                                });
                            }
                            catch (Exception ex2)
                            {
                                Console.WriteLine("Invalid prefab: " + ex2.Message);
                            }
                        }

                        // move right by character spacing
                        currentPos -= rotation * new Vector3(spacing, 0f, 0f);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("CreateTxt Error: " + ex.Message);
                    }

                    i++;
                }
            }

            public static uint SpawnPrefab(char inputText)
            {
                if (prefabMap.TryGetValue(inputText, out uint prefabId)) { return prefabId; }
                // Return 0 if no matching prefab is found
                return 0;
            }

            public static float PrefabSize(char inputText)
            {
                if (sizeMap.TryGetValue(inputText, out float prefabSize)) { return prefabSize; }
                // Return 0 if no matching prefab is found
                return 0.17f;
            }

            public static void StaticMapProcess(WorldSerialization staticMap)
            {
                var maps = new[] { "alpha", "biome", "height", "terrain", "splat", "topology", "water" };
                var dataDict = maps.ToDictionary(map => map, map => staticMap?.GetMap(map)?.data); //Store data from static.map
                //Compare static map data verse orignal map
                var applyFlags = maps.ToDictionary(map => map, map => true);
                foreach (var map in maps)
                {
                    var staticData = dataDict[map];
                    var worldData = World.Serialization.GetMap(map)?.data;
                    if (staticData?.Length != worldData?.Length) { applyFlags[map] = false; } //Maps resolutions dont match
                }
                Dictionary<string, byte[]> mapData = World.Serialization.world.maps.ToDictionary(md => md.name, md => md.data); //Store data from procgen map.
                World.Serialization.world.maps.Clear(); //Clear procgen data
                if (applyFlags["height"])
                {
                    var terrainIndexes = ApplyStaticHeight(dataDict["height"], mapData["height"], new List<int>(), "height"); //Do height modifications, Store indexes where its made modifications
                    if (terrainIndexes.Count > 0)
                    {
                        if (mapData.ContainsKey("terrain")) { ApplyStaticHeight(dataDict["terrain"], mapData["terrain"], new List<int>(), "terrain"); } //Correct terrain data
                        if (mapData.ContainsKey("topology")) { CopyTopologyData(dataDict["topology"], mapData["topology"], GetStepSize("topology")); } //Merge topology data from static map
                        foreach (var map in maps)
                        {
                            if (applyFlags[map] && map != "height" && map != "terrain" && map != "topology" && mapData.ContainsKey(map))
                            {
                                ApplyStaticData(dataDict[map], mapData[map], GetStepSize(map), map, terrainIndexes, dataDict["height"].Length); //Apply splat,biome,alpha,water only where terrain has been modified
                            }
                        }
                    }
                }
                //Save any left over mapdata
                foreach (var mapEntry in mapData)
                {
                    if (World.Serialization.GetMap(mapEntry.Key)?.name != null) { continue; } //Data already exsists
                    World.AddMap(mapEntry.Key, mapEntry.Value);
                }
            }

            //Scale indexs for different resolutions
            public static double ScaleValue(double value, double fromMin, double fromMax, double toMin, double toMax)
            {
                if (fromMax == fromMin) return toMin;
                double ratio = (value - fromMin) / (fromMax - fromMin);
                return ratio * (toMax - toMin) + toMin;
            }

            public static List<int> ApplyStaticHeight(byte[] newBytes, byte[] originalBytes, List<int> moddedterrain, string type)
            {
                //Loop height and terrain data.
                //Terrain data is 2 bytes to make a short value
                for (int i = 0; i < newBytes.Length; i += 2)
                {
                    //static.map terrain higher then original so apply
                    if (BitConverter.ToUInt16(newBytes, i) > BitConverter.ToUInt16(originalBytes, i))
                    {
                        originalBytes[i] = newBytes[i];
                        originalBytes[i + 1] = newBytes[i + 1];
                        moddedterrain.Add(i); //Keep track of mods
                    }
                }
                World.AddMap(type, originalBytes);
                return moddedterrain;
            }

            private static void ApplyStaticData(byte[] newBytes, byte[] originalBytes, int step, string mapType, List<int> terrainIndexes, int maxValue)
            {
                //Work out resolutions
                int byteDepth = originalBytes.Length / step;
                int newRes = (int)Math.Sqrt(byteDepth);
                int heightRes = (int)Math.Sqrt(maxValue / 2);
                //Loop only indexes that have had terrain modification
                foreach (var tIndex in terrainIndexes)
                {
                    //Calculate difference in indexes for resolution missmatches
                    int rowIndex = (int)ScaleValue((tIndex / 2) % heightRes, 0, heightRes, 0, newRes);
                    int colIndex = (int)ScaleValue((tIndex / 2) / heightRes, 0, heightRes, 0, newRes);
                    int index = rowIndex + newRes * colIndex;
                    //Apply changes
                    for (int i = 0; i < step; i++) { originalBytes[index + (byteDepth * i)] = newBytes[index + (byteDepth * i)]; }
                }
                World.AddMap(mapType, originalBytes);
            }

            private static void CopyTopologyData(byte[] newBytes, byte[] originalBytes, int step)
            {
                //Set topology to precgen map if there has been one set in the static.map
                for (int i = 0; i <= newBytes.Length - step; i += step)
                {
                    if (BitConverter.ToUInt32(newBytes, i) != 0) { Array.Copy(newBytes, i, originalBytes, i, step); }
                }
                World.AddMap("topology", originalBytes);
            }

            private static int GetStepSize(string mapType)
            {
                switch (mapType)
                {
                    case "alpha": return 1; //1 byte (0 off, 255 on)
                    case "biome": return 5; //1 byte per channel 5 channels (arid,temperate,tundra,arctic,jungle, DeepSea) all channels can only add upto 255 together for each pixel
                    case "splat": return 8; //1 byte per channel, 8 channels (dirt,snow,sand,rock,grass,forest,stone,gravel) All channels can only add upto 255 for each pixel
                    case "topology": return 4; //1 bit per channel (32 channels, 4 bytes = 32bit) bit mask (Field,Cliff,Summit,Beachside,Beach,Forest,Forestside,Ocean,Oceanside,Decor,Monument,Road,Roadside,Swamp,River,Riverside,Lake,Lakeside,Offshore,Rail,Railside,Building,Cliffside,Mountain,Clutter,Alt,Tier0,Tier1,Tier2,Mainland,Hilltop)
                    case "water": return 2; //2 bytes to make a short for water height (similar to terrain)
                    default: return 1;
                }
            }

            public static void RustEditData(Dictionary<uint, CustomPrefab> CustomMapPrefabs, WorldSerialization ws, Vector3 MonumentCenter, Quaternion MonumentRotation, uint dataid)
            {
                //Correct positions and merge
                bool flag;
                void PatchSerializedData<T>(string dataName, Action<T> patchAction)
                {
                    var serializedData = DeserializeFromByteArray<T>((byte[])CustomMapPrefabs[dataid].GetType().GetProperty(dataName).GetValue(CustomMapPrefabs[dataid]), out flag);
                    if (flag && serializedData != null) { patchAction(serializedData); }
                }

                PatchSerializedData<SerializedVehicleData>("VehicleData", data =>
                {
                    foreach (var pd in data.vehicles)
                    {
                        pd.position = MonumentCenter + MonumentRotation * new Vector3(pd.position.x, pd.position.y, pd.position.z);
                        pd.rotation = MonumentRotation * Quaternion.Euler(pd.rotation);
                    }
                    REserializedVehicleData.vehicles.AddRange(data.vehicles);
                });

                PatchSerializedData<SerializedNPCData>("NPCData", data =>
                {
                    foreach (var npcs in data.npcSpawners)
                    {
                        npcs.position = MonumentCenter + MonumentRotation * new Vector3(npcs.position.x, npcs.position.y, npcs.position.z);
                    }
                    REserializedNPCData.npcSpawners.AddRange(data.npcSpawners);
                });

                PatchSerializedData<SerializedIOData>("IOData", data =>
                {
                    foreach (var pd in data.entities)
                    {
                        if (REserializedIOData.entities.Contains(pd)) { REserializedIOData.entities.Remove(pd); }
                        pd.position = MonumentCenter + MonumentRotation * new Vector3(pd.position.x, pd.position.y, pd.position.z);

                        if (pd.inputs != null)
                        {
                            foreach (var ent in pd.inputs)
                            {
                                if (ent != null) { ent.position = MonumentCenter + MonumentRotation * new Vector3(ent.position.x, ent.position.y, ent.position.z); }
                            }
                        }

                        if (pd.outputs != null)
                        {
                            foreach (var ent in pd.outputs)
                            {
                                if (ent != null)
                                {
                                    ent.position = MonumentCenter + MonumentRotation * new Vector3(ent.position.x, ent.position.y, ent.position.z);
                                }
                            }
                        }
                    }
                    REserializedIOData.entities.AddRange(data.entities);
                });

                // Merge loot and vending data
                PatchSerializedData<SerializedLootableContainerData>("LootData", data => { REserializedLootableContainerData.entities.AddRange(data.entities); });
                PatchSerializedData<SerializedVendingContainerData>("VendingData", data => { REserializedVendingContainerData.entities.AddRange(data.entities); });

                //Merge anchorpaths
                PatchSerializedData<AnchoredPathList>("AnchorPaths", data => { RESerializedAnchoredPathList.paths.AddRange(data.paths); });

                //Merge buildingblocks
                PatchSerializedData<SerializedBlockList>("BuildingBlocks", data => { RESerializedBlockList.list.AddRange(data.list); });

                // Merge APC
                PatchSerializedData<SerializedAPCPathList>("APCData", data =>
                {
                    foreach (var path in data.paths)
                    {
                        for (int a = 0; a < path.nodes.Count; a++)
                        {
                            path.nodes[a] = MonumentCenter + MonumentRotation * new Vector3(path.nodes[a].x, path.nodes[a].y, path.nodes[a].z);
                        }

                        for (int a = 0; a < path.interestNodes.Count; a++)
                        {
                            path.interestNodes[a] = MonumentCenter + MonumentRotation * new Vector3(path.interestNodes[a].x, path.interestNodes[a].y, path.interestNodes[a].z);
                        }
                    }
                    REserializedAPCPathList.paths.AddRange(data.paths);
                });
            }

            public static string MapDataName(int PreFabCount, string DataName)
            {
                //Correct Map data names for rustedit (Encrypted with prefab count + salt)
                try
                {
                    using (var aes = Aes.Create())
                    {
                        var rfc2898DeriveBytes = new Rfc2898DeriveBytes(PreFabCount.ToString(), new byte[] { 73, 118, 97, 110, 32, 77, 101, 100, 118, 101, 100, 101, 118 });
                        aes.Key = rfc2898DeriveBytes.GetBytes(32);
                        aes.IV = rfc2898DeriveBytes.GetBytes(16);
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                            {
                                var D = Encoding.Unicode.GetBytes(DataName);
                                cryptoStream.Write(D, 0, D.Length);
                                cryptoStream.Close();
                            }

                            return Convert.ToBase64String(memoryStream.ToArray());
                        }
                    }
                }
                catch { }
                return DataName;
            }

            public static List<T> RemoveDuplicates<T>(List<T> list, Func<T, string> keySelector)
            {
                var uniqueItems = new HashSet<string>();
                var duplicates = new List<T>();
                foreach (var item in list) { if (!uniqueItems.Add(keySelector(item))) { duplicates.Add(item); } }
                foreach (var item in duplicates) { list.Remove(item); }
                return list;
            }

            public static List<LootableContainerData> RemoveDuplicateLoot(List<LootableContainerData> list)
            {
                return RemoveDuplicates(list, item => item.filename);
            }

            public static List<VendingContainerData> RemoveDuplicateVending(List<VendingContainerData> list)
            {
                return RemoveDuplicates(list, item => item.filename);
            }

            //Serialize rustedit XMLs
            public static byte[] SerializeToByteArray<T>(T data)
            {
                if (data == null) return null;
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        new XmlSerializer(typeof(T)).Serialize(memoryStream, data);
                        return memoryStream.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Serialization error: {ex.Message}\n{ex.StackTrace}");
                    return null;
                }
            }

            //Deserialize rustedit XMLs
            public static T DeserializeFromByteArray<T>(byte[] bytes, out bool success)
            {
                success = false;
                if (bytes == null || bytes.Length == 0) return default;
                try
                {
                    using (var memoryStream = new MemoryStream(bytes))
                    {
                        var serializer = new XmlSerializer(typeof(T));
                        success = true;
                        return (T)serializer.Deserialize(memoryStream);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Deserialization error: {ex.Message} {ex.StackTrace}");
                    return default;
                }
            }

#if DEBUG
            private static void DumpRaw(string name, Texture2D texture)
            {
                List<byte> list = new List<byte>();
                TextureData textureData = new TextureData(texture);
                for (int w = 0; w < texture.width; w++)
                {
                    for (int h = 0; h < texture.height; h++)
                    {
                        var s = textureData.GetShort(h, w);
                        list.Add((byte)(s & 0xFF));         // Low byte
                        list.Add((byte)((s >> 8) & 0xFF));  // High byte
                    }
                }
                File.WriteAllBytes(name, list.ToArray());
            }

            //Dump function for debug build to save Texture2D
            private static void SaveMapData(string path, Texture2D texture)
            {
                string fullPath = Path.Combine("CustomPrefabs", path);
                if (!File.Exists(fullPath))
                {
                    string directoryPath = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath)) { Directory.CreateDirectory(directoryPath); }
                    if (fullPath.Contains("height")) { texture?.SaveAsPng(fullPath); DumpRaw(fullPath.Replace(".png", ".raw"), texture); }
                    else { texture?.SaveAsPng(fullPath); }
                }
            }
#endif

            //Create smallest map posiable
            private static WorldSerialization InitializeWorldSerialization()
            {
                var worldSerialization = new WorldSerialization();
                worldSerialization.world = new WorldData(); worldSerialization.world.size = 1;
                worldSerialization.world.paths = new List<PathData>();
                worldSerialization.world.prefabs = new List<PrefabData>();
                worldSerialization.world.maps = new List<MapData>();
                worldSerialization.AddMap("terrain", new byte[(512 * 512) * 2]);
                worldSerialization.AddMap("height", new byte[(512 * 512) * 2]);
                worldSerialization.AddMap("water", new byte[(512 * 512) * 2]);
                worldSerialization.AddMap("splat", new byte[(512 * 512) * 8]);
                worldSerialization.AddMap("biome", new byte[(512 * 512) * 4]);
                worldSerialization.AddMap("alpha", new byte[(512 * 512) * 1]);
                worldSerialization.AddMap("topology", new byte[(512 * 512) * 4]);
                return worldSerialization;
            }

#if DEBUG
            //Dump function for debug build to save Prefabs to prefabs.map
            private static void SavePrefabData(string path, Monument monument)
            {
                path = path.Replace("heighttexture.png", "prefab.map");
                string fullPath = Path.Combine("CustomPrefabs", path);
                if (!File.Exists(fullPath))
                {
                    var newworld = InitializeWorldSerialization();
                    newworld.world.prefabs.Add(new PrefabData() { category = "Decor", id = monument.prefabID, position = Vector3.zero, rotation = Vector3.zero, scale = Vector3.one });
                    png2cubes = true;
                    newworld.Save(fullPath);
                    png2cubes = false;
                }
                fullPath = fullPath.Replace(".map", ".info");
                if (!File.Exists(fullPath)) { File.WriteAllText(fullPath, monument.size + " | " + monument.offset + " | " + monument.Radius + " | " + monument.Fade); }
            }

            private static void SavePrefabDataMountain(string path, Mountain mountain)
            {
                path = path.Replace("heighttexture.png", "prefab.map");
                string fullPath = Path.Combine("CustomPrefabs", path);
                if (!File.Exists(fullPath))
                {
                    var newworld = InitializeWorldSerialization();
                    newworld.world.prefabs.Add(new PrefabData() { category = "Decor", id = mountain.prefabID, position = Vector3.zero, rotation = Vector3.zero, scale = Vector3.one });
                    png2cubes = true;
                    newworld.Save(fullPath);
                    png2cubes = false;
                }
                fullPath = fullPath.Replace(".map", ".info");
                if (!File.Exists(fullPath)) { File.WriteAllText(fullPath, mountain.size + " | " + mountain.offset + " | " + mountain.Fade + " | " + mountain.Fade); }
            }
#endif

            private static TextureData LoadMapData(string path, Texture2D texture, TextureData originalData)
            {
#if !DEBUG
                //If Texture2D exsists in customprefabs file structure load and use that instead of facepunches
                string fullPath = Path.Combine("CustomPrefabs", path);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        Texture2D newTexture = new Texture2D(texture.width, texture.height);
                        newTexture.LoadImage(File.ReadAllBytes(fullPath));
                        return new TextureData(newTexture);
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
                fullPath = fullPath.Replace(".png", ".raw");
                if (File.Exists(fullPath))
                {
                    try
                    {
                        byte[] rawhight = File.ReadAllBytes(fullPath);
                        for (int i = 0; i < rawhight.Length - 2;)
                        {
                            originalData.colors[i / 2] = BitUtility.EncodeShort(BitConverter.ToInt16(new byte[] { rawhight[i], rawhight[i + 1] }, 0));
                            i += 2;
                        }
                        return originalData;
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
#endif
                return originalData;
            }

            public static Vector3 ParseVector3(string vectorString)
            {
                // Remove parentheses and whitespace
                vectorString = vectorString.Trim('(', ')').Replace(" ", "");

                // Split the string into components
                string[] components = vectorString.Split(',');

                // Ensure there are exactly three components
                if (components.Length != 3)
                {
                    throw new System.FormatException("Input string is not in the correct format.");
                }

                // Parse each component and create a Vector3
                return new Vector3(
                    float.Parse(components[0]),
                    float.Parse(components[1]),
                    float.Parse(components[2])
                );
            }

            private static void LoadPrefabData(string path, object obj)
            {
                Monument monument = obj as Monument;
                Mountain mountain = obj as Mountain;
                uint prefabId = ((monument != null) ? monument.prefabID : mountain.prefabID);
                if (prefabId == 0) { return; }
                //Find prefabs.map in the same file structure as a monument. Load and store that to apply later.
                path = path.Replace("heighttexture.png", "prefab.map");
                string fullPath = Path.Combine("CustomPrefabs", path);
                if (File.Exists(fullPath) && !CustomMapPrefabs.ContainsKey(prefabId))
                {
                    var tempLoad = new WorldSerialization();
                    try { tempLoad.Load(fullPath); }
                    catch { return; }
                    int PC = tempLoad.world?.prefabs != null ? tempLoad.world.prefabs.Count : 0;
                    var customPrefab = new CustomPrefab
                    {
                        CustomPrefabs = tempLoad?.world?.prefabs,
                        IOData = tempLoad?.GetMap(MapDataName(PC, "ioentitydata"))?.data,
                        VehicleData = tempLoad?.GetMap(MapDataName(PC, "vehiclespawnpoints"))?.data,
                        LootData = tempLoad?.GetMap(MapDataName(PC, "lootcontainerdata"))?.data,
                        VendingData = tempLoad?.GetMap(MapDataName(PC, "vendingdata"))?.data,
                        NPCData = tempLoad?.GetMap(MapDataName(PC, "npcspawnpoints"))?.data,
                        APCData = tempLoad?.GetMap(MapDataName(PC, "bradleypathpoints"))?.data,
                        AnchorPaths = tempLoad?.GetMap(MapDataName(PC, "anchorpaths"))?.data,
                        MapPassword = tempLoad?.GetMap(MapDataName(PC, "mappassword"))?.data,
                        BuildingBlocks = tempLoad?.GetMap("buildingblocks")?.data,
                    };
                    CustomMapPrefabs.Add(prefabId, customPrefab);
                }
                fullPath = fullPath.Replace(".map", ".info");
                if (File.Exists(fullPath))
                {
                    //monument.size + " | " + monument.offset + " | " + monument.Radius + " | " + monument.Fade
                    string[] settings = File.ReadAllText(fullPath).Split(new string[] { " | " }, StringSplitOptions.None);
                    if (monument != null)
                    {
                        monument.Fade = float.Parse(settings[3]);
                        monument.Radius = float.Parse(settings[2]);
                        monument.offset = ParseVector3(settings[1]);
                        monument.size = ParseVector3(settings[0]);
                    }
                    else
                    {
                        mountain.Fade = float.Parse(settings[3]);
                        mountain.offset = ParseVector3(settings[1]);
                        mountain.size = ParseVector3(settings[0]);
                    }
                }
            }

            //Methods the trainspiler points too
            public static TextureData HeightMapOverRide(TextureData heightData, Monument monument)
            {
                //Find by ResourcePath
                //Limit lowest value to set value.
                if (IsSwitchEnabled("height.flatlakes", false))
                {
                    switch (monument.heightmap.resourcePath)
                    {
                        case "assets/scenes/prefabs/unique environments/ue_lake_a/heighttexture.png":
                            return FlattenFloor(heightData, 15035);
                        case "assets/scenes/prefabs/unique environments/ue_lake_b/heighttexture.png":
                            return FlattenFloor(heightData, 14575);
                        case "assets/scenes/prefabs/unique environments/ue_lake_c/heighttexture.png":
                            return FlattenFloor(heightData, 14580);
                        case "assets/scenes/prefabs/unique environments/ue_oasis_a/heighttexture.png":
                            return FlattenFloor(heightData, 5845);
                        case "assets/scenes/prefabs/unique environments/ue_oasis_b/heighttexture.png":
                            return FlattenFloor(heightData, 5845);
                        case "assets/scenes/prefabs/unique environments/ue_oasis_c/heighttexture.png":
                            return FlattenFloor(heightData, 5845);
                    }
                }
                return OverrideMapData(monument.heightmap.Get(), monument.heightmap.resourcePath, monument, heightData);
            }

            public static TextureData HeightMapOverRideMountain(TextureData heightData, Mountain mountain)
            {
                if (IsSwitchEnabled("height.volcano", false))
                {
                    switch (mountain.heightmap.resourcePath)
                    {
                        case "assets/scenes/prefabs/mountain/mountain_1/heighttexture.png":
                            return Convert2Volcano(heightData, mountain, new Vector3(-28, -30));
                        case "assets/scenes/prefabs/mountain/mountain_2/heighttexture.png":
                            return Convert2Volcano(heightData, mountain, new Vector3(4, 51));
                        case "assets/scenes/prefabs/mountain/mountain_3/heighttexture.png":
                            return Convert2Volcano(heightData, mountain, new Vector3(159, -132));
                        case "assets/scenes/prefabs/mountain/mountain_4/heighttexture.png":
                            return Convert2Volcano(heightData, mountain, new Vector3(44, 169));
                        case "assets/scenes/prefabs/mountain/mountain_5/heighttexture.png":
                            return Convert2Volcano(heightData, mountain, new Vector3(-380, -115));
                    }
                }
                return OverrideMapDataMountain(mountain.heightmap.Get(), mountain.heightmap.resourcePath, mountain, heightData);
            }
            public static TextureData AlphaMapOverRide(TextureData alphaData, Monument monument)
            {
                return OverrideMapData(monument.alphamap.Get(), monument.alphamap.resourcePath, null, alphaData);
            }

            public static TextureData BiomeMapOverRide(TextureData biomeData, Monument monument)
            {
                return OverrideMapData(monument.biomemap.Get(), monument.biomemap.resourcePath, null, biomeData);
            }

            public static TextureData TopologyMapOverRide(TextureData topologyData, Monument monument)
            {
                return OverrideMapData(monument.topologymap.Get(), monument.topologymap.resourcePath, null, topologyData);
            }

            public static TextureData Splat0MapOverRide(TextureData splat0Data, Monument monument)
            {
                return OverrideMapData(monument.splatmap0.Get(), monument.splatmap0.resourcePath, null, splat0Data);
            }

            public static TextureData Splat1MapOverRide(TextureData splat1Data, Monument monument)
            {
                return OverrideMapData(monument.splatmap1.Get(), monument.splatmap1.resourcePath, null, splat1Data);
            }

            private static TextureData OverrideMapData(Texture2D texture, string path, Monument monument, TextureData originalData)
            {
#if DEBUG

            SaveMapData(path, texture);
#endif
                if (monument != null)
                {
#if DEBUG
                SavePrefabData(path, monument);
#else
                    LoadPrefabData(path, monument);
#endif
                }
                return LoadMapData(path, texture, originalData);
            }
            private static TextureData OverrideMapDataMountain(Texture2D texture, string path, Mountain mountain, TextureData originalData)
            {
#if DEBUG

                SaveMapData(path, texture);
#endif
                if (mountain != null)
                {
#if DEBUG
                    SavePrefabDataMountain(path, mountain);
#else
                    LoadPrefabData(path, mountain);
#endif
                }
                return LoadMapData(path, texture, originalData);
            }
            #endregion

            #region Methods
            public static void CutEdges(ref NativeArray<short> height, int pixels, short depth)
            {
                int HeightRes = TerrainGenerator.GetHeightMapRes();
                int minBound = pixels;
                int maxBound = HeightRes - pixels;
                // Fill the top and bottom rows with the specified depth
                for (int y = 0; y < minBound; y++) { Fill(height, y * HeightRes, HeightRes, depth); }
                for (int y = maxBound; y < HeightRes; y++) { Fill(height, y * HeightRes, HeightRes, depth); }

                // Left and right edges
                for (int y = minBound; y < maxBound; y++)
                {
                    int yOffset = y * HeightRes;
                    for (int x = 0; x < minBound; x++) // Left side
                    {
                        height[yOffset + x] = depth;
                    }
                    for (int x = maxBound; x < HeightRes; x++) // Right side
                    {
                        height[yOffset + x] = depth;
                    }
                }
            }

            // Burst-parallel DeepOcean replacement
            public static void DeepOcean_Burst(ref NativeArray<short> height, int radius, short depth, int trimDepth, int seaBed)
            {
                int resolution = TerrainGenerator.GetHeightMapRes();
                int len = height.Length;
                int hightradius = resolution * radius;
                int maxIndex = len - 1;

                // Snapshot original heights (read-only source for job)
                var src = new NativeArray<short>(len, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                NativeArray<short>.Copy(height, src);

                // Destination array (job writes here)
                var dst = new NativeArray<short>(len, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                var job = new DeepOceanBurstJob
                {
                    src = src,
                    dst = dst,
                    Resolution = resolution,
                    Radius = radius,
                    HighTRadius = hightradius,
                    Depth = depth,
                    TrimDepth = trimDepth,
                    SeaBed = seaBed,
                    MaxIndex = maxIndex
                };

                // schedule across whole linear index space (one Execute per index)
                JobHandle h = job.Schedule(len, 64);
                h.Complete();

                // copy result back to original array
                NativeArray<short>.Copy(dst, height);

                // dispose temporaries
                src.Dispose();
                dst.Dispose();
            }

            [BurstCompile]
            private struct DeepOceanBurstJob : IJobParallelFor
            {
                [ReadOnly] public NativeArray<short> src;
                [NativeDisableParallelForRestriction] public NativeArray<short> dst;

                [ReadOnly] public int Resolution;
                [ReadOnly] public int Radius;
                [ReadOnly] public int HighTRadius;
                [ReadOnly] public short Depth;
                [ReadOnly] public int TrimDepth;
                [ReadOnly] public int SeaBed;
                [ReadOnly] public int MaxIndex;

                public void Execute(int i)
                {
                    // Preserve edges/top/bottom rows exactly like original
                    if (i < (Resolution * 4) || i >= src.Length - (Resolution * 4))
                    {
                        dst[i] = src[i];
                        return;
                    }

                    short currentHeight = src[i];

                    // Ignore if height is above trimDepth or below seaBed (same checks as original)
                    if (currentHeight <= SeaBed || currentHeight > TrimDepth)
                    {
                        dst[i] = src[i];
                        return;
                    }

                    // precompute neighbor indices with clamping
                    int n1 = math.max(i - Radius, 0);
                    int n2 = math.min(i + Radius, MaxIndex);
                    int n3 = math.max(i - HighTRadius, 0);
                    int n4 = math.min(i + HighTRadius, MaxIndex);
                    int n5 = math.max(i - (HighTRadius + 1), 0);
                    int n6 = math.max(i - (HighTRadius - 1), 0);
                    int n7 = math.min(i + (HighTRadius + 1), MaxIndex);
                    int n8 = math.min(i + (HighTRadius - 1), MaxIndex);

                    // If any neighbor is >= TrimDepth, we do NOT set to deep
                    bool allBelow = (src[n1] < TrimDepth) & (src[n2] < TrimDepth) &
                                    (src[n3] < TrimDepth) & (src[n4] < TrimDepth) &
                                    (src[n5] < TrimDepth) & (src[n6] < TrimDepth) &
                                    (src[n7] < TrimDepth) & (src[n8] < TrimDepth);

                    if (allBelow)
                        dst[i] = Depth;
                    else
                        dst[i] = src[i];
                }
            }

            public static void ExtractZip(string outputFolder, byte[] ZipData)
            {
                try
                {
                    if (!Directory.Exists(outputFolder))
                        Directory.CreateDirectory(outputFolder);

                    using (var inputStream = new MemoryStream(ZipData))
                    using (var archive = new ZipArchive(inputStream, ZipArchiveMode.Read))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            string fullPath = Path.Combine(outputFolder, entry.FullName);

                            // Create directory structure if needed
                            string directory = Path.GetDirectoryName(fullPath);
                            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                                Directory.CreateDirectory(directory);

                            // Skip directories
                            if (string.IsNullOrEmpty(entry.Name))
                                continue;

                            entry.ExtractToFile(fullPath, overwrite: true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to extract ZIP: {ex}");
                }
            }

            public static void StartWebServer()
            {
                if (_listener != null) return;

                _listener = new HttpListener();
                foreach (var url in URL)
                {
                    try
                    {
                        _listener.Prefixes.Add(url);
                    }
                    catch { }
                }
                System.Net.ServicePointManager.Expect100Continue = false;
                _listener.Start();

                try
                {
                    if (File.Exists("restart"))
                    {
                        Library.ForcePageRefresh = true;
                        File.Delete("restart");
                    }
                    else
                    {
                        if (Library.IP == "localhost")
                        {
                            Process.Start(URL[0]);
                        }
                    }
                }
                catch { /* ignore if Process.Start fails on some environments */ }

                _listenerTask = Task.Run(async () =>
                {
                    while (_listener.IsListening)
                    {
                        try
                        {
                            var ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                            _ = HandleContext(ctx);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Webserver listener error: {ex}");
                        }
                    }
                });
            }

            private static async Task HandleContext(HttpListenerContext ctx)
            {
                try
                {
                    string path = ctx.Request.Url.AbsolutePath ?? "/";
                    if (CubesOnly)
                    {
                        if (path.StartsWith("/Cubes.map", StringComparison.OrdinalIgnoreCase))
                        {
                            await HandleCubesMap(ctx).ConfigureAwait(false);
                            return;
                        }
                        if (ctx.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
                        {
                            await ServeCubesHtml(ctx).ConfigureAwait(false);
                        }
                        else if (ctx.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                        {
                            await HandleCubesUpload(ctx).ConfigureAwait(false);
                        }
                        return;
                    }
                    if (path.StartsWith("/status/ackrefresh", StringComparison.OrdinalIgnoreCase))
                    {
                        ForcePageRefresh = false;
                        ctx.Response.StatusCode = 200;
                        await SafeWrite(ctx.Response, Encoding.UTF8.GetBytes("OK"), "text/plain");
                        return;
                    }
                    if (path.StartsWith("/status", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleStatus(ctx).ConfigureAwait(false);
                    }
                    else if (path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleFavIco(ctx).ConfigureAwait(false);
                    }
                    else if (AllowUpload && path.StartsWith("/upload", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ctx.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
                        {
                            await ServeUploadHtml(ctx).ConfigureAwait(false);
                        }
                        else if (ctx.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                        {
                            await HandleMapUpload(ctx).ConfigureAwait(false);
                        }
                    }
                    else if (path.StartsWith("/download", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleDownload(ctx).ConfigureAwait(false);
                    }
                    else if (path.StartsWith("/png2cubes", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ctx.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
                        {
                            await ServeCubesHtml(ctx).ConfigureAwait(false);
                        }
                        else if (ctx.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                        {
                            await HandleCubesUpload(ctx).ConfigureAwait(false);
                        }
                    }
                    else if (path.StartsWith("/jobs", StringComparison.OrdinalIgnoreCase))
                    {
                        if (path.EndsWith(".map", StringComparison.OrdinalIgnoreCase))
                        {
                            // Decode any URL-encoded characters (e.g. %20 → space)
                            string decodedPath = Uri.UnescapeDataString(path);

                            // Build the correct absolute file path
                            string fullFilePath = Path.Combine(
                                AppDomain.CurrentDomain.BaseDirectory,
                                decodedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
                            );

                            if (File.Exists(fullFilePath))
                            {
                                try
                                {
                                    byte[] fileData = File.ReadAllBytes(fullFilePath);
                                    string fileName = Path.GetFileName(fullFilePath);

                                    // Set download headers
                                    ctx.Response.AddHeader("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                                    await SafeWrite(ctx.Response, fileData, "application/octet-stream");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error serving map file: {ex}");
                                    ctx.Response.StatusCode = 500;
                                    byte[] msg = Encoding.UTF8.GetBytes("Error reading map file");
                                    await SafeWrite(ctx.Response, msg, "text/plain");
                                }
                            }
                            else
                            {
                                ctx.Response.StatusCode = 404;
                                byte[] msg = Encoding.UTF8.GetBytes("File not found");
                                await SafeWrite(ctx.Response, msg, "text/plain");
                            }
                        }
                        else
                        {
                            if (ctx.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
                            {
                                await ServeJobsHtml(ctx).ConfigureAwait(false);
                            }
                            else if (ctx.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                            {
                                await HandleJobsUpload(ctx).ConfigureAwait(false);
                            }
                        }
                    }
                    else if (path.StartsWith("/stopdelete", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleJobsStopAndDelete(ctx).ConfigureAwait(false);
                    }
                    else if (path.StartsWith("/restart", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Library.Generating)
                        {
                            Library.Restart = true;
                            await Library.HandleRestart(ctx).ConfigureAwait(false);
                            await Task.Delay(TimeSpan.FromSeconds(2.0));
                            Library._continueEvent.Set();
                        }
                        else
                        {
                            await Library.ServeHtml(ctx).ConfigureAwait(false);
                        }
                    }
                    else if (path.StartsWith("/quit", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleQuit(ctx).ConfigureAwait(false);
                    }
                    else if (path.StartsWith("/map.png", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleMapPng(ctx).ConfigureAwait(false);
                    }
                    else if (path.StartsWith("/Cubes.map", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleCubesMap(ctx).ConfigureAwait(false);
                    }
                    else if (ctx.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandlePost(ctx).ConfigureAwait(false);
                    }
                    else if (ctx.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
                    {
                        await ServeHtml(ctx).ConfigureAwait(false);
                    }
                    else
                    {
                        var buffer = Encoding.UTF8.GetBytes("Unsupported");
                        ctx.Response.ContentLength64 = buffer.Length;
                        await ctx.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                        ctx.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Context handling error: {ex}");
                    try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
                }
            }

            private static async Task HandlePost(HttpListenerContext ctx)
            {
                // Parse multipart/form-data if present, otherwise parse raw body (simple)
                try
                {
                    if (!string.IsNullOrEmpty(ctx.Request.ContentType) && ctx.Request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleMultipartForm(ctx).ConfigureAwait(false);
                    }
                    else
                    {
                        using (var sr = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
                        {
                            var raw = await sr.ReadToEndAsync().ConfigureAwait(false);
                            // if raw is key=value pairs, parse simple ones
                            var parts = raw.Split('&');
                            foreach (var p in parts)
                            {
                                var kv = p.Split('=');
                                if (kv.Length == 2)
                                {
                                    ConfigVars[WebUtility.UrlDecode(kv[0])] = WebUtility.UrlDecode(kv[1]);
                                }
                            }
                        }
                    }

                    Generating = true;
                    await ServeHtml(ctx).ConfigureAwait(false);
                    _continueEvent.Set(); // resume startup
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"POST handler error: {ex}");
                    try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
                }
            }

            private static async Task<byte[]> ReadFullyAsync(Stream input)
            {
                using (var ms = new MemoryStream())
                {
                    await input.CopyToAsync(ms).ConfigureAwait(false);
                    return ms.ToArray();
                }
            }

            // Basic but robust multipart parser for small uploads
            private static async Task HandleMultipartForm(HttpListenerContext ctx)
            {
                byte[] data = await ReadFullyAsync(ctx.Request.InputStream).ConfigureAwait(false);
                string contentType = ctx.Request.ContentType;
                var boundaryMatch = Regex.Match(contentType, "boundary=(.*)");
                if (!boundaryMatch.Success) { return; }
                var boundary = Encoding.UTF8.GetBytes("--" + boundaryMatch.Groups[1].Value);
                var segments = SplitOnBoundary(data, boundary);

                foreach (var seg in segments)
                {
                    if (seg.Length == 0) continue;
                    // headers end at \r\n\r\n
                    var headerEnd = IndexOf(seg, Encoding.UTF8.GetBytes("\r\n\r\n"));
                    if (headerEnd < 0) continue;
                    var headerBytes = seg.Take(headerEnd).ToArray();
                    var bodyBytes = seg.Skip(headerEnd + 4).ToArray();
                    var headers = Encoding.UTF8.GetString(headerBytes);

                    // content-disposition: form-data; name="field"; filename="..."
                    var nameMatch = Regex.Match(headers, "name=\"(?<name>[^\"]+)\"");
                    if (!nameMatch.Success) continue;
                    var fieldName = nameMatch.Groups["name"].Value;

                    var filenameMatch = Regex.Match(headers, "filename=\"(?<fn>[^\"]+)\"");
                    if (filenameMatch.Success)
                    {
                        // file content
                        if (fieldName == "height.png")
                        {
                            HeightPngData = bodyBytes;
                        }
                        else if (fieldName == "height.customprefabs")
                        {
                            PrefabsZipData = bodyBytes;
                        }
                        else if (fieldName == "wc.prefabblacklist" || fieldName == "wc.prefabwhitelist")
                        {
                            // allow file uploads for prefab lists too (interpreted as UTF8 text)
                            try
                            {
                                string txt = Encoding.UTF8.GetString(bodyBytes);
                                ConfigVars[fieldName] = txt;
                            }
                            catch { }
                        }
                        else
                        {
                            // unknown file, ignore or store by name
                            // store as base64 so it can be inspected if needed
                            ConfigVars[$"file.{fieldName}"] = Convert.ToBase64String(bodyBytes);
                        }
                    }
                    else
                    {
                        // normal form field—bodybytes are utf8 text
                        var val = Encoding.UTF8.GetString(bodyBytes).TrimEnd('\r', '\n');
                        ConfigVars[fieldName] = val;
                    }
                }

                // Validate server-side a subset of values:
                ValidateAndNormalizeConfigVars();
            }

            // Validate some important fields and normalize to ConfigVars
            private static void ValidateAndNormalizeConfigVars()
            {
                // map.size
                if (ConfigVars.TryGetValue("map.size", out var ms))
                {
                    if (!uint.TryParse(ms, out var msv))
                    {
                        ConfigVars.Remove("map.size");
                    }
                }

                // map.seed
                if (ConfigVars.TryGetValue("map.seed", out var sd))
                {
                    if (!uint.TryParse(sd, out var sdv))
                    {
                        Console.WriteLine($"Invalid map.seed '{sd}', removing.");
                        ConfigVars.Remove("map.seed");
                    }
                    if (sdv == 0)
                    {
                        ConfigVars["map.seed"] = ((uint)UnityEngine.Random.Range(uint.MinValue, uint.MaxValue)).ToString();
                        Console.WriteLine("[Using Random Seed: " + ConfigVars["map.seed"] + "]");
                    }
                }

                // Validate world config percentages: ensure floats 0..1 (or >0) and normalize names
                string[] percentNames = new string[] { "wc.tier0", "wc.tier1", "wc.tier2", "wc.biome.arid", "wc.biome.temperate", "wc.biome.tundra", "wc.biome.arctic", "wc.biome.jungle" };
                foreach (var name in percentNames)
                {
                    if (ConfigVars.TryGetValue(name, out var raw))
                    {
                        if (float.TryParse(raw, out var f))
                        {
                            if (f < 0f || float.IsNaN(f) || float.IsInfinity(f))
                            {
                                ConfigVars.Remove(name);
                            }
                            else
                            {
                                // keep the value but clamp to [0,1] for biomes/tiers
                                if (f > 1f) f = 1f;
                                ConfigVars[name] = f.ToString("0.##");
                            }
                        }
                        else
                        {
                            ConfigVars.Remove(name);
                        }
                    }
                }

                // Generation toggles: ensure 'true'/'false'
                string[] toggles = new string[] { "wc.mainroads", "wc.sideroads", "wc.trails", "wc.rivers", "wc.powerlines", "wc.aboverails", "wc.belowrails", "wc.underwaterlabs" };
                foreach (var t in toggles)
                {
                    if (ConfigVars.TryGetValue(t, out var v))
                    {
                        if (!bool.TryParse(v, out var b))
                        {
                            // accept '1'/'0' or 'on'/'off'
                            if (v == "1" || v.Equals("on", StringComparison.OrdinalIgnoreCase)) ConfigVars[t] = "true";
                            else if (v == "0" || v.Equals("off", StringComparison.OrdinalIgnoreCase)) ConfigVars[t] = "false";
                            else ConfigVars.Remove(t);
                        }
                        else
                        {
                            ConfigVars[t] = b ? "true" : "false";
                        }
                    }
                }

                // Prefab lists: accept CSV or JSON arrays; normalize to comma separated string
                NormalizePrefabList("wc.prefabblacklist");
                NormalizePrefabList("wc.prefabwhitelist");
            }

            private static void NormalizePrefabList(string key)
            {
                if (!ConfigVars.TryGetValue(key, out var val)) return;
                val = val.Trim();
                if (val.StartsWith("[") && val.EndsWith("]"))
                {
                    // try parse JSON array shallowly
                    try
                    {
                        var items = Regex.Matches(val, "\"([^\"]+)\"").Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
                        ConfigVars[key] = string.Join(",", items);
                        return;
                    }
                    catch { }
                }
                // else keep CSV but normalize whitespace
                var parts = val.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToArray();
                ConfigVars[key] = string.Join(",", parts);
            }

            private static async Task HandleMapUpload(HttpListenerContext ctx)
            {
                try
                {
                    string DownloadURL = "";
                    byte[] data = await ReadFullyAsync(ctx.Request.InputStream).ConfigureAwait(false);

                    string contentType = ctx.Request.ContentType;
                    var boundaryMatch = Regex.Match(contentType, "boundary=(.*)");
                    if (!boundaryMatch.Success)
                    {
                        ctx.Response.StatusCode = 400;
                        var bytes2 = Encoding.UTF8.GetBytes("No boundary found");
                        await ctx.Response.OutputStream.WriteAsync(bytes2, 0, bytes2.Length);
                        ctx.Response.Close();
                        return;
                    }

                    var boundary = Encoding.UTF8.GetBytes("--" + boundaryMatch.Groups[1].Value);
                    var segments = SplitOnBoundary(data, boundary);

                    foreach (var seg in segments)
                    {
                        if (seg.Length == 0) continue;

                        int headerEnd = IndexOf(seg, Encoding.UTF8.GetBytes("\r\n\r\n"));
                        if (headerEnd < 0) continue;

                        var headerBytes = seg.Take(headerEnd).ToArray();
                        var bodyBytes = seg.Skip(headerEnd + 4).ToArray();

                        // Trim trailing CRLF (\r\n) for .NET 4.8
                        while (bodyBytes.Length >= 2 &&
                               bodyBytes[bodyBytes.Length - 2] == (byte)'\r' &&
                               bodyBytes[bodyBytes.Length - 1] == (byte)'\n')
                        {
                            byte[] tmp = new byte[bodyBytes.Length - 2];
                            Array.Copy(bodyBytes, tmp, tmp.Length);
                            bodyBytes = tmp;
                        }

                        string headers = Encoding.UTF8.GetString(headerBytes);
                        var filenameMatch = Regex.Match(headers, "filename=\"(?<fn>[^\"]+)\"");
                        if (!filenameMatch.Success) continue;

                        string fn = filenameMatch.Groups["fn"].Value;
                        if (!fn.EndsWith(".map", StringComparison.OrdinalIgnoreCase)) continue;

                        var method = typeof(MapUploader).GetMethod("UploadMapImpl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                        if (method == null) { continue; }

                        using (var ms = new MemoryStream(bodyBytes))
                        {
                            var task = (Task<string>)method.Invoke(null, new object[] { ms, fn });
                            string text = await task;
                            if (text != null)
                            {
                                DownloadURL = text;
                                Console.WriteLine("[Rust.MapCache] Map uploaded to backend: " + text);
                            }
                        }
                    }

                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "text/plain";
                    var bytes = Encoding.UTF8.GetBytes(DownloadURL);
                    await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                    ctx.Response.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Upload error: {ex}");
                    ctx.Response.StatusCode = 500;
                    var bytes = Encoding.UTF8.GetBytes("Upload failed");
                    await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                    ctx.Response.Close();
                }
            }

            private static async Task HandleCubesUpload(HttpListenerContext ctx)
            {
                string DownloadURL = "";
                try
                {
                    if (!ctx.Request.ContentType?.StartsWith("multipart/form-data") ?? true)
                        throw new Exception("Invalid content type.");

                    // Extract boundary
                    var boundary = ctx.Request.ContentType.Split(new string[] { "boundary=" }, StringSplitOptions.None)[1];
                    var boundaryBytes = Encoding.UTF8.GetBytes("--" + boundary);

                    // Read full request body
                    var memoryStream = new MemoryStream();
                    await ctx.Request.InputStream.CopyToAsync(memoryStream);
                    var requestData = memoryStream.ToArray();

                    // Initialize variables
                    List<string> selectedColors = new List<string>();
                    byte[] imageBytes = Array.Empty<byte>();
                    VectorData scale = new VectorData();
                    int width = 16, height = 16, likeness = 20;
                    float smoothing = 2;

                    int pos = 0;
                    while (pos < requestData.Length)
                    {
                        int boundaryIndex = IndexOf(requestData, boundaryBytes, pos);
                        if (boundaryIndex == -1) break;

                        int headersStart = boundaryIndex + boundaryBytes.Length;
                        if (requestData.Length > headersStart + 2 && requestData[headersStart] == '\r' && requestData[headersStart + 1] == '\n')
                            headersStart += 2;

                        int headersEnd = IndexOf(requestData, Encoding.UTF8.GetBytes("\r\n\r\n"), headersStart);
                        if (headersEnd == -1) break;

                        string headers = Encoding.UTF8.GetString(requestData, headersStart, headersEnd - headersStart);
                        int contentStart = headersEnd + 4;

                        int nextBoundary = IndexOf(requestData, boundaryBytes, contentStart);
                        if (nextBoundary == -1) nextBoundary = requestData.Length;

                        // Trim trailing CRLF before boundary
                        int contentEnd = nextBoundary;
                        while (contentEnd > contentStart && (requestData[contentEnd - 1] == '\n' || requestData[contentEnd - 1] == '\r'))
                            contentEnd--;

                        int contentLength = contentEnd - contentStart;
                        if (contentLength < 0) contentLength = 0;

                        byte[] content = new byte[contentLength];
                        Array.Copy(requestData, contentStart, content, 0, contentLength);

                        // Handle fields
                        if (headers.Contains("name=\"colors\""))
                            selectedColors.Add(Encoding.UTF8.GetString(content).Trim());
                        else if (headers.Contains("name=\"pngfile\""))
                            imageBytes = content;
                        else if (headers.Contains("name=\"scaleX\"") && float.TryParse(Encoding.UTF8.GetString(content), out float sx))
                            scale.x = sx;
                        else if (headers.Contains("name=\"scaleY\"") && float.TryParse(Encoding.UTF8.GetString(content), out float sy))
                            scale.y = sy;
                        else if (headers.Contains("name=\"scaleZ\"") && float.TryParse(Encoding.UTF8.GetString(content), out float sz))
                            scale.z = sz;
                        else if (headers.Contains("name=\"height\"") && int.TryParse(Encoding.UTF8.GetString(content), out int h))
                            width = h;
                        else if (headers.Contains("name=\"width\"") && int.TryParse(Encoding.UTF8.GetString(content), out int w))
                            height = w;
                        else if (headers.Contains("name=\"smoothing\"") && float.TryParse(Encoding.UTF8.GetString(content), out float sm))
                            smoothing = sm;
                        else if (headers.Contains("name=\"likeness\"") && int.TryParse(Encoding.UTF8.GetString(content), out int lk))
                            likeness = lk;

                        pos = nextBoundary;
                    }

                    // Validate inputs
                    var predefinedColors = Library.GetPredefinedColors(selectedColors);
                    if (predefinedColors.Count < 2)
                    {
                        DownloadURL = "Error: You must have at least 2 colors selected!";
                    }
                    else if (imageBytes == null || imageBytes.Length == 0)
                    {
                        DownloadURL = "Error: No image file provided or file is corrupted!";
                    }
                    else
                    {
                        // Process image safely
                        DownloadURL = Library.PopulatePrefabs(
                            scale,
                            predefinedColors,
                            ProcessImage(imageBytes, width, height, smoothing, true),
                            likeness);
                    }
                }
                catch (Exception ex)
                {
                    DownloadURL = $"Error processing image: {ex.Message}";
                }

                // Send response
                try
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "text/plain";
                    var bytes = Encoding.UTF8.GetBytes(DownloadURL);
                    await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }
                finally
                {
                    ctx.Response.Close();
                }
            }

            public static Bitmap ProcessImage(byte[] imageBytes, int targetWidth, int targetHeight, double smoothing, bool rotate)
            {
                using (var ms = new MemoryStream(imageBytes))
                using (var original = new Bitmap(ms))
                {
                    // Optionally rotate 90°
                    Bitmap img = rotate ? RotateBitmap(original, RotateFlipType.Rotate90FlipNone) : new Bitmap(original);

                    // Optionally resize
                    if (img.Width != targetWidth || img.Height != targetHeight)
                    {
                        img = ResizeImage(img, targetWidth, targetHeight);
                    }

                    // Limit maximum size to 1024×1024
                    if (img.Width > 1024 || img.Height > 1024)
                    {
                        double scale = Math.Min(1024.0 / img.Width, 1024.0 / img.Height);
                        int newW = (int)(img.Width * scale);
                        int newH = (int)(img.Height * scale);
                        img = ResizeImage(img, newW, newH);
                    }

                    // Optional blur
                    if (smoothing > 0.5)
                    {
                        img = GaussianBlur(img, (float)smoothing);
                    }

                    return img;
                }
            }

            private static Bitmap RotateBitmap(Bitmap src, RotateFlipType rotateFlipType)
            {
                Bitmap rotated = (Bitmap)src.Clone();
                rotated.RotateFlip(rotateFlipType);
                return rotated;
            }

            private static Bitmap ResizeImage(Bitmap src, int width, int height)
            {
                Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(result))
                {
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.DrawImage(src, 0, 0, width, height);
                }
                return result;
            }

            private static Bitmap GaussianBlur(Bitmap src, float radius)
            {
                // Very simple box blur approximation to Gaussian for speed
                Bitmap blurred = new Bitmap(src.Width, src.Height);
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(blurred))
                {
                    using (var attributes = new ImageAttributes())
                    {
                        attributes.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        for (int i = 0; i < radius; i++) { g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attributes); }
                    }
                }
                return blurred;
            }

            private static async Task HandleStatus(HttpListenerContext ctx)
            {
                var runtime = RoundSeconds(DateTime.Now - _serverStart);
                bool done = _savedFilePath != null;
                var payload = new
                {
                    uptime = runtime.ToString(),
                    console = log.GetLogs(),
                    savedFilePath = done,
                    generating = Generating,
                    refreshpage = ForcePageRefresh ? 1 : 0
                };
                await SafeWrite(ctx.Response, Encoding.UTF8.GetBytes(JsonUtilityToString(payload)), "application/json");
            }

            private static string JsonUtilityToString(object o)
            {
                // Very small serializer for strings/bools only used here
                if (o is null) return "{}";
                var props = o.GetType().GetProperties();
                var sb = new StringBuilder();
                sb.Append("{");
                bool first = true;
                foreach (var p in props)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    var name = p.Name;
                    var val = p.GetValue(o);
                    if (val is string s)
                    {
                        sb.Append($"\"{name}\":\"{EscapeJson(s)}\"");
                    }
                    else if (val is bool b)
                    {
                        sb.Append($"\"{name}\":{b.ToString().ToLower()}");
                    }
                    else
                    {
                        sb.Append($"\"{name}\":\"{EscapeJson(val?.ToString() ?? string.Empty)}\"");
                    }
                }
                sb.Append("}");
                return sb.ToString();
            }

            private static string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

            private static async Task HandleDownload(HttpListenerContext ctx)
            {
                try
                {
                    if (_savedFilePath == null || !File.Exists(_savedFilePath))
                    {
                        ctx.Response.StatusCode = 404;
                        ctx.Response.Close();
                        return;
                    }
                    ctx.Response.AddHeader("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(_savedFilePath)}\"");
                    await SafeWrite(ctx.Response, File.ReadAllBytes(_savedFilePath), "application/octet-stream");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Download error: {ex}");
                    try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
                }
            }

            private static string WhitePNG() { return "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII="; }

            private static async Task HandleMapPng(HttpListenerContext ctx)
            {
                byte[] whitePng = Convert.FromBase64String(WhitePNG());
                if (File.Exists("preview.png")) { whitePng = File.ReadAllBytes("preview.png"); }
                await SafeWrite(ctx.Response, whitePng, "image/png");
            }

            private static async Task HandleCubesMap(HttpListenerContext ctx)
            {
                byte[] Data = null;
                if (File.Exists("Cubes.map")) { Data = File.ReadAllBytes("Cubes.map"); }
                await SafeWrite(ctx.Response, Data, "application/octet-stream");
            }

            private static async Task HandleFavIco(HttpListenerContext ctx) { await SafeWrite(ctx.Response, favico, "image/x-icon"); }

            private static async Task HandleQuit(HttpListenerContext ctx)
            {
                string password = null;
                string action = "shutdown"; // default
                string query = ctx.Request.Url.Query;
                if (!string.IsNullOrEmpty(query) && query.StartsWith("?"))
                {
                    string[] pairs = query.Substring(1).Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string pair in pairs)
                    {
                        string[] kv = pair.Split(new[] { '=' }, 2);
                        if (kv.Length != 2)
                            continue;

                        string key = kv[0];
                        string value = Uri.UnescapeDataString(kv[1]);

                        if (key == "password")
                            password = value;
                        else if (key == "action")
                            action = value.ToLowerInvariant();
                    }
                }

                if (password == QuitPassword && action == "restart")
                {
                    Restart = true;
                    await HandleRestart(ctx).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(2.0));
                    RestartServer();
                    ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "quit", new object[0]);
                    _continueEvent.Set();
                    System.Environment.Exit(0);
                    return;
                }

                if (password == QuitPassword || (QuitPassword == "quit" && IP == "localhost"))
                {
                    await SafeWrite(ctx.Response, QuitPage, "text/html");
                    ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "quit", new object[0]);
                    _continueEvent.Set();
                    System.Environment.Exit(0);
                }
                else
                {
                    // Password input page
                    ctx.Response.StatusCode = 401;
                    await SafeWrite(ctx.Response, PasswordPage, "text/html");
                }
            }

            private static async Task HandleRestart(HttpListenerContext ctx) { await SafeWrite(ctx.Response, RestartPage, "text/html"); }

            private static async Task ServeUploadHtml(HttpListenerContext ctx) { await SafeWrite(ctx.Response, UploadPage, "text/html"); }

            private static async Task ServeCubesHtml(HttpListenerContext ctx) { await SafeWrite(ctx.Response, PNG2CubesPage, "text/html"); }

            private static async Task HandleJobsUpload(HttpListenerContext ctx)
            {
                try
                {
                    if (!ctx.Request.HasEntityBody)
                    {
                        ctx.Response.StatusCode = 400;
                        await SafeWrite(ctx.Response, Encoding.UTF8.GetBytes("No file uploaded"), "text/html");
                        return;
                    }

                    // Read the incoming data stream (the .zip file)
                    using (var ms = new MemoryStream())
                    {
                        await ctx.Request.InputStream.CopyToAsync(ms);
                        var JobZipData = ms.ToArray();
                        if (!Directory.Exists("jobs")) { Directory.CreateDirectory("jobs"); } //Create CustomPrefabs folder
                        string extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jobs");
                        try
                        {
                            if (!Directory.Exists(extractPath)) { Directory.CreateDirectory(extractPath); }
                            ExtractZip(extractPath, JobZipData);
                            RestartServer();
                            await HandleRestart(ctx).ConfigureAwait(false);
                            ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "quit", new object[0]);
                            _continueEvent.Set();
                            System.Environment.Exit(0);
                            return;
                        }
                        catch (Exception ex) { Console.WriteLine($"An error occurred: {ex.Message}"); }
                        await ServeJobsHtml(ctx).ConfigureAwait(false);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    if (Directory.Exists("jobs")) { Directory.Delete("jobs", true); }
                    ctx.Response.StatusCode = 500;
                    await SafeWrite(ctx.Response, Encoding.UTF8.GetBytes("Error uploading jobs: " + ex.Message), "text/html");
                }
            }

            private static async Task ServeJobsPasswordHtml(HttpListenerContext ctx)
            {
                await SafeWrite(ctx.Response, JobsPasswordPage, "text/html");
            }

            private static async Task ServeJobsHtml(HttpListenerContext ctx)
            {
                string html = Encoding.UTF8.GetString(JobsPage);
                if (!HasPendingJobs) { html = html.Replace(@"<button class=""btn"" id=""deleteStopBtn"">Stop All Jobs & Delete</button>", ""); }
                await SafeWrite(ctx.Response, Encoding.UTF8.GetBytes(html.Replace("<$JOBSDATA$>", GetJobs())), "text/html");
            }

            private static async Task HandleJobsStopAndDelete(HttpListenerContext ctx)
            {
                string password = null;
                string query = ctx.Request.Url.Query;
                if (!string.IsNullOrEmpty(query) && query.StartsWith("?"))
                {
                    string[] pairs = query.Substring(1).Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string pair in pairs)
                    {
                        string[] kv = pair.Split(new[] { '=' }, 2);
                        if (kv.Length != 2) { continue; }
                        string key = kv[0];
                        string value = Uri.UnescapeDataString(kv[1]);
                        if (key == "password") { password = value; }
                    }
                }
                if (password == QuitPassword || (QuitPassword == "quit" && IP == "localhost"))
                {
                    try
                    {
                        if (Directory.Exists("jobs")) { Directory.Delete("jobs", true); }
                        RestartServer();
                        await HandleRestart(ctx).ConfigureAwait(false);
                        ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "quit", new object[0]);
                        Library._continueEvent.Set();
                        System.Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        ctx.Response.StatusCode = 500;
                        await SafeWrite(ctx.Response, Encoding.UTF8.GetBytes("Error stopping jobs: " + ex.Message), "text/html");
                    }
                    return;
                }
                await ServeJobsPasswordHtml(ctx).ConfigureAwait(false);
            }

            private static async Task ServeHtml(HttpListenerContext ctx)
            {
                if (HasPendingJobs)
                {
                    await ServeJobsHtml(ctx).ConfigureAwait(false);
                }
                else
                {
                    await SafeWrite(ctx.Response, MainPage, "text/html");
                }
            }

            public static async Task SafeWrite(HttpListenerResponse resp, byte[] data, string contentType = "application/octet-stream")
            {
                try
                {
                    if (resp == null || !resp.OutputStream.CanWrite) { return; }
                    resp.ContentType = contentType;
                    resp.ContentLength64 = data.Length;
                    resp.OutputStream.Write(data, 0, data.Length);
                    resp.OutputStream.Flush();
                }
                catch (HttpListenerException) { /* client disconnected, safe to ignore */ }
                catch (IOException) { /* socket closed, safe to ignore */ }
                catch (ObjectDisposedException) { /* stream already closed */ }
                catch (Exception) { }
                finally
                {
                    try { resp.OutputStream.Close(); } catch { }
                    try { resp.Close(); } catch { }
                }
            }

            private static readonly object JobLock = new object();

            public static string GetJobs()
            {
                lock (JobLock)
                {
                    if (!Directory.Exists("jobs"))
                    {
                        Library.HasPendingJobs = false;
                        return string.Empty;
                    }

                    List<Job> completedJobs = new List<Job>();
                    List<Job> localPending = new List<Job>(); // use local list first
                    string[] folders = Directory.GetDirectories("jobs", "*", SearchOption.AllDirectories);

                    pendingJobs.Clear();

                    foreach (var folder in folders)
                    {
                        string jobFile = Path.Combine(folder, "job.json");
                        if (!File.Exists(jobFile))
                            continue;

                        string statusFile = Path.Combine(folder, "status.json");
                        Job job = null;

                        if (File.Exists(statusFile))
                        {
                            try
                            {
                                string json = File.ReadAllText(statusFile);
                                job = JsonConvert.DeserializeObject<Job>(json);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error reading {statusFile}: {ex.Message}");
                            }
                        }

                        // Create new status if missing
                        if (job == null)
                        {
                            try
                            {
                                MapConfig mapConfig = JsonConvert.DeserializeObject<MapConfig>(File.ReadAllText(jobFile));
                                if (mapConfig != null)
                                {
                                    job = new Job
                                    {
                                        name = mapConfig.HeightName,
                                        size = mapConfig.MapSize.ToString(),
                                        seed = mapConfig.MapSeed.ToString(),
                                        status = "Pending",
                                        img = "",
                                        path = folder
                                    };

                                    try
                                    {
                                        string json = JsonConvert.SerializeObject(job, Formatting.Indented);
                                        File.WriteAllText(statusFile, json);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error creating {statusFile}: {ex.Message}");
                                    }
                                }
                            }
                            catch { }
                        }

                        if (job != null)
                        {
                            HasPendingJobs = true;
                            if (job.status == "Done")
                                completedJobs.Add(job);
                            else
                                localPending.Add(job);
                        }
                    }

                    // Replace pendingJobs with the newly built list atomically
                    pendingJobs = new List<Job>(localPending);

                    // Build HTML safely
                    var sb = new StringBuilder();

                    // Completed jobs
                    if (completedJobs.Count > 0)
                    {
                        sb.Append(@"<fieldset><legend>Completed Jobs</legend><div class=""job-list"">");
                        foreach (var job in completedJobs)
                        {
                            string[] mapFiles = Directory.GetFiles(job.path, "*.map", SearchOption.TopDirectoryOnly);
                            string mapname = mapFiles.Length > 0 ? Path.GetFileName(mapFiles[0]) : "";

                            sb.Append($@"
<div class=""job-item"">
    <img src =""data:image/png;base64,{job.img}"">
    <span>Name: {job.name} Size: {job.size}, Seed: {job.seed}</span>");

                            if (!string.IsNullOrEmpty(mapname))
                            {
                                string downloadLink = ($"{job.path}/{mapname}").Replace(" ", "%20");
                                sb.Append($@" <a href=""{downloadLink}"" class=""btn"" download>Download Map</a>");
                            }

                            sb.Append("</div>");
                        }
                        sb.Append("</div></fieldset>");
                    }

                    // Pending jobs
                    if (pendingJobs.Count > 0)
                    {
                        sb.Append(@"<fieldset><legend>Pending Jobs</legend><div class=""job-list"">");
                        foreach (var job in pendingJobs)
                        {
                            sb.Append($@"
<div class=""job-item"">
    <img src=""data:image/png;base64,{WhitePNG()}"">
    <span>Name: {job.name} Size: {job.size}, Seed: {job.seed}</span>
</div>");
                        }
                        sb.Append("</div></fieldset>");
                    }

                    return sb.ToString();
                }
            }

            private static TimeSpan RoundSeconds(TimeSpan span) => TimeSpan.FromSeconds(Math.Round(span.TotalSeconds));

            public static string PopulatePrefabs(VectorData scale, Dictionary<Color, uint> predefinedColors, Bitmap bitmap, int likeness)
            {
                var WorldSerialization = InitializeWorldSerialization();
                int width = bitmap.Width;
                int height = bitmap.Height;
                bool[,] processed = new bool[width, height];
                Color[,] colorMappedBitmap = ConvertBitmapToPredefinedColors(predefinedColors, bitmap);

                // Detect background color by sampling corners
                Color corner1 = colorMappedBitmap[0, 0];
                Color corner2 = colorMappedBitmap[width - 1, 0];
                Color corner3 = colorMappedBitmap[0, height - 1];
                Color corner4 = colorMappedBitmap[width - 1, height - 1];

                uint bgPrefabID = 0;
                if (ColorsMatch(corner1, corner2, 25) && ColorsMatch(corner1, corner3, 25) && ColorsMatch(corner1, corner4, 25))
                    predefinedColors.TryGetValue(corner1, out bgPrefabID);

                // Loop through pixels
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (processed[x, y]) continue;
                        Color pixelColor = colorMappedBitmap[x, y];
                        if (pixelColor.A < 100) continue;

                        if (!predefinedColors.TryGetValue(pixelColor, out uint prefabID)) continue;
                        if (bgPrefabID == prefabID) continue;

                        Rectangle rect = ExpandRectangle(x, y, pixelColor, processed, colorMappedBitmap, likeness);
                        float centerX = (rect.Left + rect.Right) / 2.0f;
                        float centerY = (rect.Top + rect.Bottom) / 2.0f;
                        float widthScale = rect.Width * scale.z;
                        float heightScale = rect.Height * scale.x;

                        VectorData position = new VectorData(
                            (float)Math.Round(centerY * scale.x, 3),
                            1,
                            (float)Math.Round(centerX * scale.z, 3));

                        VectorData newScale = new VectorData(heightScale, scale.y, widthScale);
                        WorldSerialization.world.prefabs.Add(new PrefabData
                        {
                            category = "Decor",
                            id = prefabID,
                            position = position,
                            rotation = new VectorData(0, 0, 0),
                            scale = newScale
                        });
                    }
                }

                // Add single background prefab
                if (bgPrefabID != 0)
                {
                    RectInt bgBounds = new RectInt(0, 0, width, height);
                    float bgCenterX = (bgBounds.xMin + bgBounds.xMax) / 2f;
                    float bgCenterY = (bgBounds.yMin + bgBounds.yMax) / 2f;
                    float bgWidthScale = bgBounds.width * scale.z;
                    float bgHeightScale = bgBounds.height * scale.x;

                    VectorData bgPosition = new VectorData(
                        (float)Math.Round(bgCenterY * scale.x, 3),
                        0.99f,
                        (float)Math.Round(bgCenterX * scale.z, 3));

                    VectorData bgScale = new VectorData(bgHeightScale, scale.y, bgWidthScale);

                    WorldSerialization.world.prefabs.Add(new PrefabData
                    {
                        category = "background",
                        id = bgPrefabID,
                        position = bgPosition,
                        rotation = new VectorData(0, 0, 0),
                        scale = bgScale
                    });
                }
                png2cubes = true;
                if (File.Exists("Cubes.map")) { File.Delete("Cubes.map"); }
                WorldSerialization.Save("Cubes.map");
                png2cubes = false;
                return "/Cubes.map";
            }

            private static bool ColorsMatch(Color c1, Color c2, int tolerance = 10)
            {
                int dr = c1.R - c2.R;
                int dg = c1.G - c2.G;
                int db = c1.B - c2.B;
                return (dr * dr + dg * dg + db * db) <= (tolerance * tolerance);
            }

            private static Rectangle ExpandRectangle(int startX, int startY, Color targetColor, bool[,] processed, Color[,] colorMappedBitmap, int likeness = 10)
            {
                int maxWidth = 1, maxHeight = 1;
                int width = colorMappedBitmap.GetLength(0);
                int height = colorMappedBitmap.GetLength(1);
                while (startX + maxWidth < width && !processed[startX + maxWidth, startY]
                       && colorMappedBitmap[startX + maxWidth, startY] != Color.Transparent
                       && ColorsMatch(colorMappedBitmap[startX + maxWidth, startY], targetColor, likeness))
                {
                    maxWidth++;
                }

                bool canExpand = true;
                while (startY + maxHeight < height && canExpand)
                {
                    for (int i = 0; i < maxWidth; i++)
                    {
                        if (processed[startX + i, startY + maxHeight]
                            || colorMappedBitmap[startX + i, startY + maxHeight] == Color.Transparent
                            || !ColorsMatch(colorMappedBitmap[startX + i, startY + maxHeight], targetColor, likeness))
                        {
                            canExpand = false;
                            break;
                        }
                    }
                    if (canExpand) maxHeight++;
                }
                for (int i = 0; i < maxWidth; i++)
                {
                    for (int j = 0; j < maxHeight; j++)
                    {
                        processed[startX + i, startY + j] = true;
                    }
                }

                return new Rectangle(startX, startY, maxWidth, maxHeight);
            }

            private static Color[,] ConvertBitmapToPredefinedColors(Dictionary<Color, uint> predefinedColors, Bitmap bitmap)
            {
                int width = bitmap.Width, height = bitmap.Height;
                Color[,] newBitmap = new Color[width, height];

                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = bitmapData.Stride;
                IntPtr scan0 = bitmapData.Scan0;
                byte[] pixels = new byte[stride * height];
                Marshal.Copy(scan0, pixels, 0, pixels.Length);
                bitmap.UnlockBits(bitmapData);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * stride) + (x * 4);
                        Color originalColor = Color.FromArgb(pixels[index + 3], pixels[index + 2], pixels[index + 1], pixels[index]);

                        if (originalColor.A < 100)
                        {
                            newBitmap[x, y] = Color.Transparent;
                        }
                        else
                        {
                            newBitmap[x, y] = GetClosestColor(originalColor, predefinedColors);
                        }
                    }
                }
                return newBitmap;
            }

            private static Color GetClosestColor(Color target, Dictionary<Color, uint> predefinedColors)
            {
                double bestDistance = double.MaxValue;
                Color bestMatch = Color.Empty;

                foreach (var color in predefinedColors.Keys)
                {
                    int dr = target.R - color.R;
                    int dg = target.G - color.G;
                    int db = target.B - color.B;
                    double distance = dr * dr + dg * dg + db * db; // Euclidean distance in RGB

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestMatch = color;
                    }
                }

                return bestMatch;
            }

            public static void RemoveTopology(bool removeRoad, bool removeRail)
            {
                if (!removeRoad && !removeRail) { return; }// Nothing to remove
                int resolution = TerrainGenerator.GetSplatMapRes();
                int roadMask = 2048;
                int railMask = 524288;
                int removeMask = 0;

                if (removeRoad) removeMask |= roadMask;
                if (removeRail) removeMask |= railMask;

                int keepMask = ~removeMask;
                bool updated = false;

                Console.WriteLine($"[Removing Topology - Road: {removeRoad}, Rail: {removeRail}]");
                for (int z = 0; z < resolution; z++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        if (TerrainMeta.TopologyMap.GetTopology(x, z, removeMask)) // Only modify if one of the targeted bits is present
                        {
                            int currentTopo = TerrainMeta.TopologyMap.GetTopology(x, z);
                            TerrainMeta.TopologyMap.SetTopology(x, z, currentTopo & keepMask);
                            updated = true;
                        }
                    }
                }

                // Update the serialized topology map if changes were made
                if (updated)
                {
                    TryPersistMapBytes(TerrainMeta.TopologyMap.ToByteArray(), "topology");
                }
            }

            public static void ModOceanSplatBiome(bool splat = true)
            {
                int resolution = TerrainGenerator.GetHeightMapRes();
                float halfWorld = World.Size / 2f;
                for (int y = 0; y < resolution; y++)
                {
                    int yOffset = y * resolution;

                    for (int x = 0; x < resolution; x++)
                    {
                        int currentIndex = yOffset + x;

                        // Convert map position to world position
                        Vector3 worldPos = new Vector3((x / (float)resolution) * World.Size - halfWorld, 0f, (y / (float)resolution) * World.Size - halfWorld);

                        // Skip if not ocean or is river
                        try
                        {
                            if ((TerrainMeta.TopologyMap.GetTopology(worldPos) & (int)TerrainTopology.OCEAN) == 0) { continue; }
                            if ((TerrainMeta.TopologyMap.GetTopology(worldPos) & (int)TerrainTopology.RIVER) != 0) { continue; }
                        }
                        catch
                        {
                            continue;
                        }

                        if (splat)
                        {
                            // Set the terrain splat at this world position to sand
                            TerrainMeta.SplatMap.SetSplat(worldPos, TerrainSplat.SAND);
                        }
                        else
                        {
                            // Set the terrain biome at this world position to normal
                            TerrainMeta.BiomeMap.SetBiome(worldPos, TerrainBiome.TEMPERATE);
                        }
                    }
                }
            }

            public static void ModArcticToTundraGrassBelowHeight(float heightThreshold)
            {
                int resolution = TerrainGenerator.GetSplatMapRes();
                float halfWorld = World.Size / 2f;
                bool changedBiome = false;
                bool changedSplat = false;
                for (int z = 0; z < resolution; z++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        Vector3 worldPos = new Vector3((x / (float)resolution) * World.Size - halfWorld, 0f, (z / (float)resolution) * World.Size - halfWorld);
                        float h = TerrainMeta.HeightMap.GetHeight(worldPos);
                        if (h >= heightThreshold) { continue; }
                        try
                        {
                            if ((TerrainMeta.SplatMap.GetSplat(worldPos, TerrainSplat.SNOW) > 0))
                            {
                                TerrainMeta.SplatMap.SetSplat(worldPos, TerrainSplat.GRASS);
                                changedSplat = true;
                            }
                            if ((TerrainMeta.BiomeMap.GetBiome(worldPos, TerrainBiome.ARCTIC) > 0))
                            {
                                TerrainMeta.BiomeMap.SetBiome(worldPos, TerrainBiome.TUNDRA);
                                changedBiome = true;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                if (changedBiome)
                {
                    TryPersistMapBytes(TerrainMeta.BiomeMap.ToByteArray(), "biome");
                }

                if (changedSplat)
                {
                    TryPersistMapBytes(TerrainMeta.SplatMap.ToByteArray(), "splat");
                }
            }

            // helper - tries to call ToByteArray() on the map instance and write into World.Serialization.world.maps[name].data
            static void TryPersistMapBytes(byte[] data, string mapName)
            {
                try
                {
                    if (data == null) { return; }

                    // find the right world map entry and overwrite data
                    for (int i = World.Serialization.world.maps.Count - 1; i >= 0; i--)
                    {
                        var m = World.Serialization.world.maps[i];
                        if (m.name == mapName)
                        {
                            m.data = data;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to persist map '{mapName}': {ex}");
                }
            }

            public static void FlattenZeroArea(ref NativeArray<short> heightMap, short setheight, float zeroRadius)
            {
                int resolution = TerrainGenerator.GetHeightMapRes();
                int maxIndex = heightMap.Length - 1;
                float halfWorld = World.Size / 2f;
                Vector3 origin = Vector3.zero;
                for (int y = 0; y < resolution; y++)
                {
                    int yOffset = y * resolution;
                    for (int x = 0; x < resolution; x++)
                    {
                        int currentIndex = yOffset + x;
                        var worldPos = new Vector3((x / (float)resolution) * World.Size - halfWorld, 0f, (y / (float)resolution) * World.Size - halfWorld);
                        if (Vector3.Distance(worldPos, origin) <= zeroRadius)
                        {
                            if (heightMap[currentIndex] < setheight)
                            {
                                heightMap[currentIndex] = setheight;
                            }
                        }
                    }
                }
            }

            public static void FlattenBaysShores(ref NativeArray<short> heightMap, short waterLevel = 16360, short shoreDepth = 16160, int scanSteps = 128)
            {
                int resolution = TerrainGenerator.GetHeightMapRes();
                int maxIndex = heightMap.Length - 1;
                float halfWorld = World.Size / 2f;
                List<Vector3> Harbors = new List<Vector3>();
                foreach (var pd in World.Serialization.world.prefabs)
                {
                    if (pd.id == 779654519 || pd.id == 3348191966 || pd.id == 2958463062)
                    {
                        Harbors.Add(pd.position);
                    }
                }
                for (int y = 0; y < resolution; y++)
                {
                    int yOffset = y * resolution;
                    for (int x = 0; x < resolution; x++)
                    {
                        int currentIndex = yOffset + x;

                        // Skip if height is above water level
                        short currentHeight = heightMap[currentIndex];
                        if (currentHeight >= waterLevel) { continue; }

                        var worldpos = new Vector3((x / (float)resolution) * World.Size - halfWorld, 0f, (y / (float)resolution) * World.Size - halfWorld);
                        // Skip if not ocean
                        try
                        {
                            if ((TerrainMeta.TopologyMap.GetTopology(worldpos) & (int)TerrainTopology.OCEAN) == 0) { continue; }
                            if ((TerrainMeta.TopologyMap.GetTopology(worldpos) & (int)TerrainTopology.MONUMENT) != 0) { continue; }
                        }
                        catch { }

                        //Skip if close to Harbours
                        //Skip if within 200m of any Harbor
                        bool nearHarbor = false;
                        for (int i = 0; i < Harbors.Count; i++)
                        {
                            if (Vector3.Distance(worldpos, Harbors[i]) <= 200)
                            {
                                nearHarbor = true;
                                break;
                            }
                        }
                        if (nearHarbor) { continue; }

                        // Flatten areas between shore depth and water level
                        if (currentHeight > shoreDepth)
                        {
                            heightMap[currentIndex] = waterLevel;
                            { continue; }
                        }

                        if (Detections(heightMap, resolution, currentIndex, scanSteps, 1, maxIndex, waterLevel) > 5)
                            heightMap[currentIndex] = waterLevel;
                        else if (Detections(heightMap, resolution, currentIndex, scanSteps, 2, maxIndex, waterLevel) > 5)
                            heightMap[currentIndex] = waterLevel;
                        else if (Detections(heightMap, resolution, currentIndex, scanSteps, 4, maxIndex, waterLevel) > 5)
                            heightMap[currentIndex] = waterLevel;
                    }
                }

                // Smooth edges
                for (int i = 0; i < 20; i++)
                {
                    SmoothMap(ref heightMap, waterLevel + 8, Harbors, 200f); // 200m buffer
                }
            }

            [BurstCompile]
            public static void SmoothMap(ref NativeArray<short> height, int TrimDepth = 0, System.Collections.Generic.List<Vector3> harbors = null, float harborBuffer = 0f)
            {
                int HeightRes = TerrainGenerator.GetHeightMapRes();
                short SeaFloor = (short)(GetMin(height) + 5);
                int maxa = height.Length - 1;
                float halfWorld = World.Size / 2f;
                float worldSize = World.Size;

                NativeArray<Vector3> harborArray = default;
                if (harbors != null && harbors.Count > 0)
                {
                    harborArray = new NativeArray<Vector3>(harbors.Count, Allocator.Temp);
                    for (int i = 0; i < harbors.Count; i++)
                        harborArray[i] = harbors[i];
                }

                // Main loop (Burst will vectorize this even though it's serial)
                int offset = HeightRes - 10;
                for (int y = 0; y < HeightRes; y++)
                {
                    int index = y * HeightRes;

                    for (int x = 0; x < HeightRes; x++)
                    {
                        int i = index + x;

                        short cur = height[i];

                        // Skip trimming if outside trim/coast
                        if (TrimDepth != 0)
                        {
                            if (cur < SeaFloor || cur >= TrimDepth) continue;
                        }
                        else
                        {
                            if (y >= offset || x >= offset || y <= 10 || x <= 10) continue;
                        }

                        // Skip if near a harbor
                        if (harborArray.IsCreated && harborArray.Length > 0)
                        {
                            float3 worldPos = new float3(
                                (x / (float)HeightRes) * worldSize - halfWorld,
                                0f,
                                (y / (float)HeightRes) * worldSize - halfWorld
                            );
                            bool nearHarbor = false;
                            for (int h = 0; h < harborArray.Length; h++)
                            {
                                float3 d = worldPos - (float3)harborArray[h];
                                if (math.lengthsq(d) <= (harborBuffer * harborBuffer))
                                {
                                    nearHarbor = true;
                                    break;
                                }
                            }
                            if (nearHarbor) continue;
                        }

                        // Floor check
                        if (cur < SeaFloor)
                        {
                            height[i] = SeaFloor;
                            continue;
                        }

                        // Average smoothing
                        height[i] = GetAverage(
                            height[ValidMin(i - 1)],
                            height[ValidMax(i + 1, maxa)],
                            height[ValidMin(i - HeightRes)],
                            height[ValidMax(i + HeightRes, maxa)],
                            height[ValidMin(i - (HeightRes + 1))],
                            height[ValidMin(i - (HeightRes - 1))],
                            height[ValidMax(i + HeightRes + 1, maxa)],
                            height[ValidMax(i + (HeightRes - 1), maxa)]
                        );
                    }
                }

                if (harborArray.IsCreated)
                    harborArray.Dispose();
            }

            [BurstCompile]
            private static short GetMin(NativeArray<short> arr)
            {
                short m = short.MaxValue;
                for (int i = 0; i < arr.Length; i++)
                    if (arr[i] < m) m = arr[i];
                return m;
            }

            public static Bitmap SmoothCoastlineWithGradient(Bitmap source, byte coastThreshold = 15, int blurRadius = 4)
            {
                int width = source.Width;
                int height = source.Height;

                Bitmap result = new Bitmap(width, height, PixelFormat.Format24bppRgb);

                BitmapData sourceData = source.LockBits(new Rectangle(0, 0, width, height),
                                                        ImageLockMode.ReadOnly,
                                                        PixelFormat.Format24bppRgb);

                BitmapData resultData = result.LockBits(new Rectangle(0, 0, width, height),
                                                        ImageLockMode.WriteOnly,
                                                        PixelFormat.Format24bppRgb);

                int stride = sourceData.Stride;

                unsafe
                {
                    byte* srcPtr = (byte*)sourceData.Scan0;
                    byte* resPtr = (byte*)resultData.Scan0;

                    Parallel.For(0, height, y =>
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * stride + x * 3;
                            byte centerBrightness = srcPtr[idx]; // grayscale R == G == B

                            if (centerBrightness <= coastThreshold)
                            {
                                int total = 0;
                                int count = 0;

                                for (int dy = -blurRadius; dy <= blurRadius; dy++)
                                {
                                    int ny = y + dy;
                                    if (ny < 0 || ny >= height) continue;

                                    for (int dx = -blurRadius; dx <= blurRadius; dx++)
                                    {
                                        int nx = x + dx;
                                        if (nx < 0 || nx >= width) continue;

                                        int nIdx = ny * stride + nx * 3;
                                        total += srcPtr[nIdx]; // use R (grayscale)
                                        count++;
                                    }
                                }

                                byte blurred = (byte)(total / count);
                                resPtr[idx] = blurred;
                                resPtr[idx + 1] = blurred;
                                resPtr[idx + 2] = blurred;
                            }
                            else
                            {
                                resPtr[idx] = srcPtr[idx];
                                resPtr[idx + 1] = srcPtr[idx + 1];
                                resPtr[idx + 2] = srcPtr[idx + 2];
                            }
                        }
                    });
                }

                source.UnlockBits(sourceData);
                result.UnlockBits(resultData);

                return result;
            }


            private static Bitmap EnsureOrientation(Bitmap src)
            {
                int w = src.Width;
                int h = src.Height;
                Bitmap result = new Bitmap(w, h);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Rotate 90° counter-clockwise
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            result.SetPixel(y, w - 1 - x, src.GetPixel(x, y));
                    // Vertical flip
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            result.SetPixel(x, h - 1 - y, src.GetPixel(x, y));
                }
                else
                {
                    // Vertical flip
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            result.SetPixel(x, h - 1 - y, src.GetPixel(x, y));
                }
                return result;
            }

            public static void ConvertPNG2HeightMap(ref NativeArray<short> HeightData)
            {
                if (Library.HasPendingJobs && Library.pendingJobs.Count > 0)
                {
                    //Get root custom height first
                    string customheight = Path.Combine("jobs", "height.png");
                    if (File.Exists(customheight)) { HeightPngData = File.ReadAllBytes(customheight); }
                    //Get override from jobs folder
                    customheight = Path.Combine(Library.pendingJobs[0].path, "height.png");
                    if (File.Exists(customheight)) { HeightPngData = File.ReadAllBytes(customheight); }
                }
                if (HeightPngData == null) { return; }
                //Get Heigth Map Settings Switches
                var MinHeight = Math.Max(float.Parse(GetSwitch("height.min", "498")), 1);
                var MaxHeight = Math.Min(float.Parse(GetSwitch("height.max", "700")), 999);
                var Smoothing = int.Parse(GetSwitch("height.smooth", "20"));
                var OceanCuts = int.Parse(GetSwitch("height.cuts", "40"));
                var SeaBed = BitUtility.Float2Short(float.Parse(GetSwitch("height.floor", "465")) / 1000);
                var WaterLevel = BitUtility.Float2Short(float.Parse(GetSwitch("height.water", "500")) / 1000);
                var res = TerrainGenerator.GetHeightMapRes();
                string temp_WorldConfig = GetSwitch("height.bangle", "");
                if (!string.IsNullOrEmpty(temp_WorldConfig))
                {
                    if (int.TryParse(temp_WorldConfig, out int BiomeAxisAngle))
                    {
                        AccessTools.Property(typeof(TerrainMeta), nameof(TerrainMeta.BiomeAxisAngle)).SetValue(null, (float)BiomeAxisAngle);
                    }
                }

                temp_WorldConfig = GetSwitch("height.langle", "");
                if (!string.IsNullOrEmpty(temp_WorldConfig))
                {
                    if (int.TryParse(temp_WorldConfig, out int LootAxisAngle))
                    {
                        AccessTools.Property(typeof(TerrainMeta), nameof(TerrainMeta.LootAxisAngle)).SetValue(null, (float)LootAxisAngle);
                    }
                }

                Bitmap bmp;
                using (var ms = new MemoryStream(HeightPngData))
                {
                    bmp = new Bitmap(ms);
                }
                Bitmap newImage = new Bitmap(resizeImage(bmp, new Size(res, res)));
                newImage = EnsureOrientation(newImage);
                newImage = SmoothCoastlineWithGradient(newImage);
                var smin = (short)ScaleValue(MinHeight, 1, 999, 0, short.MaxValue);
                var smax = (short)ScaleValue(MaxHeight, 1, 999, 0, short.MaxValue);
                HeightData = new NativeArray<short>(res * res, Allocator.Persistent);
                //Read each pixel of image to short array
                for (var Ycount = 0; Ycount < res; Ycount++)
                {
                    int pos = Ycount * res;
                    for (var Xcount = 0; Xcount < res; Xcount++)
                    {
                        var C = newImage.GetPixel(Xcount, Ycount);
                        HeightData[pos + Xcount] = (short)ScaleValue((C.R + C.G + C.B) / 3, 0, 254, smin, smax);
                    }
                }

                int halfrate = -1;
                for (var i = 1; i <= OceanCuts; i++)
                {
                    DeepOcean_Burst(ref HeightData, i + i, (short)Math.Min((smin - (i * (i - 15) / 3)), smin - (2 * i)), WaterLevel, SeaBed);
                    SmoothMap(ref HeightData, WaterLevel + 16);
                    if (halfrate > 2)
                    {
                        Console.WriteLine("Converting Png: " + 100 * i / OceanCuts + "%");
                        halfrate = -1;
                    }
                    halfrate++;
                }
                DeepOcean_Burst(ref HeightData, OceanCuts + 2, SeaBed, WaterLevel, SeaBed);
                SmoothMap(ref HeightData, WaterLevel + 16);
                for (var loops = 0; loops < Smoothing; loops++) //Times to smooth
                {
                    SmoothMap(ref HeightData);
                    if (halfrate > 0)
                    {
                        for (int BeachExtra = 0; BeachExtra <= 4; BeachExtra++) { SmoothMap(ref HeightData, WaterLevel + 40); } //Improve Beaches
                        for (int BeachExtra = 0; BeachExtra <= 4; BeachExtra++) { SmoothMap(ref HeightData, WaterLevel + 20); } //Improve Beaches
                        for (int BeachExtra = 0; BeachExtra <= 4; BeachExtra++) { SmoothMap(ref HeightData, WaterLevel + 10); } //Improve Beaches
                        Console.WriteLine("Smoothing: " + 100 * (loops + 1) / Smoothing + " %");
                        halfrate = -1;
                    }
                    halfrate++;
                }
            }

            public static void GenerateOceanPatrolPath()
            {
                if (!IsSwitchEnabled("height.cargo", false)) { return; }
                if (IsSwitchEnabled("height.cargofast", true))
                {
                    List<Vector3> patrolPath = new List<Vector3>();
                    Console.WriteLine("Creating a new Cargo path corner to corner.");
                    CreateCornerToCornerPath(patrolPath);
                    SaveCargoPath(patrolPath, "oceanpathpoints");
                    return;
                }
                Console.WriteLine("Creating a new Cargo path may take a while depending on map size.");
                var shoredistance = float.Parse(GetSwitch("height.cargoshore", "200"));
                var shoredepth = float.Parse(GetSwitch("height.cargomin", "8"));
                SaveCargoPath(BaseBoat.GenerateOceanPatrolPath(shoredistance, shoredepth), "oceanpathpoints");
            }
            #endregion
        }

        public class CustomPrefab
        {
            public List<PrefabData> CustomPrefabs { get; set; }
            public byte[] IOData { get; set; }
            public byte[] LootData { get; set; }
            public byte[] NPCData { get; set; }
            public byte[] VehicleData { get; set; }
            public byte[] VendingData { get; set; }
            public byte[] APCData { get; set; }
            public byte[] AnchorPaths { get; set; }
            public byte[] BuildingBlocks { get; set; }
            public byte[] MapPassword { get; set; }
            public List<PathData> Paths { get; set; }
        }

        //RustEdit Classes
        public class AnchoredPathList
        {
            public List<AnchoredPath> paths = new List<AnchoredPath>();
        }

        public class AnchoredPath
        {
            public string Name { get; set; }
            public List<AnchoredNode> nodes { get; set; }
        }

        public class AnchoredNode
        {
            public int Mode { get; set; }

            public AnchorPosition anchorPosition { get; set; }

            public bool alignControlNodes { get; set; }
        }
        public class AnchorPosition
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

        public class SerializedBlockList
        {
            public List<BlockData> list = new List<BlockData>();
        }

        public class BlockData
        {
            public PrefabData prefabData;
            public int grade;
        }

        public class SerializedPathList
        {
            public List<VectorData> vectorData = new List<VectorData>();
        }

        public class SerializedAPCPathList
        {
            public List<SerializedAPCPath> paths = new List<SerializedAPCPath>();
        }

        public class SerializedVehicleData
        {
            public List<PrefabData> vehicles = new List<PrefabData>();
        }

        public class SerializedIOData
        {
            public List<SerializedIOEntity> entities = new List<SerializedIOEntity>();
        }

        public class SerializedConnectionData
        {
            public int connectedTo;
            public string fullPath;
            public bool input;
            public VectorData position;
            public int type;
        }

        public class SerializedIOEntity
        {
            public int accessLevel;

            public string autoTurretWeapon;

            public int branchAmount;

            public bool counterPassthrough;

            public int doorEffect;

            public int floors = 1;

            public int frequency;
            public string fullPath;

            public SerializedConnectionData[] inputs;

            public SerializedConnectionData[] outputs;

            public bool peaceKeeper;

            public string phoneName;

            public VectorData position;

            public string rcIdentifier;

            public int targetCounterNumber;

            public float timerLength;

            public bool unlimitedAmmo;
        }

        public class SerializedLootableContainerData
        {
            public List<LootableContainerData> entities = new List<LootableContainerData>();
        }

        public class LootableContainerData
        {
            public string filename = string.Empty;
            public List<LootableItemData> items = new List<LootableItemData>();
            public int refreshRateMax = 1;
            public int refreshRateMin = 1;
            public int respawnRateMax = 1;
            public int respawnRateMin = 1;
            public int spawnAmountMax = 1;
            public int spawnAmountMin = 1;
        }

        public class LootableItemData
        {
            public bool blueprint;
            public int maximum;
            public int minimum;
            public string shortname;
        }

        public class SerializedVendingContainerData
        {
            public List<VendingContainerData> entities = new List<VendingContainerData>();
        }

        public class VendingContainerData
        {
            public string filename = string.Empty;
            public List<VendingItemData> items = new List<VendingItemData>();
        }

        public class VendingItemData
        {
            public int currencyItemAmount;
            public bool currencyItemBlueprint;
            public string currencyItemShortname;
            public int sellItemAmount;
            public bool sellItemBlueprint;
            public string sellItemShortname;
            public int weight;
        }

        public class SerializedNPCData
        {
            public List<SerializedNPCSpawner> npcSpawners = new List<SerializedNPCSpawner>();
        }

        public class SerializedNPCSpawner
        {
            public string category;
            public int npcType;
            public VectorData position;
            public int respawnMax;
            public int respawnMin;
        }

        public class SerializedAPCPath
        {
            public List<VectorData> nodes = new List<VectorData>();
            public List<VectorData> interestNodes = new List<VectorData>();
        }

        public class PartsData
        {
            public string category;
            public uint id;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
        }

        public class UnifiedLogger : TextWriter, IDisposable
        {
            private readonly TextWriter _originalOut;
            private readonly TextWriter _originalErr;
            public StringBuilder _buffer = new StringBuilder();
            private readonly object _lock = new object();

            public override Encoding Encoding => Encoding.UTF8;

            private Thread _stdoutThread;
            private Thread _stderrThread;
            private bool _running = true;

            public UnifiedLogger()
            {
                // Save original writers
                _originalOut = Console.Out;
                _originalErr = Console.Error;

                // Replace with this logger
                Console.SetOut(this);
                Console.SetError(this);

                // Start background threads to intercept native stdout/stderr
                _stdoutThread = new Thread(() => ReadStdStream(Console.OpenStandardOutput()));
                _stdoutThread.IsBackground = true;
                _stdoutThread.Start();

                _stderrThread = new Thread(() => ReadStdStream(Console.OpenStandardError()));
                _stderrThread.IsBackground = true;
                _stderrThread.Start();
            }

            // --- Filtering logic ---
            private bool ShouldFilter(string message)
            {
                if (string.IsNullOrWhiteSpace(message)) return false;
                string ml = message.ToLowerInvariant();
                if (ml.Contains("the referenced script")) return true;
                if (ml.Contains("hdr render texture")) return true;
                if (ml.Contains("asset warmup")) return true;
                if (ml.Contains("**")) return true;
                if (ml.Contains("empty decor")) return true;
                if (ml.Contains("unsupported texture format")) return true;
                if (ml.Contains("texture with id")) return true;
                if (ml.Contains("trying to load")) return true;
                if (ml.Contains("missing shader")) return true;
                return false;
            }

            // --- Internal write helper ---
            private void WriteInternal(string message, bool newLine)
            {
                if (string.IsNullOrEmpty(message)) return;
                if (ShouldFilter(message)) return;

                lock (_lock)
                {
                    if (newLine)
                    {
                        _originalOut.WriteLine(message);
                        _buffer.AppendLine(message);
                    }
                    else
                    {
                        _originalOut.Write(message);
                        _buffer.Append(message);
                    }
                }
            }

            public override void Write(char value) => WriteInternal(value.ToString(), false);
            public override void Write(string value) => WriteInternal(value, false);
            public override void WriteLine() => WriteInternal(string.Empty, true);
            public override void WriteLine(string value) => WriteInternal(value, true);

            // --- Capture native stdout/stderr in background ---
            private void ReadStdStream(Stream stream)
            {
                try
                {
                    var reader = new StreamReader(stream, Encoding.UTF8);
                    while (_running)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;
                        WriteInternal(line, true);
                    }
                }
                catch { /* swallow errors */ }
            }

            public string GetLogs()
            {
                lock (_lock) return _buffer.ToString();
            }

            public override void Flush()
            {
                lock (_lock)
                {
                    _originalOut.Flush();
                    _originalErr.Flush();
                }
            }

            protected override void Dispose(bool disposing)
            {
                _running = false;
                base.Dispose(disposing);

                if (disposing)
                {
                    lock (_lock)
                    {
                        try { _originalOut.Flush(); } catch { }
                        try { _originalErr.Flush(); } catch { }
                    }
                }
            }
        }

    }
}

//Source code of compressed HTML pages
#region MainPage HTML

public class Pages
{
    //Main Page HTML
    public static string MainPagehtml = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta http-equiv='X-Frame-Options' content='SAMEORIGIN'>
<meta http-equiv='X-Content-Type-Options' content='nosniff'>
<meta http-equiv='Referrer-Policy' content='strict-origin-when-cross-origin'>
<title>Rust Map Genny</title>
<link rel=""icon"" type=""image/png"" href=""/favicon.ico""/>
<style>
body { font-family:'Segoe UI', Tahoma, sans-serif; background:linear-gradient(135deg,#2c3e50,#4ca1af); color:#f0f0f0; margin:0; }
.container { max-width:980px; margin:20px auto; background:rgba(0,0,0,0.75); padding:20px; border-radius:12px; }
h1 { text-align:center; margin:0 0 12px 0; }
.form-group { margin-bottom:12px; display:flex; flex-direction:column; }
label { font-weight:600; margin-bottom:6px; }
input, select, textarea {
background: #1e1e1e;
color: #f0f0f0;
border: 1px solid #444;
border-radius: 6px;
padding: 8px;
transition: background 0.2s, box-shadow 0.2s;
}
input[type=file] {
background: #2a2a2a;
color: #f0f0f0;
border: 1px solid #555;
}
textarea { resize:vertical; min-height:80px; width:100%; }
.btn { margin-top:10px; padding:10px 14px; font-size:14px; font-weight:700; border-radius:8px; border:none; background:#27ae60; color:white; cursor:pointer; }
.btn:disabled { background:#666; cursor:not-allowed; }
legend { padding:0 8px; font-weight:700; }
#previewArea { text-align:center; margin-top:12px; }
#previewImg { width:500px; height:500px; background:#fff; margin:10px auto; border-radius:10px; box-shadow:0 4px 10px rgba(0,0,0,0.4); display:flex; justify-content:center; align-items:center; }
#downloadBtn { display:none; margin:0 auto 8px; }
#restartBtn { display:none; margin:0 auto 8px; }
.small { font-size:12px; color:#ccc; }
input[type=file]:hover, .btn:hover {
opacity:0.9;
transform:translateY(-1px);
transition:all 0.15s ease-in-out;
}
fieldset {
border:1px solid #555;
border-radius:10px;
margin-top:14px;
padding:14px;
box-shadow:0 0 8px rgba(0,0,0,0.2);
}
#consoleBox {
width:100%;
height:250px;
background:#0a0a0a;
color:#0f0;
font-family:monospace;
font-size:13px;
padding:10px;
border-radius:8px;
border:none;
box-shadow:inset 0 0 10px #000;
}
#heightPreview {
max-width:256px;
max-height:256px;
width:auto;
height:auto;
object-fit:contain;
background:#222;
border-radius:8px;
margin-top:8px;
display:none;
box-shadow:0 0 6px rgba(0,0,0,0.4);
}
.form-group.inline { display:flex; align-items:center; gap:12px; }
.btn:hover:not(:disabled) { background:#2ecc71; }
.btn:active:not(:disabled) { transform:scale(0.98); }
legend {
padding:0 8px;
font-weight:700;
color:#4ca1af;
text-shadow:0 0 5px rgba(76,161,175,0.5);
}
.small { font-size:12px; color:#aaa; line-height:1.4; }
.container {
animation:fadeIn 0.6s ease-out;
}
@keyframes fadeIn {
from { opacity:0; transform:translateY(10px); }
to { opacity:1; transform:translateY(0); }
}
body.light {
background: linear-gradient(135deg,#e8e8e8,#fdfdfd);
color: #222;
}
body.light .container {
background: rgba(255,255,255,0.85);
color: #222;
}
body.light input, body.light select, body.light textarea {
background:#fff;
color:#000;
}
body.light fieldset { border:1px solid #ccc; box-shadow:0 0 6px rgba(0,0,0,0.1); }
body.light .btn { background:#3498db; color:#fff; }
body.light .btn:hover:not(:disabled){ background:#2980b9; }
body.light legend { color:#3498db; text-shadow:none; }
body.light #consoleBox {
background:#f3f3f3;
color:#222;
box-shadow:inset 0 0 6px rgba(0,0,0,0.15);
}
body.light .small { color:#444; }
body.light .theme-toggle { background:rgba(0,0,0,0.1); color:#000; }
body.light .theme-toggle:hover { background:rgba(0,0,0,0.2); }
input:focus, select:focus, textarea:focus {
background: #2c2c2c;
box-shadow: 0 0 6px rgba(76,161,175,0.5);
outline: none;
}
input.invalid, select.invalid, textarea.invalid {
border: 2px solid #e74c3c !important;
background: rgba(231,76,60,0.1);
}
input.valid, select.valid, textarea.valid {
border: 2px solid #27ae60 !important;
background: rgba(39,174,96,0.05);
}
.top-buttons {
display: flex;
justify-content: space-between;
align-items: center;
width: 100%;
margin-bottom: 10px;
position: relative;
}
.theme-toggle,
.upload-btn {
background: rgba(255,255,255,0.15);
border: none;
color: #fff;
padding: 8px 14px;
border-radius: 20px;
cursor: pointer;
font-size: 13px;
transition: background 0.3s, transform 0.2s;
}
.theme-toggle:hover,
.upload-btn:hover {
background: rgba(255,255,255,0.25);
transform: translateY(-1px);
}
body.light .theme-toggle,
body.light .upload-btn {
background: rgba(0,0,0,0.1);
color: #000;
}
body.light .theme-toggle:hover,
body.light .upload-btn:hover {
background: rgba(0,0,0,0.2);
}
.right-buttons {
display: flex;
gap: 8px;
}
input.valid, select.valid, textarea.valid {
  border: 2px solid #4CAF50 !important; /* green */
}
input.invalid, select.invalid, textarea.invalid {
  border: 2px solid #f44336 !important; /* red */
}
#imageModal {
  position: fixed;
  top: 0;
  left: 0;
  right:0;
  bottom:0;
  background: rgba(0,0,0,0.85);
  display: none;
  align-items: center;
  justify-content: center;
  z-index: 9999;
}
#imageModal img {
  max-width: 90%;
  max-height: 90%;
  border-radius: 12px;
  box-shadow: 0 0 20px rgba(0,0,0,0.5);
}
</style>
</head>
<body>
<!-- Top-right buttons -->
<div class='container'>
<h1>Rust Map Genny</h1>
<div class=""top-buttons"">
<button class=""theme-toggle"" id=""themeToggle"">🌙 Dark Mode</button>
<div class=""right-buttons"">
<button class=""upload-btn"" id=""png2cubesBtn"" onclick=""window.location.href='/png2cubes'"">🧊 Png2Cubes</button>
<button class=""upload-btn"" id=""runJobsBtn"" onclick=""window.location.href='/jobs'"">🎯 Run Jobs</button>
<button class=""upload-btn"" id=""uploadBtn"" onclick=""window.location.href='/upload'"">📤 Upload Map</button>
<button class=""upload-btn"" id=""quitBtn"">💀 Shut Down</button>
</div>
</div>
<form id='configForm' enctype='multipart/form-data' method='post'>
<!-- Map Info -->
<fieldset>
<legend>Map Info</legend>
<div class='form-group'><label>Map Name (Appended to start of filename)</label><input type='text' name='height.name'></div>
<div class='form-group'><label>Map Size (150-8000)</label><input type='number' name='map.size' value='4250' min='150' max='8000' required></div>
<div class='form-group'><label>Map Seed</label><input type='number' name='map.seed' value='1337' min='0' max='2147483647' required></div>
</fieldset>
<!-- Height Map & Terrain Settings -->
<fieldset>
<legend>Height Map & Terrain</legend>
<div class=""form-group"">
<label for=""customPrefabFile"">Custom Prefabs:</label>
<div style=""display:flex; align-items:center; gap:6px; margin-bottom:8px;"">
<input type=""file"" id=""customPrefabFile"" name=""customPrefabFile"" accept="".zip"">
<button type=""button"" class=""btn"" id=""clearCustomPrefabBtn"" onclick=""document.getElementById('customPrefabFile').value='';"">Clear</button>
</div>
</div>
<!-- Height Map -->
<div class=""form-group"">
<label for=""heightInput"">Height Map Image (8 bit black and white PNG)</label>
<div style=""display:flex; align-items:center; gap:6px;"">
<input type=""file"" name=""height.png"" id=""heightInput"" accept=""image/png"">
<button type=""button"" class=""btn"" onclick=""document.getElementById('heightInput').value=''; document.getElementById('heightPreview').style.display='none';"">Clear</button>
</div>
<img id=""heightPreview"" alt=""Height Preview"" style=""display:none; margin-top:4px;"">
</div>
<div class='form-group'><label>Min Height (Height of color black, 500 = sealevel)</label><input type='number' name='height.min' value='498' min='1' max='999' required></div>
<div class='form-group'><label>Max Height (Height of color white)</label><input type='number' name='height.max' value='700' min='1' max='999' required></div>
<div class='form-group'><label>Smoothing Passes</label><input type='number' name='height.smooth' value='12' min='0' max='99' required></div>
<div class='form-group'><label>Ocean Cuts (Steps between beach and ocean floor)</label><input type='number' name='height.cuts' value='60' min='0' max='999' required></div>
<div class='form-group'><label>Ocean Floor Depth (Sea level in game 500)</label><input type='number' name='height.floor' value='467' min='1' max='700' required></div>
<div class='form-group'><label>Water Map Height</label><input type='number' name='height.water' value='500' min='1' max='700' required></div>
<div class='form-group'><label>Biome Angle (0 = N-W, 90 = E-W, 180 = W-N, 270 = W-E for Cold - Arid)</label><input type='number' name='height.bangle' value='180' min='0' max='360' required></div>
<div class='form-group'><label>Tier/Loot Angle (0 = N-W, 90 = E-W, 180 = W-N, 270 = W-E for T3-T1 monuments)</label><input type='number' name='height.langle' value='359' min='0' max='360' required></div>
</fieldset>
<!-- Edge & Topology Options -->
<fieldset>
<legend>Edge & Topology Options</legend>
<div class='form-group'><label>Cut Edge (Flattens around the edge of the map)</label><select name='height.edge'><option>true</option><option>false</option></select></div>
<div class='form-group'><label>Pixels To Use On Edge Cut</label><input type='number' name='height.pixels' value='5' min='0' max='1000' required></div>
<div class='form-group'><label>Remove Road Topology</label><select name='height.roadtopology'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Remove Rail Topology</label><select name='height.railtopology'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Flatten Lakes (Makes lakes shallow to build on)</label><select name='height.flatlakes'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Shallow Beaches (You must block prefab ""coastal_rocks"")</label><select name='height.shallow'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>All Ocean Splat Sand (Coverts everything with ocean topology to have sand splat)</label><select name='height.allsand'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>No Arctic Ocean (Will remove arctic biome if in ocean topology)</label><select name='height.warmocean'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Only Mountain Arctic (Limits arctic to height)</label><select name='height.mountainarctic'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Height To Consider As Mountain</label><input type='number' name='height.mountainheight' value='120' min='0' max='1000' required></div>
<div class='form-group'><label>Move Oil Rigs Inside Grid</label><select name='height.oilrigingrid'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Cover Vector3.Zero (Makes sure GC Vector3.Zero/Center of the map is covered)</label><select name='height.covergc'><option>false</option><option>true</option></select></div>
</fieldset>
<!-- Mountain, Volcano-->
<fieldset>
<legend>Mountain & Volcano Settings</legend>
<div class='form-group'><label>Add Mountain Fog Effect</label><select name='mountain.fog'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Add Snow FX To Mountains</label><select name='mountain.snow'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Convert Mountains Into Volcanos</label><select name='height.volcano'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Apply Gravel Splat Around Lava</label><select name='mountain.splat'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Use Larva Without Damage FX</label><select name='mountain.nodamage'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Remove Vocano Smoke FX</label><select name='mountain.nosmoke'><option>false</option><option>true</option></select></div>
</fieldset>
<!-- Roads, Trails & Rail-->
<fieldset>
<legend>Road, Trail Widths & Underground Rail</legend>
<div class='form-group'><label>Road Width</label><input type='number' name='height.roadwidth' value='10' min='0' max='50' required></div>
<div class='form-group'><label>Trail Width (Greater then 4 turns it into a road)</label><input type='number' name='height.trailwidth' value='4' min='0' max='50' required></div>
<div class='form-group'><label>Underground Rail Depth (Will snap to nearest rail grid height on spawn)</label><input type='number' name='height.raildepth' value='3' min='3' max='360' step='3' required id='railDepthInput'></div>
</fieldset>
<!-- River & Water Settings -->
<fieldset>
<legend>Rivers & Underwater Labs Features</legend>
<div class='form-group'><label>Max Amount Of Rivers (Only limits max doesnt set max to spawn)</label><input type='number' name='river.max' value='10' min='0' max='50' required></div>
<div class='form-group'><label>River Width</label><input type='number' name='river.width' value='8' min='1' max='50' required></div>
<div class='form-group'><label>River Depth</label><input type='number' name='river.depth' value='1.5' min='0.1' max='10' step='any' required></div>
<div class='form-group'><label>Additinal Underwater Labs (Extra to spawn over vanilla 1-2)</label><input type='number' name='lab.amount' value='0' min='0' max='50' required></div>
<div class='form-group'><label>Number Of Attempts At Spawning Lab Parts (More attempts slower but bigger labs)</label><input type='number' name='lab.iterations' value='25' min='10' max='999' required></div>
<div class='form-group'><label>Numnber Of Lab Small Segment Branches</label><input type='number' name='lab.minsegmentcount' value='5' min='1' max='100' required></div>
<div class='form-group'><label>Numnber Of Lab Large Segment Branches</label><input type='number' name='lab.largesegmentcount' value='25' min='5' max='100' required></div>
<div class='form-group'><label>Start Budget For Lab Segments</label><input type='number' name='lab.startbudget' value='3' min='3' max='100' required></div>
<div class='form-group'><label>Start Lab Floors Limit</label><input type='number' name='lab.startfloor' value='2' min='2' max='100' required></div>
<div class='form-group'><label>Mid Lab Segment Budget</label><input type='number' name='lab.midbudget' value='4' min='4' max='100' required></div>
<div class='form-group'><label>End Lab Segment Budget</label><input type='number' name='lab.endbudget' value='5' min='5' max='100' required></div>
<div class='form-group'><label>Map Edge Buffer For Lab Placement</label><input type='number' name='lab.edgebuffer' value='400' min='100' max='2000' required></div>
<div class='form-group'><label>Water Min Depth For Lab Placement</label><input type='number' name='lab.mindept' value='20' min='0' max='500' required></div>
</fieldset>
<!-- Cargoship Settings -->
<fieldset>
<legend>Cargoship Settings</legend>
<div class='form-group'><label>Generate Cargo Path (Embed rust edit cargoship path into map)</label><select name='height.cargo'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Fast Cargo Path (Only creates 1 node in each corner)</label><select name='height.cargofast'><option>false</option><option>true</option></select></div>
<div class='form-group'><label>Min Cargo Shore Distance</label><input type='number' name='height.cargoshore' value='70' min='0' max='8000' required></div>
<div class='form-group'><label>Min Cargo Depth</label><input type='number' name='height.cargomin' value='8' min='0' max='1000' required></div>
</fieldset>
<!-- Advertisement Slots -->
<fieldset>
<legend>Advertisement Slots - Supports markup</legend>
<div class='form-group'><label>Compound Wall</label><input type='text' name='ads.compound'></div>
<div class='form-group'><label>Compound Gate 1</label><input type='text' name='ads.gate1'></div>
<div class='form-group'><label>Compound Gate 2</label><input type='text' name='ads.gate2'></div>
<div class='form-group'><label>Bandit Camp Wall</label><input type='text' name='ads.bandit'></div>
</fieldset>
<!-- Terrain Tiers -->
<fieldset>
<legend>Terrain Tiers - Used for monument spawns</legend>
<div class='form-group'><label>Percentage Tier 0</label><input type='number' step='0.01' name='wc.tier0' value='0.3' min='0' max='1' required></div>
<div class='form-group'><label>Percentage Tier 1</label><input type='number' step='0.01' name='wc.tier1' value='0.3' min='0' max='1' required></div>
<div class='form-group'><label>Percentage Tier 2</label><input type='number' step='0.01' name='wc.tier2' value='0.4' min='0' max='1' required></div>
</fieldset>
<!-- Biome Distribution -->
<fieldset>
<legend>Biome Distribution</legend>
<div class='form-group'><label>Percentage Arid Biome</label><input type='number' step='0.01' name='wc.biome.arid' value='0.4' min='0' max='1' required></div>
<div class='form-group'><label>Percentage Temperate Biome</label><input type='number' step='0.01' name='wc.biome.temperate' value='0.15' min='0' max='1' required></div>
<div class='form-group'><label>Percentage Tundra Biome</label><input type='number' step='0.01' name='wc.biome.tundra' value='0.15' min='0' max='1' required></div>
<div class='form-group'><label>Percentage Arctic Biome</label><input type='number' step='0.01' name='wc.biome.arctic' value='0.3' min='0' max='1' required></div>
<div class='form-group'><label>Percentage Jungle Biome</label><input type='number' step='0.01' name='wc.biome.jungle' value='0.5' min='0' max='1' required></div>
</fieldset>
<!-- Generation Toggles -->
<fieldset>
<legend>Generation Toggles</legend>
<div class='form-group'><label>Main Roads</label><select name='wc.mainroads'><option>true</option><option>false</option></select></div>
<div class='form-group'><label>Side Roads</label><select name='wc.sideroads'><option>true</option><option>false</option></select></div>
<div class='form-group'><label>Trails</label><select name='wc.trails'><option>true</option><option>false</option></select></div>
<div class='form-group'><label>Rivers</label><select name='wc.rivers'><option>true</option><option>false</option></select></div>
<div class='form-group'><label>Powerlines</label><select name='wc.powerlines'><option>true</option><option>false</option></select></div>
<div class='form-group'><label>Above Ground Rails</label><select name='wc.aboverails'><option>true</option><option>false</option></select></div>
<div class='form-group'><label>Below Ground Rails</label><select name='wc.belowrails'><option>true</option><option>false</option></select></div>
<div class='form-group'><label>Underwater Labs</label><select name='wc.underwaterlabs'><option>true</option><option>false</option></select></div>
</fieldset>
<!-- Prefab Lists -->
<fieldset>
<legend>Prefab Lists</legend>
<div class='form-group'><label>Prefab Blacklist (comma-separated)</label>
<textarea id='prefabBlacklist' name='wc.prefabblacklist' placeholder='prefab1,prefab2,...'></textarea>
</div>
<div class='form-group'><label>Prefab Whitelist (comma-separated)</label>
<textarea id='prefabWhitelist' name='wc.prefabwhitelist' placeholder='prefab1,prefab2,...'></textarea>
</div>
</fieldset>
<div style='display:flex;gap:8px;align-items:center;margin-top:10px;'>
<button class='btn' id='continueBtn' type='submit'>Start Generation</button>
<button class='btn' id='restoreDefaultsBtn' type='button'>Restore Defaults</button>
<button class='btn' id='exportAllBtn' type='button'>Export JSON</button>
<button class='btn' id='importAllBtn' type='button'>Import JSON</button>
</div>
</form>
<div id='runtimeBox' style='margin-top:14px;font-weight:700;'></div>
<textarea id='consoleBox' readonly></textarea>
<div id='previewArea'>
<button class='btn' id='downloadBtn'>Download Map File</button><br>
<img id=""previewImg"" style=""display: none; width: 500px; height: 500px; border-radius:10px; box-shadow:0 4px 10px rgba(0,0,0,0.4); "" />
<button class='btn' id='restartBtn'>Restart Server</button><br>
</div>
<footer>© Copyright bmgjet</footer>
</div>
<div id=""imageModal"">
<img src="""" alt=""Preview"">
</div>
<script>
document.addEventListener('DOMContentLoaded', () => {
  const form = document.getElementById('configForm');
  const continueBtn = document.getElementById('continueBtn');
  const restoreDefaultsBtn = document.getElementById('restoreDefaultsBtn');
  const exportAllBtn = document.getElementById('exportAllBtn');
  const importAllBtn = document.getElementById('importAllBtn');
  const downloadBtn = document.getElementById('downloadBtn');
  const restartBtn = document.getElementById('restartBtn');
  const quitBtn = document.getElementById('quitBtn');
  const uploadBtn = document.getElementById('uploadBtn');
    if (uploadBtn) {
    uploadBtn.addEventListener('click', () => window.location.href = '/upload');
    }
  const png2cubesBtn = document.getElementById('png2cubesBtn');
  const previewImg = document.getElementById('previewImg');
  const consoleBox = document.getElementById('consoleBox');
  const runtimeBox = document.getElementById('runtimeBox');
  const heightInput = document.getElementById('heightInput');
  const heightPreview = document.getElementById('heightPreview');
  const themeToggle = document.getElementById('themeToggle');
  const railDepthInput = document.getElementById('railDepthInput');
  const runJobsBtn = document.getElementById('runJobsBtn');
  const imageModal = document.getElementById('imageModal');
  const modalImg = imageModal ? imageModal.querySelector('img') : null;
  let polling = null;
  const savedTheme = localStorage.getItem('theme') || 'dark';
  if (savedTheme === 'light') document.body.classList.add('light');
  updateThemeLabel();
  themeToggle.addEventListener('click', () => {
    document.body.classList.toggle('light');
    localStorage.setItem('theme', document.body.classList.contains('light') ? 'light' : 'dark');
    updateThemeLabel();
  });
  function updateThemeLabel() {
    themeToggle.textContent = document.body.classList.contains('light') ? '🌙 Dark Mode' : '☀️ Light Mode';
  }
  heightInput.addEventListener('change', e => {
    const file = e.target.files[0];
    if (!file) { heightPreview.style.display = 'none'; return; }
    const reader = new FileReader();
    reader.onload = ev => {
      heightPreview.src = ev.target.result;
      heightPreview.style.display = 'block';
    };
    reader.readAsDataURL(file);
  });
  form.querySelectorAll('input, select, textarea').forEach(el => {
    if (el.type === 'file') return;
    const saved = localStorage.getItem(el.name);
    if (saved !== null) {
      if (el.type === 'checkbox' || el.type === 'radio') el.checked = saved === 'true';
      else el.value = saved;
    }
    el.addEventListener('change', () => {
      if (el.type === 'checkbox' || el.type === 'radio')
        localStorage.setItem(el.name, el.checked);
      else if (el.type !== 'file')
        localStorage.setItem(el.name, el.value);
    });
  });
   exportAllBtn.addEventListener('click', () => {
    const obj = {};
    form.querySelectorAll('input, select, textarea').forEach(el => {
      if (el.type === 'file') return;
      obj[el.name] = el.type === 'checkbox' ? el.checked : el.value;
    });
    const blob = new Blob([JSON.stringify(obj, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'job.json';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  });
  importAllBtn.addEventListener('click', () => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'application/json';
    input.addEventListener('change', event => {
      const file = event.target.files[0];
      if (!file) {return;}
      const reader = new FileReader();
      reader.onload = e => {
        try {
          const data = JSON.parse(e.target.result);
          form.querySelectorAll('input, select, textarea').forEach(el => {
            if (el.type === 'file') return;
            if (data.hasOwnProperty(el.name)) {
              if (el.type === 'checkbox') el.checked = data[el.name];
              else el.value = data[el.name];
              el.dispatchEvent(new Event('change'));
            }
          });
          alert('JSON imported successfully!');
        } catch { alert('Invalid JSON file'); }
      };
      reader.readAsText(file);
    });
    input.click();
  });
  const requiredInputs = form.querySelectorAll('input[required], select[required], textarea[required]');
  requiredInputs.forEach(input => {
    input.addEventListener('blur', () => validateField(input));
    input.addEventListener('change', () => validateField(input));
  });
  railDepthInput.addEventListener('input', () => {
    let val = parseInt(railDepthInput.value);
    if (!isNaN(val)) {
      val = Math.round(val / 3) * 3;
      if (val < parseInt(railDepthInput.min)) val = parseInt(railDepthInput.min);
      if (val > parseInt(railDepthInput.max)) val = parseInt(railDepthInput.max);
      railDepthInput.value = val;
    }
  });
  function validateField(input) {
    const val = input.value.trim();
    let valid = true;
    if (input.required && val === '') valid = false;
    if (input.type === 'number') {
      const num = Number(val);
      if (isNaN(num) || num < Number(input.min) || num > Number(input.max)) valid = false;
    }
    input.classList.toggle('valid', valid);
    input.classList.toggle('invalid', !valid);
    return valid;
  }
function validateForm() {
  let ok = true;
  requiredInputs.forEach(i => { if (!validateField(i)) ok = false; });
  const size = parseInt(form['map.size'].value, 10);
  const seed = parseInt(form['map.seed'].value, 10);
  if (isNaN(size) || size < 150 || size > 8000) {
    alert('Map size must be between 150–8000');
    ok = false;
  }
  if (isNaN(seed) || seed < 0) {
    alert('Map seed must be 0 or greater');
    ok = false;
  }

  const t0 = +form['wc.tier0'].value,
        t1 = +form['wc.tier1'].value,
        t2 = +form['wc.tier2'].value;

  if (t0 + t1 + t2 <= 0) {
    alert('At least one tier must be greater than 0');
    ok = false;
  }

  const biomes = [
    'wc.biome.arid',
    'wc.biome.temperate',
    'wc.biome.tundra',
    'wc.biome.arctic',
    'wc.biome.jungle'
  ];
  for (const b of biomes) {
    const v = parseFloat(form[b].value);
    if (isNaN(v) || v < 0 || v > 1) {
      alert(`${b} must be between 0–1`);
      ok = false;
    }
  }
  return ok;
}

  async function startPolling() {
  if (polling) clearInterval(polling);

  async function poll() {
    try {
      const res = await fetch('/status');
      const data = await res.json();
      runtimeBox.textContent = `Uptime: ${data.uptime || 'N/A'}`;
      consoleBox.value = data.console || '';
      consoleBox.scrollTop = consoleBox.scrollHeight;
      const generating = !!data.generating;
      continueBtn.disabled = generating;
      restoreDefaultsBtn.disabled = generating;
      exportAllBtn.disabled = generating;
      importAllBtn.disabled = generating;
      runJobsBtn.disabled = generating;
      continueBtn.textContent = generating ? (data.savedFilePath ? 'Done' : 'Generating...') : 'Start Generation';

      if (data.savedFilePath) {
        previewImg.style.display = 'block';
        previewImg.src = '/map.png?ts=' + Date.now();
        downloadBtn.style.display = 'inline-block';
        restartBtn.style.display = 'inline-block';
        downloadBtn.onclick = () => window.location.href = '/download';
        restartBtn.onclick = () => window.location.href = '/restart';
      } else {
        previewImg.style.display = 'none';
        downloadBtn.style.display = 'none';
        restartBtn.style.display = 'none';
      }
    } catch (err) {
      console.error('Polling error:', err);
    }
  }
  poll();
  polling = setInterval(poll, 1000);
}
  form.addEventListener('submit', async e => {
    e.preventDefault();
    if (!validateForm()) return;
    continueBtn.disabled = true;
    continueBtn.textContent = 'Generating...';
    const fd = new FormData(form);
    await fetch('/', { method: 'POST', body: fd });
    startPolling();
  });
  restoreDefaultsBtn.addEventListener('click', () => {
    if (!confirm('Restore default values? This will reset all saved options.')) {return;}
    localStorage.clear();
    form.reset();
    document.getElementById('prefabBlacklist').value = '';
    document.getElementById('prefabWhitelist').value = '';
    form.querySelectorAll('input, select, textarea').forEach(el => {
      el.classList.remove('valid', 'invalid');
    });
  });
  quitBtn.addEventListener('click', () => { if (!confirm('Are you sure you want to shutdown the server')) { return; } window.location.href = '/quit'; });
  png2cubesBtn.addEventListener('click', () => window.location.href = '/png2cubes');
  runJobsBtn.addEventListener('click', () => window.location.href = '/jobs');
if (imageModal && modalImg) {
  const enlargeableImages = [document.getElementById('previewImg'), document.getElementById('heightPreview')];
  enlargeableImages.forEach(img => {
    if (!img) return;
    img.addEventListener('click', () => {
      if (!img.src) return;
      modalImg.src = img.src;
      imageModal.style.display = 'flex';
    });
  });
  imageModal.addEventListener('click', () => { imageModal.style.display = 'none'; });
}
  startPolling();
});
</script>
</body>
</html>";
    #endregion

    #region PNG2Cubes HTML
    public static string PNG2Cubeshtml = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta http-equiv='X-Frame-Options' content='SAMEORIGIN'>
<meta http-equiv='X-Content-Type-Options' content='nosniff'>
<meta http-equiv='Referrer-Policy' content='strict-origin-when-cross-origin'>
<title>Image 2 Cubes</title>
<link rel=""icon"" type=""image/png"" href=""/favicon.ico""/>
<style>
body {
  font-family:'Segoe UI',Tahoma,sans-serif;
  background:linear-gradient(135deg,#2c3e50,#4ca1af);
  background-repeat:no-repeat;
  background-attachment:fixed;
  background-size:cover;
  color:#f0f0f0;
  margin:0;
  min-height:100vh;
}
.container {
  max-width:750px;
  margin:60px auto;
  background:rgba(0,0,0,0.75);
  padding:25px;
  border-radius:14px;
  text-align:center;
  animation:fadeIn 0.6s ease-out;
  box-shadow:0 0 25px rgba(0,0,0,0.4);
}
h1,h3 { margin-bottom:16px; }
.instructions {
  text-align:left;
  font-size:13px;
  color:#ccc;
  background:rgba(255,255,255,0.05);
  padding:12px;
  border-radius:10px;
  margin-bottom:20px;
}
input,select,button {
  font-family:inherit;
  border-radius:8px;
  border:1px solid #444;
  padding:8px;
  margin:5px;
  color:#f0f0f0;
  background:#1e1e1e;
}
input[type='file'] {
  cursor:pointer;
  background:#1e1e1e;
  border:1px solid #555;
  padding:10px;
}
input[type='checkbox'],input[type='radio'] {
  transform:scale(1.2);
  margin-right:6px;
  cursor:pointer;
}
input[type='number'],input[type='text'] {
  text-align:center;
  width:70px;
}
button {
  background:#27ae60;
  color:#fff;
  font-weight:700;
  padding:10px 18px;
  border:none;
  cursor:pointer;
  transition:background 0.2s,transform 0.15s;
}
button:hover { background:#2ecc71; transform:translateY(-1px); }
button:active { transform:scale(0.98); }
button:disabled { background:#555; cursor:not-allowed; }
.preview-container {
  display:flex;
  justify-content:center;
  align-items:center;
  margin-top:10px;
}
.preview {
  max-width:256px;
  max-height:256px;
  border:1px solid #555;
  border-radius:8px;
  background:#222;
  display:none;
}
.radio-group {
  display:inline-block;
  background:rgba(255,255,255,0.05);
  padding:10px 15px;
  border-radius:10px;
  margin-top:10px;
}
.radio-container {
  display:flex;
  align-items:center;
  justify-content:center;
  gap:6px;
  margin:4px 0;
}
#progress { width:100%; max-width:400px; height:12px; border-radius:8px; background:#222; overflow:hidden; margin:10px auto; display:none; }
#progressBar { height:100%; width:0%; background:#4ca1af; transition:width 0.3s; }
#statusBox { margin-top:10px; font-size:13px; color:#aaa; }
.theme-toggle {
  background:rgba(255,255,255,0.15);
  border:none;
  color:#fff;
  padding:8px 14px;
  border-radius:20px;
  cursor:pointer;
  font-size:13px;
  transition:background 0.3s,transform 0.2s;
  float:right;
}
.theme-toggle:hover {
  background:rgba(255,255,255,0.25);
  transform:translateY(-1px);
}
body.light {
  background:linear-gradient(135deg,#f4f4f4,#fff);
  color:#222;
}
body.light .container { background:rgba(255,255,255,0.9); color:#000; }
body.light input,body.light select { background:#fff; color:#000; border:1px solid #ccc; }
body.light button { background:#3498db; }
body.light button:hover { background:#2980b9; }
body.light .theme-toggle { background:rgba(0,0,0,0.1); color:#000; }
body.light .instructions { background:rgba(0,0,0,0.05); color:#333; }
@keyframes fadeIn { from{opacity:0;transform:translateY(10px);} to{opacity:1;transform:translateY(0);} }
</style>
<script>
let aspectRatio=1,MAX_SIZE=1024,MIN_SIZE=1;function checkFileSelected(){let f=document.querySelector(""input[type='file']""),u=document.getElementById('uploadButton'),p=document.getElementById('preview'),w=document.getElementById('width'),h=document.getElementById('height'),c=[w,h,u,document.getElementById('smoothing'),document.getElementById('likeness')];if(f.files.length){let file=f.files[0],r=new FileReader();r.onload=e=>{p.src=e.target.result,p.style.display='block'},r.readAsDataURL(file);let img=new Image();img.src=URL.createObjectURL(file),img.onload=function(){let ow=img.width,oh=img.height;aspectRatio=ow/oh;if(ow>oh){ow=Math.min(ow,MAX_SIZE),oh=Math.round(ow/aspectRatio)}else{oh=Math.min(oh,MAX_SIZE),ow=Math.round(oh*aspectRatio)}w.value=Math.max(ow,MIN_SIZE),h.value=Math.max(oh,MIN_SIZE),w.disabled=h.disabled=false,URL.revokeObjectURL(img.src)},c.forEach(el=>el.disabled=false)}else p.style.display='none',c.forEach(el=>el.disabled=true)}function enforceLimits(i){let m=parseInt(i.min,10),x=parseInt(i.max,10);i.value=Math.min(Math.max(i.value,m),x)}function maintainAspectRatio(i){let w=document.getElementById('width'),h=document.getElementById('height');if(i.id==='width'){w.value=Math.min(w.value,MAX_SIZE),h.value=Math.round(w.value/aspectRatio),h.value=Math.min(h.value,MAX_SIZE),w.value=Math.round(h.value*aspectRatio)}else{h.value=Math.min(h.value,MAX_SIZE),w.value=Math.round(h.value*aspectRatio),w.value=Math.min(w.value,MAX_SIZE),h.value=Math.round(w.value/aspectRatio)}}function restrictInput(e){/^[0-9.]*$/.test(e.key)||e.preventDefault()}document.addEventListener('DOMContentLoaded',loadSavedValues);function loadSavedValues(){['smoothing','likeness','scaleX','scaleY','scaleZ'].forEach(i=>{let el=document.getElementById(i),v=localStorage.getItem(i);el&&(el.value=v!==null?v:el.getAttribute('value'),el.addEventListener('input',()=>saveValue(i)))}),document.querySelectorAll(""input[type='checkbox']"").forEach(c=>{let s=localStorage.getItem(c.name+'-'+c.value);c.checked=s!==null?s===""true"":c.defaultChecked,c.addEventListener('change',()=>saveCheckbox(c))}),document.querySelectorAll(""input[type='radio']"").forEach(r=>{let s=localStorage.getItem(r.name);s===r.value&&(r.checked=true),r.addEventListener('change',()=>saveRadio(r))})}function saveValue(i){let el=document.getElementById(i);el&&localStorage.setItem(i,el.value)}function saveCheckbox(c){localStorage.setItem(c.name+'-'+c.value,c.checked)}function saveRadio(r){r.checked&&localStorage.setItem(r.name,r.value)}
</script>
</head>
<body>
<div class='container'>
<button class='theme-toggle' id='themeToggle'>🌙 Dark Mode</button>
<h1>🧊 Image 2 Cubes</h1>
<p class='instructions'>
<strong>Instructions:</strong><br>
1. <strong>Select a PNG image</strong> (Max: <strong>1024x1024</strong> / <strong>2MB</strong>).<br>
2. Adjust <strong>Width, Height, Smoothing, Likeness</strong> for quality.<br>
3. Choose <strong>colors</strong> used in your PNG.<br>
4. Modify <strong>scale</strong> to fit your world.<br>
5. Pick a <strong>download format</strong> (.map / prefab).<br>
6. Click <strong>Upload</strong> to generate your cubes.
</p>
<form id='uploadForm' enctype='multipart/form-data' method='post' action='/png2cubes'>
<h3>Select PNG:</h3>
<input type='file' id='pngfile' name='pngfile' accept='image/png,image/jpeg' required onchange='checkFileSelected()'>
<div class='preview-container'><img id='preview' class='preview' /></div>
<div>
Width: <input type='number' id='width' name='width' min='1' max='1024' step='1' disabled required oninput='enforceLimits(this); maintainAspectRatio(this)' onkeypress='restrictInput(event)'>
Height: <input type='number' id='height' name='height' min='1' max='1024' step='1' disabled required oninput='enforceLimits(this); maintainAspectRatio(this)' onkeypress='restrictInput(event)'>
Smoothing: <input type='number' id='smoothing' name='smoothing' value='1' min='0' max='10' disabled required oninput='enforceLimits(this)' onchange='saveValue(this.id)'>
Likeness: <input type='number' id='likeness' name='likeness' value='20' min='10' max='30' disabled required oninput='enforceLimits(this)' onchange='saveValue(this.id)'>
</div>
<h3>Select Colors:</h3>
<div>
<input type='checkbox' name='colors' value='white' checked onchange='saveCheckbox(this)'> White  
<input type='checkbox' name='colors' value='black' checked onchange='saveCheckbox(this)'> Black  
<input type='checkbox' name='colors' value='brown' checked onchange='saveCheckbox(this)'> Brown  
<input type='checkbox' name='colors' value='lightgray' checked onchange='saveCheckbox(this)'> Light Gray  
<input type='checkbox' name='colors' value='gray' checked onchange='saveCheckbox(this)'> Gray  
<input type='checkbox' name='colors' value='darkgray' checked onchange='saveCheckbox(this)'> Dark Gray  
<br>
<input type='checkbox' name='colors' value='red' checked onchange='saveCheckbox(this)'> Red  
<input type='checkbox' name='colors' value='orange' checked onchange='saveCheckbox(this)'> Orange  
<input type='checkbox' name='colors' value='yellow' onchange='saveCheckbox(this)'> Yellow  
<input type='checkbox' name='colors' value='limegreen' checked onchange='saveCheckbox(this)'> Lime Green  
<input type='checkbox' name='colors' value='olivegreen' checked onchange='saveCheckbox(this)'> Olive Green  
<input type='checkbox' name='colors' value='purple' checked onchange='saveCheckbox(this)'> Purple  
<input type='checkbox' name='colors' value='lightblue' checked onchange='saveCheckbox(this)'> Light Blue  
</div>
<h3>Scale:</h3>
X: <input type='text' id='scaleX' name='scaleX' value='0.01' maxlength='5' pattern='[0-9.]*' required onkeypress='restrictInput(event)' oninput='saveValue(this.id)'>
Y: <input type='text' id='scaleY' name='scaleY' value='1' maxlength='5' pattern='[0-9.]*' required onkeypress='restrictInput(event)' oninput='saveValue(this.id)'>
Z: <input type='text' id='scaleZ' name='scaleZ' value='0.01' maxlength='5' pattern='[0-9.]*' required onkeypress='restrictInput(event)' oninput='saveValue(this.id)'>
<br>
<button type='submit' id='uploadButton' disabled>Upload</button>
<div id='progress'><div id='progressBar'></div></div>
<div id='statusBox'>Ready to generate cubes.</div>
</form>
    <div style='margin-top:20px;'>
      <a href='/' style='color:#4ca1af;text-decoration:none;font-weight:bold;'>← Back to Generator</a>
    </div>
<footer>© Copyright bmgjet</footer>
</div>
<script>
const themeToggle=document.getElementById('themeToggle');
const currentTheme=localStorage.getItem('theme')||'dark';
document.body.classList.toggle('light',currentTheme==='light');
updateToggleLabel();
function updateToggleLabel(){ themeToggle.textContent=document.body.classList.contains('light')?'🌙 Dark Mode':'☀️ Light Mode'; }
themeToggle.addEventListener('click',()=>{
  document.body.classList.toggle('light');
  localStorage.setItem('theme',document.body.classList.contains('light')?'light':'dark');
  updateToggleLabel();
});
const form=document.getElementById('uploadForm');
form.addEventListener('submit',e=>{
  e.preventDefault();
  const fileInput=document.getElementById('pngfile');
  if(!fileInput.files.length){alert('Please select a file first.');return;}
  const file=fileInput.files[0];
  const xhr=new XMLHttpRequest();
  const progress=document.getElementById('progress');
  const bar=document.getElementById('progressBar');
  const status=document.getElementById('statusBox');
  progress.style.display='block';
  status.textContent='Uploading...';
  xhr.upload.addEventListener('progress',ev=>{
    if(ev.lengthComputable){
      const pct=(ev.loaded/ev.total)*100;
      bar.style.width=pct+'%';
    }
  });
  xhr.onreadystatechange=()=>{
if (xhr.readyState === 4) {
  if (xhr.status === 200) {
    const responseText = xhr.responseText?.trim();
    bar.style.width = '100%';
    if (responseText) {
      status.innerHTML = `Upload complete!<br><span style=""font-size:13px;color:#9fd;"">URL: <a href=""${responseText}"" target=""_blank"" style=""font-size:13px;color:#9fd;"">${responseText}</a></span>`;
    } else {
      status.textContent = 'Upload complete (no response text).';
    }
  } else { status.textContent = '❌ Upload failed: ' + xhr.status; }
}
  };
  xhr.open('POST','/png2cubes');
  const fd = new FormData();
  fd.append('pngfile', file);
  document.querySelectorAll(""input[name='colors']:checked"").forEach(cb => fd.append('colors', cb.value));
  fd.append('width', document.getElementById('width').value);
  fd.append('height', document.getElementById('height').value);
  fd.append('smoothing', document.getElementById('smoothing').value);
  fd.append('likeness', document.getElementById('likeness').value);
  fd.append('scaleX', document.getElementById('scaleX').value);
  fd.append('scaleY', document.getElementById('scaleY').value);
  fd.append('scaleZ', document.getElementById('scaleZ').value);
  xhr.send(fd);
});
</script>
</body>
</html>";
    #endregion

    #region UploadPage HTML
    public static string UploadPagehtml = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta http-equiv='X-Frame-Options' content='SAMEORIGIN'>
<meta http-equiv='X-Content-Type-Options' content='nosniff'>
<meta http-equiv='Referrer-Policy' content='strict-origin-when-cross-origin'>
<title>Upload Rust Map File</title>
<link rel=""icon"" type=""image/png"" href=""/favicon.ico""/>
<style>
body {
  font-family: 'Segoe UI', Tahoma, sans-serif;
  background: linear-gradient(135deg, #2c3e50, #4ca1af);
  background-repeat: no-repeat;
  background-attachment: fixed;
  background-size: cover;
  color: #f0f0f0;
  margin: 0;
  min-height: 100vh;
}
  .container { max-width:600px; margin:60px auto; background:rgba(0,0,0,0.75); padding:20px; border-radius:12px; text-align:center; animation:fadeIn 0.6s ease-out; }
  h1 { margin-bottom:18px; }
  .form-group { margin-bottom:16px; display:flex; flex-direction:column; align-items:center; }
  input[type=file] { background:#1e1e1e; color:#f0f0f0; border:1px solid #444; border-radius:8px; padding:10px; width:100%; max-width:360px; text-align:center; cursor:pointer; }
  .btn { margin-top:14px; padding:10px 16px; border:none; border-radius:8px; font-weight:700; background:#27ae60; color:#fff; cursor:pointer; transition:background 0.2s, transform 0.15s; }
  .btn:hover { background:#2ecc71; transform:translateY(-1px); }
  .btn:active { transform:scale(0.98); }
  #statusBox { margin-top:20px; font-size:14px; color:#aaa; }
  #progress { width:100%; max-width:400px; height:12px; border-radius:8px; background:#222; overflow:hidden; margin:10px auto; display:none; }
  #progressBar { height:100%; width:0%; background:#4ca1af; transition:width 0.3s; }
.theme-toggle,
.upload-btn {
  background: rgba(255,255,255,0.15);
  border: none;
  color: #fff;
  padding: 8px 14px;
  border-radius: 20px;
  cursor: pointer;
  font-size: 13px;
  transition: background 0.3s, transform 0.2s;
}
.theme-toggle:hover,
.upload-btn:hover {
  background: rgba(255,255,255,0.25);
  transform: translateY(-1px);
}
  @keyframes fadeIn { from{opacity:0;transform:translateY(10px);} to{opacity:1;transform:translateY(0);} }
  body.light { background:linear-gradient(135deg,#f4f4f4,#fff); color:#222; }
  body.light .container { background:rgba(255,255,255,0.9); }
  body.light input[type=file] { background:#fff; color:#000; border:1px solid #ccc; }
  body.light .btn { background:#3498db; }
  body.light .btn:hover { background:#2980b9; }
  body.light .theme-toggle { background:rgba(0,0,0,0.1); color:#000; }
.disclaimer { font-size:13px; color:#f9c; margin-bottom:16px; }
</style>
</head>
<body>
  <div class='container'>
  <button class='theme-toggle' id='themeToggle'>🌙 Dark Mode</button>
    <h1>Upload Rust Map File</h1>
  <div class='disclaimer'>
    ⚠️ By uploading a map file, you are sending it to Facepunch's CDN.<br>
    Abuse or spam may result in your IP address being banned.
  </div>
    <form id='uploadForm' enctype='multipart/form-data' method='post' action='/upload'>
      <div class='form-group'>
        <label for='mapFile'>Select a .map file to upload:</label>
        <input type='file' id='mapFile' name='mapfile' accept='.map' required>
      </div>
      <button type='submit' class='btn'>Upload Map</button>
      <div id='progress'><div id='progressBar'></div></div>
      <div id='statusBox'>Ready to upload your map file.</div>
    </form>
    <div style='margin-top:20px;'>
      <a href='/' style='color:#4ca1af;text-decoration:none;font-weight:bold;'>← Back to Generator</a>
    </div>
<footer>© Copyright bmgjet</footer>
  </div>
<script>
const themeToggle=document.getElementById('themeToggle');
const currentTheme=localStorage.getItem('theme')||'dark';
document.body.classList.toggle('light',currentTheme==='light');
updateToggleLabel();
function updateToggleLabel(){ themeToggle.textContent=document.body.classList.contains('light')?'🌙 Dark Mode':'☀️ Light Mode';}
themeToggle.addEventListener('click',()=>{
  document.body.classList.toggle('light');
  localStorage.setItem('theme',document.body.classList.contains('light')?'light':'dark');
  updateToggleLabel();
});
const form = document.getElementById('uploadForm');
const uploadBtn = form.querySelector('.btn');
form.addEventListener('submit', e => {
  e.preventDefault();
  const fileInput = document.getElementById('mapFile');
  if (!fileInput.files.length) {
    alert('Please select a file first.');
    return;
  }
  uploadBtn.disabled = true;
  uploadBtn.style.opacity = ""0.6"";
  uploadBtn.style.cursor = ""not-allowed"";
  const file = fileInput.files[0];
  const xhr = new XMLHttpRequest();
  const progress = document.getElementById('progress');
  const bar = document.getElementById('progressBar');
  const status = document.getElementById('statusBox');
  progress.style.display = 'block';
  status.textContent = 'Uploading...';
  xhr.upload.addEventListener('progress', ev => {
    if (ev.lengthComputable) {
      const pct = (ev.loaded / ev.total) * 100;
      bar.style.width = pct + '%';
    }
  });
  xhr.onreadystatechange = () => {
    if (xhr.readyState === 4) {
      uploadBtn.disabled = false;
      uploadBtn.style.opacity = ""1"";
      uploadBtn.style.cursor = ""pointer"";
      if (xhr.status === 200) {
        const responseText = xhr.responseText?.trim();
        bar.style.width = '100%';
        if (responseText) {
          status.innerHTML =
            `Upload complete!<br><span style=""font-size:13px;color:#9fd;"">URL: 
            <a href=""${responseText}"" target=""_blank"" style=""color:#9fd;"">${responseText}</a></span>`;
        } else {
          status.textContent = 'Upload complete (no response text).';
        }
      } else {
        status.textContent = '❌ Upload failed: ' + xhr.status;
      }
    }
  };
  xhr.open('POST', '/upload');
  const fd = new FormData();
  fd.append('mapfile', file);
  xhr.send(fd);
});
</script>
</body>
</html>";
    #endregion

    #region RestartPage HTML
    public static string RestartPagehtml = @"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta http-equiv='X-Frame-Options' content='SAMEORIGIN'>
<meta http-equiv='X-Content-Type-Options' content='nosniff'>
<meta http-equiv='Referrer-Policy' content='strict-origin-when-cross-origin'>
<title>Server Restarting</title>
<link rel=""icon"" type=""image/png"" href=""/favicon.ico""/>
<style>
  body {
    font-family: 'Segoe UI', Tahoma, sans-serif;
    background: linear-gradient(135deg,#2c3e50,#4ca1af);
    color: #f0f0f0;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100vh;
    margin: 0;
  }
  h1 { margin-bottom: 20px; }
</style>
<meta http-equiv=""refresh"" content=""20; url =/"">
</head>
<body>
<h1>Restarting Server...</h1>
<p>Page will refresh every 20 seconds</p>
</body>
</html>
";
    #endregion

    #region QuitPageHTML

    public static string QuitPagehtml = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta http-equiv='X-Frame-Options' content='SAMEORIGIN'>
<meta http-equiv='X-Content-Type-Options' content='nosniff'>
<meta http-equiv='Referrer-Policy' content='strict-origin-when-cross-origin'>
<title>Server Shutting Down</title>
<link rel=""icon"" type=""image/png"" href=""/favicon.ico""/>
<style>
body {
  font-family: 'Segoe UI', Tahoma, sans-serif;
  background: linear-gradient(135deg, #2c3e50, #4ca1af);
  background-repeat: no-repeat;
  background-attachment: fixed;
  background-size: cover;
  color: #f0f0f0;
  margin: 0;
  min-height: 100vh;
}
.container {
  max-width: 600px;
  margin: 60px auto;
  background: rgba(0,0,0,0.75);
  padding: 20px;
  border-radius: 12px;
  text-align: center;
  animation: fadeIn 0.6s ease-out;
}
h1 { margin-bottom: 18px; }
.theme-toggle {
  background: rgba(255,255,255,0.15);
  border: none;
  color: #fff;
  padding: 8px 14px;
  border-radius: 20px;
  cursor: pointer;
  font-size: 13px;
  transition: background 0.3s, transform 0.2s;
  margin-bottom: 16px;
}
.theme-toggle:hover { background: rgba(255,255,255,0.25); transform: translateY(-1px); }
@keyframes fadeIn { from{opacity:0;transform:translateY(10px);} to{opacity:1;transform:translateY(0);} }
body.light { background: linear-gradient(135deg,#f4f4f4,#fff); color:#222; }
body.light .container { background: rgba(255,255,255,0.9); }
body.light .theme-toggle { background: rgba(0,0,0,0.1); color:#000; }
footer { margin-top:20px; font-size:12px; color:#aaa; }
</style>
</head>
<body>
<div class='container'>
<button class='theme-toggle' id='themeToggle'>🌙 Dark Mode</button>
<h1>Server Shut Down...</h1>
<footer>© Copyright bmgjet</footer>
</div>
<script>
const themeToggle = document.getElementById('themeToggle');
const currentTheme = localStorage.getItem('theme') || 'dark';
document.body.classList.toggle('light', currentTheme === 'light');
function updateToggleLabel() { themeToggle.textContent = document.body.classList.contains('light') ? '🌙 Dark Mode' : '☀️ Light Mode'; }
updateToggleLabel();
themeToggle.addEventListener('click', () => {
  document.body.classList.toggle('light');
  localStorage.setItem('theme', document.body.classList.contains('light') ? 'light' : 'dark');
  updateToggleLabel();
});
</script>
</body>
</html>";
    #endregion

    #region PasswordPage HTML
    public static string PasswordPagehtml = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta http-equiv='X-Frame-Options' content='SAMEORIGIN'>
<meta http-equiv='X-Content-Type-Options' content='nosniff'>
<meta http-equiv='Referrer-Policy' content='strict-origin-when-cross-origin'>
<title>Enter Shutdown Password</title>
<link rel=""icon"" type=""image/png"" href=""/favicon.ico""/>
<style>
body {
  font-family: 'Segoe UI', Tahoma, sans-serif;
  background: linear-gradient(135deg, #2c3e50, #4ca1af);
  background-repeat: no-repeat;
  background-attachment: fixed;
  background-size: cover;
  color: #fff;
  margin: 0;
  min-height: 100vh;
}
.container {
  max-width: 600px;
  margin: 60px auto;
  background: rgba(0,0,0,0.75);
  padding: 20px;
  border-radius: 12px;
  text-align: center;
  animation: fadeIn 0.6s ease-out;
}
h1 { margin-bottom: 18px; }
form { display: flex; flex-direction: column; gap: 10px; align-items:center; }
input[type=password], input[type=submit] {
  padding: 10px;
  font-size: 16px;
  border: none;
  border-radius: 8px;
  width: 100%;
  max-width: 360px;
  text-align: center;
}
input[type=submit] {
  background-color: #e74c3c;
  color: white;
  cursor: pointer;
  font-weight: 700;
  transition: background 0.2s, transform 0.15s;
}
input[type=submit]:hover { background-color: #c0392b; transform: translateY(-1px); }
input[type=submit]:active { transform: scale(0.98); }
#status { margin-top: 12px; font-size: 14px; color: #f9c; }
.theme-toggle {
  background: rgba(255,255,255,0.15);
  border: none;
  color: #fff;
  padding: 8px 14px;
  border-radius: 20px;
  cursor: pointer;
  font-size: 13px;
  transition: background 0.3s, transform 0.2s;
  margin-bottom: 16px;
}
.theme-toggle:hover { background: rgba(255,255,255,0.25); transform: translateY(-1px); }
@keyframes fadeIn { from{opacity:0;transform:translateY(10px);} to{opacity:1;transform:translateY(0);} }
body.light { background: linear-gradient(135deg,#f4f4f4,#fff); color:#222; }
body.light .container { background: rgba(255,255,255,0.9); }
body.light input[type=password], body.light input[type=submit] { color:#000; background:#fff; }
body.light input[type=submit] { background:#3498db; }
body.light input[type=submit]:hover { background:#2980b9; }
body.light .theme-toggle { background: rgba(0,0,0,0.1); color:#000; }
footer { margin-top:20px; font-size:12px; color:#aaa; }
</style>
</head>
<body>
<div class='container'>
  <button class='theme-toggle' id='themeToggle'>🌙 Dark Mode</button>
  <h1>Enter Shutdown Password</h1>
  <form method='get' action='/quit'>
    <input type='password' name='password' placeholder='Password' required />
    <input type='submit' value='Shutdown Server' />
  </form>
  <div id='status'>Incorrect or missing password.</div>
  <div style='margin-top:20px;'>
    <a href='/' style='color:#4ca1af;text-decoration:none;font-weight:bold;'>← Back to Generator</a>
  </div>
  <footer>© Copyright bmgjet</footer>
</div>
<script>
const themeToggle = document.getElementById('themeToggle');
const currentTheme = localStorage.getItem('theme') || 'dark';
document.body.classList.toggle('light', currentTheme === 'light');
function updateToggleLabel() { themeToggle.textContent = document.body.classList.contains('light') ? '🌙 Dark Mode' : '☀️ Light Mode'; }
updateToggleLabel();
themeToggle.addEventListener('click', () => {
  document.body.classList.toggle('light');
  localStorage.setItem('theme', document.body.classList.contains('light') ? 'light' : 'dark');
  updateToggleLabel();
});
</script>
</body>
</html>";
    #endregion

    #region JobsPage HTML

    public static string JobsPagehtml = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta http-equiv='X-Frame-Options' content='SAMEORIGIN'>
<meta http-equiv='X-Content-Type-Options' content='nosniff'>
<meta http-equiv='Referrer-Policy' content='strict-origin-when-cross-origin'>
<title>Rust Map Genny - Jobs</title>
<link rel=""icon"" type=""image/png"" href=""/favicon.ico""/>
<style>
body {
      font-family:'Segoe UI', Tahoma, sans-serif;
      background: linear-gradient(135deg,#2c3e50,#4ca1af);
      background-repeat: no-repeat;
      background-attachment: fixed;
      background-size: cover;
      color:#f0f0f0;
      margin:0;
      min-height:100vh;
}
.container { max-width:980px; margin:20px auto; background:rgba(0,0,0,0.75); padding:20px; border-radius:12px; }
    h1 { text-align:center; margin:0 0 12px 0; }
    .form-group { margin-bottom:12px; display:flex; flex-direction:column; }
    label { font-weight:600; margin-bottom:6px; }
    input, select, textarea {
    background: #1e1e1e;
    color: #f0f0f0;
    border: 1px solid #444;
    border-radius: 6px;
    padding: 8px;
    transition: background 0.2s, box-shadow 0.2s;
    }
    input[type=file] {
    background: #2a2a2a;
    color: #f0f0f0;
    border: 1px solid #555;
}
.btn { margin-top:10px; padding:10px 14px; font-size:14px; font-weight:700; border-radius:8px; border:none; background:#27ae60; color:white; cursor:pointer; }
.btn:disabled { background:#666; cursor:not-allowed; }
#deleteStopBtn {
        background-color: #e74c3c;
        color: white;
}
#deleteStopBtn:hover {
        background-color: #c0392b;
}
fieldset {
    border:1px solid #555;
    border-radius:10px;
    margin-top:14px;
    padding:14px;
    box-shadow:0 0 8px rgba(0,0,0,0.2);
}
.job-list { display:flex; flex-direction:column; gap:8px; margin-top:8px; }
.job-item { display:flex; align-items:center; gap:10px; background:rgba(255,255,255,0.05); padding:6px 8px; border-radius:6px; }
.job-item img { width:50px; height:50px; border-radius:6px; background:#333; }
.job-item span { flex:1; }
.small { font-size:12px; color:#aaa; }
#consoleBox {
    width:100%;
    height:250px;
    background:#0a0a0a;
    color:#0f0;
    font-family:monospace;
    font-size:13px;
    padding:10px;
    border-radius:8px;
    border:none;
    box-shadow:inset 0 0 10px #000;
}
#imageModal {
      position: fixed;
      top: 0;
      left: 0;
      right:0;
      bottom:0;
      background: rgba(0,0,0,0.85);
      display: none;
      align-items: center;
      justify-content: center;
      z-index: 9999;
}
#imageModal img {
      max-width: 90%;
      max-height: 90%;
      border-radius: 12px;
      box-shadow: 0 0 20px rgba(0,0,0,0.5);
}
</style>
</head>
<body>
<div class=""container"">
<h1>Rust Map Genny - Jobs</h1>
<!-- Upload Jobs -->
<fieldset>
<legend>Upload Jobs (.zip)</legend>
<div class=""form-group"">
<label for=""jobsFile"">Jobs Zip File</label>
<input type=""file"" id=""jobsFile"" accept="".zip"">
<button class=""btn"" id=""uploadRunBtn"">Upload & Run All Jobs</button>
<button class=""btn"" id=""deleteStopBtn"">Stop All Jobs & Delete</button>
</div>
<div id='runtimeBox' style='margin-top:14px;font-weight:700;'></div>
<textarea id='consoleBox' readonly></textarea>
</fieldset>
<$JOBSDATA$>
<div style='margin-top:20px;'>
<a href='/' style='color:#4ca1af;text-decoration:none;font-weight:bold;'>← Back to Generator</a>
</div>
<footer>© Copyright bmgjet</footer>
</div>
<div id=""imageModal"">
<img src="""" alt=""Preview"">
</div>
<script>
    document.addEventListener('DOMContentLoaded', () => {
      const consoleBox = document.getElementById('consoleBox');
      const runtimeBox = document.getElementById('runtimeBox');
      const uploadRunBtn = document.getElementById('uploadRunBtn');
      const fileInput = document.getElementById('jobsFile');
      const deleteStopBtn = document.getElementById('deleteStopBtn');
      const imageModal = document.getElementById('imageModal');
      const modalImg = imageModal ? imageModal.querySelector('img') : null;
      let polling = null;
      let uploadDisableCount = parseInt(localStorage.getItem('uploadDisableCount')) || 0;
      async function poll() {
        try {
          if (uploadDisableCount > 0) { uploadDisableCount--; localStorage.setItem('uploadDisableCount', uploadDisableCount); }
          const res = await fetch('/status?nocache=' + Date.now(), { cache: 'no-store' });
          const data = await res.json();
          runtimeBox.textContent = `Uptime: ${data.uptime || 'N/A'}`;
          consoleBox.value = data.console || '';
          consoleBox.scrollTop = consoleBox.scrollHeight;
          const generating = !!data.generating;
          if (uploadDisableCount <= 0) {
          uploadRunBtn.disabled = generating;
          fileInput.disabled = generating;
          }
          const refreshFlag = parseInt(data.refreshpage, 10) || 0;
          if (refreshFlag === 1) {
            console.log('Forced Refresh Of Page');
            localStorage.setItem('uploadDisableCount', 0);
            await fetch('/status/ackrefresh', { method: 'GET' }).catch(() => {});
            setTimeout(() => location.reload(), 1000);
          }
        } catch (err) {
          console.error('Polling error:', err);
        }
      }
      function startPolling() {
        if (polling) clearInterval(polling);
        poll();
        polling = setInterval(poll, 1000);
      }
      uploadRunBtn.addEventListener('click', async () => {
      const file = fileInput.files[0];
      if (!file) {
        alert('Please select a jobs zip file.');
        return;
      }
      uploadDisableCount = 10;
      localStorage.setItem('uploadDisableCount', uploadDisableCount);
      uploadRunBtn.disabled = true;
      fileInput.disabled = true;
      await new Promise(r => setTimeout(r));
      try {
        const res = await fetch('/jobs', { method: 'POST', body: file });
        if (res.ok) {
          console.log('Jobs uploading, refreshing in 10 seconds...');
          setTimeout(() => location.reload(), 10000);
        } else { alert('Upload failed: ' + (await res.text())); }
      } catch (err) { alert('Error uploading: ' + err.message); } 
    }); 
      if(deleteStopBtn)
     {
      deleteStopBtn.addEventListener('click', async () => {
        if (!confirm('Stop all jobs and delete data?')) return;
        try {
          localStorage.setItem('uploadDisableCount', 0);
          const res = await fetch('/stopdelete', { method: 'POST' });
          if (res.ok) {
            console.log('Jobs stopped and deleted. Refreshing in 10 seconds...');
            setTimeout(() => location.reload(), 10000);
            alert('Restarting Server Please Wait 10 Sec');
          } else {
            alert('Failed: ' + (await res.text()));
          }
        } catch (err) {
          alert('Error: ' + err.message);
        }
      });
      }
      if (imageModal && modalImg) {
        document.body.addEventListener('mousedown', e => {
          if (e.target.tagName === 'IMG' && e.target.closest('.job-item')) {
            modalImg.src = e.target.src;
            imageModal.style.display = 'flex';
          }
        });
        document.body.addEventListener('mouseup', () => {imageModal.style.display = 'none';});
        imageModal.addEventListener('click', () => { imageModal.style.display = 'none'; });
}
startPolling();
});
</script>
</body>
</html>";
    #endregion

    #region StopPasswordPage
    public static string StopPasswordPagehtml = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta http-equiv='X-Frame-Options' content='SAMEORIGIN'>
<meta http-equiv='X-Content-Type-Options' content='nosniff'>
<meta http-equiv='Referrer-Policy' content='strict-origin-when-cross-origin'>
<title>Confirm Map Jobs Shutdown</title>
<link rel=""icon"" type=""image/png"" href=""/favicon.ico""/>
<style>
body {
  font-family: 'Segoe UI', Tahoma, sans-serif;
  background: linear-gradient(135deg, #2c3e50, #4ca1af);
  background-repeat: no-repeat;
  background-attachment: fixed;
  background-size: cover;
  color: #fff;
  margin: 0;
  min-height: 100vh;
}
.container {
  max-width: 600px;
  margin: 60px auto;
  background: rgba(0,0,0,0.75);
  padding: 20px;
  border-radius: 12px;
  text-align: center;
  animation: fadeIn 0.6s ease-out;
}
h1 { margin-bottom: 18px; }
p { margin-bottom: 24px; font-size: 15px; color: #ccc; }
form { display: flex; flex-direction: column; gap: 10px; align-items:center; }
input[type=password], input[type=submit] {
  padding: 10px;
  font-size: 16px;
  border: none;
  border-radius: 8px;
  width: 100%;
  max-width: 360px;
  text-align: center;
}
input[type=submit] {
  background-color: #e74c3c;
  color: white;
  cursor: pointer;
  font-weight: 700;
  transition: background 0.2s, transform 0.15s;
}
input[type=submit]:hover { background-color: #c0392b; transform: translateY(-1px); }
input[type=submit]:active { transform: scale(0.98); }
#status { margin-top: 12px; font-size: 14px; color: #f9c; }
.theme-toggle {
  background: rgba(255,255,255,0.15);
  border: none;
  color: #fff;
  padding: 8px 14px;
  border-radius: 20px;
  cursor: pointer;
  font-size: 13px;
  transition: background 0.3s, transform 0.2s;
  margin-bottom: 16px;
}
.theme-toggle:hover { background: rgba(255,255,255,0.25); transform: translateY(-1px); }
@keyframes fadeIn { from{opacity:0;transform:translateY(10px);} to{opacity:1;transform:translateY(0);} }
body.light { background: linear-gradient(135deg,#f4f4f4,#fff); color:#222; }
body.light .container { background: rgba(255,255,255,0.9); }
body.light input[type=password], body.light input[type=submit] { color:#000; background:#fff; }
body.light input[type=submit] { background:#3498db; }
body.light input[type=submit]:hover { background:#2980b9; }
body.light .theme-toggle { background: rgba(0,0,0,0.1); color:#000; }
footer { margin-top:20px; font-size:12px; color:#aaa; }
a.back-link {
  display: inline-block;
  margin-top: 20px;
  color: #4ca1af;
  text-decoration: none;
  font-weight: bold;
}
a.back-link:hover { text-decoration: underline; }
</style>
</head>
<body>
<div class='container'>
  <button class='theme-toggle' id='themeToggle'>🌙 Dark Mode</button>
  <h1>Confirm Shutdown & Delete</h1>
  <p>This action will <strong>shut down</strong> and <strong>delete all Map Jobs</strong>. Please enter the shutdown password to continue.</p>
  <form method='get' action='/stopdelete'>
    <input type='password' name='password' placeholder='Enter Password' required />
    <input type='submit' value='Confirm Shutdown & Delete' />
  </form>
  <div id='status'></div>
  <a href='/jobs' class='back-link'>← Back to Jobs</a>
  <footer>© Copyright bmgjet</footer>
</div>
<script>
const themeToggle = document.getElementById('themeToggle');
const currentTheme = localStorage.getItem('theme') || 'dark';
document.body.classList.toggle('light', currentTheme === 'light');
function updateToggleLabel() { themeToggle.textContent = document.body.classList.contains('light') ? '🌙 Dark Mode' : '☀️ Light Mode'; }
updateToggleLabel();
themeToggle.addEventListener('click', () => {
  document.body.classList.toggle('light');
  localStorage.setItem('theme', document.body.classList.contains('light') ? 'light' : 'dark');
  updateToggleLabel();
});
</script>
</body>
</html>";
    #endregion
}