using System;

namespace Interface
{
    [Flags]
    public enum PanelAnimationType
    {
        None = 0,
        Fade = 1 << 0,
        Slide = 1 << 1
    }
}

