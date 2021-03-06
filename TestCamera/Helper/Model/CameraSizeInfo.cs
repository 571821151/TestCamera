﻿using System;
using Windows.Media.MediaProperties;

namespace TestCamera
{
    public class CameraSizeInfo
    {
        public CameraSizeInfo(IMediaEncodingProperties item, uint width, uint height)
        {
            Data = item;
            Width = width;
            Height = height;
            SizeTag = Math.Round(Convert.ToDouble(width) * height / 1000000.0, 1);
            ShowText = width + " x " + height + " (" + SizeTag + "MP)";
        }
        public IMediaEncodingProperties Data;
        public uint Width;
        public uint Height;
        public double SizeTag;
        public string ShowText { get; set; }
    }
}