using System;

namespace FramePFX.Themes {
    public enum ThemeType {
        Dark,
        Light,
    }

    public static class ThemeTypeExtension {
        public static string GetName(this ThemeType type) {
            switch (type) {
                case ThemeType.Dark:        return "Dark";
                case ThemeType.Light:       return "Light";
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}