using ETABSv1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPLEtabs.common
{
    public static class EtabsSelectionUtils
    {
        /// <summary>
        /// Select tất cả Area (không lọc)
        /// </summary>
        public static void SelectAllAreas(cSapModel sapModel)
        {
            if (sapModel == null) return;

            sapModel.SetModelIsLocked(false);
            sapModel.SelectObj.ClearSelection();

            int count = 0;
            string[] names = null;

            sapModel.AreaObj.GetNameList(ref count, ref names);

            if (names == null) return;

            foreach (var name in names)
            {
                sapModel.AreaObj.SetSelected(name, true);
            }
        }

        /// <summary>
        /// Select chỉ SLAB (KHÔNG dùng enum → dùng int mapping)
        /// </summary>
        public static void SelectSlabs(cSapModel sapModel)
        {
            if (sapModel == null) return;

            sapModel.SetModelIsLocked(false);
            sapModel.SelectObj.ClearSelection();

            int count = 0;
            string[] names = null;

            sapModel.AreaObj.GetNameList(ref count, ref names);

            if (names == null) return;

            foreach (var name in names)
            {
                string propName = "";
                sapModel.AreaObj.GetProperty(name, ref propName);

                if (string.IsNullOrEmpty(propName)) continue;

                // 🔥 LOGIC CHÍNH
                if (IsSlabProperty(propName))
                {
                    sapModel.AreaObj.SetSelected(name, true);
                }
            }
        }

        private static bool IsSlabProperty(string propName)
        {
            if (string.IsNullOrEmpty(propName)) return false;

            propName = propName.ToUpper();

            // 🔥 RULE TỰ ĐỊNH NGHĨA
            return propName.Contains("SLAB")
                || propName.Contains("SÀN")
                || propName.StartsWith("S");
        }

        /// <summary>
        /// Select slab theo tên property (fallback an toàn)
        /// </summary>
        public static void SelectSlabsByName(cSapModel sapModel, string keyword = "SLAB")
        {
            if (sapModel == null) return;

            sapModel.SetModelIsLocked(false);
            sapModel.SelectObj.ClearSelection();

            int count = 0;
            string[] names = null;

            sapModel.AreaObj.GetNameList(ref count, ref names);

            if (names == null) return;

            foreach (var name in names)
            {
                string propName = "";
                sapModel.AreaObj.GetProperty(name, ref propName);

                if (propName != null && propName.ToUpper().Contains(keyword))
                {
                    sapModel.AreaObj.SetSelected(name, true);
                }
            }
        }

        

        private static bool IsSlab(string propName)
        {
            propName = propName.ToUpper();

            return propName.Contains("SLAB")   // chuẩn quốc tế
                || propName.Contains("SAN")   // tiếng Việt không dấu
                || propName.Contains("SÀN");  // tiếng Việt có dấu
        }

        /// <summary>
        /// Select nhanh theo group
        /// </summary>
        public static void SelectGroup(cSapModel sapModel, string groupName)
        {
            if (sapModel == null) return;

            sapModel.SelectObj.ClearSelection();

            sapModel.AreaObj.SetSelected(groupName, true, eItemType.Group);
        }
    }
}
