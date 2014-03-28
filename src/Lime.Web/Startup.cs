using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Lime.Web.Startup))]
namespace Lime.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
