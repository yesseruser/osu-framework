// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Platform;

namespace osu.Framework
{
    /// <summary>
    /// Various configuration properties for a <see cref="Host"/>.
    /// </summary>
    public class HostOptions
    {
        /// <summary>
        /// The IPC port to bind. This port should be between 1024 and 49151,
        /// should be shared by all instances of a given osu!framework app,
        /// but be distinct from IPC ports specified by other osu!framework apps.
        /// See <see cref="IIpcHost"/> for more details on usage.
        /// </summary>
        public int? IPCPort { get; set; }

        /// <summary>
        /// Whether this is a portable installation. Will cause all game files to be placed alongside the executable, rather than in the standard data directory.
        /// </summary>
        public bool PortableInstallation { get; set; }

        /// <summary>
        /// Whether to bypass the compositor. Defaults to <c>true</c>.
        /// </summary>
        /// <remarks>
        /// On Linux, the compositor re-buffers the application to apply various window effects,
        /// increasing latency in the process. Thus it is a good idea for games to bypass it,
        /// though normal applications would generally benefit from letting the window effects untouched. <br/>
        /// If the SDL_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR environment variable is set, this property will have no effect.
        /// </remarks>
        public bool BypassCompositor { get; set; } = true;

        /// <summary>
        /// The friendly name of the game to be hosted. This is used to display the name to the user,
        /// for example in the window title bar or in OS windows and prompts.
        /// </summary>
        /// <remarks>
        /// If empty, GameHost will choose a default name based on the gameName.
        /// </remarks>
        public string FriendlyGameName { get; set; } = string.Empty;
    }
}
