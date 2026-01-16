// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Sharpmake
{
    [Serializable]
    public class Error : Exception
    {
        public Error()
            : base("Sharpmake Error")
        { }
        public Error(string message)
            : base(message)
        { }
        public Error(string message, params object[] args)
            : base(string.Format(message, args))
        { }
        public Error(Exception innerException)
            : base("Sharpmake Error", innerException)
        { }
        public Error(Exception innerException, string message, params object[] args)
            : base(string.Format(message, args), innerException)
        { }

        public static void Valid(bool condition)
        {
            if (!condition)
                throw new Error();
        }

        public static void Valid(bool condition, string message, params object[] args)
        {
            if (!condition)
                throw new Error(message, args);
        }
    }

    [Serializable]
    public class InternalError : Exception
    {
        public InternalError()
            : base("Sharpmake Internal Error")
        { }
        public InternalError(string message)
            : base(message)
        { }
        public InternalError(string message, params object[] args)
            : base(string.Format(message, args))
        { }
        public InternalError(Exception innerException)
            : base("Sharpmake Internal Error", innerException)
        { }
        public InternalError(Exception innerException, string message, params object[] args)
            : base(string.Format(message, args), innerException)
        { }

        public static void Valid(bool condition)
        {
            if (!condition)
                throw new InternalError();
        }
        public static void Valid(bool condition, string message, params object[] args)
        {
            if (!condition)
                throw new InternalError(message, args);
        }
    }
}
