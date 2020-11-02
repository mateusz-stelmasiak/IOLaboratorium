using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerLibrary
{
    using System.Runtime.Serialization;

    [Serializable()]
    public class AuthenticationException : Exception
    {
        public int ErrorCategory { get; }

        public AuthenticationException(string message)
           : base(message)
        {
          
        }
        public AuthenticationException(string message,int category)
          : base(message)
        {
            ErrorCategory = category;
        }


        protected AuthenticationException(SerializationInfo info,
                                    StreamingContext context)
           : base(info, context)
        { }

        public override string ToString()
        {
            return base.Message;
        }
    }
}
