using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        var dictionaryWithTraceParent = new Dictionary<string, string>
        {
            { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
        };

        Assert.DoesNotThrow(() => ((IDictionary<string, string>)null).CopyTraceParent(null));
        Assert.DoesNotThrow(() => ((IDictionary<string, string>)null).CopyTraceParent(new Dictionary<string, string>()));
        Assert.DoesNotThrow(() => new Dictionary<string, string>().CopyTraceParent(null));
        Assert.DoesNotThrow(() => dictionaryWithTraceParent.CopyTraceParent(null));
        Assert.DoesNotThrow(() => dictionaryWithTraceParent.CopyTraceParent(new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())));
    }

    [Test]
    [Category("CodeSafety")]
    public void CopyTraceParent_Envelope_CodeSafety()
    {
        var envelopeWithTraceParent = new Command
        {
            Metadata = new Dictionary<string, string>
            {
                { TraceContext.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
            }
        };

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
}