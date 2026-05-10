using ETABSv1;
using Newtonsoft.Json;
using NPL.SBIM.Rhi.App.Item;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.IO;

namespace NPLEtabs.common
{
    internal class GenerateModel
    {
        //        public static void DrawFrames_FromLayer_Only(cSapModel _sapModel, ObservableCollection<RhinoObjectInfo> RhinoObjectItems, string rhinoPath)
        //        {
        //            if (_sapModel == null)
        //            {
        //                MessageBox.Show("SapModel = null");
        //                return;
        //            }

        //            if (RhinoObjectItems == null || RhinoObjectItems.Count == 0)
        //            {
        //                MessageBox.Show("Không có dữ liệu");
        //                return;
        //            }

        //            _sapModel.SetPresentUnits(eUnits.kN_m_C);
        //            _sapModel.SetModelIsLocked(false);

        //            // lấy story
        //            int count = 0;
        //            string[] stories = null;
        //            _sapModel.Story.GetNameList(ref count, ref stories);

        //            if (stories == null || stories.Length == 0)
        //            {
        //                MessageBox.Show("Không có Story");
        //                return;
        //            }

        //            string story = stories[0];

        //            int created = 0, skipped = 0, failed = 0;
        //            string name = "";
        //            List<FrameLog> logs = new List<FrameLog>();
        //            foreach (var obj in RhinoObjectItems)
        //            {
        //                try
        //                {
        //                    // ===== FILTER =====
        //                    if (obj.ObjectCategory == null || !obj.ObjectCategory.ToUpper().Contains("CURVE"))
        //                    {
        //                        skipped++;
        //                        continue;
        //                    }

        //                    if (string.IsNullOrWhiteSpace(obj.CurveStart) ||
        //                        string.IsNullOrWhiteSpace(obj.CurveEnd))
        //                    {
        //                        skipped++;
        //                        continue;
        //                    }

        //                    var start = PointUtils.ParsePoint(obj.CurveStart);
        //                    var end = PointUtils.ParsePoint(obj.CurveEnd);

        //                    // tránh line = 0
        //                    if (Math.Abs(start.x - end.x) < 1e-6 &&
        //                        Math.Abs(start.y - end.y) < 1e-6 &&
        //                        Math.Abs(start.z - end.z) < 1e-6)
        //                    {
        //                        skipped++;
        //                        continue;
        //                    }

        //                    // ===== LẤY SECTION =====
        //                    string section = ParseSectionFromLayer(obj.LayerName);

        //                    if (string.IsNullOrWhiteSpace(section))
        //                    {
        //                        System.Diagnostics.Debug.WriteLine($"⚠️ No section: {obj.LayerName}");
        //                        skipped++;
        //                        continue;
        //                    }

        //                    // ===== CHECK TỒN TẠI =====
        //                    if (!CreateSection.SectionExists(_sapModel, section))
        //                    {
        //                        System.Diagnostics.Debug.WriteLine($"❌ Section not found: {section}");
        //                        failed++;
        //                        continue;
        //                    }

        //                    int ret = _sapModel.FrameObj.AddByCoord(
        //    start.x, start.y, start.z,
        //    end.x, end.y, end.z,
        //    ref name,
        //    section,
        //    story
        //);

        //                    if (ret != 0)
        //                    {
        //                        failed++;
        //                        continue;
        //                    }

        //                    // 🔥 LẤY GUID
        //                    string guid = "";
        //                    _sapModel.FrameObj.GetGUID(name, ref guid);

        //                    // 🔥 LOG
        //                    logs.Add(new FrameLog
        //                    {
        //                        ObjectID = obj.ObjectID,
        //                        EtabsName = name,
        //                        EtabsGUID = guid,
        //                        Section = section
        //                    });

        //                    created++;
        //                }


        //                catch (Exception ex)
        //                {
        //                    System.Diagnostics.Debug.WriteLine($"❌ Exception: {ex.Message}");
        //                    failed++;
        //                }
        //            }

        //            string json = JsonConvert.SerializeObject(logs, Formatting.Indented);


