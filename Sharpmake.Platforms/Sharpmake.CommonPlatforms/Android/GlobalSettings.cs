// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Sharpmake
{
    public static partial class Android
    {
        public static class GlobalSettings
        {
            /// <summary>
            /// Android SDK path
            /// </summary>
            public static string AndroidHome { get; set; }

            /// <summary>
            /// Android NDK path
            /// </summary>
            public static string NdkRoot { get; set; }

            /// <summary>
            /// Java SE Development Kit path
            /// </summary>
            public static string JavaHome { get; set; }

            /// <summary>
            /// Apache Ant path
            /// </summary>
            public static string AntHome { get; set; }
        }
    }
}
