using Autofac;
using PX.Security;
using System;
using System.Collections.Generic;

namespace Tracumatica
{
    public class Registration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.Configure<AuthenticationManagerOptions>(ConfigureAuthentication);
        }



        private void ConfigureAuthentication(AuthenticationManagerOptions options)
        {
            options.AddLocation("/LocationTracking/index.html")
            .WithCookie()
            .WithAnonymous();
        }
    }
}
