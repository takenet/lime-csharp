using System;

namespace Lime.Transport.AspNetCore
{
    internal class ChannelContextProvider
    {
        private ChannelContext? _context;

        public void SetContext(ChannelContext requestContext)
        {
            if (_context != null)
            {
                throw new InvalidOperationException("The channel context has already been set");
            }

            _context = requestContext;
        }

        public ChannelContext GetContext()
        {
            if (_context == null)
            {
                throw new InvalidOperationException("The channel context was not been set");
            }            
            
            return _context;
        }
    }
}