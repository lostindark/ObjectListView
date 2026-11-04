using System;
using System.Collections;

namespace BrightIdeasSoftware {
    /// <summary>Model-independent tree access for generators, designers, and drag/drop integrations.</summary>
    public interface ITreeListView {
        /// <summary>Return a model's parent.</summary>
        object GetParent(object model);

        /// <summary>Rebuild the tree, optionally preserving its view state.</summary>
        void RebuildAll(bool preserveState);

        /// <summary>Determine whether a model can expand.</summary>
        Func<object, bool> CanExpandGetter { get; set; }

        /// <summary>Fetch the children of a model.</summary>
        Func<object, IEnumerable> ChildrenGetter { get; set; }
    }

    internal interface ITreeRenderer { }
}
