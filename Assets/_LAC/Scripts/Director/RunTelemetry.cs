using LAC.Director;

namespace LAC.Core
{
    public sealed partial class RunManager
    {
        // Không đăng ký hook SyncVar: host ghi đúng một lần ngay tại điểm quyết định.
        public TelemetrySession Telemetry { get; private set; }

        private void BeginTelemetry()
        {
            Telemetry = TelemetrySession.Begin(this);
        }

        private void LateUpdate() => Telemetry?.Tick();

        public override void OnStopServer()
        {
            Telemetry?.Finish("aborted");
            base.OnStopServer();
        }

        private void OnApplicationQuit() => Telemetry?.Finish("aborted");
    }
}
