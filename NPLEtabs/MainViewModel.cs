using ETABSv1;
using Newtonsoft.Json;
using NPL.SBIM.Rhi.App.Item;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Win32OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using NPLEtabs.common;

namespace SBIM.ETABS.App
{
    public class MainViewModel : ViewModelBase
    {
        #region Fields
        public cSapModel SapModel { get; set; }

        private RhinoJsonRoot _cachedRoot;

        private string rhinopath;
        public string rhinoPath
        {
            get => rhinopath;
            set
            {
                if (rhinopath != value)
                {
                    rhinopath = value;
                    OnPropertyChanged(nameof(rhinoPath));
                }
            }
        }
        private string logPath;
        public string LogPath
        {
            get => logPath;
            set
            {
                if (logPath != value)
                {
                    logPath = value;
                    OnPropertyChanged(nameof(LogPath));
                }
            }
        }
        public ObservableCollection<RhinoObjectTypeItem> RhinoGSAIDItems { get; set; }
        public ObservableCollection<LayerItem> LayerItems { get; set; }
        public ObservableCollection<RhinoAttributeItem> RhinoAttributes { get; set; }
        public ObservableCollection<RhinoObjectTypeItem> RhinoTypeItems { get; set; }
        public ObservableCollection<RhinoObjectInfo> RhinoObjectItems { get; set; }


        #endregion

        public MainViewModel()
        {

            LayerItems = new ObservableCollection<LayerItem>();
            RhinoTypeItems = new ObservableCollection<RhinoObjectTypeItem>();
            RhinoAttributes = new ObservableCollection<RhinoAttributeItem>();
            RhinoGSAIDItems = new ObservableCollection<RhinoObjectTypeItem>();
        }

        #region LOAD JSON PIPELINE

        public void GetRhinoPath()
        {
            try
            {
                var openDialog = new Win32OpenFileDialog
                {
                    Title = "Chọn file JSON từ Rhino",
                    Filter = "JSON files (*.json)|*.json",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if (openDialog.ShowDialog() == true)
                {
                    rhinoPath = openDialog.FileName;

                    if (Path.GetExtension(rhinoPath).ToLower() != ".json")
                    {
                        MessageBox.Show("ETABS chỉ hỗ trợ file JSON", "Lỗi");
                        return;
                    }

                    LoadJsonData(rhinoPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");
            }
        }

        public void LoadJsonData(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("File không tồn tại");
                return;
            }

            _cachedRoot = JsonConvert.DeserializeObject<RhinoJsonRoot>(
                File.ReadAllText(path)
            );

            if (_cachedRoot?.Objects == null)
            {
                MessageBox.Show("JSON không hợp lệ");
                return;
            }

            // 🔥 ADD DÒNG NÀY
            RhinoObjectItems = new ObservableCollection<RhinoObjectInfo>(_cachedRoot.Objects);
            OnPropertyChanged(nameof(RhinoObjectItems));

            BuildLayers();
            BuildTypes();
            BuildAttributes();
            BuildGSAIDItems();
        }

        
        #endregion

        #region BUILD DATA

        private void BuildLayers()
        {
            var layers = _cachedRoot.Objects
                .Where(IsCurve)
                .Select(x => x.LayerName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .Select(x => new LayerItem
                {
                    LayerName = x,
                    IsSelected = true
                });

            LayerItems = new ObservableCollection<LayerItem>(layers);
            OnPropertyChanged(nameof(LayerItems));
        }

        private void BuildTypes()
        {
            var types = _cachedRoot.Objects
                .Where(IsCurve)
                .Select(x => ResolveRhinoTypeName(x))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .Select(x => new RhinoObjectTypeItem
                {
                    RhinoTypeName = x,
                    IsSelected = true
                });

            RhinoTypeItems = new ObservableCollection<RhinoObjectTypeItem>(types);
            OnPropertyChanged(nameof(RhinoTypeItems));
        }

        private void BuildAttributes()
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var obj in _cachedRoot.Objects.Where(IsCurve))
            {
                if (obj.ObjectAttribute == null) continue;

                foreach (var k in obj.ObjectAttribute.Keys)
                    keys.Add(k);
            }

            RhinoAttributes = new ObservableCollection<RhinoAttributeItem>(
                keys.Select(k => new RhinoAttributeItem
                {
                    KeyName = k,
                    IsSelected =
                        k.Equals("1D_GSAMembID", StringComparison.OrdinalIgnoreCase) ||
                        k.Equals("ID", StringComparison.OrdinalIgnoreCase)
                })
            );

            OnPropertyChanged(nameof(RhinoAttributes));
        }

        private void BuildGSAIDItems()
        {
            var result = new ObservableCollection<RhinoObjectTypeItem>();
            var unique = new HashSet<string>();

            foreach (var obj in _cachedRoot.Objects.Where(IsCurve))
            {
                if (obj.ObjectAttribute == null) continue;

                string id = null;

                if (obj.ObjectAttribute.TryGetValue("1D_GSAMembID", out string id1))
                    id = id1;
                else if (obj.ObjectAttribute.TryGetValue("ID", out string id2))
                    id = id2;

                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!unique.Add(id)) continue;

                result.Add(new RhinoObjectTypeItem
                {
                    RhinoGSAID = id,
                    RhinoTypeName = ResolveRhinoTypeName(obj),
                    CurveStart = obj.CurveStart,
                    CurveEnd = obj.CurveEnd,
                    IsSelected = true
                });
            }

            RhinoGSAIDItems = result;
            OnPropertyChanged(nameof(RhinoGSAIDItems));
        }

