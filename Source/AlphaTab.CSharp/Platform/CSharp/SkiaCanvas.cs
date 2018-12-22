﻿/*
 * This file is part of alphaTab.
 * Copyright © 2018, Daniel Kuschny and Contributors, All rights reserved.
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3.0 of the License, or at your option any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library.
 */

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using AlphaTab.Platform.Model;
using AlphaTab.Rendering;
using AlphaTab.Rendering.Glyphs;
using AlphaTab.Util;
using SkiaSharp;

namespace AlphaTab.Platform.CSharp
{
    class SkiaCanvas : ICanvas
    {
        private static readonly SKTypeface MusicFont;
        private static readonly int MusicFontSize = 34;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        static SkiaCanvas()
        {
            // https://github.com/mono/SkiaSharp/issues/713
            // https://github.com/mono/SkiaSharp/issues/572
            // manually load skia lib
            switch (System.Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    // I think unix platforms should be fine, to be tested
                    break;
                default:
                    string libSkiaSharpPath = Path.GetDirectoryName(typeof(SkiaCanvas).Assembly.Location);
                    if (IntPtr.Size == 4)
                    {
                        libSkiaSharpPath = Path.Combine(libSkiaSharpPath, "x86", "libSkiaSharp.dll");
                    }
                    else
                    {
                        libSkiaSharpPath = Path.Combine(libSkiaSharpPath, "x64", "libSkiaSharp.dll");
                    }
                    Logger.Debug("Skia", "Loading native lib from '" + libSkiaSharpPath + "'");
                    var lib = LoadLibrary(libSkiaSharpPath);
                    if (lib == IntPtr.Zero)
                    {
                        Logger.Warning("Skia", "Loading native lib from '" + libSkiaSharpPath + "' failed");
                    }
                    else
                    {
                        Logger.Debug("Skia", "Loading native lib from '" + libSkiaSharpPath + "' successful");
                    }
                    break;
            }

            // attempt to load correct skia native lib
            var type = typeof(SkiaCanvas).GetTypeInfo();
            var bravura = type.Assembly.GetManifestResourceStream(type.Namespace + ".Bravura.ttf");
            {
                MusicFont = SKTypeface.FromStream(bravura);
            }
        }

        private SKSurface _surface;
        private SKPath _path;
        private string _typeFaceCache;
        private SKTypeface _typeFace;
        private float _width;
        private float _height;

        public Color Color { get; set; }
        public float LineWidth { get; set; }
        public Font Font { get; set; }

        public SKTypeface TypeFace
        {
            get
            {
                if (_typeFaceCache != Font.ToCssString(Settings.Scale))
                {
                    if (_typeFace != null)
                    {
                        _typeFace.Dispose();
                    }
                    _typeFaceCache = Font.ToCssString(Settings.Scale);
                    _typeFace = SKTypeface.FromFamilyName(Font.Family,
                        Font.IsBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                        SKFontStyleWidth.Normal,
                        Font.IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright
                    );
                }
                return _typeFace;
            }
        }

        public TextAlign TextAlign { get; set; }

        public TextBaseline TextBaseline { get; set; }
        public Settings Settings { get; set; }

        public SkiaCanvas()
        {
            Color = new Color(255, 255, 255);
            LineWidth = 1;
            Font = new Font("Arial", 10);
            TextAlign = TextAlign.Left;
            TextBaseline = TextBaseline.Top;
        }

