#nullable enable

namespace BizHawk.Client.Common
{
	public sealed class CommApi : ICommApi
	{
		private static readonly WebSocketServer _wsServer = new WebSocketServer();

		private readonly (HttpCommunication? HTTP, MemoryMappedFiles MMF, SocketServer? Sockets) _networkingHelpers;

		public HttpCommunication? HTTP => _networkingHelpers.HTTP;

		public MemoryMappedFiles MMF => _networkingHelpers.MMF;

		public SocketServer? Sockets => _networkingHelpers.Sockets;

		public WebSocketServer WebSockets => _wsServer;

		public CommApi(IMainFormForApi mainForm) => _networkingHelpers = mainForm.NetworkingHelpers;

		public string? HttpTest() => HTTP == null ? null : string.Join("\n", HttpTestGet(), HTTP.SendScreenshot(), "done testing");

		public string? HttpTestGet() => HttpCommunication.Get(HTTP?.GetUrl)?.Result;
	}
}