        #endregion

        #region HELPER

        public bool IsCurve(RhinoObjectInfo obj)
        {
            return obj.ObjectCategory?.Equals("Curve", StringComparison.OrdinalIgnoreCase) == true
                || obj.ObjectType?.Equals("Curve", StringComparison.OrdinalIgnoreCase) == true;
        }

        private string ResolveRhinoTypeName(RhinoObjectInfo obj)
        {
            if (obj == null)
                return null;

            // Ưu tiên đọc từ Attribute
            if (obj.ObjectAttribute != null &&
                obj.ObjectAttribute.TryGetValue("Section", out string section))
            {
                var parsed = ParseSectionToType(section);
                if (!string.IsNullOrEmpty(parsed))
                    return parsed;
            }

            // Fallback: lấy từ Layer
            if (!string.IsNullOrWhiteSpace(obj.LayerName))
            {
                string raw = obj.LayerName;

                if (raw.Contains("/"))
                    raw = raw.Split('/').Last().Trim();

                return NormalizeTypeName(raw);
            }

            return null;
        }

        private string ParseSectionToType(string section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return null;

            section = section.ToUpper();

            var match = System.Text.RegularExpressions.Regex.Match(
                section,
                @"(I|RHS)[\s\-]*?(\d+)[x\s]*(\d+)[x\s]*(\d+)(?:[x\s]*(\d+))?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (!match.Success)
                return null;

            string type = match.Groups[1].Value.ToUpper();

            string h = match.Groups[2].Value;
            string b = match.Groups[3].Value;
            string t1 = match.Groups[4].Value;
            string t2 = match.Groups[5].Success ? match.Groups[5].Value : t1;

            return $"{type}-{h}x{b}x{t1}x{t2}";
        }
        private string NormalizeTypeName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Trim().ToUpper();

            raw = raw.Replace(".", "-")
                     .Replace("_", "-")
                     .Replace(" ", "");

            // đảm bảo có dấu -
            if (raw.StartsWith("I") && !raw.StartsWith("I-"))
                raw = raw.Insert(1, "-");

            if (raw.StartsWith("RHS") && !raw.StartsWith("RHS-"))
                raw = raw.Insert(3, "-");

            return raw;
        }
        #endregion
       
       
        
        
        
    }
}