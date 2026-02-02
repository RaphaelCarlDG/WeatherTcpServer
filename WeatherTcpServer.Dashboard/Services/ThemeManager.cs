using System.Windows;
using System.Windows.Media;

namespace WeatherTcpServer.Dashboard.Services;

public static class ThemeManager
{
    public static void ApplyTheme(bool isDarkMode)
    {
        var resources = Application.Current.Resources;
        
        if (isDarkMode)
        {
            // Super dark mode colors
            var bgColor = (Color)ColorConverter.ConvertFromString("#000000");
            var panelColor = (Color)ColorConverter.ConvertFromString("#111111");
            var cardColor = (Color)ColorConverter.ConvertFromString("#1A1A1A");
            var borderColor = (Color)ColorConverter.ConvertFromString("#333333");
            var mutedTextColor = (Color)ColorConverter.ConvertFromString("#CCCCCC");
            var textColor = (Color)ColorConverter.ConvertFromString("#FFFFFF");

            UpdateResources(resources, bgColor, panelColor, cardColor, borderColor, mutedTextColor, textColor);
        }
        else
        {
            // Light mode colors - better contrast
            var bgColor = (Color)ColorConverter.ConvertFromString("#F8FAFC");
            var panelColor = (Color)ColorConverter.ConvertFromString("#FFFFFF");
            var cardColor = (Color)ColorConverter.ConvertFromString("#FFFFFF");
            var borderColor = (Color)ColorConverter.ConvertFromString("#D1D5DB");
            var mutedTextColor = (Color)ColorConverter.ConvertFromString("#4B5563");
            var textColor = (Color)ColorConverter.ConvertFromString("#111827");

            UpdateResources(resources, bgColor, panelColor, cardColor, borderColor, mutedTextColor, textColor);
        }
    }

    private static void UpdateResources(ResourceDictionary resources, Color bgColor, Color panelColor, Color cardColor, Color borderColor, Color mutedTextColor, Color textColor)
    {
        resources["AppBgColor"] = bgColor;
        resources["PanelColor"] = panelColor;
        resources["CardColor"] = cardColor;
        resources["BorderColor"] = borderColor;
        resources["MutedTextColor"] = mutedTextColor;
        resources["TextColor"] = textColor;

        resources["AppBgBrush"] = new SolidColorBrush(bgColor);
        resources["PanelBrush"] = new SolidColorBrush(panelColor);
        resources["CardBrush"] = new SolidColorBrush(cardColor);
        resources["BorderBrush"] = new SolidColorBrush(borderColor);
        resources["MutedTextBrush"] = new SolidColorBrush(mutedTextColor);
        resources["TextBrush"] = new SolidColorBrush(textColor);
    }
}