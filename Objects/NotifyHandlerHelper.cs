using System;
using System.Reflection;

namespace NanoTwitchLeafs.Objects
{
    /// <summary>
    /// helps identify if notify handler has been set with defined properties
    /// </summary>
    public static class NotifyHandlerHelper
    {
        /// <summary>
        /// returns if the callback event should be triggered for the given property for the given type
        /// </summary>
        /// <param name="type">property type</param>
        /// <param name="propertyName">property name</param>
        /// <returns>return if callback should be triggered</returns>
        public static bool Notify(this Type type, string propertyName)
        {
            // get detailed type info
            TypeInfo typeInfo = type.GetTypeInfo();

            // get notify handler attribute for the given type
            Attribute classAttribute = typeInfo.GetCustomAttribute(typeof(NotifyHandlerAttribute));

            // get if the attribute has been set to the given type and wants us to notify
            bool notifyByClassAttribute = classAttribute != null && ((NotifyHandlerAttribute)classAttribute).Notify == true;

            // try to get property info from given type
            PropertyInfo property = typeInfo.GetProperty(propertyName);

            // if property not present return notify preset of given type
            if (property == null)
            {
                return notifyByClassAttribute;
            }

            // get notify handler attribute for the given property
            Attribute attribute = property.GetCustomAttribute(typeof(NotifyHandlerAttribute));

            // if attribute is not assigned to property return notify preset of given type
            if (attribute == null)
            {
                return notifyByClassAttribute;
            }

            // cast the attribute and return its notification value for the property
            NotifyHandlerAttribute castedAttribute = attribute as NotifyHandlerAttribute;
            return castedAttribute.Notify;
        }
    }
}