        public void BeginRender(float width, float height)
        {
            _width = width;
            _height = height;
            var newImage = SKSurface.Create(new SKImageInfo((int)width, (int)height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
            _surface = newImage;
            if (_path != null)
            {
                _path.Dispose();
            }
            _path = new SKPath();
            _path.FillType = SKPathFillType.Winding;
        }

        public object EndRender()
        {
            var image = _surface.Snapshot();
            _surface.Dispose();
            return image;
        }

        public virtual object OnRenderFinished()
        {
            // nothing to do
            return null;
        }

        public void Clear()
        {
            _surface.Canvas.Clear(SKColors.Transparent);
        }

        public void FillRect(float x, float y, float w, float h)
        {
            using (var paint = CreatePaint())
            {
                paint.BlendMode = SKBlendMode.SrcOver;
                _surface.Canvas.DrawRect(SKRect.Create(x, y, w, h), paint);
            }
        }

        private SKPaint CreatePaint()
        {
            var paint = new SKPaint();
            paint.IsAntialias = true;
            paint.Color = (SKColor)((uint)((int)Color.A << 24 | (int)Color.R << 16 | (int)Color.G << 8) | (uint)Color.B);
            return paint;
        }

        public void StrokeRect(float x, float y, float w, float h)
        {
            using (var paint = CreatePaint())
            {
                paint.BlendMode = SKBlendMode.SrcOver;
                paint.StrokeWidth = LineWidth;
                paint.IsStroke = true;
                _surface.Canvas.DrawRect(SKRect.Create(x, y, w, h), paint);
            }
        }

        public void BeginPath()
        {
            _path.Reset();
        }

        public void ClosePath()
        {
            _path.Close();
        }

        public void MoveTo(float x, float y)
        {
            _path.MoveTo(x, y);
        }

        public void LineTo(float x, float y)
        {
            _path.LineTo(x, y);
        }

        public void QuadraticCurveTo(float cpx, float cpy, float x, float y)
        {
            _path.QuadTo(cpx, cpy, x, y);
        }

        public void BezierCurveTo(float cp1x, float cp1y, float cp2x, float cp2y, float x, float y)
        {
            _path.CubicTo(cp1x, cp1y, cp2x, cp2y, x, y);
        }

        public void FillCircle(float x, float y, float radius)
        {
            BeginPath();
            _path.AddCircle(x, y, radius);
            ClosePath();
            Fill();
        }

        public void Fill()
        {
            using (var paint = CreatePaint())
            {
                _surface.Canvas.DrawPath(_path, paint);
            }

            _path.Reset();
        }

        public void Stroke()
        {
            using (var paint = CreatePaint())
            {
                paint.StrokeWidth = LineWidth;
                paint.IsStroke = true;
                _surface.Canvas.DrawPath(_path, paint);
            }
            _path.Reset();
        }

        public void BeginGroup(string identifier)
        {
        }

        public void EndGroup()
        {
        }

        public void FillText(string text, float x, float y)
        {
            using (var paint = CreatePaint())
            {
                paint.Typeface = TypeFace;
                paint.TextSize = Font.Size * Settings.Scale;
                switch (TextAlign)
                {
                    case TextAlign.Left:
                        paint.TextAlign = SKTextAlign.Left;
                        break;
                    case TextAlign.Center:
                        paint.TextAlign = SKTextAlign.Center;
                        break;
                    case TextAlign.Right:
                        paint.TextAlign = SKTextAlign.Right;
                        break;
                }

                _surface.Canvas.DrawText(text, x, y + GetFontBaseline(TextBaseline, paint), paint);
            }
        }

        private float GetFontBaseline(TextBaseline baseline, SKPaint paint)
        {
            switch (baseline)
            {
                case TextBaseline.Top: // TopTextBaseline
                    return -paint.FontMetrics.Ascent;
                case TextBaseline.Middle: // MiddleTextBaseline
                    return -paint.FontMetrics.Descent + paint.TextSize / 2;
                case TextBaseline.Bottom: // BottomTextBaseline
                    return -paint.FontMetrics.Descent;
                default:
                    break;
            }

            return 0;
        }

        public float MeasureText(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            using (var paint = CreatePaint())
            {
                paint.Typeface = TypeFace;
                paint.TextSize = Font.Size;
                switch (TextAlign)
                {
                    case TextAlign.Left:
                        paint.TextAlign = SKTextAlign.Left;
                        break;
                    case TextAlign.Center:
                        paint.TextAlign = SKTextAlign.Center;
                        break;
                    case TextAlign.Right:
                        paint.TextAlign = SKTextAlign.Right;
                        break;
                }
                return paint.MeasureText(text);
            }
        }

        public void FillMusicFontSymbol(float x, float y, float scale, MusicFontSymbol symbol, bool centerAtPosition = false)
        {
            if (symbol == MusicFontSymbol.None)
            {
                return;
            }

            using (var paint = CreatePaint())
            {
                paint.Typeface = MusicFont;
                paint.TextSize = MusicFontSize * scale;
                if (centerAtPosition)
                {
                    paint.TextAlign = SKTextAlign.Center;
                }
                _surface.Canvas.DrawText(Platform.StringFromCharCode((int)symbol), x, y, paint);
            }
        }
        public void FillMusicFontSymbols(float x, float y, float scale, MusicFontSymbol[] symbols, bool centerAtPosition = false)
        {
            var s = "";
            foreach (var symbol in symbols)
            {
                if (symbol != MusicFontSymbol.None)
                {
                    s += Platform.StringFromCharCode((int)symbol);
                }
            }

            using (var paint = CreatePaint())
            {
                paint.Typeface = MusicFont;
                paint.TextSize = MusicFontSize * scale;
                if (centerAtPosition)
                {
                    paint.TextAlign = SKTextAlign.Center;
                }
                _surface.Canvas.DrawText(s, x, y, paint);
            }
        }

        public void BeginRotate(float centerX, float centerY, float angle)
        {
            _surface.Canvas.Save();
            _surface.Canvas.Translate(centerX, centerY);
            _surface.Canvas.RotateDegrees(angle);
        }

        public void EndRotate()
        {
            _surface.Canvas.Restore();
        }

    }
}