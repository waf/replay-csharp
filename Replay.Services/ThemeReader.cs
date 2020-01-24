using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Xml;

namespace Replay.UI
{
    /// <summary>
    /// Parses a vssettings file (used by Visual Studio) for the contained theme information.
    /// </summary>
    public static class ThemeReader
    {
        public const string Background = "background";
        public const string Foreground = "plain text";

        public static IReadOnlyDictionary<string, Color> GetTheme(string filename)
        {
            var theme = new Dictionary<string, Color>();
            foreach (var setting in ReadSettings(filename))
            {
                if (Foreground.Equals(setting.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    theme[Background] = AbgrToColor(setting.Background);
                }
                theme[setting.Name.ToLower()] = AbgrToColor(setting.Foreground);
            }
            return theme;
        }

        private static IReadOnlyCollection<Item> ReadSettings(string filename)
        {
            var list = new List<Item>();
            using (var stream = new StreamReader(filename))
            using (var reader = XmlReader.Create(stream))
            {
                reader.MoveToContent();
                reader.ReadToFollowing("FontsAndColors");
                var fontsAndColors = reader.ReadSubtree();
                while (fontsAndColors.Read())
                {
                    if (fontsAndColors.Name == "Item"
                        && fontsAndColors.IsStartElement())
                    {
                        list.Add(new Item(
                            background: fontsAndColors.GetAttribute("Background"),
                            boldFont: fontsAndColors.GetAttribute("BoldFont"),
                            foreground: fontsAndColors.GetAttribute("Foreground"),
                            name: fontsAndColors.GetAttribute("Name")
                        ));
                    }
                }
            }

            return list;
        }

        private static Color AbgrToColor(string abgr)
        {
            // input is like "0x008CFAF1"
            var color = abgr.AsSpan();
            byte blue = byte.Parse(color.Slice(4, 2), NumberStyles.AllowHexSpecifier);
            byte green = byte.Parse(color.Slice(6, 2), NumberStyles.AllowHexSpecifier);
            byte red = byte.Parse(color.Slice(8, 2), NumberStyles.AllowHexSpecifier);
            return Color.FromRgb(red, green, blue);
        }

        private readonly struct Item
        {
            public Item(string background, string boldFont, string foreground, string name)
            {
                Background = background;
                BoldFont = boldFont;
                Foreground = foreground;
                Name = name;
            }

            public string Background { get; }
            public string BoldFont { get; }
            public string Foreground { get; }
            public string Name { get; }
        }
    }
}
