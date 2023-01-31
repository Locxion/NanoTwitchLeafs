using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace NanoTwitchLeafs.Objects
{
    /// <summary>
    /// object to implement event callback attachment and triggering for current object and children properties
    /// </summary>
    [NotifyHandler(Notify = true)]
    public abstract class NotifyObject : INotifyPropertyChanged
    {
        /// <summary>
        /// dictionary to store properties values in
        /// </summary>
        private readonly Dictionary<string, object> propertyValues;

        /// <summary>
        /// get or sets if callback event has been attached already
        /// </summary>
        protected bool Attached { get; set; }

        /// <summary>
        /// gets or sets if the object has been fully initialized and event may be triggered
        /// </summary>
        protected bool Initialized { get; set; }

        protected NotifyObject()
        {
            propertyValues = new Dictionary<string, object>();
            Attached = false;
            Initialized = false;
        }

        [Obsolete("use " + nameof(AttachPropertyChanged) + " instead!!!")]
        /// <summary>
        /// DO NOT USE, use AttachPropertyChanged instead!!!
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// attaches callback event which executes on property value change
        /// </summary>
        /// <param name="callback">callback to execute</param>
        /// <param name="propagateToChildren">defines if callback should be attached to all subsequent properties implementing this object</param>
        public void AttachPropertyChanged(Action<object, PropertyChangedEventArgs> callback, bool propagateToChildren = true)
        {
            AttachForAll(callback, propagateToChildren);
            Initialized = true;
        }

        private void AttachForAll(Action<object, PropertyChangedEventArgs> callback, bool propagateToChildren = true)
        {
            // prevent looping if objects reference each other
            if (Attached)
            {
                return;
            }
            Attached = true;

            // attach event for object which inherits from NotifyObject
            PropertyChanged += delegate (object s, PropertyChangedEventArgs e)
            {
                callback(s, e);
            };

            // define if event handler should be attached to all properties which are also derived from NotifyObject
            if (propagateToChildren != true)
            {
                return;
            }

            // get properties and check they derive from the NotifyObject
            System.Reflection.PropertyInfo[] props = this.GetType().GetProperties();
            foreach (System.Reflection.PropertyInfo prop in props)
            {
                if (!prop.PropertyType.IsSubclassOf(typeof(NotifyObject)))
                {
                    continue;
                }

                object propValue = prop.GetValue(this);
                MethodInfo propMethod = typeof(NotifyObject).GetMethod(nameof(AttachPropertyChanged));
                propMethod.Invoke(propValue, new object[] { callback, propagateToChildren });
            }
        }

        /// <summary>
        /// fire property changed event
        /// </summary>
        /// <typeparam name="T">type of property</typeparam>
        /// <param name="expression">property expression</param>
        protected void OnPropertyChanged<T>(Expression<Func<T>> expression)
        {
            string propertyName = GetPropertyNameFromExpression(expression);
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// fire property changed event
        /// </summary>
        /// <param name="propertyName">property name</param>
        protected void OnPropertyChanged(string propertyName)
        {
            // guarantee object has been fully initialized
            if (!Initialized)
            {
                return;
            }

            // check if property change event should be raised
            if (NotifyHandlerHelper.Notify(this.GetType(), propertyName) != true)
            {
                return;
            }

            // raise event if any listeners registered
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// wrappes setter to store property value to dictionary and trigger event
        /// </summary>
        /// <typeparam name="T">type of property expression</typeparam>
        /// <param name="expression">property expression</param>
        /// <param name="value">property value</param>
        protected void Set<T>(Expression<Func<T>> expression, T value)
        {
            string propertyName = GetPropertyNameFromExpression(expression);
            Set(propertyName, value);
        }

        /// <summary>
        /// wrappes setter to store property value to dictionary and trigger event
        /// </summary>
        /// <typeparam name="T">type of property expression</typeparam>
        /// <param name="name">property name</param>
        /// <param name="value">property value</param>
        protected void Set<T>(string name, T value)
        {
            if (propertyValues.ContainsKey(name))
            {
                propertyValues[name] = value;
                OnPropertyChanged(name);
            }
            else
            {
                propertyValues.Add(name, value);
                OnPropertyChanged(name);
            }
        }

        /// <summary>
        /// wrappes getter to retrieved property value from dictionary
        /// </summary>
        /// <typeparam name="T">type of property expression</typeparam>
        /// <param name="expression">property expression</param>
        /// <param name="value">property value</param>
        protected T Get<T>(Expression<Func<T>> expression)
        {
            string propertyName = GetPropertyNameFromExpression(expression);
            return Get<T>(propertyName);
        }

        /// <summary>
        /// wrappes getter to retrieved property value from dictionary
        /// </summary>
        /// <typeparam name="T">type of property expression</typeparam>
        /// <param name="name">property name</param>
        /// <param name="value">property value</param>
        protected T Get<T>(string name)
        {
            if (propertyValues.ContainsKey(name))
            {
                return (T)propertyValues[name];
            }
            return default(T);
        }

        /// <summary>
        /// get property name by exppression
        /// </summary>
        /// <typeparam name="T">type of property expression</typeparam>
        /// <param name="expression">property expression</param>
        /// <returns></returns>
        private static string GetPropertyNameFromExpression<T>(Expression<Func<T>> expression)
        {
            MemberExpression memberExpression = (MemberExpression)expression.Body;
            return memberExpression.Member.Name;
        }
    }
}
