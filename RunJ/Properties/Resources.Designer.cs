﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RunJ.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("RunJ.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to command.
        /// </summary>
        internal static string CommandFileName {
            get {
                return ResourceManager.GetString("CommandFileName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .bk.
        /// </summary>
        internal static string CommandFileNameBackupSuffix {
            get {
                return ResourceManager.GetString("CommandFileNameBackupSuffix", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .csv.
        /// </summary>
        internal static string CommandFileNameSuffix {
            get {
                return ResourceManager.GetString("CommandFileNameSuffix", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to #.
        /// </summary>
        internal static string CommentHeader {
            get {
                return ResourceManager.GetString("CommentHeader", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ddd, MMM d, yyyy - MMddyy.
        /// </summary>
        internal static string DateFormat {
            get {
                return ResourceManager.GetString("DateFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The hotkey associated with this application (Alt+Ctrl+Q) is currrently in use. Please make sure no two similar instances are running at the same time. To quit previous instance type `$q`. Quitting now ....
        /// </summary>
        internal static string ErrorHotkeyAlreadyRegistered {
            get {
                return ResourceManager.GetString("ErrorHotkeyAlreadyRegistered", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to HH:mm.
        /// </summary>
        internal static string TimeFormat {
            get {
                return ResourceManager.GetString("TimeFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .9.
        /// </summary>
        internal static string WindowGotFocusOpacity {
            get {
                return ResourceManager.GetString("WindowGotFocusOpacity", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .1.
        /// </summary>
        internal static string WindowLostFocusOpacity {
            get {
                return ResourceManager.GetString("WindowLostFocusOpacity", resourceCulture);
            }
        }
    }
}
