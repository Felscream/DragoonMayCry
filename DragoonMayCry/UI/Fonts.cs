using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Plugin;
using System;
using System.IO;
using System.Reflection;

namespace DragoonMayCry.UI
{
    public class Fonts : IDisposable
    {
        private const string HitCountFontResource = "DragoonMayCry.Assets.DMC5Font.otf";
        private readonly IFontHandle dmcFont14;
        private readonly IFontHandle dmcFont21;
        private readonly IFontHandle dmcFont28;
        private readonly IFontHandle dmcFont42;
        private readonly IFontHandle dmcFont56;
        private readonly IDalamudPluginInterface pluginInterface;

        public Fonts()
        {
            pluginInterface = Plugin.PluginInterface;
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(HitCountFontResource);
            using var ms = new MemoryStream();
            stream?.CopyTo(ms);
            var bytes = ms.ToArray();
            dmcFont14 = BuildFrom(bytes, 14);
            dmcFont21 = BuildFrom(bytes, 21);
            dmcFont28 = BuildFrom(bytes, 28);
            dmcFont42 = BuildFrom(bytes, 42);
            dmcFont56 = BuildFrom(bytes, 56);
        }

        public void Dispose()
        {
            dmcFont14.Dispose();
            dmcFont21.Dispose();
            dmcFont28.Dispose();
            dmcFont42.Dispose();
            dmcFont56.Dispose();
        }

        private IFontHandle BuildFrom(byte[] bytes, int pxSize)
        {
            return pluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
                e => e.OnPreBuild(
                    tk => tk.AddFontFromMemory(new ReadOnlySpan<byte>(bytes),
                                               new SafeFontConfig { SizePx = pxSize },
                                               pxSize.ToString())
                )
            );
        }

        public IFontHandle GetHitCountFontByScale(int scale)
        {
            return scale switch
            {
                > 160 => dmcFont56,
                > 130 => dmcFont42,
                > 90 => dmcFont28,
                > 72 => dmcFont21,
                _ => dmcFont14,
            };
        }
    }
}
