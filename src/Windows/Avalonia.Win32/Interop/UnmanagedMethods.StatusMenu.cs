using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop
{
    [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Using Win32 naming for consistency.")]
    [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "Using Win32 naming for consistency.")]
    [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "Using Win32 naming for consistency.")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements must be documented", Justification = "Look in Win32 docs.")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items must be documented", Justification = "Look in Win32 docs.")]
    internal static partial class UnmanagedMethods
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern bool Shell_NotifyIcon(NotifyIconMessage dwMessage, [In] ref NOTIFYICONDATA lpData);
        
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        [DllImport("user32.dll")]
        internal static extern IntPtr CreatePopupMenu();
        
        [DllImport("user32.dll")]
        internal static extern bool DestroyMenu(IntPtr hMenu);
        
        [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool InsertMenuItem(IntPtr hMenu, uint uposition, uint uflags, ref MenuItemInfo mii);

        [DllImport("user32")]
        internal static extern bool AppendMenu(IntPtr hMenu, MenuFlag uflags, int uIDNewItemOrSubmenu, string text);
        
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern int SetMenuItemBitmaps(IntPtr hMenu, int nPosition, MenuFlag uflags, IntPtr hBitmapUnchecked, IntPtr hBitmapChecked);
        
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "TrackPopupMenuEx")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TrackPopupMenuExReturnSuccess(IntPtr hmenu, TPM fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);
        
        [DllImport("user32.dll", SetLastError = false, EntryPoint = "TrackPopupMenuEx")]
        internal static extern uint TrackPopupMenuExReturnCmd(IntPtr hmenu, TPM fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

        internal enum NotifyIconMessage : int
        {
            ADD      = 0x00000000,
            MODIFY       = 0x00000001,
            DELETE       = 0x00000002,
            SETFOCUS     = 0x00000003,
            SETVERSION   = 0x00000004,
        }
        
        [Flags]
        internal enum NotifyIconFlag
        {
            MESSAGE = 0x00000001,
            ICON = 0x00000002,
            TIP = 0x00000004,  
            STATE = 0x00000008,
            INFO = 0x00000010,
            GUID = 0x00000020,   
            REALTIME = 0x00000040,
            SHOWTIP = 0x00000080,        
        }
        
        /// <summary>
        /// Contains information that the system needs to display notifications in the notification area.
        /// Used by Shell_NotifyIcon.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct NOTIFYICONDATA 
        {
            public int cbSize;
            public IntPtr hwnd;
            public uint uID;
            public NotifyIconFlag uFlags;
            public WindowsMessage uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)]
            public string szTip;
            public IconState dwState;
            public IconState dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=256)]
            public string szInfo;
            public uint uVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)]
            public string szInfoTitle;
            public int dwInfoFlags;
        }

        [Flags]
        internal enum IconState
        {
            Visible = 0x00,
            Hidden = 0x01,
            Shared = 0x02,
        }
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MenuItemInfo {
            public Int32 cbSize;
            public MenuItemMask fMask;
            public MenuFlag fType;
            public MenuState fState;
            public UInt32 wID;
            public IntPtr hSubMenu;
            public IntPtr hbmpChecked;
            public IntPtr hbmpUnchecked;
            public IntPtr dwItemData;
            public string? dwTypeData;
            public UInt32 cch; // length of dwTypeData
            public IntPtr hbmpItem;

            public static MenuItemInfo NewSeparator(uint id)
                => new MenuItemInfo() { 
                    cbSize = Marshal.SizeOf(typeof(MenuItemInfo)), 
                    dwTypeData = null, 
                    cch = 0,
                    wID = id,
                    fMask = MenuItemMask.ID | MenuItemMask.TYPE | MenuItemMask.STATE | MenuItemMask.DATA,
                    fType = MenuFlag.MF_SEPARATOR,
                    fState = 0,
                    hSubMenu = IntPtr.Zero,
                    hbmpChecked = IntPtr.Zero,
                    hbmpUnchecked = IntPtr.Zero,
                    hbmpItem = IntPtr.Zero,
                };
            
            public MenuItemInfo(uint id, string text, bool isDefault = false)
            {
                cbSize = Marshal.SizeOf(typeof(MenuItemInfo));
                dwTypeData = text;
                cch = 0;//(uint)text.Length;
                wID = id;
                fMask = MenuItemMask.ID | MenuItemMask.TYPE | MenuItemMask.STATE | MenuItemMask.DATA;
                fType = MenuFlag.MF_STRING;
                fState = isDefault ? MenuState.DEFAULT : MenuState.ENABLED;
                dwItemData = new IntPtr(id);
                hSubMenu = IntPtr.Zero;
                hbmpChecked = IntPtr.Zero;
                hbmpUnchecked = IntPtr.Zero;
                hbmpItem = IntPtr.Zero;
            }
        }
        
         [Flags]
        internal enum TPM: uint
        {
            // Centers the shortcut menu horizontally relative to the coordinate specified by the x parameter.
            CENTERALIGN = 0x0004, 
            // Positions the shortcut menu so that its left side is aligned with the coordinate specified by the x parameter.
            LEFTALIGN = 0x0000, 
            // Positions the shortcut menu so that its right side is aligned with the coordinate specified by the x parameter.
            RIGHTALIGN = 0x0008, 
            // Positions the shortcut menu so that its bottom side is aligned with the coordinate specified by the y parameter.
            PM_BOTTOMALIGN = 0x0020, 
            // Positions the shortcut menu so that its top side is aligned with the coordinate specified by the y parameter.
            TOPALIGN = 0x0000, 
            // Centers the shortcut menu vertically relative to the coordinate specified by the y parameter.
            VCENTERALIGN = 0x0010, 
            // The function does not send notification messages when the user clicks a menu item.
            NONOTIFY = 0x0080, 
            // The function returns the menu item identifier of the user's selection in the return value.
            RETURNCMD = 0x0100, 
            // The user can select menu items with only the left mouse button.
            LEFTBUTTON = 0x0000, 
            // The user can select menu items with both the left and right mouse buttons.
            RIGHTBUTTON = 0x0002,
            // Animates the menu from right to left.
            HORNEGANIMATION = 0x0800, 
            // Animates the menu from left to right.
            HORPOSANIMATION = 0x0400, 
            // Displays menu without animation.
            NOANIMATION = 0x4000, 
            // Animates the menu from bottom to top.
            VERNEGANIMATION = 0x2000, 
            // Animates the menu from top to bottom.
            VERPOSANIMATION = 0x1000, 
        }

        [Flags]
        internal enum MenuFlag
        {
            // Uses a bitmap as the menu item. The lpNewItem parameter contains a handle to the bitmap.
            MF_BITMAP = 0x00000004,
            // Places a check mark next to the menu item. If the application provides check-mark bitmaps (see SetMenuItemBitmaps, this flag displays the check-mark bitmap next to the menu item.
            MF_CHECKED = 0x00000008,
            // Disables the menu item so that it cannot be selected, but the flag does not gray it.
            MF_DISABLED = 0x00000002,
            // Enables the menu item so that it can be selected, and restores it from its grayed state.
            MF_ENABLED = 0x00000000,
            // Disables the menu item and grays it so that it cannot be selected.
            MF_GRAYED = 0x00000001,
            // Functions the same as the MF_MENUBREAK flag for a menu bar. For a drop-down menu, submenu, or shortcut menu, the new column is separated from the old column by a vertical line.
            MF_MENUBARBREAK = 0x00000020,
            // Places the item on a new line (for a menu bar) or in a new column (for a drop-down menu, submenu, or shortcut menu) without separating columns.
            MF_MENUBREAK = 0x00000040,
            // Specifies that the item is an owner-drawn item. Before the menu is displayed for the first time, the window that owns the menu receives a WM_MEASUREITEM message to retrieve the width and height of the menu item. The WM_DRAWITEM message is then sent to the window procedure of the owner window whenever the appearance of the menu item must be updated.
            MF_OWNERDRAW = 0x00000100,
            // Specifies that the menu item opens a drop-down menu or submenu. The uIDNewItem parameter specifies a handle to the drop-down menu or submenu. This flag is used to add a menu name to a menu bar, or a menu item that opens a submenu to a drop-down menu, submenu, or shortcut menu.
            MF_POPUP = 0x00000010,
            // Draws a horizontal dividing line. This flag is used only in a drop-down menu, submenu, or shortcut menu. The line cannot be grayed, disabled, or highlighted. The lpNewItem and uIDNewItem parameters are ignored.
            MF_SEPARATOR = 0x00000800,
            // Specifies that the menu item is a text string; the lpNewItem parameter is a pointer to the string.
            MF_STRING = 0x00000000,
            // Does not place a check mark next to the item (default). If the application supplies check-mark bitmaps (see SetMenuItemBitmaps), this flag displays the clear bitmap next to the menu item.
            MF_UNCHECKED = 0x00000000,
        }

        [Flags]
        internal enum MenuItemMask
        {
            ///<summary> Retrieves or sets the hbmpItem member.</summary>
            BITMAP = 0x00000080, 
            ///<summary> Retrieves or sets the hbmpChecked and hbmpUnchecked members.</summary>
            CHECKMARKS = 0x00000008, 
            ///<summary> Retrieves or sets the dwItemData member.</summary>
            DATA = 0x00000020, 
            ///<summary> Retrieves or sets the fType member.</summary>
            FTYPE = 0x00000100, 
            ///<summary> Retrieves or sets the wID member.</summary>
            ID = 0x00000002, 
            ///<summary> Retrieves or sets the fState member.</summary>
            STATE = 0x00000001, 
            ///<summary> Retrieves or sets the dwTypeData member.</summary>
            STRING = 0x00000040, 
            ///<summary> Retrieves or sets the hSubMenu member.</summary>
            SUBMENU = 0x00000004, 
            ///<summary>
            ///Retrieves or sets the fType and dwTypeData members.
            /// TYPE is replaced by BITMAP, FTYPE, and STRING.</summary> 
            TYPE = 0x00000010, 
         
        }

        [Flags]
        internal enum MenuState
        {
            /// <summary> Checks the menu item. For more information about selected menu items, see the hbmpChecked member</summary>
            CHECKED = 0x00000008,
            /// <summary> Specifies that the menu item is the default. A menu can contain only one default menu item, which is displayed in bold</summary>
            DEFAULT = 0x00001000,
            /// <summary> Disables the menu item and grays it so that it cannot be selected. This is equivalent to GRAYED</summary>
            DISABLED = 0x00000003,
            /// <summary> Enables the menu item so that it can be selected. This is the default state</summary>
            ENABLED = 0x00000000,
            /// <summary> Disables the menu item and grays it so that it cannot be selected. This is equivalent to DISABLED</summary>
            GRAYED = 0x00000003,
            /// <summary> Highlights the menu item</summary>
            HILITE = 0x00000080,
            /// <summary> Unchecks the menu item. For more information about clear menu items, see the hbmpChecked member</summary>
            UNCHECKED = 0x00000000,
            /// <summary> Removes the highlight from the menu item. This is the default state</summary>
            UNHILITE = 0x00000000,
        }
    }
}