        //            if (string.IsNullOrWhiteSpace(rhinoPath) || !File.Exists(rhinoPath))
        //            {
        //                MessageBox.Show("Không tìm thấy file JSON Rhino");
        //                return;
        //            }


        //            // folder
        //            string folder = Path.GetDirectoryName(rhinoPath);

        //            // tên gốc
        //            string fileName = Path.GetFileNameWithoutExtension(rhinoPath);

        //            // 🔥 timestamp
        //            string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        //            // tên file log
        //            string path = Path.Combine(folder, $"{fileName}_etabs_log_{time}.json");


        //            File.WriteAllText(path, json);

        //            System.Diagnostics.Debug.WriteLine($"✅ Log saved: {path}");

        //            MessageBox.Show($"DONE\nCreated: {created}\nSkipped: {skipped}\nFailed: {failed}");
        //        }
        public static void DrawFrames_FromLayer_Only(
            cSapModel _sapModel,
            ObservableCollection<RhinoObjectInfo> RhinoObjectItems,
            //string rhinoPath,
            List<Log> logs)
        {
            if (_sapModel == null)
            {
                MessageBox.Show("SapModel = null");
                return;
            }

            if (RhinoObjectItems == null || RhinoObjectItems.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu");
                return;
            }

            _sapModel.SetPresentUnits(eUnits.kN_m_C);
            _sapModel.SetModelIsLocked(false);

            int created = 0, skipped = 0, failed = 0;

            //List<Log> logs = new List<Log>();

            foreach (var obj in RhinoObjectItems)
            {
                var log = sGeneral.EnsureLog(logs, obj.ObjectID, SyncType.Frame);

                try
                {
                    // ===== FILTER =====
                    if (obj.ObjectCategory == null || !obj.ObjectCategory.ToUpper().Contains("CURVE"))
                    {
                        log.Status = SyncStatus.Skipped;
                        log.Message = "Not curve";
                        skipped++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(obj.CurveStart) ||
                        string.IsNullOrWhiteSpace(obj.CurveEnd))
                    {
                        log.Status = SyncStatus.Skipped;
                        log.Message = "Missing curve";
                        skipped++;
                        continue;
                    }

                    var start = PointUtils.ParsePoint(obj.CurveStart);
                    var end = PointUtils.ParsePoint(obj.CurveEnd);

                    if (Math.Abs(start.x - end.x) < 1e-6 &&
                        Math.Abs(start.y - end.y) < 1e-6 &&
                        Math.Abs(start.z - end.z) < 1e-6)
                    {
                        log.Status = SyncStatus.Skipped;
                        log.Message = "Zero length";
                        skipped++;
                        continue;
                    }

                    string section = ParseSectionFromLayer(obj.LayerName);

                    if (string.IsNullOrWhiteSpace(section))
                    {
                        log.Status = SyncStatus.Skipped;
                        log.Message = "No section";
                        skipped++;
                        continue;
                    }

                    if (!CreateSection.SectionExists(_sapModel, section))
                    {
                        log.Status = SyncStatus.Failed;
                        log.Message = "Section not found";
                        failed++;
                        continue;
                    }

                    string name = "";
                    int ret = _sapModel.FrameObj.AddByCoord(
                        start.x, start.y, start.z,
                        end.x, end.y, end.z,
                        ref name,
                        section
                    );

                    if (ret != 0)
                    {
                        log.Status = SyncStatus.Failed;
                        log.Message = "AddByCoord failed";
                        failed++;
                        continue;
                    }

                    string guid = "";
                    _sapModel.FrameObj.GetGUID(name, ref guid);

                    log.EtabsName = name;
                    log.EtabsGUID = guid;
                    log.Section = section;
                    log.Status = SyncStatus.Created;
                    log.Time = DateTime.Now;

                    created++;
                }
                catch (Exception ex)
                {
                    log.Status = SyncStatus.Failed;
                    log.Message = ex.Message;
                    failed++;
                }
            }

            //SaveLog(rhinoPath, logs);

            MessageBox.Show($"FRAME DONE\nCreated: {created}\nSkipped: {skipped}\nFailed: {failed}");
        }
        private static string ParseSectionFromLayer(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return null;

            string s = layerName.ToUpper();

            // bỏ prefix
            if (s.Contains("/"))
                s = s.Split('/').Last();

            if (s.Contains("_"))
                s = s.Split('_').Last();

            s = s.Trim();

            // ===== I =====
            var iMatch = Regex.Match(s,
                @"I[\-\s]?(\d+)[xX](\d+)[xX](\d+)[xX](\d+)");

            if (iMatch.Success)
                return $"I-{iMatch.Groups[1]}x{iMatch.Groups[2]}x{iMatch.Groups[3]}x{iMatch.Groups[4]}";

            // ===== RHS =====
            var rhsMatch = Regex.Match(s,
                @"RHS[\-\s]?(\d+)[xX](\d+)[xX](\d+)");

            if (rhsMatch.Success)
                return $"RHS-{rhsMatch.Groups[1]}x{rhsMatch.Groups[2]}x{rhsMatch.Groups[3]}";

            // ===== RECT (QUAN TRỌNG: GIỮ PREFIX) =====
            var rectMatch = Regex.Match(s,
                @"([BC])[\-\s]?(\d+)[xX](\d+)");

            if (rectMatch.Success)
                return $"{rectMatch.Groups[1]}{rectMatch.Groups[2]}x{rectMatch.Groups[3]}";

            // ===== CIRCLE =====
            var circleMatch = Regex.Match(s,
                @"([C])[\-\s]?(\d+)");

            if (circleMatch.Success)
                return $"{circleMatch.Groups[1]}{circleMatch.Groups[2]}";

            return null;
        }


