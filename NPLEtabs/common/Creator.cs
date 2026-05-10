using ETABSv1;
using NPL.SBIM.Rhi.App.Item;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NPLEtabs.common
{
    /// <summary>
    /// Create property for model
    /// </summary>
    internal static class CreateSection
    {
        #region material
        public static string EnsureConcreteMaterial(cSapModel SapModel)
        {
            string mat = "CONC";

            int count = 0;
            string[] names = null;
            SapModel.PropMaterial.GetNameList(ref count, ref names);

            if (names != null && names.Contains(mat))
                return mat;

            // tạo vật liệu bê tông
            SapModel.PropMaterial.SetMaterial(mat, eMatType.Concrete);

            // thông số cơ bản
            SapModel.PropMaterial.SetMPIsotropic(mat, 2.5e10, 0.2, 1e-5);

            return mat;
        }

        public static string EnsureSteelMaterial(cSapModel SapModel)
        {
            string target = "STEEL";

            int count = 0;
            string[] names = null;

            SapModel.PropMaterial.GetNameList(ref count, ref names);

            // ✔ đã tồn tại
            if (names != null && names.Contains(target))
                return target;

            string matName = "Temp";

            SapModel.PropMaterial.AddMaterial(
                ref matName,
                eMatType.Steel,
                "United States",
                "ASTM A36",
                "Grade 36",   // 🔥 FIX ở đây
                ""
            );

            SapModel.PropMaterial.ChangeName(matName, target);

            return target;
        }
        #endregion
        
        public static void CreateSectionsFromRhinoItems(cSapModel SapModel, ObservableCollection<RhinoObjectTypeItem> RhinoTypeItems)
        {
            if (RhinoTypeItems == null || RhinoTypeItems.Count == 0)
                return;
            SapModel.SetModelIsLocked(false);
            SapModel.SetPresentUnits(eUnits.kN_m_C);
           
            EnsureSteelMaterial(SapModel);

            HashSet<string> created = new HashSet<string>();

            foreach (var item in RhinoTypeItems)
            {
                if (item == null)
                    continue;             

                string typeName = item.RhinoTypeName;

                if (string.IsNullOrWhiteSpace(typeName))
                    continue;

                // 🔥 normalize
                typeName = typeName.Trim().ToUpper()
                                   .Replace(".", "-");

                // tránh trùng
                if (created.Contains(typeName))
                    continue;

                created.Add(typeName);

                if (typeName.StartsWith("I-"))
                {
                    CreateISectionFromString(SapModel, typeName, "Steel");
                }
                else if (typeName.StartsWith("RHS-"))
                {
                    CreateRHSSectionFromString(SapModel, typeName, "Steel");
                }
                else if (typeName.StartsWith("B") || typeName.StartsWith("C"))
                {
                    CreateRCSection(SapModel, typeName);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Unknown section: {typeName}");
                }
            }

            System.Windows.MessageBox.Show($"Created {created.Count} sections");
        }

        #region conc sections

        private enum RCSectionType
        {
            Rectangle,
            Circle,
            Unknown
        }

        private static RCSectionType DetectRCSectionType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return RCSectionType.Unknown;

            name = name.ToUpper();

            if (name.Contains("X"))
                return RCSectionType.Rectangle;

            // không có x → circle
            if (name.StartsWith("C") || name.StartsWith("D"))
                return RCSectionType.Circle;

            return RCSectionType.Unknown;
        }

        
        public static bool CreateRCSection(cSapModel SapModel, string secName)
        {
            var type = DetectRCSectionType(secName);

            switch (type)
            {
                case RCSectionType.Rectangle:
                    return CreateConcreteRectSection(SapModel, secName);

                case RCSectionType.Circle:
                    return CreateConcreteCircleSection(SapModel, secName);

                default:
                    System.Diagnostics.Debug.WriteLine($"❌ Unknown RC section: {secName}");
                    return false;
            }
        }

        public static bool CreateConcreteRectSection(cSapModel SapModel, string secName)
        {
            try
            {
                string mat = EnsureConcreteMaterial(SapModel);

                string name = secName.ToUpper();

                // 🔥 Regex lấy 2 số
                var match = System.Text.RegularExpressions.Regex.Match(
                    name,
                    @"(\d+)\s*[xX]\s*(\d+)"
                );

                if (!match.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Parse RECT fail: {secName}");
                    return false;
                }

                double b_mm = double.Parse(match.Groups[1].Value);
                double h_mm = double.Parse(match.Groups[2].Value);

                double b = b_mm / 1000.0;
                double h = h_mm / 1000.0;

                int ret = SapModel.PropFrame.SetRectangle(
                    secName,
                    mat,
                    h,
                    b
                );

                System.Diagnostics.Debug.WriteLine($"Create RECT {secName} → ret = {ret}");

                return ret == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Rect error: {secName} | {ex.Message}");
                return false;
            }
        }

        public static bool CreateConcreteCircleSection(cSapModel SapModel, string secName)
        {
            try
            {
                string mat = EnsureConcreteMaterial(SapModel);

                string num = new string(secName.Where(char.IsDigit).ToArray());

                if (!double.TryParse(num, out double d_mm))
                    return false;

                double d = d_mm / 1000.0;

                int ret = SapModel.PropFrame.SetCircle(
                    secName,
                    mat,
                    d
                );

                return ret == 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion
        public static bool CreateISectionFromString(cSapModel SapModel, string name, string mat)
        {
            try
            {
                string s = name.ToUpper();

                var match = System.Text.RegularExpressions.Regex.Match(
                    s,
                    @"I[\-\s]?(\d+)\s*[xX]\s*(\d+)\s*[xX]\s*(\d+)\s*[xX]\s*(\d+)"
                );

                if (!match.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Parse I fail: {name}");
                    return false;
                }

                double h = double.Parse(match.Groups[1].Value) / 1000.0;
                double b = double.Parse(match.Groups[2].Value) / 1000.0;
                double tw = double.Parse(match.Groups[3].Value) / 1000.0;
                double tf = double.Parse(match.Groups[4].Value) / 1000.0;

                int ret = SapModel.PropFrame.SetISection_1(
                    name,
                    mat,
                    h, b,
                    tf, tf,
                    tw,
                    b, b,
                    -1,
                    "",
                    ""
                );

                return ret == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ I error: {name} | {ex.Message}");
                return false;
            }
        }
        public static bool CreateRHSSectionFromString(cSapModel SapModel, string name, string mat)
        {
            try
            {
                string s = name.ToUpper();

                var match = System.Text.RegularExpressions.Regex.Match(
                    s,
                    @"RHS[\-\s]?(\d+)\s*[xX]\s*(\d+)\s*[xX]\s*(\d+)(?:\s*[xX]\s*(\d+))?"
                );

                if (!match.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Parse RHS fail: {name}");
                    return false;
                }

                double h = double.Parse(match.Groups[1].Value) / 1000.0;
                double b = double.Parse(match.Groups[2].Value) / 1000.0;

                double t1 = double.Parse(match.Groups[3].Value);
                double t2 = match.Groups[4].Success ? double.Parse(match.Groups[4].Value) : t1;

                double t = Math.Min(t1, t2) / 1000.0;

                int ret = SapModel.PropFrame.SetTube(
                    name,
                    mat,
                    h, b,
                    t, t
                );

                return ret == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ RHS error: {name} | {ex.Message}");
                return false;
            }
        }
        public static bool SectionExists(cSapModel SapModel, string name)
        {
            double A = 0, As2 = 0, As3 = 0, J = 0, I22 = 0, I33 = 0;
            double S22 = 0, S33 = 0, Z22 = 0, Z33 = 0, R22 = 0, R33 = 0;

            int ret = SapModel.PropFrame.GetSectProps(
                name,
                ref A, ref As2, ref As3,
                ref J, ref I22, ref I33,
                ref S22, ref S33,
                ref Z22, ref Z33,
                ref R22, ref R33
            );

            return ret == 0;
        }
        public static void CreateSlabThickness(cSapModel sapModel, string name, double thickness)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            int count = 0;
            string[] names = null;
            sapModel.PropArea.GetNameList(ref count, ref names);

            if (names != null && names.Contains(name))
                return;

            int ret = sapModel.PropArea.SetSlab(
                name,
                eSlabType.Slab,
                eShellType.ShellThin,
                "CONC",
                thickness);

            if (ret != 0)
            {
                MessageBox.Show($"❌ SLAB FAIL: {name} | ret={ret}");
            }
        }
        
        public static void CreateWallProperty(cSapModel sapModel, string name, double thickness)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            int count = 0;
            string[] names = null;
            sapModel.PropArea.GetNameList(ref count, ref names);

            if (names != null && names.Contains(name))
                return;

            int ret = sapModel.PropArea.SetWall(
                name,
                eWallPropType.Specified,
                eShellType.ShellThin,
                "CONC",
                thickness,
                -1,
                "",
                ""
            );

            if (ret != 0)
            {
                MessageBox.Show($"❌ WALL FAIL: {name} | ret={ret}");
            }
        }
        public static void EnsureAreaSection(cSapModel sapModel, string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName)) return;

            string name = NormalizeAreaName(layerName);
            double thickness = ExtractThickness(name);

            var type = sGeneral.GetAreaType(name);

            switch (type)
            {
                case sGeneral.AreaType.Slab:
                    CreateSlab(sapModel, name, thickness);
                    break;

                case sGeneral.AreaType.Wall:
                    CreateWall(sapModel, name, thickness);
                    break;
            }
        }
        private static string NormalizeAreaName(string layer)
        {
            string s = layer.ToUpper();

            if (s.Contains("/"))
                s = s.Split('/').Last();

            return s.Trim().Replace(" ", "");
        }
        private static double ExtractThickness(string name)
        {
            var match = System.Text.RegularExpressions.Regex.Match(name, @"\d+");

            if (!match.Success)
                return 0.2;

            return double.Parse(match.Value) / 1000.0;
        }
        private static void CreateSlab(cSapModel sapModel, string name, double t)
        {
            string mat = EnsureConcreteMaterial(sapModel);

            int count = 0;
            string[] names = null;
            sapModel.PropArea.GetNameList(ref count, ref names);

            if (names != null && names.Contains(name))
                return;

            int ret = sapModel.PropArea.SetSlab(
                name,
                eSlabType.Slab,
                eShellType.ShellThin,
                mat,
                t
            );

            if (ret != 0)
                System.Diagnostics.Debug.WriteLine($"❌ SLAB FAIL: {name}");
        }
        private static void CreateWall(cSapModel sapModel, string name, double t)
        {
            string mat = EnsureConcreteMaterial(sapModel);

            int count = 0;
            string[] names = null;
            sapModel.PropArea.GetNameList(ref count, ref names);

            if (names != null && names.Contains(name))
                return;

            int ret = sapModel.PropArea.SetWall(
                name,
                eWallPropType.Specified,
                eShellType.ShellThin,
                mat,
                t
            );

            if (ret != 0)
                System.Diagnostics.Debug.WriteLine($"❌ WALL FAIL: {name}");
        }
    }


    /// <summary>
    /// Create Grids Class
    /// </summary>
    internal class CreateGrids
    {
    }
}
