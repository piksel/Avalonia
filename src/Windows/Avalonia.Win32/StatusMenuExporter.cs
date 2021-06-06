using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Threading;
using JetBrains.Annotations;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    public class StatusMenuExporter : ITopLevelStatusMenuExporter, IDisposable
    {
        private const uint ItemIndexBase = 1000u;
        private const uint IconId = 0;

        private bool _resetQueued = true;
        private bool _exported;
        private readonly WindowImpl _nativeWindow;
        [CanBeNull] private NativeMenuItem _menu;
        [CanBeNull] private IconImpl _icon;
        private bool _notifyIconCreated;
        private IntPtr _menuHandle;
        private int _defaultItemIndex = -1;
        private readonly object _lockRef = new();
        private NOTIFYICONDATA _notifyIconData;

        public StatusMenuExporter(WindowImpl nativeWindow)
        {
            _nativeWindow = nativeWindow;

            DoLayoutReset();
        }

        public bool IsStatusMenuExported => _exported;

        public event EventHandler OnIsStatusMenuExportedChanged;

        internal void UpdateIfNeeded()
        {
            if (_resetQueued)
            {
                DoLayoutReset();
            }
        }

        private void DoLayoutReset(bool forceUpdate = false)
        {
            if (!_resetQueued && !forceUpdate) return;
            _resetQueued = false;
            if (_menu is not {} menu) return;
            
            lock (_lockRef)
            {
                if (_menuHandle != IntPtr.Zero)
                {
                    DestroyMenu(_menuHandle);
                }

                _menuHandle = CreatePopupMenu();
                for (var index = 0; index < menu.Menu.Items.Count; index++)
                {
                    AddMenuItem(menu.Menu.Items[index], index);
                }

                CreateNotifyIcon(menu);
                _exported = true;
            }
        }

        internal void QueueReset()
        {
            if (_resetQueued)
                return;
            _resetQueued = true;
            Dispatcher.UIThread.Post(() => DoLayoutReset(), DispatcherPriority.Background);
        }

        public void SetStatusMenu(NativeMenuItem menu)
        {
            _menu = menu ?? new NativeMenuItem();
            DoLayoutReset(forceUpdate: true);
        }

        private void AddMenuItem([CanBeNull] NativeMenuItemBase menuItem, int index)
        {
            if (_defaultItemIndex < 0) _defaultItemIndex = index;

            MenuItemInfo? mii = menuItem switch
            {
                NativeMenuItemSeparator => MenuItemInfo.NewSeparator(ItemIndexBase + (uint)index),
                NativeMenuItem mi => new MenuItemInfo(ItemIndexBase + (uint)index, mi.Header.Replace('_', '&'), _defaultItemIndex == index),
                _ => null,
            };
            if (mii is not {} menuItemInfo)
            {
                Debug.WriteLine("Attempted to insert empty menu item, nmib is: {0}", menuItem?.GetType().FullName);
                return;
            }
            
            Debug.WriteLine($"Adding menu item #{menuItemInfo.wID} ({menuItemInfo.fType:F}), text: {menuItemInfo.dwTypeData}, state: {menuItemInfo.fState:F}");
            if (InsertMenuItem(_menuHandle, (uint)index, 1, ref menuItemInfo)) return;
            LogWin32Error("Failed to insert menu item");
        }

        private void CreateNotifyIcon(NativeMenuItem menu)
        {
            if (menu.Icon is null)
            {
                Debug.WriteLine("Tried to create notify icon with null icon");
                return;
            }
            
            if(_notifyIconCreated) return;

            using var ms = new MemoryStream();
            menu.Icon.Save(ms);
            _icon = AvaloniaLocator.Current.GetService<IPlatformIconLoader>().LoadIcon(ms) as IconImpl;
            if(_icon is null) return;
            _notifyIconData = CreateNotifyIconData(_nativeWindow.Handle.Handle, _icon.HIcon, menu.Header);

            if (!Shell_NotifyIcon(NotifyIconMessage.ADD, ref _notifyIconData))
            {
                Debug.WriteLine("Failed to create NotifyIcon");
            }
            else
            {
                _notifyIconCreated = true;
            }
        }


        private static NOTIFYICONDATA CreateNotifyIconData(IntPtr hWnd, IntPtr hIcon, string tooltipText)
        {
            var data = new NOTIFYICONDATA
            {
                hwnd = hWnd,
                uCallbackMessage = WindowsMessage.WM_NOTIFY_CALLBACK,
                uVersion =
                    0x4, // Vista+, ref: https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona#remarks
                dwState = IconState.Hidden,
                dwStateMask = IconState.Hidden,
                hIcon = hIcon,
                uFlags = NotifyIconFlag.MESSAGE |
                         NotifyIconFlag.ICON |
                         NotifyIconFlag.TIP |
                         NotifyIconFlag.SHOWTIP,
                szTip = tooltipText,
                uID = IconId,
            };
            data.cbSize = Marshal.SizeOf(data);

            return data;
        }

        public void CallbackMessageReceived(IntPtr wParam, IntPtr lParam)
        {
            var msg = (WindowsMessage)lParam.ToInt32();
            switch (msg)
            {
                case WindowsMessage.WM_LBUTTONDBLCLK:
                    // MouseEventReceived?.Invoke(MouseEvent.IconDoubleClick);
                    Debug.WriteLine("Icon double-clicked");
                    if(_defaultItemIndex < 0) return;
                    CommandSelected((uint)(ItemIndexBase + _defaultItemIndex));
                    break;

                case WindowsMessage.WM_CONTEXTMENU:
                case WindowsMessage.WM_RBUTTONUP:
                    ShowMenu(false);
                    break;
                
                case WindowsMessage.WM_MOUSEFIRST:
                    break;
                
                default:
                    Debug.WriteLine($"Unhandled message: {msg:G} (0x{msg:X} with WParam: 0x{wParam.ToInt64():x8})");
                    break;
            }
        }
        
        public void CommandMessageReceived(IntPtr wParam, IntPtr lParam)
        {
            Debug.WriteLine($"Command, W: 0x{wParam.ToInt64():x16}, L: {lParam.ToInt64():x16}");
            CommandSelected((uint)wParam.ToInt32());
        }

        private void ShowMenu(bool waitForResult)
        {
            if(!GetCursorPos(out var pos)) return;
            Debug.WriteLine($"Attempted to show menu at X: {pos.X}, Y: {pos.Y}");
            var ownerHandle = _nativeWindow.Handle.Handle;

            if (waitForResult)
            {
                var result = TrackPopupMenuExReturnCmd(_menuHandle, 
                    TPM.LEFTALIGN | TPM.RIGHTALIGN | TPM.RETURNCMD | TPM.NONOTIFY,
                    pos.X, pos.Y,
                    ownerHandle, IntPtr.Zero);
                if (result != 0)
                {
                    CommandSelected(result);
                    return;
                }

                Debug.WriteLine("Failed to show menu or no item selected.");
            }
            else
            {
                if (TrackPopupMenuExReturnSuccess(_menuHandle, TPM.LEFTALIGN | TPM.RIGHTALIGN, pos.X, pos.Y,
                    ownerHandle, IntPtr.Zero)) return;
                LogWin32Error("Failed to show menu");
            }
        }

        private void CommandSelected(uint result)
        {
            if(_menu is not {}) return;
            var index = (int)(result - ItemIndexBase);
            var item = _menu.Menu.Items.ElementAtOrDefault(index) as NativeMenuItem;

            Debug.WriteLine($"Item selected: #{index} (id: {result}): {item?.Header}");
            ((INativeMenuItemExporterEventsImplBridge) item)?.RaiseClicked();
        }

        private static void LogWin32Error(string message)
        {
            var w32e = Marshal.GetLastWin32Error();
            var hRes = Marshal.GetHRForLastWin32Error();
            var ex = Marshal.GetExceptionForHR(hRes);
            Debug.Print("{0}: {1}: {2} (Win32 Error: 0x{3:x8}, hRes: 0x{4:x8})", 
                message, ex.GetType(), ex.Message, w32e, hRes);
        }

        private void ReleaseUnmanagedResources()
        {
            if (_menuHandle != IntPtr.Zero)
            {
                DestroyMenu(_menuHandle);
                _menuHandle = IntPtr.Zero;
            }

            if (_notifyIconCreated)
            {
                Shell_NotifyIcon(NotifyIconMessage.DELETE, ref _notifyIconData);
                _notifyIconCreated = false;
            }
        }
        
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~StatusMenuExporter()
        {
            ReleaseUnmanagedResources();
        }
    }
}
