using ETABSv1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPLEtabs.common
{
    internal class CleanSection
    {
        public static void DeleteUnusedFrameSections(cSapModel SapModel)
        {
            SapModel.SetModelIsLocked(false);

            int ret;

            // 1. Lấy section
            int secCount = 0;
            string[] secNames = null;
            SapModel.PropFrame.GetNameList(ref secCount, ref secNames);

            if (secNames == null || secCount == 0)
                return;

            // 2. Lấy frame
            int frameCount = 0;
            string[] frameNames = null;
            SapModel.FrameObj.GetNameList(ref frameCount, ref frameNames);

            HashSet<string> usedSections = new HashSet<string>();

            // 3. Nếu KHÔNG có frame → xoá hết
            if (frameNames == null || frameCount == 0)
            {
                foreach (var sec in secNames)
                    SapModel.PropFrame.Delete(sec);

                return;
            }

            // 4. Lấy section đang dùng (ONLY direct)
            foreach (var frame in frameNames)
            {
                string sec = "";
                string auto = "";

                SapModel.FrameObj.GetSection(frame, ref sec, ref auto);

                if (!string.IsNullOrEmpty(sec))
                    usedSections.Add(sec);
            }

            // 5. Xoá
            foreach (var sec in secNames)
            {
                if (!usedSections.Contains(sec))
                {
                    SapModel.PropFrame.Delete(sec);
                }
            }
        }
    }
}
