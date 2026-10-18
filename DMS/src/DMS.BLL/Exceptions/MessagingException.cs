using System;
using System.Collections.Generic;
using System.Text;

namespace DMS.BLL.Exceptions
{
    public class MessagingException : Exception
    {
        public MessagingException(string message) : base(message) { }
        public MessagingException(string message, Exception innerException) : base(message, innerException) { } 
    }
}
