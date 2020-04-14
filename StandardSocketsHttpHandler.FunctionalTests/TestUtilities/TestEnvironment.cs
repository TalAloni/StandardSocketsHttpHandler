﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System
{
    public static class TestEnvironment
    {
        /// <summary>
        /// Check if the stress mode is enabled.
        /// </summary>
        public static bool IsStressModeEnabled
        {
            get
            {
                return true;
            }
        }
    }
}
