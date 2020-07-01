using Replay.UI;
using System.Globalization;
using System.Windows;
using Xunit;

namespace Replay.Tests.UI
{
    public class NullToVisibilityConverterTest
    {
        private readonly NullToVisibilityConverter converter;

        public NullToVisibilityConverterTest()
        {
            this.converter = new NullToVisibilityConverter();
        }

        [Theory]
        [InlineData(null, Visibility.Collapsed)]
        [InlineData("", Visibility.Visible)]
        [InlineData("null", Visibility.Visible)]
        public void Convert_EmptyString_IsVisible(object input, Visibility expectedOutput)
        {
            var actualOutput = this.converter.Convert(input, typeof(Visibility), null, CultureInfo.CurrentCulture);
            Assert.Equal(expectedOutput, actualOutput);
        }
    }
}
