// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.VisualStudio
{
    public partial class ProjectOptionsGenerator
    {
        public static class Template
        {
            public static class Manifest
            {
                // Manifest file contents copied from example on msdn page here:
                // https://learn.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process#setting-default-awareness-with-the-application-manifest
                public const string FileBegin = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">
  <application xmlns=""urn:schemas-microsoft-com:asm.v3"">
    <windowsSettings>";

                // Include both dpiAware and dpiAwareness values for "backwards compatibility", though it should be good enough to just specify dpiAwareness = PerMonitorV2
                // See msdn link above for more info on the dpiAwareness configuration values.
                public const string DPIAwarenessSettings = @"
      <dpiAware xmlns=""http://schemas.microsoft.com/SMI/2005/WindowsSettings"">true/PM</dpiAware>
      <dpiAwareness xmlns=""http://schemas.microsoft.com/SMI/2016/WindowsSettings"">PerMonitorV2,PerMonitor</dpiAwareness>
";

                public const string FileEnd =
@"    </windowsSettings>
  </application>
</assembly>";
            }
        }
    }
}
