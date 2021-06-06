using System;
using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    public interface ITopLevelNativeMenuExporter
    {
        bool IsNativeMenuExported { get; }
        event EventHandler OnIsNativeMenuExportedChanged;
        void SetNativeMenu(NativeMenu menu);
    }
    
    public interface ITopLevelImplWithNativeMenuExporter : ITopLevelImpl
    {
        ITopLevelNativeMenuExporter NativeMenuExporter { get; }
    }
    
    public interface ITopLevelStatusMenuExporter
    {
        bool IsStatusMenuExported { get; }
        event EventHandler OnIsStatusMenuExportedChanged;
        void SetStatusMenu(NativeMenuItem menu);
    }
    
    public interface ITopLevelImplWithStatusMenuExporter : ITopLevelImpl
    {
        ITopLevelStatusMenuExporter StatusMenuExporter { get; }
    }
}
