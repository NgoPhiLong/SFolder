using ETABSv1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPL.SBIM.Rhi.App.Item;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NPLEtabs.common
{
    internal class UpdateETABSModel
    {
        
        private static bool IsFrame(RhinoObjectInfo obj)
        {
            return obj.ObjectCategory?.ToUpper().Contains("CURVE") == true;
        }
        private static bool IsSame(double a, double b, double tol = 1e-6)
        {
            return Math.Abs(a - b) < tol;
        }
        private static List<(double x, double y, double z)> GetBoundaryPoints(List<string> raw)
        {
            return raw
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(PointUtils.ParsePoint)
                .Select(p => (p.x, p.y, p.z))
                .ToList();
        }
        private static bool IsSamePolygon(
    List<(double x, double y, double z)> a,
    List<(double x, double y, double z)> b,
    double tol = 1e-6)
        {
            if (a.Count != b.Count) return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (!IsSame(a[i].x, b[i].x, tol) ||
                    !IsSame(a[i].y, b[i].y, tol) ||
                    !IsSame(a[i].z, b[i].z, tol))
                    return false;
            }

            return true;
        }
        private static int CreateAreaFromPoints(
    cSapModel sapModel,
    List<(double x, double y, double z)> pts,
    string prop,
    ref string name)
        {
            int n = pts.Count;

            double[] x = pts.Select(p => p.x).ToArray();
            double[] y = pts.Select(p => p.y).ToArray();
            double[] z = pts.Select(p => p.z).ToArray();

            return sapModel.AreaObj.AddByCoord(
                n,
                ref x,
                ref y,
                ref z,
                ref name,
                prop
            );
        }
       
        private static bool IsArea(RhinoObjectInfo obj)
        {
            return obj.Boundary != null && obj.Boundary.Count >= 3;
        }
        private static Dictionary<string, string> BuildFrameGuidMap(cSapModel sapModel)
        {
            var dict = new Dictionary<string, string>();

            int count = 0;
            string[] names = null;
            sapModel.FrameObj.GetNameList(ref count, ref names);

            foreach (var name in names ?? new string[0])
            {
                string guid = "";
                sapModel.FrameObj.GetGUID(name, ref guid);

                if (!string.IsNullOrEmpty(guid))
                    dict[guid] = name;
            }

            return dict;
        }
        private static Dictionary<string, string> BuildAreaGuidMap(cSapModel sapModel)
        {
            var dict = new Dictionary<string, string>();

            int count = 0;
            string[] names = null;
            sapModel.AreaObj.GetNameList(ref count, ref names);

            foreach (var name in names ?? new string[0])
            {
                string guid = "";
                sapModel.AreaObj.GetGUID(name, ref guid);

                if (!string.IsNullOrEmpty(guid))
                    dict[guid] = name;
            }

            return dict;
        }

        public static void FullSync_GUID(
    cSapModel sapModel,
    string rhinoPath)
        {
            if (sapModel == null)
                throw new Exception("SapModel null");

            if (!File.Exists(rhinoPath))
                throw new Exception("Rhino JSON not found");

            sapModel.SetModelIsLocked(false);
            sapModel.SetPresentUnits(eUnits.kN_m_C);

            // =====================================================
            // LOAD JSON
            // =====================================================

            var root = JsonConvert.DeserializeObject<RhinoJsonRoot>(
                File.ReadAllText(rhinoPath)
            );

            if (root == null)
                throw new Exception("Invalid JSON");

            var objs = root.Objects ?? new List<RhinoObjectInfo>();

            // =====================================================
            // BUILD ETABS MAP
            // =====================================================

            var frameMap = BuildFrameGuidMap(sapModel);
            var areaMap = BuildAreaGuidMap(sapModel);

            // =====================================================
            // RHINO GUID SET
            // =====================================================

            var rhinoGuids = new HashSet<string>();

            // =====================================================
            // LOG
            // =====================================================

            List<Log> logs = new List<Log>();

            int created = 0;
            int updated = 0;
            int deleted = 0;
            int skipped = 0;
            int failed = 0;

            // =====================================================
            // SYNC
            // =====================================================

            foreach (var obj in objs)
            {
                var log = new Log()
                {
                    ObjectID = obj.ObjectID,
                    Time = DateTime.Now
                };

                logs.Add(log);

                try
                {
                    // =================================================
                    // GET GUID
                    // =================================================

                    string guid = null;

                    obj.ObjectAttribute?.TryGetValue(
                        "Etabs_GUID",
                        out guid
                    );

                    bool hasGuid = !string.IsNullOrWhiteSpace(guid);

                    // =================================================
                    // FRAME
                    // =================================================

                    if (IsFrame(obj))
                    {
                        log.Type = SyncType.Frame;

                        string section =
                            sGeneral.ParseSectionFromLayer(obj.LayerName);

                        if (string.IsNullOrWhiteSpace(section))
                        {
                            log.Status = SyncStatus.Skipped;
                            log.Message = "Section invalid";
                            skipped++;
                            continue;
                        }

                        CreateSection.CreateRCSection(sapModel, section);

                        var s = PointUtils.ParsePoint(obj.CurveStart);
                        var e = PointUtils.ParsePoint(obj.CurveEnd);

                        // =============================================
                        // CREATE
                        // =============================================

                        if (!hasGuid)
                        {
                            string newName = "";

                            int ret = sapModel.FrameObj.AddByCoord(
                                s.x, s.y, s.z,
                                e.x, e.y, e.z,
                                ref newName,
                                section
                            );

                            if (ret != 0)
                            {
                                log.Status = SyncStatus.Failed;
                                log.Message = "Create frame fail";
                                failed++;
                                continue;
                            }

                            string newGuid = "";

                            sapModel.FrameObj.GetGUID(
                                newName,
                                ref newGuid
                            );

                            if (obj.ObjectAttribute == null)
                                obj.ObjectAttribute =
                                    new Dictionary<string, string>();

                            obj.ObjectAttribute["Etabs_GUID"] = newGuid;

                            rhinoGuids.Add(newGuid);

                            log.EtabsGUID = newGuid;
                            log.EtabsName = newName;
                            log.Section = section;
                            log.Status = SyncStatus.Created;

                            created++;
                            continue;
                        }

                        // =============================================
                        // UPDATE
                        // =============================================

                        rhinoGuids.Add(guid);

                        if (!frameMap.ContainsKey(guid))
                        {
                            log.Status = SyncStatus.Failed;
                            log.Message = "GUID not found in ETABS";
                            failed++;
                            continue;
                        }

                        string frameName = frameMap[guid];

                        // =============================================
                        // GET CURRENT GEOMETRY
                        // =============================================

                        string p1 = "";
                        string p2 = "";

                        sapModel.FrameObj.GetPoints(
                            frameName,
                            ref p1,
                            ref p2
                        );

                        double x1 = 0, y1 = 0, z1 = 0;
                        double x2 = 0, y2 = 0, z2 = 0;

                        sapModel.PointObj.GetCoordCartesian(
                            p1,
                            ref x1,
                            ref y1,
                            ref z1
                        );

                        sapModel.PointObj.GetCoordCartesian(
                            p2,
                            ref x2,
                            ref y2,
                            ref z2
                        );

                        bool sameGeom =
                            IsSame(x1, s.x) &&
                            IsSame(y1, s.y) &&
                            IsSame(z1, s.z) &&
                            IsSame(x2, e.x) &&
                            IsSame(y2, e.y) &&
                            IsSame(z2, e.z);

                        // =============================================
                        // UPDATE GEOMETRY
                        // =============================================

                        if (!sameGeom)
                        {
                            sapModel.FrameObj.Delete(frameName);

                            string newName = "";

                            sapModel.FrameObj.AddByCoord(
                                s.x, s.y, s.z,
                                e.x, e.y, e.z,
                                ref newName,
                                section
                            );

                            string newGuid = "";

                            sapModel.FrameObj.GetGUID(
                                newName,
                                ref newGuid
                            );

                            obj.ObjectAttribute["Etabs_GUID"] = newGuid;

                            rhinoGuids.Remove(guid);
                            rhinoGuids.Add(newGuid);

                            log.EtabsGUID = newGuid;
                            log.EtabsName = newName;
                            log.Section = section;
                            log.Status = SyncStatus.Updated;
                            log.Message = "Geometry updated";

                            updated++;
                            continue;
                        }

                        // =============================================
                        // UPDATE SECTION
                        // =============================================

                        string curSec = "";
                        string auto = "";

                        sapModel.FrameObj.GetSection(
                            frameName,
                            ref curSec,
                            ref auto
                        );

                        if (!string.Equals(
                            curSec,
                            section,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            sapModel.FrameObj.SetSection(
                                frameName,
                                section
                            );

                            log.Status = SyncStatus.Updated;
                            log.Message = "Section updated";

                            updated++;
                        }
                        else
                        {
                            log.Status = SyncStatus.Skipped;
                            skipped++;
                        }
                    }

                    // =================================================
                    // AREA
                    // =================================================

                    else if (IsArea(obj))
                    {
                        log.Type = SyncType.Area;

                        var pts = GetBoundaryPoints(obj.Boundary);

                        if (pts.Count < 3)
                        {
                            log.Status = SyncStatus.Skipped;
                            skipped++;
                            continue;
                        }

                        string prop =
                            obj.LayerName.ToUpper();

                        CreateSection.EnsureAreaSection(
                            sapModel,
                            prop
                        );

                        // =============================================
                        // CREATE
                        // =============================================

                        if (!hasGuid)
                        {
                            string name = "";

                            CreateAreaFromPoints(
                                sapModel,
                                pts,
                                prop,
                                ref name
                            );

                            string newGuid = "";

                            sapModel.AreaObj.GetGUID(
                                name,
                                ref newGuid
                            );

                            if (obj.ObjectAttribute == null)
                                obj.ObjectAttribute =
                                    new Dictionary<string, string>();

                            obj.ObjectAttribute["Etabs_GUID"] = newGuid;

                            rhinoGuids.Add(newGuid);

                            log.EtabsGUID = newGuid;
                            log.EtabsName = name;
                            log.Status = SyncStatus.Created;

                            created++;
                            continue;
                        }

                        rhinoGuids.Add(guid);

                        // =============================================
                        // UPDATE
                        // =============================================

                        if (!areaMap.ContainsKey(guid))
                        {
                            log.Status = SyncStatus.Failed;
                            log.Message = "Area GUID not found";
                            failed++;
                            continue;
                        }

                        string areaName = areaMap[guid];

                        sapModel.AreaObj.Delete(areaName);

                        string newArea = "";

                        CreateAreaFromPoints(
                            sapModel,
                            pts,
                            prop,
                            ref newArea
                        );

                        string areaGuid = "";

                        sapModel.AreaObj.GetGUID(
                            newArea,
                            ref areaGuid
                        );

                        obj.ObjectAttribute["Etabs_GUID"] =
                            areaGuid;

                        rhinoGuids.Remove(guid);
                        rhinoGuids.Add(areaGuid);

                        log.EtabsGUID = areaGuid;
                        log.EtabsName = newArea;
                        log.Status = SyncStatus.Updated;

                        updated++;
                    }
                }
                catch (Exception ex)
                {
                    log.Status = SyncStatus.Failed;
                    log.Message = ex.Message;
                    failed++;
                }
            }

            // =====================================================
            // DELETE ORPHAN FRAME
            // =====================================================

            int fCount = 0;
            string[] fNames = null;

            sapModel.FrameObj.GetNameList(
                ref fCount,
                ref fNames
            );

            foreach (var name in fNames ?? new string[0])
            {
                string g = "";

                sapModel.FrameObj.GetGUID(name, ref g);

                if (string.IsNullOrWhiteSpace(g))
                    continue;

                if (!rhinoGuids.Contains(g))
                {
                    sapModel.FrameObj.Delete(name);
                    deleted++;
                }
            }

            // =====================================================
            // DELETE ORPHAN AREA
            // =====================================================

            int aCount = 0;
            string[] aNames = null;

            sapModel.AreaObj.GetNameList(
                ref aCount,
                ref aNames
            );

            foreach (var name in aNames ?? new string[0])
            {
                string g = "";

                sapModel.AreaObj.GetGUID(name, ref g);

                if (string.IsNullOrWhiteSpace(g))
                    continue;

                if (!rhinoGuids.Contains(g))
                {
                    sapModel.AreaObj.Delete(name);
                    deleted++;
                }
            }

            // =====================================================
            // SAVE JSON BACK
            // =====================================================

            File.WriteAllText(
                rhinoPath,
                JsonConvert.SerializeObject(
                    root,
                    Formatting.Indented
                )
            );

            // =====================================================
            // SAVE LOG
            // =====================================================

            SaveSyncLog(
                rhinoPath,
                logs
            );

            MessageBox.Show(
                $"SYNC DONE\n" +
                $"Created: {created}\n" +
                $"Updated: {updated}\n" +
                $"Deleted: {deleted}\n" +
                $"Skipped: {skipped}\n" +
                $"Failed: {failed}"
            );
        }
        private static void SaveSyncLog(
    string rhinoPath,
    List<Log> logs)
        {
            string folder =
                Path.GetDirectoryName(rhinoPath);

            string file =
                Path.GetFileNameWithoutExtension(rhinoPath);

            string time =
                DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string path = Path.Combine(
                folder,
                $"{file}_sync_log_{time}.json"
            );

            File.WriteAllText(
                path,
                JsonConvert.SerializeObject(
                    logs,
                    Formatting.Indented
                )
            );
        }
    }
}
