﻿#pragma checksum "..\..\..\Windows\PairingWindow.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "786EFDCA591C418649C4C71EFECCE2C9BB5F5499D70EE14763912F816B4BD8B0"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using NanoTwitchLeafs.Properties;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace NanoTwitchLeafs.Windows {
    
    
    /// <summary>
    /// PairingWindow
    /// </summary>
    public partial class PairingWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 1 "..\..\..\Windows\PairingWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal NanoTwitchLeafs.Windows.PairingWindow Pairing_Window;
        
        #line default
        #line hidden
        
        
        #line 10 "..\..\..\Windows\PairingWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button autoDetect_Button;
        
        #line default
        #line hidden
        
        
        #line 11 "..\..\..\Windows\PairingWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label ipAddress_Label;
        
        #line default
        #line hidden
        
        
        #line 12 "..\..\..\Windows\PairingWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox nanoIP_TextBox;
        
        #line default
        #line hidden
        
        
        #line 13 "..\..\..\Windows\PairingWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button manualDetect_Button;
        
        #line default
        #line hidden
        
        
        #line 14 "..\..\..\Windows\PairingWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox autoDetectIps_ListBox;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/NanoTwitchLeafs;component/windows/pairingwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Windows\PairingWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.Pairing_Window = ((NanoTwitchLeafs.Windows.PairingWindow)(target));
            return;
            case 2:
            this.autoDetect_Button = ((System.Windows.Controls.Button)(target));
            
            #line 10 "..\..\..\Windows\PairingWindow.xaml"
            this.autoDetect_Button.Click += new System.Windows.RoutedEventHandler(this.AutoDetect_Button_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            this.ipAddress_Label = ((System.Windows.Controls.Label)(target));
            return;
            case 4:
            this.nanoIP_TextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 5:
            this.manualDetect_Button = ((System.Windows.Controls.Button)(target));
            
            #line 13 "..\..\..\Windows\PairingWindow.xaml"
            this.manualDetect_Button.Click += new System.Windows.RoutedEventHandler(this.ManualDetect_Button_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            this.autoDetectIps_ListBox = ((System.Windows.Controls.ListBox)(target));
            
            #line 14 "..\..\..\Windows\PairingWindow.xaml"
            this.autoDetectIps_ListBox.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.AutoDetectIps_ListBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

