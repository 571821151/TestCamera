﻿

#pragma checksum "G:\testcamera\TestCamera\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "F4E7643A063B02915E58ACFDF28A0361"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TestCamera
{
    partial class MainPage : global::Windows.UI.Xaml.Controls.Page, global::Windows.UI.Xaml.Markup.IComponentConnector
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
 
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                #line 8 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.FrameworkElement)(target)).Loaded += this.Page_Loaded;
                 #line default
                 #line hidden
                #line 8 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.FrameworkElement)(target)).SizeChanged += this.Page_SizeChanged;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 125 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.btnShowLog_Click;
                 #line default
                 #line hidden
                break;
            case 3:
                #line 130 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.btnClearLog_Click;
                 #line default
                 #line hidden
                break;
            case 4:
                #line 135 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.btnSettings_Click;
                 #line default
                 #line hidden
                break;
            case 5:
                #line 172 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.btnShowCameraInfo_Click;
                 #line default
                 #line hidden
                break;
            case 6:
                #line 177 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.btnCopy_Click;
                 #line default
                 #line hidden
                break;
            case 7:
                #line 144 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.cbAutoFocus;
                 #line default
                 #line hidden
                break;
            case 8:
                #line 150 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.cbAutoExposure;
                 #line default
                 #line hidden
                break;
            case 9:
                #line 156 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.cbBacklightCompensation;
                 #line default
                 #line hidden
                break;
            case 10:
                #line 162 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.cbWhiteBalance;
                 #line default
                 #line hidden
                break;
            case 11:
                #line 85 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.btnCaptureMorePhoto_Click;
                 #line default
                 #line hidden
                break;
            case 12:
                #line 89 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.btnNormalCapture_Click;
                 #line default
                 #line hidden
                break;
            case 13:
                #line 95 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.cbEnableLowLagCapture;
                 #line default
                 #line hidden
                break;
            case 14:
                #line 101 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.btnClickLowLagCapture;
                 #line default
                 #line hidden
                break;
            case 15:
                #line 108 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.btnMoire_Click;
                 #line default
                 #line hidden
                break;
            case 16:
                #line 114 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.btn_Click;
                 #line default
                 #line hidden
                break;
            case 17:
                #line 118 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.btnClearPhoto_Click;
                 #line default
                 #line hidden
                break;
            case 18:
                #line 53 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.Selector)(target)).SelectionChanged += this.cbxCameraList_SelectionChanged;
                 #line default
                 #line hidden
                break;
            case 19:
                #line 61 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.Selector)(target)).SelectionChanged += this.cbxSizeList_SelectionChanged;
                 #line default
                 #line hidden
                break;
            case 20:
                #line 79 "..\..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.sldZoom_ValueChanged;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}

