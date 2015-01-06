using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// Default server reason codes
    /// </summary>
    public static class ReasonCodes
    {
        /// <summary>
        /// General error
        /// </summary>
        public const int GENERAL_ERROR = 1;
        /// <summary>
        /// General session error
        /// </summary>
        public const int SESSION_ERROR = 11;
        /// <summary>
        /// The session resource is already registered
        /// </summary>
        public const int SESSION_REGISTRATION_ERROR = 12;
        /// <summary>
        /// An authentication error occurred
        /// </summary>
        public const int SESSION_AUTHENTICATION_FAILED = 13;
        /// <summary>
        /// An error occurred while unregistering the session 
        /// in the server
        /// </summary>
        public const int SESSION_UNREGISTER_FAILED = 14;
        /// <summary>
        /// The required action is invalid for
        /// current session state
        /// </summary>
        public const int SESSION_INVALID_ACTION_FOR_STATE = 15;
        /// <summary>
        /// The session negotiation has timed out
        /// </summary>
        public const int SESSION_NEGOTIATION_TIMEOUT = 16;
        /// <summary>
        /// Invalid selected negotiation options
        /// </summary>
        public const int SESSION_NEGOTIATION_INVALID_OPTIONS = 17;
        /// <summary>
        /// Invalid session mode requested
        /// </summary>
        public const int SESSION_INVALID_SESSION_MODE_REQUESTED = 18;
        /// <summary>
        /// General validation error
        /// </summary>
        public const int VALIDATION_ERROR = 21;
        /// <summary>
        /// The envelope document is null
        /// </summary>
        public const int VALIDATION_EMPTY_DOCUMENT = 22;
        /// <summary>
        /// The envelope document MIME type is invalid
        /// </summary>
        public const int VALIDATION_INVALID_RESOURCE = 23;
        /// <summary>
        /// The request status is invalid
        /// </summary>
        public const int VALIDATION_INVALID_STATUS = 24;
        /// <summary>
        /// The request identity is invalid
        /// </summary>
        public const int VALIDATION_INVALID_IDENTITY = 25;
        /// <summary>
        /// The envelope originator or destination is invalid
        /// </summary>
        public const int VALIDATION_INVALID_RECIPIENTS = 26;
        /// <summary>
        /// The command method is invalid
        /// </summary>
        public const int VALIDATION_INVALID_METHOD = 27;
        /// <summary>
        /// The command URI format is invalid
        /// </summary>
        public const int VALIDATION_INVALID_URI = 27;
        /// <summary>
        /// General authorization error
        /// </summary>
        public const int AUTHORIZATION_ERROR = 31;
        /// <summary>
        /// The sender is not authorized to send
        /// messages to the message destination
        /// </summary>
        public const int AUTHORIZATION_UNAUTHORIZED_SENDER = 32;
        /// <summary>
        /// The destination doesn't have an active 
        /// account
        /// </summary>
        public const int AUTHORIZATION_DESTINATION_ACCOUNT_NOT_FOUND = 33;
        /// <summary>
        /// The envelope quota limit has been exceeded
        /// </summary>
        public const int AUTHORIZATION_QUOTA_THRESHOLD_EXCEEDED = 34;
        /// <summary>
        /// General routing error
        /// </summary>
        public const int ROUTING_ERROR = 41;
        /// <summary>
        /// The message destination was not found
        /// </summary>
        public const int ROUTING_DESTINATION_NOT_FOUND = 42;
        /// <summary>
        /// The message destination gateway was not found
        /// </summary>
        public const int ROUTING_GATEWAY_NOT_FOUND = 43;
        /// <summary>
        /// The message destination was not found
        /// </summary>
        public const int ROUTING_ROUTE_NOT_FOUND = 44;
        /// <summary>
        /// General dispatching error
        /// </summary>
        public const int DISPATCH_ERROR = 51;
        /// <summary>
        /// General command processing error
        /// </summary>
        public const int COMMAND_PROCESSING_ERROR = 61;
        /// <summary>
        /// There's no command processor available 
        /// for process the request
        /// </summary>
        public const int COMMAND_RESOURCE_NOT_SUPPORTED = 62;
        /// <summary>
        /// The command method is not supported
        /// </summary>
        public const int COMMAND_METHOD_NOT_SUPPORTED = 63;
        /// <summary>
        /// The command method has an invalid argument value
        /// </summary>
        public const int COMMAND_INVALID_ARGUMENT = 64;
        /// <summary>
        /// The requested command is not valid for current
        /// session mode
        /// </summary>
        public const int COMMAND_INVALID_SESSION_MODE = 65;
        /// <summary>
        /// The command method was not allowed
        /// </summary>
        public const int COMMAND_NOT_ALLOWED = 66;
        /// <summary>
        /// The command resource was not found
        /// </summary>
        public const int COMMAND_RESOURCE_NOT_FOUND = 67;
        /// <summary>
        /// General message processing error
        /// </summary>
        public const int MESSAGE_PROCESSING_ERROR = 61;
        /// <summary>
        /// The message content type
        /// is not supported
        /// </summary>
        public const int MESSAGE_UNSUPPORTED_CONTENT_TYPE = 71;
        /// <summary>
        /// General gateway processing error
        /// </summary>
        public const int GATEWAY_ERROR = 81;
        /// <summary>
        /// The content type is not supported
        /// by the gateway
        /// </summary>
        public const int GATEWAY_CONTENT_TYPE_NOT_SUPPORTED = 82;
        /// <summary>
        /// The message destination was not found
        /// on gateway
        /// </summary>
        public const int GATEWAY_DESTINATION_NOT_FOUND = 83;
        /// <summary>
        /// The functionality is not supported 
        /// by the gateway
        /// </summary>
        public const int GATEWAY_NOT_SUPPORTED = 84;
        /// <summary>
        /// General application processing error
        /// </summary>
        public const int APPLICATION_ERROR = 101;        
    }
}
