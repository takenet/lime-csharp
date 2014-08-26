using System;
using System.Linq;
using Lime.Protocol.Tcp;
using System.Threading;
using Lime.Protocol.Client;
using System.Threading.Tasks;
using Lime.Protocol.Security;
using Lime.Protocol.Network;
using Lime.Protocol.Resources;
using System.Net.NetworkInformation;
using Lime.Protocol.Contents;

namespace Lime.Protocol.Console.Mono
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			MainAsync (args).Wait ();
		}

		private static async Task MainAsync(string[] args)
		{
			var identity = GetNodeFromConsole ("Enter the identity (name@domain format): ");

			if (identity == null)
			{
				return;
			}

			System.Console.Write ("Enter the password: ");
			var password = System.Console.ReadLine ();

			System.Console.WriteLine ("Connecting...");

			var channel = await ConnectAsync ();

			System.Console.WriteLine ("Connected - Remote node: {0}", channel.RemoteNode);

			System.Console.WriteLine ("Authenticating...");

			var session = await AuthenticateAsync (channel, identity, password);

			if (session.State == SessionState.Established) 
			{
				System.Console.WriteLine ("Session established - Id: {0} - Local node: {1}", session.Id, channel.LocalNode);

				System.Console.WriteLine ("Setting presence...");

				var presenceResult = await SetPresenceAvailableAsync (channel);

				if (presenceResult.Status == CommandStatus.Success)
				{
					System.Console.WriteLine ("Presence set.");

					var cancellationTokenSource = new CancellationTokenSource ();

					var receiveMessagesTask = ReceiveMessagesAsync (channel, cancellationTokenSource.Token);
					var receiveNotificationsTask = ReceiveNotificationsAsync (channel, cancellationTokenSource.Token);


					while (true)
					{
						var destinationIdentity = GetNodeFromConsole ("Enter the destination (name@domain/instance format) or 'exit' to quit: ");
						if (destinationIdentity == null)
						{
							break;
						}

						System.Console.Write ("Enter the message text: ") ;
						var messageText = System.Console.ReadLine ();

						if (string.IsNullOrWhiteSpace ((messageText)))
						{
							break;
						}

						var message = new Message () 
						{
							To = destinationIdentity,
							Content = new PlainText () 
							{
								Text = messageText
							}
						};

						await channel.SendMessageAsync (message);
					}

					cancellationTokenSource.Cancel ();
					await Task.WhenAll (receiveMessagesTask, receiveNotificationsTask);
				}
				else
				{
					System.Console.WriteLine ("Presence command failed - Reason: {0}", presenceResult.Reason);
				}
			}
			else
			{
				System.Console.WriteLine ("Session failed - Reason: {0}", session.Reason);
			}
		}

		private static async Task<IClientChannel> ConnectAsync()
		{
			var transport = new TcpTransport ();

			await transport.OpenAsync (
				new Uri ("net.tcp://iris.limeprotocol.org:55321"),
				CancellationToken.None);

			var channel = new ClientChannel (
				transport,
				TimeSpan.FromSeconds (60));

			return channel;
		}

		private static Node GetNodeFromConsole (string message)
		{
			Node node = null;
			do 
			{
				System.Console.Write (message);

				var input = System.Console.ReadLine ();
				if (input != null &&
					input.Equals ("exit", StringComparison.OrdinalIgnoreCase))
				{
					break;
				}

				Node.TryParse (input, out node);
			}
			while (node == null);
			return node;
		}

		private static async Task<Session> AuthenticateAsync(IClientChannel channel, Identity identity, string password)
		{
			var authentication = new PlainAuthentication ();
			authentication.SetToBase64Password (password);

			return await channel.EstablishSessionAsync (
				(compressionOptions) => compressionOptions[0],
				(encryptionOptions) => encryptionOptions[0],
				identity,
				(schemeOptions, roundtrip) => authentication,
				"default",
				CancellationToken.None);
		}

		private static Task<Command> SetPresenceAvailableAsync(IClientChannel channel)
		{
			var command = new Command()
			{
				Method = CommandMethod.Set,
				Uri = new LimeUri (UriTemplates.PRESENCE),
				Resource = new Presence () 
				{
					Status = PresenceStatus.Available,
					RoutingRule = RoutingRule.Instance
				}
			};

			return channel.ProcessCommandAsync (command, CancellationToken.None);
		}

		private static async Task ReceiveMessagesAsync(IClientChannel channel, CancellationToken cancellationToken)
		{
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					var message = await channel.ReceiveMessageAsync (cancellationToken);

					System.Console.ForegroundColor = ConsoleColor.Red;
					System.Console.WriteLine ();

					System.Console.WriteLine ("Message received - From: {0} - Id: {1} - Content: {2}", 
						message.From, message.Id, message.Content);

					System.Console.ResetColor ();
				}
			}
			catch (OperationCanceledException)
			{
				return;
			}
		}

		private static async Task ReceiveNotificationsAsync(IClientChannel channel, CancellationToken cancellationToken)
		{
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					var notification = await channel.ReceiveNotificationAsync (cancellationToken);

					System.Console.ForegroundColor = ConsoleColor.Blue;
					System.Console.WriteLine ();

					if (notification.Reason != null)
					{
						System.Console.WriteLine ("Notification received - From: {0} - Id: {1} - Event: {2} - Reason: {3}", 
							notification.From, notification.Id, notification.Event, notification.Reason);
					}
					else
					{
						System.Console.WriteLine ("Notification received - From: {0} - Id: {1} - Event: {2}", 
							notification.From, notification.Id, notification.Event);
					}

					System.Console.ResetColor ();
				}
			}
			catch (OperationCanceledException)
			{
				return;
			}
		}
	}
}
