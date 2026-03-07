using System;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace BLS.Tests
{
    internal static class TestStartup
    {
        public static IServiceProvider? Services { get; private set; }

        [ModuleInitializer]
        public static void Initialize()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var services = new ServiceCollection();
            services.AddSingleton(CodePagesEncodingProvider.Instance);
            Services = services.BuildServiceProvider();
        }
    }
}
