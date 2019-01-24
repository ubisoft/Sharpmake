// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
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
            : base(String.Format(message, args))
        { }
        public Error(Exception innerException)
            : base("Sharpmake Error", innerException)
        { }
        public Error(Exception innerException, string message, params object[] args)
            : base(String.Format(message, args), innerException)
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

        protected Error(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
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
            : base(String.Format(message, args))
        { }
        public InternalError(Exception innerException)
            : base("Sharpmake Internal Error", innerException)
        { }
        public InternalError(Exception innerException, string message, params object[] args)
            : base(String.Format(message, args), innerException)
        { }

        protected InternalError(SerializationInfo info, StreamingContext context)
            : base(info, context)
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