        #region slab/wall
        public static int CreateAreaFromBoundary(
    cSapModel sapModel,
    List<string> boundaryRaw,
    string propName,
    out string areaName)
        {
            areaName = "";

            if (boundaryRaw == null)
                return -1;

            // ===== CLEAN DATA =====
            var boundary = boundaryRaw
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToList();

            if (boundary.Count < 3)
                return -2;

            // remove điểm đóng
            if (boundary.First() == boundary.Last())
                boundary.RemoveAt(boundary.Count - 1);

            int n = boundary.Count;

            double[] x = new double[n];
            double[] y = new double[n];
            double[] z = new double[n];

            for (int i = 0; i < n; i++)
            {
                var sp = boundary[i].Split(',');

                if (sp.Length != 3)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ INVALID POINT: {boundary[i]}");
                    return -3;
                }

                if (!double.TryParse(sp[0], out double rx) ||
                    !double.TryParse(sp[1], out double ry) ||
                    !double.TryParse(sp[2], out double rz))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ PARSE FAIL: {boundary[i]}");
                    return -4;
                }

                // 🔥 mapping
                x[i] = rx;
                y[i] = rz;
                z[i] = ry;
            }

            // ===== FINAL CHECK =====
            if (x.Length != y.Length || y.Length != z.Length)
            {
                System.Diagnostics.Debug.WriteLine("❌ ARRAY LENGTH MISMATCH");
                return -5;
            }

            int ret = sapModel.AreaObj.AddByCoord(
                n,
                ref x,
                ref y,
                ref z,
                ref areaName,
                propName
            );

