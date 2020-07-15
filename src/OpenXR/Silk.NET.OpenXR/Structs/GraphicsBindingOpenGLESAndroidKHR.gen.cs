// This file is part of Silk.NET.
// 
// You may modify and distribute Silk.NET under the terms
// of the MIT license. See the LICENSE file for details.


using System;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Core.Attributes;
using Ultz.SuperInvoke;

#pragma warning disable 1591

namespace Silk.NET.OpenXR
{
    [NativeName("Name", "XrGraphicsBindingOpenGLESAndroidKHR")]
    public unsafe partial struct GraphicsBindingOpenGLESAndroidKHR
    {
        public GraphicsBindingOpenGLESAndroidKHR
        (
            StructureType type = StructureType.TypeGraphicsBindingOpenglESAndroidKhr,
            void* next = default,
            IntPtr display = default,
            IntPtr config = default,
            IntPtr context = default
        )
        {
            Type = type;
            Next = next;
            Display = display;
            Config = config;
            Context = context;
        }

/// <summary></summary>
        [NativeName("Type", "XrStructureType")]
        [NativeName("Type.Name", "XrStructureType")]
        [NativeName("Name", "type")]
        public StructureType Type;
/// <summary></summary>
        [NativeName("Type", "void*")]
        [NativeName("Type.Name", "void")]
        [NativeName("Name", "next")]
        public void* Next;
/// <summary></summary>
        [NativeName("Type", "EGLDisplay")]
        [NativeName("Type.Name", "EGLDisplay")]
        [NativeName("Name", "display")]
        public IntPtr Display;
/// <summary></summary>
        [NativeName("Type", "EGLConfig")]
        [NativeName("Type.Name", "EGLConfig")]
        [NativeName("Name", "config")]
        public IntPtr Config;
/// <summary></summary>
        [NativeName("Type", "EGLContext")]
        [NativeName("Type.Name", "EGLContext")]
        [NativeName("Name", "context")]
        public IntPtr Context;
    }
}