using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;

namespace CodBackupConfig
{
    // activision's hitmarker faces live in process memory only: a gdi+
    // private font collection fed from the embedded ttf bytes. no files
    // are written, no registry keys touched, nothing left behind when
    // the process exits. on any failure the ui silently degrades to
    // segoe ui, the last resort behind the hitmarker cascade.
    // the same bytes are also registered in gdi (addfontmemresourceex)
    // because textrenderer/label/textbox route through gdi, which cannot
    // see a gdi+ private collection and would otherwise substitute a
    // different face with different metrics (clipped button labels)
    internal static class FontHost
    {
        [DllImport("gdiplus.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        static extern int GdipNewPrivateFontCollection(out IntPtr coll);

        [DllImport("gdiplus.dll", EntryPoint = "GdipPrivateAddMemoryFont", ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        static extern int GdipPrivateAddMemoryFont(IntPtr coll, IntPtr data, int size, out int num);

        [DllImport("gdiplus.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        static extern int GdipCreateFontFamilyFromName(string name, IntPtr coll, out IntPtr ff);

        [DllImport("gdi32.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        static extern int AddFontMemResourceEx(IntPtr bytes, uint length, IntPtr param, ref uint faces);

        // pins keep the ttf bytes reachable for the whole run; gdi+ holds
        // onto the pointers and stops using the faces if they move
        static readonly List<GCHandle> _pins = new();
        static readonly Dictionary<string, FontFamily> _families = new(StringComparer.Ordinal);

        public static bool Ready { get; private set; }

        public static void Load()
        {
            try
            {
                // the GdipNew* entry points need an initialized gdi+ session;
                // old entry points do that lazily on first use, these do not
                using var warm = new Bitmap(1, 1);

                int rc = GdipNewPrivateFontCollection(out IntPtr coll);
                if (rc != 0 || coll == IntPtr.Zero) return;

                string[] files =
                {
                    "HitmarkerCondensed-Bold.ttf",
                    "HitmarkerNormal-Bold.ttf",
                    "HitmarkerNormal-Regular.ttf",
                    "HitmarkerText-Bold.ttf",
                    "HitmarkerText-Medium.ttf",
                    "HitmarkerText-Regular.ttf",
                };
                foreach (string file in files)
                {
                    using Stream s = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("font/" + file);
                    if (s == null) continue;
                    byte[] bytes = new byte[s.Length];
                    s.ReadExactly(bytes);

                    GCHandle pin = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    _pins.Add(pin);
                    GdipPrivateAddMemoryFont(coll, pin.AddrOfPinnedObject(), bytes.Length, out _);
                    // mirror the face into gdi so the non-gdi+ text paths
                    // (textrenderer, label, textbox, checkbox) resolve the
                    // family by name; the pin keeps the bytes alive for the
                    // whole process lifetime
                    uint faces = 0;
                    AddFontMemResourceEx(pin.AddrOfPinnedObject(), (uint)bytes.Length, IntPtr.Zero, ref faces);
                }

                // distinct gdi+ families (the medium cut carries its own
                // family name in the ttf name table, verified by dump)
                Wrap("Hitmarker Condensed", coll);
                Wrap("Hitmarker Normal", coll);
                Wrap("Hitmarker Text", coll);
                Wrap("Hitmarker Text Medium", coll);
                Ready = _families.Count > 0;
            }
            catch { }
        }

        // gdi+ font families built from a private collection have no public
        // managed constructor, so wrap the raw handle into an uninitialized
        // FontFamily via its internal pointer field
        static void Wrap(string name, IntPtr coll)
        {
            try
            {
                int rc = GdipCreateFontFamilyFromName(name, coll, out IntPtr ff);
                if (rc != 0 || ff == IntPtr.Zero) return;

                Type t = typeof(FontFamily);
#pragma warning disable SYSLIB0050 // formatter services still own this call
                object fam = System.Runtime.Serialization.FormatterServices
                    .GetUninitializedObject(t);
#pragma warning restore SYSLIB0050
                FieldInfo fld = t.GetField("_nativeFamily", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fld == null)
                    fld = Array.Find(t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic),
                                     x => x.FieldType == typeof(IntPtr));
                if (fld == null) return;
                fld.SetValue(fam, ff);
                t.GetField("_createDefaultOnFail", BindingFlags.Instance | BindingFlags.NonPublic)
                 ?.SetValue(fam, true);
                _families[name] = (FontFamily)fam;
            }
            catch { }
        }

        public static FontFamily Get(string name) =>
            _families.TryGetValue(name, out var f) ? f : null;
    }
}
