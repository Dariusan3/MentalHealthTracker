using MudBlazor;

namespace MentalHealthTracker.Shared
{
    public static class MentalHealthTheme
    {
        public static MudTheme DefaultTheme => new MudTheme()
        {
            Palette = new PaletteLight()
            {
                Primary = "#3f51b5",
                Secondary = "#2196f3",
                AppbarBackground = "#3f51b5",
                Background = "#f5f5f5",
                DrawerBackground = "#ffffff",
                DrawerText = "rgba(0,0,0, 0.7)",
                Success = "#4caf50",
                Info = "#2196f3",
                Warning = "#ff9800",
                Error = "#f44336"
            },
            Typography = new Typography()
            {
                Default = new Default()
                {
                    FontFamily = new[] { "Roboto", "Helvetica", "Arial", "sans-serif" },
                    FontSize = "0.875rem",
                    FontWeight = 400,
                    LineHeight = 1.43,
                    LetterSpacing = ".01071em"
                },
                H1 = new H1()
                {
                    FontSize = "2rem",
                    FontWeight = 500,
                    LineHeight = 1.6,
                },
                H2 = new H2()
                {
                    FontSize = "1.75rem",
                    FontWeight = 500,
                    LineHeight = 1.6,
                },
                H3 = new H3()
                {
                    FontSize = "1.5rem",
                    FontWeight = 500,
                    LineHeight = 1.6,
                },
                H4 = new H4()
                {
                    FontSize = "1.25rem",
                    FontWeight = 500,
                    LineHeight = 1.6,
                },
                H5 = new H5()
                {
                    FontSize = "1.125rem",
                    FontWeight = 500,
                    LineHeight = 1.6,
                },
                H6 = new H6()
                {
                    FontSize = "1rem",
                    FontWeight = 500,
                    LineHeight = 1.6,
                }
            }
        };
    }
} 