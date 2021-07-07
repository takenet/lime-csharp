using System;
using System.Linq;

namespace Lime.Transport.AspNetCore
{
    public sealed class HttpEndPointOptions : EndPointOptions
    {
        public string MessagesPath { get; set; } = "/messages";
        public string NotificationsPath { get; set; } = "/notifications";
        public string CommandsPath { get; set; } = "/commands";

        public bool IsValid() => new[] { MessagesPath, NotificationsPath, CommandsPath}.Distinct().Count() == 3;
        internal bool ContainsPath(string path) =>
            CommandsPath.Equals(path, StringComparison.OrdinalIgnoreCase) ||
            NotificationsPath.Equals(path, StringComparison.OrdinalIgnoreCase) ||
            MessagesPath.Equals(path, StringComparison.OrdinalIgnoreCase);
    }
}