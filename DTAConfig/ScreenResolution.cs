﻿using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework.Graphics;

namespace DTAConfig
{
    /// <summary>
    /// A single screen resolution.
    /// </summary>
    public sealed record ScreenResolution : IComparable<ScreenResolution>
    {
        public ScreenResolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// The width of the resolution in pixels.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The height of the resolution in pixels.
        /// </summary>
        public int Height { get; set; }

        public override string ToString()
        {
            return Width + "x" + Height;
        }

        public void Deconstruct(out int width, out int height)
        {
            width = this.Width;
            height = this.Height;
        }

        public static implicit operator ScreenResolution((int Width, int Height) resolutionTuple) => new(resolutionTuple.Width, resolutionTuple.Height);

        public static implicit operator (int Width, int Height)(ScreenResolution resolution) => new(resolution.Width, resolution.Height);

        public static implicit operator ScreenResolution(string resolution)
        {
            List<int> resolutionList = resolution.Split('x').Take(2).Select(int.Parse).ToList();
            return new(resolutionList[0], resolutionList[1]);
        }

        public static implicit operator string(ScreenResolution resolution) => resolution.ToString();

        public bool Fit(ScreenResolution child)
        {
            return this.Width >= child.Width && this.Height >= child.Height;
        }

        public int CompareTo(ScreenResolution res2)
        {
            if (this.Width < res2.Width)
                return -1;
            else if (this.Width > res2.Width)
                return 1;
            else // equal
            {
                if (this.Height < res2.Height)
                    return -1;
                else if (this.Height > res2.Height)
                    return 1;
                else return 0;
            }
        }

        private static ScreenResolution _desktopResolution = null;
        public static ScreenResolution DesktopResolution => _desktopResolution ??= new(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height);

        // The default graphic profile supports resolution up to 4096x4096. The number gets even smaller in practice. Therefore, we select 3840 as the limit.
        public static ScreenResolution HiDefLimitResolution { get; } = "3840x3840";

        private static ScreenResolution _safeDesktopResolution = null;
        public static ScreenResolution SafeDesktopResolution
        {
            get
            {
#if XNA
                return _safeDesktopResolution ??= HiDefLimitResolution.Fit(DesktopResolution) ? DesktopResolution : HiDefLimitResolution;
#else
                return _safeDesktopResolution ??= DesktopResolution;
#endif
            }
        }

        public static List<ScreenResolution> GetFullScreenResolutions(int minWidth, int minHeight) => GetFullScreenResolutions(minWidth, minHeight, SafeDesktopResolution.Width, SafeDesktopResolution.Height);
        public static List<ScreenResolution> GetFullScreenResolutions(int minWidth, int minHeight, int maxWidth, int maxHeight)
        {
            var screenResolutions = new List<ScreenResolution>();

            foreach (DisplayMode dm in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                if (dm.Width < minWidth || dm.Height < minHeight || dm.Width > maxWidth || dm.Height > maxHeight)
                    continue;

                var resolution = new ScreenResolution(dm.Width, dm.Height);

                // SupportedDisplayModes can include the same resolution multiple times
                // because it takes the refresh rate into consideration.
                // Which means that we have to check if the resolution is already listed
                if (screenResolutions.Find(res => res.Equals(resolution)) != null)
                    continue;

                screenResolutions.Add(resolution);
            }

            return screenResolutions;
        }

        public static IReadOnlyList<ScreenResolution> OptimalWindowedResolutions { get; } = [
            "1024x600",
            "1024x720",
            "1280x600",
            "1280x720",
            "1280x768",
            "1280x800",
        ];

        public const int MAX_INT_SCALE = 10;

        public static List<ScreenResolution> GetWindowedResolutions(int minWidth, int minHeight) => GetWindowedResolutions(minWidth, minHeight, SafeDesktopResolution.Width, SafeDesktopResolution.Height);
        public static List<ScreenResolution> GetWindowedResolutions(int minWidth, int minHeight, int maxWidth, int maxHeight)
        {
            ScreenResolution maxResolution = (maxWidth, maxHeight);

            var windowedResolutions = new List<ScreenResolution>();

            foreach (ScreenResolution optimalResolution in OptimalWindowedResolutions)
            {
                for (int i = 1; i < MAX_INT_SCALE; i++)
                {
                    ScreenResolution scaledResolution = (optimalResolution.Width * i, optimalResolution.Height * i);

                    if (scaledResolution.Width < minWidth || scaledResolution.Height < minHeight)
                        continue;

                    if (maxResolution.Fit(scaledResolution))
                        windowedResolutions.Add(scaledResolution);
                    else
                        break;
                }
            }

            return windowedResolutions;
        }
    }
}
