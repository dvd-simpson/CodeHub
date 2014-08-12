using CodeFramework.Core.Services;
using MonoTouch.Foundation;
using MonoTouch.JavaScriptCore;
using CodeHub;

namespace CodeFramework.iOS.Services
{
	public class MarkdownService : IMarkdownService
    {
		private readonly JSVirtualMachine _vm = new JSVirtualMachine();
		private readonly JSContext _ctx;
		private readonly JSValue _val;

		public MarkdownService()
		{
			_ctx = new JSContext(_vm);
            _ctx.EvaluateScript(Resources.MarkdownScript);
			_val = _ctx[new NSString("marked")];
		}

		public string Convert(string c)
		{
			if (string.IsNullOrEmpty(c))
				return string.Empty;
			return _val.Call(JSValue.From(c, _ctx)).ToString();
		}
    }
}

