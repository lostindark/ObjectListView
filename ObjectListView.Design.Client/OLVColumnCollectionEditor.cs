using System;
using System.Collections.Generic;
using Microsoft.DotNet.DesignTools.Client.Editors;
using Microsoft.DotNet.DesignTools.Client.TypeRouting;

namespace BrightIdeasSoftware.Design.Client
{
    public sealed class OLVColumnCollectionEditor : CollectionEditor
    {
        public OLVColumnCollectionEditor(Type collectionType) : base(collectionType) { }

        protected override string Name { get { return "ObjectListView.Columns"; } }
    }

    [ExportTypeRoutingDefinitionProvider]
    internal sealed class TypeRoutingProvider : TypeRoutingDefinitionProvider
    {
        public override IEnumerable<TypeRoutingDefinition> GetDefinitions() {
            yield return new TypeRoutingDefinition(TypeRoutingKinds.Editor,
                "OLVColumnCollectionEditor", typeof(OLVColumnCollectionEditor));
            yield return new TypeRoutingDefinition(TypeRoutingKinds.Editor, "BrushDataEditor", typeof(BrushDataEditor));
            yield return new TypeRoutingDefinition(TypeRoutingKinds.Editor, "PenDataEditor", typeof(PenDataEditor));
        }
    }
}
