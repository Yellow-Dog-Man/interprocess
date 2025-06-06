using System.Runtime.InteropServices;

namespace Cloudtoid.Interprocess.Tests;

public sealed class FactAttribute : Xunit.FactAttribute
{
    private static readonly Platform? CurrentPlatform = GetPlatform();

    /// <summary>
    /// Gets or sets the supported OS Platforms
    /// </summary>
    public Platform Platforms { get; set; } = Platform.All;

    public override string? Skip
    {
        get
        {
            if (base.Skip is not null || CurrentPlatform is null)
                return base.Skip;

            if ((Platforms & CurrentPlatform) == 0)
#pragma warning disable RCS1198 // Avoid unnecessary boxing of value type
                return $"Skipped on {CurrentPlatform}";
#pragma warning restore RCS1198 // Avoid unnecessary boxing of value type

            return null;
        }
        set => base.Skip = value;
    }

    private static Platform? GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Platform.Windows;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Platform.Linux;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return Platform.OSX;

#if NET9_0_OR_GREATER
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) // No FreeBSD support on older .NET :(
            return Platform.FreeBSD;
#endif
        return null;
    }
}