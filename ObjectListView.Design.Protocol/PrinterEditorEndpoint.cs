using Microsoft.DotNet.DesignTools.Protocol;
using Microsoft.DotNet.DesignTools.Protocol.DataPipe;
using Microsoft.DotNet.DesignTools.Protocol.Endpoints;

namespace BrightIdeasSoftware.Design
{
    public static class PrinterEditorNames
    {
        public const string Endpoint = "ObjectListView.CreatePrinterEditor";
        public const string ViewModel = "ObjectListView.PrinterEditor";
    }

    [System.Composition.Shared, ExportEndpoint]
    public sealed class PrinterEditorEndpoint : Endpoint<PrinterEditorRequest, PrinterEditorResponse>
    {
        public override string Name { get { return PrinterEditorNames.Endpoint; } }
        protected override PrinterEditorRequest CreateRequest(IDataPipeReader reader) {
            return new PrinterEditorRequest(reader);
        }
        protected override PrinterEditorResponse CreateResponse(IDataPipeReader reader) {
            return new PrinterEditorResponse(reader);
        }
    }

    public sealed class PrinterEditorRequest : Request
    {
        public SessionId SessionId { get; private set; }
        public object Value { get; private set; }
        public bool IsPen { get; private set; }
        public PrinterEditorRequest(SessionId sessionId, object value, bool isPen) {
            this.SessionId = sessionId;
            this.Value = value;
            this.IsPen = isPen;
        }
        public PrinterEditorRequest(IDataPipeReader reader) : base(reader) { }
        protected override void ReadProperties(IDataPipeReader reader) {
            this.SessionId = reader.ReadSessionId(nameof(SessionId));
            this.Value = reader.ReadObject(nameof(Value));
            this.IsPen = reader.ReadBoolean(nameof(IsPen));
        }
        protected override void WriteProperties(IDataPipeWriter writer) {
            writer.Write(nameof(SessionId), this.SessionId);
            writer.WriteObject(nameof(Value), this.Value);
            writer.Write(nameof(IsPen), this.IsPen);
        }
    }

    public sealed class PrinterEditorResponse : Response
    {
        public object ViewModel { get; private set; }
        public PrinterEditorResponse(object viewModel) { this.ViewModel = viewModel; }
        public PrinterEditorResponse(IDataPipeReader reader) : base(reader) { }
        protected override void ReadProperties(IDataPipeReader reader) {
            this.ViewModel = reader.ReadObject(nameof(ViewModel));
        }
        protected override void WriteProperties(IDataPipeWriter writer) {
            writer.WriteObject(nameof(ViewModel), this.ViewModel);
        }
    }
}
