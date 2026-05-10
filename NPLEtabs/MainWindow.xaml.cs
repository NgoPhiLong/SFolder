using ETABSv1;
using Newtonsoft.Json;
using NPL.SBIM.Rhi.App.Item;
using NPLEtabs.common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace SBIM.ETABS.App
{
    public partial class MainWindow : Window
    {
        private cSapModel _sapModel;
        public MainViewModel _viewModel { get; set; }
      
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            this.DataContext =_viewModel;
        }

        // ===== CONNECT =====
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var etabs = (cOAPI)Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");
                _sapModel = etabs.SapModel;
                // 🔥 unlock model
                _sapModel.SetModelIsLocked(false);

                _sapModel.SetPresentUnits(eUnits.kN_m_C);

               
                // ép refresh UI
                //_sapModel.View.RefreshView(0, false);

                StatusText.Text = "Connected";


            }
            catch
            {
                MessageBox.Show("Hãy mở ETABS trước!");
            }
        }

        // ===== RUN =====
        private void Draw_Click(object sender, RoutedEventArgs e)
        {
            if (_sapModel == null)
            {
                MessageBox.Show("Chưa connect ETABS");
                return;
            }

            try
            {
                CreateSection.CreateSectionsFromRhinoItems(_sapModel, _viewModel.RhinoTypeItems);
                //GenerateModel.DrawFrames_FromLayer_Only(_sapModel, _viewModel.RhinoObjectItems, _viewModel.rhinoPath);
                //GenerateModel.CreateAreasFromRhino(_sapModel,_viewModel.RhinoObjectItems, _viewModel.rhinoPath);
                GenerateModel.GenerateAll(_sapModel, _viewModel.RhinoObjectItems, _viewModel.rhinoPath);
                StatusText.Text = "Done!";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnGetPath_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.GetRhinoPath();
        }
       
        private void btnUpdateSection_Click(object sender, RoutedEventArgs e)
        {
            UpdateETABSModel.FullSync_GUID(_sapModel,_viewModel.LogPath);
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {

        }

        
        
        
       
       
        private void CleanSections_Click(object sender, RoutedEventArgs e)
        {
            CleanSection.DeleteUnusedFrameSections(_sapModel);
            //CreateSlab6Points(_sapModel);
        }

   
        
        private void CreateSection_Click(object sender, RoutedEventArgs e)
        {
            CreateSection.CreateSectionsFromRhinoItems(_sapModel,_viewModel.RhinoTypeItems);
            //TestCreateISection3(_sapModel,_viewModel.RhinoTypeItems);
        }
   
        //public void UpdateSectionsFromJson()
        //{
        //    if (_sapModel == null)
        //        throw new Exception("SapModel null");

        //    if (_viewModel == null)
        //        throw new Exception("ViewModel null");

        //    string rhinoPath = _viewModel.rhinoPath;

        //    if (string.IsNullOrWhiteSpace(rhinoPath) || !File.Exists(rhinoPath))
        //        throw new Exception("Rhino JSON không hợp lệ");

        //    string folder = Path.GetDirectoryName(rhinoPath);

        //    if (string.IsNullOrEmpty(folder))
        //        throw new Exception("Không lấy được folder");

        //    // 🔥 lấy file log mới nhất
        //    string logPath = Directory.GetFiles(folder, "*_etabs_log_*.json")
        //                              .OrderByDescending(f => f)
        //                              .FirstOrDefault();

        //    if (string.IsNullOrEmpty(logPath))
        //        throw new Exception("Không tìm thấy file log");

        //    // ===== LOAD JSON =====
        //    var rhinoItems = JsonConvert.DeserializeObject<List<RhinoObjectInfo>>(File.ReadAllText(rhinoPath));
        //    var logs = JsonConvert.DeserializeObject<List<FrameLog>>(File.ReadAllText(logPath));

        //    if (rhinoItems == null || logs == null)
        //        throw new Exception("JSON parse lỗi");

        //    // ===== CACHE ETABS FRAME =====
        //    int count = 0;
        //    string[] names = null;
        //    _sapModel.FrameObj.GetNameList(ref count, ref names);

        //    var etabsSet = new HashSet<string>(names ?? new string[0]);

        //    int updated = 0;
        //    int skipped = 0;
        //    int failed = 0;

        //    foreach (var log in logs)
        //    {
        //        try
        //        {
        //            // ===== VALIDATE LOG =====
        //            if (log == null ||
        //                string.IsNullOrEmpty(log.ObjectID) ||
        //                string.IsNullOrEmpty(log.EtabsName))
        //            {
        //                skipped++;
        //                continue;
        //            }

        //            // ===== CHECK FRAME EXIST =====
        //            if (!etabsSet.Contains(log.EtabsName))
        //            {
        //                System.Diagnostics.Debug.WriteLine($"❌ Frame not found: {log.EtabsName}");
        //                failed++;
        //                continue;
        //            }

        //            // ===== MATCH RHINO =====
        //            var rhino = rhinoItems.FirstOrDefault(x => x?.ObjectID == log.ObjectID);

        //            if (rhino == null || string.IsNullOrWhiteSpace(rhino.LayerName))
        //            {
        //                skipped++;
        //                continue;
        //            }

        //            // ===== PARSE SECTION =====
        //            string newSection = sGeneral.ParseSectionFromLayer(rhino.LayerName);

        //            if (string.IsNullOrWhiteSpace(newSection))
        //            {
        //                skipped++;
        //                continue;
        //            }

        //            // ===== CHECK SECTION EXIST =====
        //            if (!CreateSection.SectionExists(_sapModel, newSection))
        //            {
        //                System.Diagnostics.Debug.WriteLine($"❌ Section not found: {newSection}");
        //                failed++;
        //                continue;
        //            }

        //            // ===== SKIP IF SAME =====
        //            if (log.Section == newSection)
        //            {
        //                skipped++;
        //                continue;
        //            }

        //            // ===== UPDATE =====
        //            int ret = _sapModel.FrameObj.SetSection(log.EtabsName, newSection);

        //            if (ret != 0)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"❌ Update fail: {log.EtabsName}");
        //                failed++;
        //                continue;
        //            }

        //            updated++;
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Diagnostics.Debug.WriteLine($"❌ Exception: {ex.Message}");
        //            failed++;
        //        }
        //    }

        //    MessageBox.Show(
        //        $"UPDATE DONE\n" +
        //        $"Updated: {updated}\n" +
        //        $"Skipped: {skipped}\n" +
        //        $"Failed: {failed}"
        //    );
        //}
       
        private List<Log> LoadLog()
        {
            string folder = Path.GetDirectoryName(_viewModel.rhinoPath);

            string logPath = Directory.GetFiles(folder, "*_etabs_log_*.json")
                                      .OrderByDescending(f => f)
                                      .FirstOrDefault();

            if (logPath == null)
                throw new Exception("Không tìm thấy log");

            return JsonConvert.DeserializeObject<List<Log>>(File.ReadAllText(logPath))
                   ?? new List<Log>();
        }
        private HashSet<string> GetEtabsFrameSet()
        {
            int count = 0;
            string[] names = null;

            _sapModel.FrameObj.GetNameList(ref count, ref names);

            return new HashSet<string>(names ?? new string[0]);
        }
        private bool IsValidCurve(RhinoObjectInfo x)
        {
            return x != null
                && !string.IsNullOrEmpty(x.ObjectID)
                && x.ObjectCategory?.ToUpper().Contains("CURVE") == true
                && !string.IsNullOrWhiteSpace(x.CurveStart)
                && !string.IsNullOrWhiteSpace(x.CurveEnd);
        }
        private void SaveLog(List<Log> logs)
        {
            string folder = Path.GetDirectoryName(_viewModel.rhinoPath);
            string file = Path.GetFileNameWithoutExtension(_viewModel.rhinoPath);

            string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string path = Path.Combine(folder, $"{file}_etabs_log_{time}.json");

            File.WriteAllText(path, JsonConvert.SerializeObject(logs, Formatting.Indented));
        }
        private bool DeleteFrame(string name)
        {
            return _sapModel.FrameObj.Delete(name) == 0;
        }
        private bool CreateFrame(XYZ s, XYZ e, string section, ref string newName)
        {
            int ret = _sapModel.FrameObj.AddByCoord(
                s.X, s.Y, s.Z,
                e.X, e.Y, e.Z,
                ref newName,
                section
            );

            return ret == 0;
        }
        private List<XYZ> GetBoundaryClean(List<string> boundary)
        {
            if (boundary == null || boundary.Count < 3)
                return new List<XYZ>();

            var pts = boundary
                .Select(ParsePoint)
                .ToList();

            // 🔥 remove last nếu trùng first
            if (pts.Count > 1)
            {
                var first = pts.First();
                var last = pts.Last();

                if (IsSamePoint(first, last))
                    pts.RemoveAt(pts.Count - 1);
            }

            return pts;
        }
        private bool IsSamePoint(XYZ p1, XYZ p2, double tol = 1e-6)
        {
            return Math.Abs(p1.X - p2.X) < tol &&
                   Math.Abs(p1.Y - p2.Y) < tol &&
                   Math.Abs(p1.Z - p2.Z) < tol;
        }

        
        private bool UpdateFrameGeometry(string name, XYZ s, XYZ e, string section)
        {
            // 🔥 lấy section hiện tại nếu cần
            string sec = section;

            // delete
            int ret = _sapModel.FrameObj.Delete(name);
            if (ret != 0) return false;

            string newName = name;

            // tạo lại
            ret = _sapModel.FrameObj.AddByCoord(
                s.X, s.Y, s.Z,
                e.X, e.Y, e.Z,
                ref newName,
                sec
            );

            return ret == 0;
        }
        private List<RhinoObjectInfo> LoadRhino()
        {
            string path = _viewModel.rhinoPath;

            if (!File.Exists(path))
                throw new Exception("Rhino JSON không tồn tại");

            return JsonConvert.DeserializeObject<List<RhinoObjectInfo>>(File.ReadAllText(path))
                   ?? new List<RhinoObjectInfo>();
        }

        public XYZ ParsePoint(string input)
        {
            // ví dụ: "0,0,3000"
            var parts = input.Split(',');

            double x = double.Parse(parts[0]) / 1000.0;
            double y = double.Parse(parts[1]) / 1000.0;
            double z = double.Parse(parts[2]) / 1000.0;

            return new XYZ(x, y, z);
        }
        public struct XYZ
        {
            public double X;
            public double Y;
            public double Z;

            public XYZ(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }


        public string EnsureSlabSection(cSapModel sapModel, string name, double thickness)
        {
            string mat = CreateSection.EnsureConcreteMaterial(sapModel);

            int ret = sapModel.PropArea.SetSlab(
                name,
                eSlabType.Slab,      // loại sàn
                eShellType.ShellThick,    // 🔥 nên dùng Shell (membrane + bending)
                mat,
                thickness,
                -1,
                "",
                ""
            );

            System.Diagnostics.Debug.WriteLine($"SetSlab {name} → ret = {ret}");

            return name;
        }
        public void CreateSlab(cSapModel sapModel)
        {
            sapModel.SetModelIsLocked(false);
            sapModel.SetPresentUnits(eUnits.kN_m_C);

            string slab = EnsureSlabSection(sapModel, "S200", 0.2);

            double[] x = { 0, 5, 5, 0 };
            double[] y = { 0, 0, 5, 5 };
            double[] z = { 0, 0, 0, 0 };

            string name = "";

            int ret = sapModel.AreaObj.AddByCoord(
                4,
                ref x,
                ref y,
                ref z,
                ref name,
                slab
            );

            MessageBox.Show($"Slab created: {name}");
        }
        public void CreateSlab6Points(cSapModel sapModel)
        {
            sapModel.SetModelIsLocked(false);
            sapModel.SetPresentUnits(eUnits.kN_m_C);

            string slab = EnsureSlabSection(sapModel, "S200", 0.2);

            double[] x = { 0, 4, 6, 5, 2, 0 };
            double[] y = { 0, 0, 2, 5, 4, 2 };
            double[] z = { 0, 0, 0, 0, 0, 0 };

            int n = 6;
            string name = "";

            int ret = sapModel.AreaObj.AddByCoord(
                n,
                ref x,
                ref y,
                ref z,
                ref name,
                slab
            );
        }
        public void CreateRamp(cSapModel sapModel)
        {
            sapModel.SetModelIsLocked(false);
            sapModel.SetPresentUnits(eUnits.kN_m_C);

            string slab = EnsureSlabSection(sapModel, "S200", 0.2);

            // 🔥 ramp nghiêng theo Y
            double[] x = { 0, 5, 5, 0 };
            double[] y = { 0, 0, 5, 5 };
            double[] z = { 0, 0, 1, 1 }; // 👈 khác nhau

            int n = 4;
            string name = "";

            int ret = sapModel.AreaObj.AddByCoord(
                n,
                ref x,
                ref y,
                ref z,
                ref name,
                slab
            );

            MessageBox.Show($"Ramp created: {name}");
        }

       
       
        
      
        
        
        

        private void UpdateModel_Click(object sender, RoutedEventArgs e)
        {
           // UpdateETABSModel.FullSync_GUID(_sapModel,_viewModel.rhinoPath,_viewModel.LogPath);
        }

    

        private void SelSlab_Click(object sender, RoutedEventArgs e)
        {
            EtabsSelectionUtils.SelectSlabs(_sapModel);
        }
    }
}