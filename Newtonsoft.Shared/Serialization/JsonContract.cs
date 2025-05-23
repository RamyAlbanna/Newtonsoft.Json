﻿#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using SharedNewtonsoft.Json.Linq;
using SharedNewtonsoft.Json.Utilities;

namespace SharedNewtonsoft.Json.Serialization
{
    internal enum JsonContractType
    {
        None = 0,
        Object = 1,
        Array = 2,
        Primitive = 3,
        String = 4,
        Dictionary = 5,
        Dynamic = 6,
        Serializable = 7,
        Linq = 8
    }

    /// <summary>
    /// Handles <see cref="JsonSerializer"/> serialization callback events.
    /// </summary>
    /// <param name="o">The object that raised the callback event.</param>
    /// <param name="context">The streaming context.</param>
    public delegate void SerializationCallback(object o, StreamingContext context);

    /// <summary>
    /// Handles <see cref="JsonSerializer"/> serialization error callback events.
    /// </summary>
    /// <param name="o">The object that raised the callback event.</param>
    /// <param name="context">The streaming context.</param>
    /// <param name="errorContext">The error context.</param>
    public delegate void SerializationErrorCallback(object o, StreamingContext context, ErrorContext errorContext);

    /// <summary>
    /// Sets extension data for an object during deserialization.
    /// </summary>
    /// <param name="o">The object to set extension data on.</param>
    /// <param name="key">The extension data key.</param>
    /// <param name="value">The extension data value.</param>
    public delegate void ExtensionDataSetter(object o, string key, object? value);

    /// <summary>
    /// Gets extension data for an object during serialization.
    /// </summary>
    /// <param name="o">The object to set extension data on.</param>
    public delegate IEnumerable<KeyValuePair<object, object>>? ExtensionDataGetter(object o);

    /// <summary>
    /// Contract details for a <see cref="System.Type"/> used by the <see cref="JsonSerializer"/>.
    /// </summary>
    public abstract class JsonContract
    {
        internal bool IsNullable;
        internal bool IsConvertable;
        internal bool IsEnum;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        internal Type NonNullableUnderlyingType;
        internal ReadType InternalReadType;
        internal JsonContractType ContractType;
        internal bool IsReadOnlyOrFixedSize;
        internal bool IsSealed;
        internal bool IsInstantiable;

        private List<SerializationCallback>? _onDeserializedCallbacks;
        private List<SerializationCallback>? _onDeserializingCallbacks;
        private List<SerializationCallback>? _onSerializedCallbacks;
        private List<SerializationCallback>? _onSerializingCallbacks;
        private List<SerializationErrorCallback>? _onErrorCallbacks;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        private Type _createdType;

        /// <summary>
        /// Gets the underlying type for the contract.
        /// </summary>
        /// <value>The underlying type for the contract.</value>
        public Type UnderlyingType { get; }

        /// <summary>
        /// Gets or sets the type created during deserialization.
        /// </summary>
        /// <value>The type created during deserialization.</value>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public Type CreatedType
        {
            get => _createdType;
            set
            {
                ValidationUtils.ArgumentNotNull(value, nameof(value));
                _createdType = value;

                IsSealed = _createdType.IsSealed();
                IsInstantiable = !(_createdType.IsInterface() || _createdType.IsAbstract());
            }
        }

        /// <summary>
        /// Gets or sets whether this type contract is serialized as a reference.
        /// </summary>
        /// <value>Whether this type contract is serialized as a reference.</value>
        public bool? IsReference { get; set; }

        /// <summary>
        /// Gets or sets the default <see cref="JsonConverter" /> for this contract.
        /// </summary>
        /// <value>The converter.</value>
        public JsonConverter? Converter { get; set; }

        /// <summary>
        /// Gets the internally resolved <see cref="JsonConverter"/> for the contract's type.
        /// This converter is used as a fallback converter when no other converter is resolved.
        /// Setting <see cref="Converter"/> will always override this converter.
        /// </summary>
        public JsonConverter? InternalConverter { get; internal set; }

        /// <summary>
        /// Gets or sets all methods called immediately after deserialization of the object.
        /// </summary>
        /// <value>The methods called immediately after deserialization of the object.</value>
        public IList<SerializationCallback> OnDeserializedCallbacks
        {
            get
            {
                if (_onDeserializedCallbacks == null)
                {
                    _onDeserializedCallbacks = new List<SerializationCallback>();
                }

                return _onDeserializedCallbacks;
            }
        }

