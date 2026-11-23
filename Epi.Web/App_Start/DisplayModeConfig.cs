using System;
using System.Collections.Generic;
using System.Web.WebPages;

namespace Epi.Web.MVC
{
    public class DisplayModeConfig
    {
        // Each Insert(0, ...) goes ahead of the previous one, so evaluation order is iPad, Opera Mobi, Android,
        // then the modes already in the list (MVC's built-in "Mobile" and the default mode).
        public static void RegisterDisplayModes(IList<IDisplayMode> modes)
        {
            modes.Insert(0, new DefaultDisplayMode("Mobile") { ContextCondition = (context => context.Request.UserAgent.IndexOf("Android", StringComparison.OrdinalIgnoreCase) >= 0) });
            modes.Insert(0, new DefaultDisplayMode("Mobile") { ContextCondition = (context => context.Request.UserAgent.IndexOf("Opera Mobi", StringComparison.OrdinalIgnoreCase) >= 0) });
            modes.Insert(0, new DefaultDisplayMode("Mobile") { ContextCondition = (context => context.Request.UserAgent.IndexOf("iPad", StringComparison.OrdinalIgnoreCase) >= 0) });
        }
    }
}
