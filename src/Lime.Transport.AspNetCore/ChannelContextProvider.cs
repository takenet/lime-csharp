using System;

namespace Lime.Transport.AspNetCore
{
    internal class ChannelContextProvider
    {
        private ChannelContext? _context;
        private bool _used;

        public void SetContext(ChannelContext requestContext)
        {
            if (_context != null || requestContext == null)
            {
                throw new InvalidOperationException();
            }

            _context = requestContext;
        }

        public ChannelContext GetContext()
        {
            if (_used || _context == null)
            {
                throw new InvalidOperationException();
            }

            _used = true;
            return _context;
        }
    }
}