using System;

namespace GerenciadorAulas.Exceptions
{
    public class MpvPathNotConfiguredException : Exception
    {
        public MpvPathNotConfiguredException() { }
        public MpvPathNotConfiguredException(string message) : base(message) { }
        public MpvPathNotConfiguredException(string message, Exception inner) : base(message, inner) { }
    }
}
