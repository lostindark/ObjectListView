namespace ObjectListViewDemo.Models {
    public enum MaritalStatus
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720", Justification = "Single describes marital status, not the floating-point type.")]
        Single,
        Married,
        Divorced,
        Partnered
    }
}