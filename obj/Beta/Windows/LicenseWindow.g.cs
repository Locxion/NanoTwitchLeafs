﻿#pragma checksum "..\..\..\Windows\LicenseWindow.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "9E5F5D2871CE3DC55D300E9ECB175942BA8F5E0BA4461A8E4118C64BD14675AA"
//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
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
    /// LicenseWindow
    /// </summary>
    public partial class LicenseWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 10 "..\..\..\Windows\LicenseWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox licenseKey_Textbox;
        
        #line default
        #line hidden
        
        
        #line 16 "..\..\..\Windows\LicenseWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button activateLicense_Button;
        
        #line default
        #line hidden
        
        
        #line 17 "..\..\..\Windows\LicenseWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button deactivateLicense_Button;
        
        #line default
        #line hidden
        
        
        #line 18 "..\..\..\Windows\LicenseWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label email_Label;
        
        #line default
        #line hidden
        
        
        #line 19 "..\..\..\Windows\LicenseWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label twitchChannel_Label;
        
        #line default
        #line hidden
        
        
        #line 20 "..\..\..\Windows\LicenseWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label licenseType_Label;
        
        #line default
        #line hidden
        
        
        #line 21 "..\..\..\Windows\LicenseWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label validFrom_Label;
        
        #line default
        #line hidden
        
        
        #line 22 "..\..\..\Windows\LicenseWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label validTo_Label;
        
        #line default
        #line hidden
        
        
        #line 23 "..\..\..\Windows\LicenseWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label GetLicenseLink;
        
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
            System.Uri resourceLocater = new System.Uri("/NanoTwitchLeafs;component/windows/licensewindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Windows\LicenseWindow.xaml"
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
            this.licenseKey_Textbox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 2:
            this.activateLicense_Button = ((System.Windows.Controls.Button)(target));
            
            #line 16 "..\..\..\Windows\LicenseWindow.xaml"
            this.activateLicense_Button.Click += new System.Windows.RoutedEventHandler(this.activateLicense_Button_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            this.deactivateLicense_Button = ((System.Windows.Controls.Button)(target));
            
            #line 17 "..\..\..\Windows\LicenseWindow.xaml"
            this.deactivateLicense_Button.Click += new System.Windows.RoutedEventHandler(this.deactivateLicense_Button_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.email_Label = ((System.Windows.Controls.Label)(target));
            return;
            case 5:
            this.twitchChannel_Label = ((System.Windows.Controls.Label)(target));
            return;
            case 6:
            this.licenseType_Label = ((System.Windows.Controls.Label)(target));
            return;
            case 7:
            this.validFrom_Label = ((System.Windows.Controls.Label)(target));
            return;
            case 8:
            this.validTo_Label = ((System.Windows.Controls.Label)(target));
            return;
            case 9:
            this.GetLicenseLink = ((System.Windows.Controls.Label)(target));
            
            #line 23 "..\..\..\Windows\LicenseWindow.xaml"
            this.GetLicenseLink.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.GetLicenseLink_OnMouseDown);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

