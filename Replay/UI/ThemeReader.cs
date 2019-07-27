using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Xml;
using System.Xml.Serialization;

namespace Replay.UI
{
    public static class ThemeReader
    {
        public const string Background = "background";
        public const string Foreground = "plain text";

        public static IReadOnlyDictionary<string, Color> GetTheme(string filename)
        {
            var settings = ReadSettings(filename);
            var theme = new Dictionary<string, Color>();
            foreach (var item in FindColorConfigurations(settings))
            {
                if (Foreground.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    theme[Background] = AbgrToColor(item.Background);
                }
                theme[item.Name.ToLower()] = AbgrToColor(item.Foreground);
            }
            return theme;
        }

        private static IEnumerable<Item> FindColorConfigurations(UserSettings settings)
        {
            return settings.Category.Category.FontsAndColors.Categories.SelectMany(category => category.Items);
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

        private static UserSettings ReadSettings(string filename)
        {
            using (var stream = new StreamReader(filename))
            using (XmlReader reader = XmlReader.Create(stream))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(UserSettings));
                return (UserSettings)serializer.Deserialize(reader);
            }
        }
    }

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public partial class UserSettings
    {

        public ApplicationIdentity ApplicationIdentity { get; set; }

        public ToolsOptions ToolsOptions { get; set; }

        public UserSettingsCategory Category { get; set; }
    }

    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class ApplicationIdentity
    {

        [XmlAttribute]
        public decimal version { get; set; }
    }

    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class ToolsOptions
    {
        public ToolsOptionsCategory ToolsOptionsCategory { get; set; }
    }

    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class ToolsOptionsCategory
    {

        [XmlAttribute]
        public string RegisteredName { get; set; }

        [XmlAttribute]
        public string name { get; set; }
    }

    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class UserSettingsCategory
    {

        public CategoryCategory Category { get; set; }

        [XmlAttribute]
        public string RegisteredName { get; set; }

        [XmlAttribute]
        public string name { get; set; }
    }

    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class CategoryCategory
    {

        public PropertyValue PropertyValue { get; set; }

        public FontsAndColors FontsAndColors { get; set; }

        [XmlAttribute]
        public string Category { get; set; }

        [XmlAttribute]
        public string Package { get; set; }

        [XmlAttribute]
        public string PackageName { get; set; }

        [XmlAttribute]
        public string RegisteredName { get; set; }

        [XmlAttribute]
        public string name { get; set; }
    }

    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class PropertyValue
    {

        [XmlAttribute]
        public string name { get; set; }

        [XmlText]
        public byte Value { get; set; }
    }

    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class FontsAndColors
    {
        public FontsAndColorsTheme Theme { get; set; }

        [XmlArrayItem("Category", IsNullable = false)]
        public FontsAndColorsCategory[] Categories { get; set; }

        [XmlAttribute]
        public decimal Version { get; set; }
    }

    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class FontsAndColorsTheme
    {
        [XmlAttribute]
        public string Id { get; set; }
    }

    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class FontsAndColorsCategory
    {
        [XmlArrayItem("Item", IsNullable = false)]
        public Item[] Items { get; set; }

        [XmlAttribute]
        public byte CharSet { get; set; }

        [XmlIgnore]
        public bool CharSetSpecified { get; set; }

        [XmlAttribute]
        public string FontIsDefault { get; set; }

        [XmlAttribute]
        public string FontName { get; set; }

        [XmlAttribute]
        public byte FontSize { get; set; }

        [XmlIgnore]
        public bool FontSizeSpecified { get; set; }

        [XmlAttribute]
        public string GUID { get; set; }
    }

    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class Item
    {

        [XmlAttribute]
        public string Background { get; set; }

        [XmlAttribute]
        public string BoldFont { get; set; }

        [XmlAttribute]
        public string Foreground { get; set; }

        [XmlAttribute]
        public string Name { get; set; }
    }
}
