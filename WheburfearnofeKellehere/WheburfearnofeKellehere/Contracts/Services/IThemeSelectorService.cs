using System;
using System.Windows.Media;

using WheburfearnofeKellehere.Models;

namespace WheburfearnofeKellehere.Contracts.Services
{
    public interface IThemeSelectorService
    {
        event EventHandler ThemeChanged;

        SolidColorBrush GetColor(string colorKey);

        void InitializeTheme();

        void SetTheme(AppTheme theme);

        AppTheme GetCurrentTheme();
    }
}