            return ret;
        }

        

        private static string GetFirstStory(cSapModel sapModel)
        {
            int count = 0;
            string[] stories = null;

            sapModel.Story.GetNameList(ref count, ref stories);

            if (stories == null || stories.Length == 0)
                throw new Exception("No Story in ETABS");

            return stories[0];
        }

        private static double ExtractThickness(string name)
        {
            var match = System.Text.RegularExpressions.Regex.Match(name, @"\d+");

            if (!match.Success)
                return 0.2;

            return double.Parse(match.Value) / 1000.0;
        }
        //public static void CreateAreasFromRhino(cSapModel sapModel, ObservableCollection<RhinoObjectInfo> objs)
        //{

        //    string story = GetFirstStory(sapModel);

        //    int created = 0;
        //    int skipped = 0;

        //    foreach (var obj in objs)
        //    {
        //        if (obj.Boundary == null || obj.Boundary.Count < 3)
        //        {
        //            skipped++;
        //            continue;
        //        }

        //        var type = sGeneral.GetAreaType(obj.LayerName);

        //        if (type == sGeneral.AreaType.Unknown)
        //        {
        //            skipped++;
        //            continue;
        //        }
        //        string propName = obj.LayerName.ToUpper();
        //        double thickness = ExtractThickness(propName);

        //        // 🔥 ADD DÒNG NÀY (critical fix)
        //        CreateSection.EnsureAreaSection(sapModel, propName);

        //        string name;

        //        int ret = CreateAreaFromBoundary(
        //            sapModel,
        //            obj.Boundary,
        //            propName,
        //            out name
        //        );



        //        if (ret == 0)
        //            created++;
        //        else
        //            skipped++;
        //    }

        //    MessageBox.Show($"Done\nCreated: {created}\nSkipped: {skipped}");
        //}
        public static void CreateAreasFromRhino(
    cSapModel sapModel,
    ObservableCollection<RhinoObjectInfo> objs,
   // string rhinoPath, 
    List<Log> logs)
        {
            int created = 0;
            int skipped = 0;
            int failed = 0;

            //List<Log> logs = new List<Log>();

            foreach (var obj in objs)
            {
                var log = sGeneral.EnsureLog(logs, obj.ObjectID, SyncType.Area);

                try
                {
                    if (obj.Boundary == null || obj.Boundary.Count < 3)
                    {
                        log.Status = SyncStatus.Skipped;
                        log.Message = "Invalid boundary";
                        skipped++;
                        continue;
                    }

                    var type = sGeneral.GetAreaType(obj.LayerName);

                    if (type == sGeneral.AreaType.Unknown)
                    {
                        log.Status = SyncStatus.Skipped;
                        log.Message = "Unknown type";
                        skipped++;
                        continue;
                    }

                    string propName = obj.LayerName.ToUpper();

                    CreateSection.EnsureAreaSection(sapModel, propName);

                    string name;
                    int ret = CreateAreaFromBoundary(
                        sapModel,
                        obj.Boundary,
                        propName,
                        out name
                    );

                    if (ret != 0)
                    {
                        log.Status = SyncStatus.Failed;
                        log.Message = $"CreateArea failed ({ret})";
                        failed++;
                        continue;
                    }

                    string guid = "";
                    sapModel.AreaObj.GetGUID(name, ref guid);

                    log.EtabsName = name;
                    log.EtabsGUID = guid;
                    log.Section = propName;
                    log.Status = SyncStatus.Created;
                    log.Time = DateTime.Now;

                    created++;
                }
                catch (Exception ex)
                {
                    log.Status = SyncStatus.Failed;
                    log.Message = ex.Message;
                    failed++;
                }
            }

            //SaveLog(rhinoPath, logs);

            MessageBox.Show($"AREA DONE\nCreated: {created}\nSkipped: {skipped}\nFailed: {failed}");
        }
        #endregion
        private static void SaveLog(string rhinoPath, List<Log> logs)
        {
            if (string.IsNullOrWhiteSpace(rhinoPath))
                throw new Exception("rhinoPath null");

            string folder = Path.GetDirectoryName(rhinoPath);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = Path.GetFileNameWithoutExtension(rhinoPath);
            string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string path = Path.Combine(folder, $"{fileName}_etabs_log_{time}.json");

            File.WriteAllText(path, JsonConvert.SerializeObject(logs, Formatting.Indented));

            MessageBox.Show($"Log saved:\n{path}");
        }
        public static void GenerateAll(
    cSapModel sapModel,
    ObservableCollection<RhinoObjectInfo> objs,
    string rhinoPath)
        {
            var logs = new List<Log>();

            // 🔥 tạo frame + ghi log
            DrawFrames_FromLayer_Only(sapModel, objs, logs);

            // 🔥 tạo area + ghi log (cùng list)
            CreateAreasFromRhino(sapModel, objs, logs);

            // 🔥 CHỈ SAVE 1 LẦN
            SaveLog(rhinoPath, logs);

            MessageBox.Show($"DONE\nTotal log: {logs.Count}");
        }
    }
}
