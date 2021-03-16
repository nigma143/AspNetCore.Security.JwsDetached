﻿using System;

namespace AspNetCore.Security.JwsDetached
{
    public enum JwsDetachedType
    {
        HeaderNotFound,
        InvalidSignature
    }

    public class JwsDetachedException : Exception
    {
        public JwsDetachedType Type { get; }

        public override string Message => Type switch
        {
            JwsDetachedType.HeaderNotFound => "Header not found",
            JwsDetachedType.InvalidSignature => "Invalid signature",
            _ => throw new ArgumentOutOfRangeException()
        };

        public JwsDetachedException(JwsDetachedType type)
        {
            Type = type;
        }
    }
}
