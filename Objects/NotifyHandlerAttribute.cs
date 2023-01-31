using System;

namespace NanoTwitchLeafs.Objects
{
    /// <summary>
    /// helps to define if the given property change may trigger the callback attached to its parent
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    internal sealed class NotifyHandlerAttribute : Attribute
    {
        /// <summary>
        /// default true
        /// </summary>
        public bool Notify { get; set; }

        public NotifyHandlerAttribute()
        {
            Notify = true;
        }
    }
}