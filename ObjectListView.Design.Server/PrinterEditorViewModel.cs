using System;
using Microsoft.DotNet.DesignTools.Protocol.Endpoints;
using Microsoft.DotNet.DesignTools.ViewModels;

namespace BrightIdeasSoftware.Design
{
    public sealed class PrinterEditorViewModel : ViewModel
    {
        private bool isPen;
        public PrinterEditorViewModel(IServiceProvider provider) : base(provider) { }
        public string State { get; set; }
        public object Value { get { return PrinterEditorState.Decode(this.State, this.isPen); } }
        public void Initialize(object value, bool pen) {
            this.isPen = pen;
            this.State = PrinterEditorState.Encode(value, pen);
        }

        [ExportViewModelFactory(PrinterEditorNames.ViewModel)]
        private sealed class Factory : ViewModelFactory<PrinterEditorViewModel>
        {
            protected override PrinterEditorViewModel CreateViewModel(IServiceProvider provider) {
                return new PrinterEditorViewModel(provider);
            }
        }
    }

    [ExportRequestHandler(PrinterEditorNames.Endpoint)]
    internal sealed class PrinterEditorHandler : RequestHandler<PrinterEditorRequest, PrinterEditorResponse>
    {
        public override PrinterEditorResponse HandleRequest(PrinterEditorRequest request) {
            var model = this.CreateViewModel<PrinterEditorViewModel>(this.GetDesignerHost(request.SessionId));
            model.Initialize(request.Value, request.IsPen);
            return new PrinterEditorResponse(model);
        }
    }
}