        /// <summary>
        /// Gets or sets all methods called during deserialization of the object.
        /// </summary>
        /// <value>The methods called during deserialization of the object.</value>
        public IList<SerializationCallback> OnDeserializingCallbacks
        {
            get
            {
                if (_onDeserializingCallbacks == null)
                {
                    _onDeserializingCallbacks = new List<SerializationCallback>();
                }

                return _onDeserializingCallbacks;
            }
        }

        /// <summary>
        /// Gets or sets all methods called after serialization of the object graph.
        /// </summary>
        /// <value>The methods called after serialization of the object graph.</value>
        public IList<SerializationCallback> OnSerializedCallbacks
        {
            get
            {
                if (_onSerializedCallbacks == null)
                {
                    _onSerializedCallbacks = new List<SerializationCallback>();
                }

                return _onSerializedCallbacks;
            }
        }

        /// <summary>
        /// Gets or sets all methods called before serialization of the object.
        /// </summary>
        /// <value>The methods called before serialization of the object.</value>
        public IList<SerializationCallback> OnSerializingCallbacks
        {
            get
            {
                if (_onSerializingCallbacks == null)
                {
                    _onSerializingCallbacks = new List<SerializationCallback>();
                }

                return _onSerializingCallbacks;
            }
        }

        /// <summary>
        /// Gets or sets all method called when an error is thrown during the serialization of the object.
        /// </summary>
        /// <value>The methods called when an error is thrown during the serialization of the object.</value>
        public IList<SerializationErrorCallback> OnErrorCallbacks
        {
            get
            {
                if (_onErrorCallbacks == null)
                {
                    _onErrorCallbacks = new List<SerializationErrorCallback>();
                }

                return _onErrorCallbacks;
            }
        }

        /// <summary>
        /// Gets or sets the default creator method used to create the object.
        /// </summary>
        /// <value>The default creator method used to create the object.</value>
        public Serialization.Func<object>? DefaultCreator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the default creator is non-public.
        /// </summary>
        /// <value><c>true</c> if the default object creator is non-public; otherwise, <c>false</c>.</value>
        public bool DefaultCreatorNonPublic { get; set; }

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        internal JsonContract(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type underlyingType)
        {
            ValidationUtils.ArgumentNotNull(underlyingType, nameof(underlyingType));

            UnderlyingType = underlyingType;

            // resolve ByRef types
            // typically comes from in and ref parameters on methods/ctors
            underlyingType = ReflectionUtils.EnsureNotByRefType(underlyingType);

            IsNullable = ReflectionUtils.IsNullable(underlyingType);
             
            NonNullableUnderlyingType = (IsNullable && ReflectionUtils.IsNullableType(underlyingType)) ? Nullable.GetUnderlyingType(underlyingType)! : underlyingType;

            _createdType = CreatedType = NonNullableUnderlyingType;

            IsConvertable = ConvertUtils.IsConvertible(NonNullableUnderlyingType);
            IsEnum = NonNullableUnderlyingType.IsEnum();

            InternalReadType = ReadType.Read;
        }

        internal void InvokeOnSerializing(object o, StreamingContext context)
        {
            if (_onSerializingCallbacks != null)
            {
                foreach (SerializationCallback callback in _onSerializingCallbacks)
                {
                    callback(o, context);
                }
            }
        }

        internal void InvokeOnSerialized(object o, StreamingContext context)
        {
            if (_onSerializedCallbacks != null)
            {
                foreach (SerializationCallback callback in _onSerializedCallbacks)
                {
                    callback(o, context);
                }
            }
        }

        internal void InvokeOnDeserializing(object o, StreamingContext context)
        {
            if (_onDeserializingCallbacks != null)
            {
                foreach (SerializationCallback callback in _onDeserializingCallbacks)
                {
                    callback(o, context);
                }
            }
        }

        internal void InvokeOnDeserialized(object o, StreamingContext context)
        {
            if (_onDeserializedCallbacks != null)
            {
                foreach (SerializationCallback callback in _onDeserializedCallbacks)
                {
                    callback(o, context);
                }
            }
        }

        internal void InvokeOnError(object o, StreamingContext context, ErrorContext errorContext)
        {
            if (_onErrorCallbacks != null)
            {
                foreach (SerializationErrorCallback callback in _onErrorCallbacks)
                {
                    callback(o, context, errorContext);
                }
            }
        }

        internal static SerializationCallback CreateSerializationCallback(MethodInfo callbackMethodInfo)
        {
            return (o, context) => callbackMethodInfo.Invoke(o, new object[] { context });
        }

        internal static SerializationErrorCallback CreateSerializationErrorCallback(MethodInfo callbackMethodInfo)
        {
            return (o, context, econtext) => callbackMethodInfo.Invoke(o, new object[] { context, econtext });
        }
    }
}