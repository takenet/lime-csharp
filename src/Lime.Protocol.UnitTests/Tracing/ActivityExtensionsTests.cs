using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using Lime.Protocol.Network;
using Lime.Protocol.Tracing;
using NUnit.Framework;

namespace Lime.Protocol.UnitTests.Tracing;

public class ActivityExtensionsTests
{
    #region CopyTraceParent

    [Test]
    [Category("CodeSafety")]
    public void CopyTraceParent_Dictionary_CodeSafety()
    {
        // Arrange
        var dictionaryWithTraceParent = new Dictionary<string, string>
        {
            { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
        };

        // Act
        // Assert
        Assert.DoesNotThrow(() => ((IDictionary<string, string>)null).CopyTraceParent(null));
        Assert.DoesNotThrow(() => ((IDictionary<string, string>)null).CopyTraceParent(new Dictionary<string, string>()));
        Assert.DoesNotThrow(() => new Dictionary<string, string>().CopyTraceParent(null));
        Assert.DoesNotThrow(() => dictionaryWithTraceParent.CopyTraceParent(null));
        Assert.DoesNotThrow(() => dictionaryWithTraceParent.CopyTraceParent(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())));
    }

    [Test]
    [Category("CodeCorrectness")]
    public void CopyTraceParent_Dictionary()
    {
        // Arrange
        var dictionaryWithTraceParent = new Dictionary<string, string>
        {
            { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
        };
        var dictionaryWithTraceContext = new Dictionary<string, string>
        {
            { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
            { TraceContext.TraceState, "test=00f067aa0ba902b7" }
        };

        var emptyDictionary = new Dictionary<string, string>();
        var emptyDictionaryContext = new Dictionary<string, string>();

        // Act
        dictionaryWithTraceParent.CopyTraceParent(emptyDictionary);
        dictionaryWithTraceContext.CopyTraceParent(emptyDictionaryContext);

        // Assert
        Assert.AreEqual(1, emptyDictionary.Count);
        Assert.AreEqual("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", emptyDictionary[TraceContext.TraceParent]);
        Assert.False(emptyDictionary.ContainsKey(TraceContext.TraceState));

        Assert.AreEqual(2, emptyDictionaryContext.Count);
        Assert.AreEqual("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", emptyDictionaryContext[TraceContext.TraceParent]);
        Assert.AreEqual("test=00f067aa0ba902b7", emptyDictionaryContext[TraceContext.TraceState]);
    }


    [Test]
    [Category("CodeCorrectness")]
    public void CopyTraceParent_Envelope()
    {
        // Arrange
        var envelopeWithTraceParent = new Command
        {
            Metadata = new Dictionary<string, string>
            {
                { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
            }
        };

        var envelopeWithTraceContext = new Command
        {
            Metadata = new Dictionary<string, string>
            {
                { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
                { TraceContext.TraceState, "test=00f067aa0ba902b7" }
            }
        };

        var emptyEnvelope = new Command();
        var emptyEnvelopeContext = new Command { Metadata = new Dictionary<string, string>() };

        // Act
        envelopeWithTraceParent.CopyTraceParent(emptyEnvelope);
        envelopeWithTraceContext.CopyTraceParent(emptyEnvelopeContext);

        // Assert
        Assert.AreEqual(1, emptyEnvelope.Metadata.Count);
        Assert.AreEqual("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", emptyEnvelope.Metadata[TraceContext.TraceParent]);
        Assert.False(emptyEnvelope.Metadata.ContainsKey(TraceContext.TraceState));

        Assert.AreEqual(2, emptyEnvelopeContext.Metadata.Count);
        Assert.AreEqual("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", emptyEnvelopeContext.Metadata[TraceContext.TraceParent]);
        Assert.AreEqual("test=00f067aa0ba902b7", emptyEnvelopeContext.Metadata[TraceContext.TraceState]);
    }


    [Test]
    [Category("CodeSafety")]
    public void CopyTraceParent_Envelope_CodeSafety()
    {
        // Arrange
        var envelopeWithTraceParent = new Command
        {
            Metadata = new Dictionary<string, string>
            {
                { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
            }
        };

        // Act
        // Assert
        Assert.DoesNotThrow(() => ((Envelope)null).CopyTraceParent(null));
        Assert.DoesNotThrow(() => ((Envelope)null).CopyTraceParent(new Command()));
        Assert.DoesNotThrow(() => new Command().CopyTraceParent(null));
        Assert.DoesNotThrow(() => envelopeWithTraceParent.CopyTraceParent(null));
        Assert.DoesNotThrow(() => envelopeWithTraceParent.CopyTraceParent(new Command
        {
            Metadata = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
        }));
    }

    #endregion

    #region ContainsW3CTraceContext

    [Test]
    [Category("CodeCorrectness")]
    public void ContainsW3CTraceContext_Dictionary()
    {
        // Arrange
        // Act
        // Assert
        Assert.False(((IDictionary<string, string>)null).ContainsW3CTraceContext());
        Assert.False(new Dictionary<string, string>().ContainsW3CTraceContext());
        Assert.True(new Dictionary<string, string>
        {
            { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
        }.ContainsW3CTraceContext());
    }

    [Test]
    [Category("CodeCorrectness")]
    public void ContainsW3CTraceContext_Envelope()
    {
        // Arrange
        // Act
        // Assert
        Assert.False(new Command().ContainsW3CTraceContext());
        Assert.False(new Command { Metadata = new Dictionary<string, string>() }.ContainsW3CTraceContext());
        Assert.True(new Command
        {
            Metadata = new Dictionary<string, string>
            {
                { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
            }
        }.ContainsW3CTraceContext());
    }

    #endregion

    #region StartActivity

    [Test]
    [Category("CodeSafety")]
    public void StartActivityWithoutListener_Dictionary_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        StartActivity_Dictionary_CodeSafety();
    }

    [Test]
    [Category("CodeSafety")]
    public void StartActivityWithListener_Dictionary_CodeSafety()
    {
        // Arrange
        using var _ = AddListener();

        // Act
        // Assert
        StartActivity_Dictionary_CodeSafety();
    }

    private static void StartActivity_Dictionary_CodeSafety()
    {
        Assert.DoesNotThrow(() => ((IDictionary<string, string>)null).StartActivity()?.Dispose());
        Assert.DoesNotThrow(() => new Dictionary<string, string>().StartActivity()?.Dispose());
        Assert.DoesNotThrow(() => new Dictionary<string, string>
        {
            { TraceContext.TraceParent, "invalid-trace-parent" },
        }.StartActivity()?.Dispose());
        Assert.DoesNotThrow(() => new Dictionary<string, string>
        {
            { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
            { TraceContext.TraceState, "invalid-trace-state" }
        }.StartActivity()?.Dispose());
    }


    [Test]
    [Category("CodeCorrectness")]
    public void StartActivity_Dictionary_ShouldCreateActivity()
    {
        // Arrange
        using var _ = AddListener();

        var dictionary = new Dictionary<string, string> { { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" } };

        // Act
        using var activity = dictionary.StartActivity();

        // Act
        Assert.NotNull(activity);
        Assert.AreEqual("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", activity.ParentId);
        Assert.AreEqual("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", dictionary[TraceContext.TraceParent]);
    }

    [Test]
    [Category("CodeSafety")]
    public void StartActivity_Envelope_WithoutListeners_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        StartActivity_Envelope_CodeSafety();
    }

    [Test]
    [Category("CodeSafety")]
    public void StartActivity_Envelope_WithListeners_CodeSafety()
    {
        // Arrange
        using var _ = AddListener();

        // Act
        // Assert
        StartActivity_Envelope_CodeSafety();
    }

    private static void StartActivity_Envelope_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        Assert.DoesNotThrow(() => ((Envelope)null).StartActivity()?.Dispose());
        Assert.DoesNotThrow(() => new Command().StartActivity()?.Dispose());
        Assert.DoesNotThrow(() => new Command { Metadata = new Dictionary<string, string>() }.StartActivity()?.Dispose());
        Assert.DoesNotThrow(() => new Command
        {
            Metadata = new Dictionary<string, string>
            {
                { TraceContext.TraceParent, "invalid-trace-parent" },
            }
        }.StartActivity()?.Dispose());
        Assert.DoesNotThrow(() => new Command
            {
                Metadata = new Dictionary<string, string>
                {
                    { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
                    { TraceContext.TraceState, "invalid-trace-state" }
                }
            }.StartActivity()
            ?.Dispose());
    }

    [Test]
    [Category("CodeCorrectness")]
    public void StartActivity_Envelope_ShouldIgnorePingCommands()
    {
        // Arrange
        using var _ = AddListener();

        // Act
        using var pingUri = new Command { Uri = "/ping" }.StartActivity();
        using var pingType = new Command { Resource = new Ping() }.StartActivity();

        // Assert
        Assert.Null(pingUri);
        Assert.Null(pingType);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void StartActivity_Envelope_ShouldCreateActivity()
    {
        // Arrange
        using var _ = AddListener();

        var command = new Command { Uri = "/etc" };

        // Act
        using var activity = command.StartActivity();

        // Assert
        Assert.NotNull(activity);
        Assert.AreEqual("/etc", activity.GetTagItem("lime.command.uri"));
        Assert.IsNotEmpty(command.Metadata[TraceContext.TraceParent]);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void StartActivity_Envelope_ShouldUseCurrentAsParent()
    {
        // Arrange
        using var _ = AddListener();

        using var parent = LimeActivitySource.Instance.StartActivity();
        var command = new Command();

        // Act
        using var activity = command.StartActivity();

        // Assert
        Assert.NotNull(parent);
        Assert.NotNull(activity);
        Assert.AreEqual(parent.Id, activity.ParentId);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void StartActivity_Envelope_ShouldUseMetadataAsParent()
    {
        // Arrange
        using var _ = AddListener();

        var command = new Command { Metadata = new Dictionary<string, string> { { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" } } };

        // Act
        using var activity = command.StartActivity();

        // Assert
        Assert.NotNull(activity);
        Assert.AreEqual("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", activity.ParentId);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void StartActivity_Envelope_ShouldInjectMetadata()
    {
        // Arrange
        using var _ = AddListener();

        var command = new Command { Metadata = new Dictionary<string, string> { { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" } } };

        // Act
        using var activity = command.StartActivity();

        // Assert
        Assert.NotNull(activity);
        Assert.AreEqual(command.Metadata[TraceContext.TraceParent], activity.Id);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void StartActivity_Envelope_ShouldNotPrioritizeMetadata()
    {
        // Arrange
        using var _ = AddListener();

        using var parent = LimeActivitySource.Instance.StartActivity();
        var message = new Message { Metadata = new Dictionary<string, string> { { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" } } };

        // Act
        using var activity = message.StartActivity();

        // Assert
        Assert.NotNull(activity);
        Assert.NotNull(parent);
        Assert.AreEqual(parent.Id, activity.ParentId);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void StartActivity_Envelope_ShouldPrioritizeMetadata()
    {
        // Arrange
        using var _ = AddListener();

        using var parent = LimeActivitySource.Instance.StartActivity();
        var message = new Message { Metadata = new Dictionary<string, string> { { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" } } };

        // Act
        using var activity = message.StartActivity(prioritizeEnvelopeActivity: true);

        // Assert
        Assert.NotNull(activity);
        Assert.NotNull(parent);
        Assert.AreEqual("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", activity.ParentId);
    }

    #endregion

    #region InjectTraceParent

    [Test]
    [Category("CodeSafety")]
    public void InjectTraceParent_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        Assert.DoesNotThrow(() => ((Activity)null).InjectTraceParent((IDictionary<string, string>)null));
        Assert.DoesNotThrow(() => ((Activity)null).InjectTraceParent((Envelope)null));
        Assert.DoesNotThrow(() => new Activity("").InjectTraceParent((Envelope)null));
        Assert.DoesNotThrow(() => new Activity("").InjectTraceParent((IDictionary<string, string>)null));
        Assert.DoesNotThrow(() => ((Activity)null).InjectTraceParent(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())));
        Assert.DoesNotThrow(() => new Activity("").InjectTraceParent(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())));
    }

    [Test]
    [Category("CodeCorrectness")]
    public void InjectTraceParent_Dictionary()
    {
        // Arrange
        using var _ = AddListener();

        using var activity = LimeActivitySource.Instance.StartActivity();
        Assert.NotNull(activity);
        activity.TraceStateString = "test=true";

        var dictionary = new Dictionary<string, string>();

        // Act
        activity.InjectTraceParent(dictionary);

        // Assert
        Assert.AreEqual(activity.Id, dictionary[TraceContext.TraceParent]);
        Assert.AreEqual("test=true", dictionary[TraceContext.TraceState]);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void InjectTraceParent_Envelope()
    {
        // Arrange
        using var _ = AddListener();

        using var activity = LimeActivitySource.Instance.StartActivity();
        Assert.NotNull(activity);

        var envelope = new Notification();

        // Act
        activity.InjectTraceParent(envelope);

        // Assert
        Assert.AreEqual(activity.Id, envelope.Metadata[TraceContext.TraceParent]);
        Assert.False(envelope.Metadata.ContainsKey(TraceContext.TraceState));
    }

    #endregion

    #region InjectTraceParentIfAbsent

    [Test]
    [Category("CodeSafety")]
    public void InjectTraceParentIfAbsent_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        Assert.DoesNotThrow(() => ((Activity)null).InjectTraceParentIfAbsent(null));
        Assert.DoesNotThrow(() => new Activity("").InjectTraceParentIfAbsent(null));
    }

    [Test]
    [Category("CodeCorrectness")]
    public void InjectTraceParentIfAbsent_ShouldSetIfAbsent()
    {
        // Arrange
        using var _ = AddListener();

        using var activity = LimeActivitySource.Instance.StartActivity();

        var envelope = new Session();

        // Act
        activity.InjectTraceParentIfAbsent(envelope);

        // Assert
        Assert.NotNull(activity);
        Assert.AreEqual(activity.Id, envelope.Metadata[TraceContext.TraceParent]);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void InjectTraceParentIfAbsent_ShouldNotSetIfNotAbsent()
    {
        // Arrange
        using var _ = AddListener();

        using var activity = LimeActivitySource.Instance.StartActivity();

        var envelope = new Session { Metadata = new Dictionary<string, string> { { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" } } };

        // Act
        activity.InjectTraceParentIfAbsent(envelope);

        // Assert
        Assert.NotNull(activity);
        Assert.AreNotEqual(activity.Id, envelope.Metadata[TraceContext.TraceParent]);
    }

    #endregion

    #region SetEnvelopeTags

    [Test]
    [Category("CodeSafety")]
    public void SetEnvelopeTags_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        Assert.DoesNotThrow(() => ((Activity)null).SetEnvelopeTags(null));
        Assert.DoesNotThrow(() => ((Activity)null).SetEnvelopeTags(new Session()));
        Assert.DoesNotThrow(() => new Activity("").SetEnvelopeTags(null));
        Assert.DoesNotThrow(() => new Activity("").SetEnvelopeTags(new Command()));
    }

    [Test]
    [Category("CodeSafety")]
    public void SetSessionEnvelopeTags_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        Assert.DoesNotThrow(() => ((Activity)null).SetSessionTags(null));
        Assert.DoesNotThrow(() => new Activity("").SetSessionTags(null));
        Assert.DoesNotThrow(() => new Activity("").SetSessionTags(new Session()));
        Assert.DoesNotThrow(() => ((Activity)null).SetSessionStatusTag(null));
        Assert.DoesNotThrow(() => new Activity("").SetSessionStatusTag(null));
        Assert.DoesNotThrow(() => new Activity("").SetSessionStatusTag(new Session()));
    }

    [Test]
    [Category("CodeSafety")]
    public void SetNotificationEnvelopeTags_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        Assert.DoesNotThrow(() => ((Activity)null).SetNotificationTags(null));
        Assert.DoesNotThrow(() => ((Activity)null).SetNotificationTags(new Notification()));
        Assert.DoesNotThrow(() => new Activity("").SetNotificationTags(null));
        Assert.DoesNotThrow(() => new Activity("").SetNotificationTags(new Notification()));
        Assert.DoesNotThrow(() => ((Activity)null).SetNotificationStatusTag(null));
        Assert.DoesNotThrow(() => ((Activity)null).SetNotificationStatusTag(new Notification()));
        Assert.DoesNotThrow(() => new Activity("").SetNotificationStatusTag(null));
        Assert.DoesNotThrow(() => new Activity("").SetNotificationStatusTag(new Notification()));
    }

    [Test]
    [Category("CodeSafety")]
    public void SetCommandEnvelopeTags_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        Assert.DoesNotThrow(() => ((Activity)null).SetCommandTags(null));
        Assert.DoesNotThrow(() => ((Activity)null).SetCommandTags(new Command()));
        Assert.DoesNotThrow(() => new Activity("").SetCommandTags(null));
        Assert.DoesNotThrow(() => new Activity("").SetCommandTags(new Command()));
        Assert.DoesNotThrow(() => ((Activity)null).SetCommandStatusTag(null));
        Assert.DoesNotThrow(() => ((Activity)null).SetCommandStatusTag(new Command()));
        Assert.DoesNotThrow(() => new Activity("").SetCommandStatusTag(null));
        Assert.DoesNotThrow(() => new Activity("").SetCommandStatusTag(new Command()));
    }

    [Test]
    [Category("CodeSafety")]
    public void SetMessageEnvelopeTags_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        Assert.DoesNotThrow(() => ((Activity)null).SetMessageTags(null));
        Assert.DoesNotThrow(() => ((Activity)null).SetMessageTags(new Message()));
        Assert.DoesNotThrow(() => new Activity("").SetMessageTags(null));
        Assert.DoesNotThrow(() => new Activity("").SetMessageTags(new Message()));
    }

    [Test]
    [Category("CodeCorrectness")]
    public void SetNotificationEnvelopeTags()
    {
        // Arrange
        var activity = new Activity("test");

        var envelope = new Notification
        {
            Id = "id",
            From = new Node("from_name", "from_domain", "from_instance"),
            To = new Node("to_name", "to_domain", "to_instance"),
            Event = Event.Consumed
        };

        // Act
        activity.SetEnvelopeTags(envelope);

        // Assert
        Assert.AreEqual("id", activity.GetTagItem("lime.envelope.id"));
        Assert.AreEqual("Notification", activity.GetTagItem("lime.envelope.type"));
        Assert.AreEqual("from_name@from_domain/from_instance", activity.GetTagItem("lime.envelope.from"));
        Assert.AreEqual("to_name@to_domain/to_instance", activity.GetTagItem("lime.envelope.to"));
        Assert.AreEqual("Consumed", activity.GetTagItem("lime.notification.event"));
    }


    [Test]
    [Category("CodeCorrectness")]
    public void SetNotificationEnvelopeStatusOk()
    {
        // Arrange
        var activity = new Activity("test");

        var envelope = new Notification
        {
            Event = Event.Dispatched
        };

        // Act
        activity.SetEnvelopeTags(envelope);

        // Assert
        Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void SetNotificationEnvelopeStatusError()
    {
        // Arrange
        var activity = new Activity("test");

        var envelope = new Notification
        {
            Event = Event.Failed,
            Reason = new Reason
            {
                Code = 12,
                Description = "test notification failed"
            }
        };

        // Act
        activity.SetEnvelopeTags(envelope);

        // Assert
        Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
        Assert.AreEqual("Code 12: test notification failed", activity.StatusDescription);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void SetMessageEnvelopeTags()
    {
        // Arrange
        var activity = new Activity("test");

        var envelope = new Message
        {
            Id = "id",
            From = new Node("from_name", "from_domain", "from_instance"),
            To = new Node("to_name", "to_domain", "to_instance"),
            Content = new Contact()
        };

        // Act
        activity.SetEnvelopeTags(envelope);

        // Assert
        Assert.AreEqual("id", activity.GetTagItem("lime.envelope.id"));
        Assert.AreEqual("Message", activity.GetTagItem("lime.envelope.type"));
        Assert.AreEqual("from_name@from_domain/from_instance", activity.GetTagItem("lime.envelope.from"));
        Assert.AreEqual("to_name@to_domain/to_instance", activity.GetTagItem("lime.envelope.to"));
        Assert.AreEqual("application/vnd.lime.contact+json", activity.GetTagItem("lime.message.type"));
    }

    [Test]
    [Category("CodeCorrectness")]
    public void SetCommandEnvelopeTags()
    {
        // Arrange
        var activity = new Activity("test");

        var envelope = new Command
        {
            Id = "id",
            From = new Node("from_name", "from_domain", "from_instance"),
            To = new Node("to_name", "to_domain", "to_instance"),
            Uri = "/test",
            Method = CommandMethod.Merge,
            Resource = new Input()
        };

        // Act
        activity.SetEnvelopeTags(envelope);

        // Assert
        Assert.AreEqual("id", activity.GetTagItem("lime.envelope.id"));
        Assert.AreEqual("Command", activity.GetTagItem("lime.envelope.type"));
        Assert.AreEqual("from_name@from_domain/from_instance", activity.GetTagItem("lime.envelope.from"));
        Assert.AreEqual("to_name@to_domain/to_instance", activity.GetTagItem("lime.envelope.to"));
        Assert.AreEqual("/test", activity.GetTagItem("lime.command.uri"));
        Assert.AreEqual("application/vnd.lime.input+json", activity.GetTagItem("lime.command.type"));
        Assert.AreEqual("Merge", activity.GetTagItem("lime.command.method"));
        Assert.AreEqual("Pending", activity.GetTagItem("lime.command.status"));
    }

    [Test]
    [Category("CodeCorrectness")]
    public void SetCommandEnvelopeStatusOk()
    {
        // Arrange
        var activity = new Activity("test");

        var envelope = new Command
        {
            Status = CommandStatus.Success
        };

        // Act
        activity.SetEnvelopeTags(envelope);

        // Assert
        Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void SetCommandEnvelopeStatusError()
    {
        // Arrange
        var activity = new Activity("test");

        var envelope = new Command
        {
            Status = CommandStatus.Failure,
            Reason = new Reason
            {
                Code = 44,
                Description = "test command failed"
            }
        };

        // Act
        activity.SetEnvelopeTags(envelope);

        // Assert
        Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
        Assert.AreEqual("Code 44: test command failed", activity.StatusDescription);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void SetCommandEnvelopeStatusUnset()
    {
        // Arrange
        var activity = new Activity("test");

        // Act
        activity.SetEnvelopeTags(new Command());

        // Assert
        Assert.AreEqual(ActivityStatusCode.Unset, activity.Status);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void SetSessionEnvelopeTags()
    {
        // Arrange
        var activity = new Activity("test");

        var envelope = new Session
        {
            Id = "id",
            From = new Node("from_name", "from_domain", "from_instance"),
            To = new Node("to_name", "to_domain", "to_instance"),
            State = SessionState.Established
        };

        // Act
        activity.SetEnvelopeTags(envelope);

        // Assert
        Assert.AreEqual("id", activity.GetTagItem("lime.envelope.id"));
        Assert.AreEqual("Session", activity.GetTagItem("lime.envelope.type"));
        Assert.AreEqual("from_name@from_domain/from_instance", activity.GetTagItem("lime.envelope.from"));
        Assert.AreEqual("to_name@to_domain/to_instance", activity.GetTagItem("lime.envelope.to"));
        Assert.AreEqual("Established", activity.GetTagItem("lime.session.state"));
    }

    [Test]
    [Category("CodeCorrectness")]
    public void SetSessionEnvelopeStatusOk()
    {
        // Arrange
        var activity = new Activity("test");

        var envelope = new Session
        {
            State = SessionState.Established
        };

        // Act
        activity.SetEnvelopeTags(envelope);

        // Assert
        Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void SetSessionEnvelopeStatusError()
    {
        // Arrange
        var activity = new Activity("test");

        var envelope = new Session
        {
            State = SessionState.Failed,
            Reason = new Reason
            {
                Code = 123,
                Description = "Test description"
            }
        };

        // Act
        activity.SetEnvelopeTags(envelope);

        // Assert
        Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
        Assert.AreEqual("Code 123: Test description", activity.StatusDescription);
    }

    [Test]
    [Category("CodeCorrectness")]
    public void SetSessionEnvelopeStatusUnset()
    {
        // Arrange
        var activity = new Activity("test");

        // Act
        activity.SetEnvelopeTags(new Session());

        // Assert
        Assert.AreEqual(ActivityStatusCode.Unset, activity.Status);
    }

    #endregion

    #region GetActivityName

    [Test]
    [Category("CodeCorrectness")]
    public void GetActivityName()
    {
        // Arrange
        var command = new Command();
        var session = new Session
        {
            State = SessionState.Negotiating
        };

        // Act
        var commandActivityName = command.GetActivityName();
        var sessionActivityName = session.GetActivityName();

        // Assert
        Assert.AreEqual("Command", commandActivityName);
        Assert.AreEqual("Session Negotiating", sessionActivityName);
    }

    [Test]
    [Category("CodeSafety")]
    public void GetActivityName_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        Assert.DoesNotThrow(() => ((Envelope)null).GetActivityName());
    }

    #endregion

    #region GetTransportTags

    [Test]
    [Category("CodeCorrectness")]
    public void SetTransportTags()
    {
        // Arrange
        var transport = new FakeTransport(true, "localEndpointTest", "remoteEndpointTest");
        var activity = new Activity("test");

        // Act
        activity.SetTransportTags(transport);

        // Assert
        activity.SetTag("lime.transport.local_endpoint", transport.LocalEndPoint);
        activity.SetTag("lime.transport.remote_endpoint", transport.RemoteEndPoint);
        activity.SetTag("lime.transport.type", transport.GetType().Name);

        Assert.AreEqual("FakeTransport", activity.GetTagItem("lime.transport.type"));
        Assert.AreEqual("localEndpointTest", activity.GetTagItem("lime.transport.local_endpoint"));
        Assert.AreEqual("remoteEndpointTest", activity.GetTagItem("lime.transport.remote_endpoint"));
    }

    [Test]
    [Category("CodeSafety")]
    public void SetTransportTags_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        Assert.DoesNotThrow(() => ((Activity)null).SetTransportTags(null));
        Assert.DoesNotThrow(() => (new Activity("")).SetTransportTags(null));
        Assert.DoesNotThrow(() => (new Activity("")).SetTransportTags(new FakeTransport(true, "", "")));
    }

    #endregion

    #region SetNodeTags

    [Test]
    [Category("CodeCorrectness")]
    public void SetNodeTags()
    {
        // Arrange
        var node = new Node("name", "domain", "instance");
        var activity = new Activity("test");

        // Act
        activity.SetNodeTags(node);

        // Assert
        activity.SetTag("lime.node", "name@domain/instance");
    }

    [Test]
    [Category("CodeSafety")]
    public void SetNodeTags_CodeSafety()
    {
        // Arrange
        // Act
        // Assert
        Assert.DoesNotThrow(() => ((Activity)null).SetNodeTags(new Node()));
        Assert.DoesNotThrow(() => new Activity("").SetNodeTags(null));
    }

    #endregion

    private static ActivityListener AddListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };

        ActivitySource.AddActivityListener(listener);

        return listener;
    }

    private class FakeTransport : TransportBase, ITransport
    {
        public FakeTransport(bool isConnected, string localEndPoint, string remoteEndPoint)
        {
            IsConnected = isConnected;
            LocalEndPoint = localEndPoint;
            RemoteEndPoint = remoteEndPoint;
        }

        public override Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((Envelope)null);
        }

        public override bool IsConnected { get; }

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override string LocalEndPoint { get; }

        public override string RemoteEndPoint { get; }
    }
}