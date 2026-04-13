using System.Runtime.CompilerServices;
using System.Text;

namespace BLS.Tests
{
    internal static class TestStartup
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
