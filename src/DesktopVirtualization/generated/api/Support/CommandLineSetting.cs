// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is regenerated.

namespace Microsoft.Azure.PowerShell.Cmdlets.DesktopVirtualization.Support
{

    /// <summary>
    /// Specifies whether this published application can be launched with command line arguments provided by the client, command
    /// line arguments specified at publish time, or no command line arguments at all.
    /// </summary>
    public partial struct CommandLineSetting :
        System.IEquatable<CommandLineSetting>
    {
        public static Microsoft.Azure.PowerShell.Cmdlets.DesktopVirtualization.Support.CommandLineSetting Allow = @"Allow";

        public static Microsoft.Azure.PowerShell.Cmdlets.DesktopVirtualization.Support.CommandLineSetting DoNotAllow = @"DoNotAllow";

        public static Microsoft.Azure.PowerShell.Cmdlets.DesktopVirtualization.Support.CommandLineSetting Require = @"Require";

        /// <summary>the value for an instance of the <see cref="CommandLineSetting" /> Enum.</summary>
        private string _value { get; set; }

        /// <summary>Creates an instance of the <see cref="CommandLineSetting" Enum class./></summary>
        /// <param name="underlyingValue">the value to create an instance for.</param>
        private CommandLineSetting(string underlyingValue)
        {
            this._value = underlyingValue;
        }

        /// <summary>Conversion from arbitrary object to CommandLineSetting</summary>
        /// <param name="value">the value to convert to an instance of <see cref="CommandLineSetting" />.</param>
        internal static object CreateFrom(object value)
        {
            return new CommandLineSetting(System.Convert.ToString(value));
        }

        /// <summary>Compares values of enum type CommandLineSetting</summary>
        /// <param name="e">the value to compare against this instance.</param>
        /// <returns><c>true</c> if the two instances are equal to the same value</returns>
        public bool Equals(Microsoft.Azure.PowerShell.Cmdlets.DesktopVirtualization.Support.CommandLineSetting e)
        {
            return _value.Equals(e._value);
        }

        /// <summary>Compares values of enum type CommandLineSetting (override for Object)</summary>
        /// <param name="obj">the value to compare against this instance.</param>
        /// <returns><c>true</c> if the two instances are equal to the same value</returns>
        public override bool Equals(object obj)
        {
            return obj is CommandLineSetting && Equals((CommandLineSetting)obj);
        }

        /// <summary>Returns hashCode for enum CommandLineSetting</summary>
        /// <returns>The hashCode of the value</returns>
        public override int GetHashCode()
        {
            return this._value.GetHashCode();
        }

        /// <summary>Returns string representation for CommandLineSetting</summary>
        /// <returns>A string for this value.</returns>
        public override string ToString()
        {
            return this._value;
        }

        /// <summary>Implicit operator to convert string to CommandLineSetting</summary>
        /// <param name="value">the value to convert to an instance of <see cref="CommandLineSetting" />.</param>

        public static implicit operator CommandLineSetting(string value)
        {
            return new CommandLineSetting(value);
        }

        /// <summary>Implicit operator to convert CommandLineSetting to string</summary>
        /// <param name="e">the value to convert to an instance of <see cref="CommandLineSetting" />.</param>

        public static implicit operator string(Microsoft.Azure.PowerShell.Cmdlets.DesktopVirtualization.Support.CommandLineSetting e)
        {
            return e._value;
        }

        /// <summary>Overriding != operator for enum CommandLineSetting</summary>
        /// <param name="e1">the value to compare against <see cref="e2" /></param>
        /// <param name="e2">the value to compare against <see cref="e1" /></param>
        /// <returns><c>true</c> if the two instances are not equal to the same value</returns>
        public static bool operator !=(Microsoft.Azure.PowerShell.Cmdlets.DesktopVirtualization.Support.CommandLineSetting e1, Microsoft.Azure.PowerShell.Cmdlets.DesktopVirtualization.Support.CommandLineSetting e2)
        {
            return !e2.Equals(e1);
        }

        /// <summary>Overriding == operator for enum CommandLineSetting</summary>
        /// <param name="e1">the value to compare against <see cref="e2" /></param>
        /// <param name="e2">the value to compare against <see cref="e1" /></param>
        /// <returns><c>true</c> if the two instances are equal to the same value</returns>
        public static bool operator ==(Microsoft.Azure.PowerShell.Cmdlets.DesktopVirtualization.Support.CommandLineSetting e1, Microsoft.Azure.PowerShell.Cmdlets.DesktopVirtualization.Support.CommandLineSetting e2)
        {
            return e2.Equals(e1);
        }
    }
}